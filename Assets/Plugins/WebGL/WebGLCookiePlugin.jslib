var WebGLCookiePlugin = {
    
    // HttpOnly 쿠키 설정 (서버를 통해서만 가능)
    SetHttpOnlyCookie: function(namePtr, valuePtr, maxAgeSeconds, sameSitePtr) {
        var name = UTF8ToString(namePtr);
        var value = UTF8ToString(valuePtr);
        var sameSite = UTF8ToString(sameSitePtr);
        
        try {
            // HttpOnly 쿠키는 클라이언트에서 직접 설정할 수 없으므로
            // 서버 API를 통해 설정해야 함
            console.warn('[WebGLCookiePlugin] HttpOnly 쿠키는 서버를 통해서만 설정 가능합니다: ' + name);
            
            // 임시로 일반 쿠키로 설정 (개발/테스트용)
            var cookieString = name + '=' + encodeURIComponent(value);
            cookieString += '; Max-Age=' + maxAgeSeconds;
            cookieString += '; SameSite=' + sameSite;
            cookieString += '; Secure';
            cookieString += '; Path=/';
            
            document.cookie = cookieString;
            
            console.log('[WebGLCookiePlugin] 쿠키 설정: ' + name + ' (일반 쿠키로 fallback)');
        } catch (error) {
            console.error('[WebGLCookiePlugin] 쿠키 설정 실패: ' + error.message);
        }
    },
    
    // 쿠키 값 읽기
    GetCookieValue: function(namePtr) {
        var name = UTF8ToString(namePtr);
        
        try {
            var nameEQ = name + '=';
            var cookies = document.cookie.split(';');
            
            for (var i = 0; i < cookies.length; i++) {
                var cookie = cookies[i];
                while (cookie.charAt(0) === ' ') {
                    cookie = cookie.substring(1, cookie.length);
                }
                if (cookie.indexOf(nameEQ) === 0) {
                    var value = cookie.substring(nameEQ.length, cookie.length);
                    return allocateUTF8(decodeURIComponent(value));
                }
            }
            
            console.log('[WebGLCookiePlugin] 쿠키를 찾을 수 없음: ' + name);
            return allocateUTF8('');
        } catch (error) {
            console.error('[WebGLCookiePlugin] 쿠키 읽기 실패: ' + error.message);
            return allocateUTF8('');
        }
    },
    
    // 쿠키 삭제
    DeleteCookie: function(namePtr) {
        var name = UTF8ToString(namePtr);
        
        try {
            var cookieString = name + '=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
            document.cookie = cookieString;
            
            console.log('[WebGLCookiePlugin] 쿠키 삭제: ' + name);
        } catch (error) {
            console.error('[WebGLCookiePlugin] 쿠키 삭제 실패: ' + error.message);
        }
    },
    
    // CSRF 토큰 생성
    GenerateCSRFToken: function() {
        try {
            // 암호학적으로 안전한 랜덤 값 생성
            var array = new Uint8Array(32);
            crypto.getRandomValues(array);
            
            var token = '';
            for (var i = 0; i < array.length; i++) {
                token += array[i].toString(16).padStart(2, '0');
            }
            
            console.log('[WebGLCookiePlugin] CSRF 토큰 생성 완료');
            return allocateUTF8(token);
        } catch (error) {
            console.error('[WebGLCookiePlugin] CSRF 토큰 생성 실패: ' + error.message);
            
            // fallback: Math.random 사용
            var fallbackToken = '';
            for (var i = 0; i < 32; i++) {
                fallbackToken += Math.floor(Math.random() * 16).toString(16);
            }
            return allocateUTF8(fallbackToken);
        }
    },
    
    // 쿠키 지원 여부 확인
    IsCookieSupported: function() {
        try {
            // 테스트 쿠키 설정 시도
            var testName = '_cookie_test_';
            var testValue = 'test';
            
            document.cookie = testName + '=' + testValue + '; SameSite=Lax';
            
            var supported = document.cookie.indexOf(testName + '=' + testValue) !== -1;
            
            // 테스트 쿠키 삭제
            document.cookie = testName + '=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
            
            console.log('[WebGLCookiePlugin] 쿠키 지원 여부: ' + supported);
            return supported;
        } catch (error) {
            console.error('[WebGLCookiePlugin] 쿠키 지원 여부 확인 실패: ' + error.message);
            return false;
        }
    },
    
    // 모든 인증 관련 쿠키 삭제
    ClearAllAuthCookies: function() {
        try {
            var authCookieNames = [
                'auth_refresh_token',
                'auth_session',
                'csrf_token',
                'auth_state'
            ];
            
            for (var i = 0; i < authCookieNames.length; i++) {
                var cookieString = authCookieNames[i] + '=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
                document.cookie = cookieString;
            }
            
            console.log('[WebGLCookiePlugin] 모든 인증 쿠키 삭제 완료');
        } catch (error) {
            console.error('[WebGLCookiePlugin] 인증 쿠키 삭제 실패: ' + error.message);
        }
    },
    
    // 브라우저 보안 정보 확인
    GetBrowserSecurityInfo: function() {
        try {
            var info = {
                isSecureContext: window.isSecureContext,
                protocol: window.location.protocol,
                cookieEnabled: navigator.cookieEnabled,
                userAgent: navigator.userAgent,
                hasLocalStorage: typeof(Storage) !== "undefined",
                hasSessionStorage: typeof(Storage) !== "undefined" && typeof(sessionStorage) !== "undefined",
                hasCrypto: typeof(crypto) !== "undefined" && typeof(crypto.getRandomValues) === "function"
            };
            
            console.log('[WebGLCookiePlugin] 브라우저 보안 정보:', info);
            return allocateUTF8(JSON.stringify(info));
        } catch (error) {
            console.error('[WebGLCookiePlugin] 브라우저 보안 정보 확인 실패: ' + error.message);
            return allocateUTF8('{}');
        }
    }
};

// Unity에 함수들을 등록
mergeInto(LibraryManager.library, WebGLCookiePlugin);
