package com.projectvg.websocket;

import android.util.Log;
import com.unity3d.player.UnityPlayer;
import okhttp3.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 안드로이드 네이티브 WebSocket 플러그인
 * 
 * OkHttp WebSocket을 사용하여 구현합니다.
 * Unity에서 호출할 수 있는 네이티브 메서드들을 제공합니다.
 */
public class WebSocketPlugin {
    private static final String TAG = "WebSocketPlugin";
    private static final ConcurrentHashMap<Integer, WebSocket> webSocketMap = new ConcurrentHashMap<>();
    private static final AtomicInteger webSocketIdCounter = new AtomicInteger(0);
    private static final OkHttpClient httpClient = new OkHttpClient();
    
    /**
     * WebSocket 연결
     * 
     * @param url WebSocket 서버 URL
     * @return WebSocket ID (성공 시 양수, 실패 시 -1)
     */
    public static int connect(String url) {
        try {
            Log.d(TAG, "WebSocket 연결 시도: " + url);
            
            Request request = new Request.Builder()
                .url(url)
                .build();
                
            WebSocket webSocket = httpClient.newWebSocket(request, new WebSocketListener() {
                @Override
                public void onOpen(WebSocket webSocket, Response response) {
                    Log.d(TAG, "WebSocket 연결 성공");
                    UnityPlayer.UnitySendMessage("WebSocketManager", "OnNativeConnected", "");
                }
                
                @Override
                public void onMessage(WebSocket webSocket, String text) {
                    Log.d(TAG, "WebSocket 메시지 수신: " + text.length() + " bytes");
                    UnityPlayer.UnitySendMessage("WebSocketManager", "OnNativeMessageReceived", text);
                }
                
                @Override
                public void onClosed(WebSocket webSocket, int code, String reason) {
                    Log.d(TAG, "WebSocket 연결 종료: " + code + " - " + reason);
                    UnityPlayer.UnitySendMessage("WebSocketManager", "OnNativeDisconnected", "");
                }
                
                @Override
                public void onFailure(WebSocket webSocket, Throwable t, Response response) {
                    Log.e(TAG, "WebSocket 연결 실패: " + t.getMessage());
                    UnityPlayer.UnitySendMessage("WebSocketManager", "OnNativeError", t.getMessage());
                }
            });
            
            int webSocketId = webSocketIdCounter.incrementAndGet();
            webSocketMap.put(webSocketId, webSocket);
            
            Log.d(TAG, "WebSocket 생성 완료 (ID: " + webSocketId + ")");
            return webSocketId;
            
        } catch (Exception e) {
            Log.e(TAG, "WebSocket 연결 중 예외 발생: " + e.getMessage());
            return -1;
        }
    }
    
    /**
     * WebSocket 연결 해제
     * 
     * @param webSocketId WebSocket ID
     */
    public static void disconnect(int webSocketId) {
        try {
            WebSocket webSocket = webSocketMap.get(webSocketId);
            if (webSocket != null) {
                Log.d(TAG, "WebSocket 연결 해제 (ID: " + webSocketId + ")");
                webSocket.close(1000, "Client disconnect");
                webSocketMap.remove(webSocketId);
            }
        } catch (Exception e) {
            Log.e(TAG, "WebSocket 연결 해제 중 예외 발생: " + e.getMessage());
        }
    }
    
    /**
     * WebSocket 메시지 전송
     * 
     * @param webSocketId WebSocket ID
     * @param message 전송할 메시지
     * @return 전송 성공 여부
     */
    public static boolean sendMessage(int webSocketId, String message) {
        try {
            WebSocket webSocket = webSocketMap.get(webSocketId);
            if (webSocket != null) {
                Log.d(TAG, "WebSocket 메시지 전송 (ID: " + webSocketId + ", 크기: " + message.length() + " bytes)");
                return webSocket.send(message);
            } else {
                Log.w(TAG, "WebSocket을 찾을 수 없음 (ID: " + webSocketId + ")");
                return false;
            }
        } catch (Exception e) {
            Log.e(TAG, "WebSocket 메시지 전송 중 예외 발생: " + e.getMessage());
            return false;
        }
    }
    
    /**
     * WebSocket 메시지 수신 (폴링 방식)
     * 
     * @param webSocketId WebSocket ID
     * @return 수신된 메시지 (없으면 null)
     */
    public static String receiveMessage(int webSocketId) {
        // OkHttp WebSocket은 콜백 방식이므로 폴링은 불필요
        // 이 메서드는 Unity 인터페이스 호환성을 위해 제공
        return null;
    }
    
    /**
     * 모든 WebSocket 연결 해제
     */
    public static void disconnectAll() {
        try {
            Log.d(TAG, "모든 WebSocket 연결 해제");
            for (WebSocket webSocket : webSocketMap.values()) {
                webSocket.close(1000, "Client shutdown");
            }
            webSocketMap.clear();
        } catch (Exception e) {
            Log.e(TAG, "모든 WebSocket 연결 해제 중 예외 발생: " + e.getMessage());
        }
    }
} 