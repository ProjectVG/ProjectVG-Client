#import <Foundation/Foundation.h>
#import <WebKit/WebKit.h>

extern "C" {
    #import <UnityFramework/UnityFramework-Swift.h>
}

/**
 * iOS 네이티브 WebSocket 플러그인
 * 
 * NSURLSessionWebSocketTask를 사용하여 구현합니다.
 * Unity에서 호출할 수 있는 C 함수들을 제공합니다.
 */

// WebSocket 인스턴스 저장용
static NSMutableDictionary<NSNumber*, NSURLSessionWebSocketTask*>* webSocketMap = nil;
static NSNumber* webSocketIdCounter = @0;
static NSLock* webSocketLock = nil;

// Unity 콜백 함수들
extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);

/**
 * WebSocket 초기화
 */
void InitializeWebSocket() {
    if (webSocketMap == nil) {
        webSocketMap = [[NSMutableDictionary alloc] init];
        webSocketIdCounter = @0;
        webSocketLock = [[NSLock alloc] init];
    }
}

/**
 * WebSocket 연결
 * 
 * @param url WebSocket 서버 URL
 * @return WebSocket ID (성공 시 양수, 실패 시 -1)
 */
extern "C" int IOSWebSocket_Connect(const char* url) {
    InitializeWebSocket();
    
    @autoreleasepool {
        NSString* urlString = [NSString stringWithUTF8String:url];
        NSLog(@"[WebSocketPlugin] iOS WebSocket 연결 시도: %@", urlString);
        
        NSURL* nsUrl = [NSURL URLWithString:urlString];
        if (!nsUrl) {
            NSLog(@"[WebSocketPlugin] 잘못된 URL: %@", urlString);
            return -1;
        }
        
        NSURLSession* session = [NSURLSession sessionWithConfiguration:[NSURLSessionConfiguration defaultSessionConfiguration]];
        NSURLSessionWebSocketTask* webSocketTask = [session webSocketTaskWithURL:nsUrl];
        
        if (!webSocketTask) {
            NSLog(@"[WebSocketPlugin] WebSocket 태스크 생성 실패");
            return -1;
        }
        
        [webSocketLock lock];
        NSNumber* webSocketId = @([webSocketIdCounter intValue] + 1);
        webSocketIdCounter = webSocketId;
        [webSocketMap setObject:webSocketTask forKey:webSocketId];
        [webSocketLock unlock];
        
        // 연결 시작
        [webSocketTask resume];
        
        // 연결 성공 콜백
        dispatch_async(dispatch_get_main_queue(), ^{
            UnitySendMessage("WebSocketManager", "OnNativeConnected", "");
        });
        
        // 메시지 수신 루프 시작
        [self receiveMessageLoop:webSocketTask withId:webSocketId];
        
        NSLog(@"[WebSocketPlugin] iOS WebSocket 연결 성공 (ID: %@)", webSocketId);
        return [webSocketId intValue];
    }
}

/**
 * WebSocket 연결 해제
 * 
 * @param webSocketId WebSocket ID
 */
extern "C" void IOSWebSocket_Disconnect(int webSocketId) {
    @autoreleasepool {
        NSNumber* wsId = @(webSocketId);
        
        [webSocketLock lock];
        NSURLSessionWebSocketTask* webSocketTask = [webSocketMap objectForKey:wsId];
        [webSocketMap removeObjectForKey:wsId];
        [webSocketLock unlock];
        
        if (webSocketTask) {
            NSLog(@"[WebSocketPlugin] iOS WebSocket 연결 해제 (ID: %d)", webSocketId);
            
            [webSocketTask cancelWithCloseCode:NSURLSessionWebSocketCloseCodeNormalClosure 
                                     reason:[@"Client disconnect" dataUsingEncoding:NSUTF8StringEncoding] 
                           completionHandler:^(NSError* error) {
                if (error) {
                    NSLog(@"[WebSocketPlugin] WebSocket 연결 해제 중 오류: %@", error.localizedDescription);
                }
            }];
        }
    }
}

