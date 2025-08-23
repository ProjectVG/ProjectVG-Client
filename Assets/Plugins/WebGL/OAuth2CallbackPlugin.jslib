var OAuth2CallbackPlugin = {
    
    _callbackObjectName: null,
    _callbackMethodName: null,
    _isListening: false,
    _originalPopstateHandler: null,
    
    // OAuth2 콜백 리스너 시작
    StartOAuth2CallbackListener: function(objectNamePtr, methodNamePtr) {
        var objectName = UTF8ToString(objectNamePtr);
        var methodName = UTF8ToString(methodNamePtr);
        
        OAuth2CallbackPlugin._callbackObjectName = objectName;
        OAuth2CallbackPlugin._callbackMethodName = methodName;
        OAuth2CallbackPlugin._isListening = true;
        
        console.log('[OAuth2CallbackPlugin] 콜백 리스너 시작: ' + objectName + '.' + methodName);
        
        // 현재 URL 즉시 확인
        OAuth2CallbackPlugin._checkCurrentUrl();
        
        // URL 변경 감지를 위한 popstate 이벤트 리스너 등록
        OAuth2CallbackPlugin._originalPopstateHandler = window.onpopstate;
        window.onpopstate = function(event) {
            if (OAuth2CallbackPlugin._isListening) {
                OAuth2CallbackPlugin._checkCurrentUrl();
            }
            
            // 기존 핸들러 호출
            if (OAuth2CallbackPlugin._originalPopstateHandler) {
                OAuth2CallbackPlugin._originalPopstateHandler(event);
            }
        };
        
        // 주기적으로 URL 변경 감지 (popstate가 동작하지 않는 경우 대비)
        OAuth2CallbackPlugin._startUrlPolling();
    },
    
    // OAuth2 콜백 리스너 중지
    StopOAuth2CallbackListener: function() {
        OAuth2CallbackPlugin._isListening = false;
        OAuth2CallbackPlugin._callbackObjectName = null;
        OAuth2CallbackPlugin._callbackMethodName = null;
        
        // popstate 이벤트 리스너 복원
        window.onpopstate = OAuth2CallbackPlugin._originalPopstateHandler;
        OAuth2CallbackPlugin._originalPopstateHandler = null;
        
        console.log('[OAuth2CallbackPlugin] 콜백 리스너 중지');
    },
    
    // 현재 URL 반환
    GetCurrentUrl: function() {
        return allocateUTF8(window.location.href);
    },
    
    // URL 변경 리스너 등록
    RegisterUrlChangeListener: function(objectNamePtr, methodNamePtr) {
        OAuth2CallbackPlugin.StartOAuth2CallbackListener(objectNamePtr, methodNamePtr);
    },
    
    // URL 변경 리스너 해제
    UnregisterUrlChangeListener: function() {
        OAuth2CallbackPlugin.StopOAuth2CallbackListener();
    },
    
    // 현재 URL 확인 및 콜백 처리
    _checkCurrentUrl: function() {
        if (!OAuth2CallbackPlugin._isListening) {
            return;
        }
        
        var currentUrl = window.location.href;
        var urlParams = new URLSearchParams(window.location.search);
        
        // OAuth2 콜백 파라미터 확인
        var code = urlParams.get('code');
        var state = urlParams.get('state');
        var error = urlParams.get('error');
        
        if (code || error) {
            console.log('[OAuth2CallbackPlugin] OAuth2 콜백 감지: ' + currentUrl);
            
            // Unity 콜백 메서드 호출
            if (OAuth2CallbackPlugin._callbackObjectName && OAuth2CallbackPlugin._callbackMethodName) {
                try {
                    SendMessage(OAuth2CallbackPlugin._callbackObjectName, OAuth2CallbackPlugin._callbackMethodName, currentUrl);
                } catch (e) {
                    console.error('[OAuth2CallbackPlugin] Unity 콜백 호출 실패: ' + e.message);
                }
            }
            
            // 리스너 자동 중지
            OAuth2CallbackPlugin.StopOAuth2CallbackListener();
        }
    },
    
    // URL 폴링 시작 (fallback)
    _startUrlPolling: function() {
        var pollInterval = 500; // 500ms마다 확인
        var maxPollTime = 300000; // 최대 5분
        var startTime = Date.now();
        
        var poll = function() {
            if (!OAuth2CallbackPlugin._isListening) {
                return;
            }
            
            if (Date.now() - startTime > maxPollTime) {
                console.log('[OAuth2CallbackPlugin] URL 폴링 타임아웃');
                OAuth2CallbackPlugin.StopOAuth2CallbackListener();
                return;
            }
            
            OAuth2CallbackPlugin._checkCurrentUrl();
            
            if (OAuth2CallbackPlugin._isListening) {
                setTimeout(poll, pollInterval);
            }
        };
        
        setTimeout(poll, pollInterval);
        console.log('[OAuth2CallbackPlugin] URL 폴링 시작 (간격: ' + pollInterval + 'ms)');
    },
    
    // OAuth2 인증 URL로 리다이렉트
    RedirectToOAuth2Url: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        console.log('[OAuth2CallbackPlugin] OAuth2 URL로 리다이렉트: ' + url);
        
        try {
            window.location.href = url;
        } catch (e) {
            console.error('[OAuth2CallbackPlugin] 리다이렉트 실패: ' + e.message);
        }
    },
    
    // 새 창에서 OAuth2 인증 열기
    OpenOAuth2InNewWindow: function(urlPtr, windowNamePtr) {
        var url = UTF8ToString(urlPtr);
        var windowName = UTF8ToString(windowNamePtr) || 'oauth2_auth';
        
        console.log('[OAuth2CallbackPlugin] 새 창에서 OAuth2 인증: ' + url);
        
        try {
            var features = 'width=500,height=600,scrollbars=yes,resizable=yes';
            var authWindow = window.open(url, windowName, features);
            
            if (authWindow) {
                // 새 창이 닫혔는지 주기적으로 확인
                var checkClosed = function() {
                    if (authWindow.closed) {
                        console.log('[OAuth2CallbackPlugin] OAuth2 창이 닫혔습니다');
                        return;
                    }
                    setTimeout(checkClosed, 1000);
                };
                setTimeout(checkClosed, 1000);
                
                return true;
            } else {
                console.error('[OAuth2CallbackPlugin] 팝업 창 열기 실패 (팝업 차단됨?)');
                return false;
            }
        } catch (e) {
            console.error('[OAuth2CallbackPlugin] 새 창 열기 실패: ' + e.message);
            return false;
        }
    },
    
    // 브라우저 정보 확인
    GetBrowserInfo: function() {
        try {
            var info = {
                userAgent: navigator.userAgent,
                cookieEnabled: navigator.cookieEnabled,
                onLine: navigator.onLine,
                language: navigator.language,
                platform: navigator.platform,
                vendor: navigator.vendor,
                isSecureContext: window.isSecureContext,
                protocol: window.location.protocol,
                hostname: window.location.hostname,
                port: window.location.port
            };
            
            console.log('[OAuth2CallbackPlugin] 브라우저 정보:', info);
            return allocateUTF8(JSON.stringify(info));
        } catch (e) {
            console.error('[OAuth2CallbackPlugin] 브라우저 정보 확인 실패: ' + e.message);
            return allocateUTF8('{}');
        }
    },
    
    // URL 파라미터 파싱
    ParseUrlParameters: function(urlPtr) {
        try {
            var url = UTF8ToString(urlPtr);
            var urlObj = new URL(url);
            var params = {};
            
            for (var pair of urlObj.searchParams.entries()) {
                params[pair[0]] = pair[1];
            }
            
            console.log('[OAuth2CallbackPlugin] URL 파라미터 파싱:', params);
            return allocateUTF8(JSON.stringify(params));
        } catch (e) {
            console.error('[OAuth2CallbackPlugin] URL 파라미터 파싱 실패: ' + e.message);
            return allocateUTF8('{}');
        }
    }
};

// Unity에 함수들을 등록
mergeInto(LibraryManager.library, OAuth2CallbackPlugin);