/**
 * WebSocket 메시지 전송
 * 
 * @param webSocketId WebSocket ID
 * @param message 전송할 메시지
 * @return 전송 성공 여부
 */
extern "C" bool IOSWebSocket_SendMessage(int webSocketId, const char* message) {
    @autoreleasepool {
        NSNumber* wsId = @(webSocketId);
        NSString* messageString = [NSString stringWithUTF8String:message];
        
        [webSocketLock lock];
        NSURLSessionWebSocketTask* webSocketTask = [webSocketMap objectForKey:wsId];
        [webSocketLock unlock];
        
        if (webSocketTask) {
            NSLog(@"[WebSocketPlugin] iOS WebSocket 메시지 전송 (ID: %d, 크기: %lu bytes)", 
                  webSocketId, (unsigned long)messageString.length);
            
            NSURLSessionWebSocketMessage* wsMessage = [[NSURLSessionWebSocketMessage alloc] initWithString:messageString];
            
            [webSocketTask sendMessage:wsMessage completionHandler:^(NSError* error) {
                if (error) {
                    NSLog(@"[WebSocketPlugin] 메시지 전송 실패: %@", error.localizedDescription);
                }
            }];
            
            return true;
        } else {
            NSLog(@"[WebSocketPlugin] WebSocket을 찾을 수 없음 (ID: %d)", webSocketId);
            return false;
        }
    }
}

/**
 * WebSocket 메시지 수신 (폴링 방식)
 * 
 * @param webSocketId WebSocket ID
 * @return 수신된 메시지 (없으면 null)
 */
extern "C" const char* IOSWebSocket_ReceiveMessage(int webSocketId) {
    // NSURLSessionWebSocketTask는 콜백 방식이므로 폴링은 불필요
    // 이 함수는 Unity 인터페이스 호환성을 위해 제공
    return nullptr;
}

/**
 * 메시지 수신 루프
 */
- (void)receiveMessageLoop:(NSURLSessionWebSocketTask*)webSocketTask withId:(NSNumber*)webSocketId {
    [webSocketTask receiveMessageWithCompletionHandler:^(NSURLSessionWebSocketMessage* message, NSError* error) {
        if (error) {
            NSLog(@"[WebSocketPlugin] 메시지 수신 오류: %@", error.localizedDescription);
            
            // 연결 종료 콜백
            dispatch_async(dispatch_get_main_queue(), ^{
                UnitySendMessage("WebSocketManager", "OnNativeDisconnected", "");
            });
            return;
        }
        
        if (message.type == NSURLSessionWebSocketMessageTypeString) {
            NSString* text = message.string;
            NSLog(@"[WebSocketPlugin] iOS WebSocket 메시지 수신: %lu bytes", (unsigned long)text.length);
            
            // Unity로 메시지 전달
            dispatch_async(dispatch_get_main_queue(), ^{
                UnitySendMessage("WebSocketManager", "OnNativeMessageReceived", [text UTF8String]);
            });
        } else if (message.type == NSURLSessionWebSocketMessageTypeData) {
            NSLog(@"[WebSocketPlugin] 바이너리 메시지 수신 (무시됨)");
        }
        
        // 다음 메시지 수신을 위해 루프 계속
        [self receiveMessageLoop:webSocketTask withId:webSocketId];
    }];
}

/**
 * 모든 WebSocket 연결 해제
 */
extern "C" void IOSWebSocket_DisconnectAll() {
    @autoreleasepool {
        NSLog(@"[WebSocketPlugin] 모든 iOS WebSocket 연결 해제");
        
        [webSocketLock lock];
        NSArray* allKeys = [webSocketMap allKeys];
        [webSocketLock unlock];
        
        for (NSNumber* wsId in allKeys) {
            IOSWebSocket_Disconnect([wsId intValue]);
        }
    }
} 