using System;
using UnityEngine;

namespace ProjectVG.Infrastructure.Auth.OAuth2.Config
{
    /// <summary>
    /// 서버 OAuth2 설정
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "ServerOAuth2Config", menuName = "Auth/Server OAuth2 Config")]
    public class ServerOAuth2Config : ScriptableObject
    {
        [Header("OAuth2 설정")]
        
        [Header("플랫폼별 리다이렉트 URI")]
        [SerializeField] private string webGLRedirectUri = "http://localhost:3000/auth/callback";
        [SerializeField] private string androidRedirectUri = "com.yourgame://auth/callback";
        [SerializeField] private string iosRedirectUri = "com.yourgame://auth/callback";
        [SerializeField] private string windowsRedirectUri = "http://localhost:3000/auth/callback";
        [SerializeField] private string macosRedirectUri = "http://localhost:3000/auth/callback";
        
        [Header("고급 설정")]
        [SerializeField] private int pkceCodeVerifierLength = 64;
        [SerializeField] private int stateLength = 16;
        [SerializeField] private float timeoutSeconds = 300f;
        
        // Singleton instance
        private static ServerOAuth2Config _instance;
        public static ServerOAuth2Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ServerOAuth2Config>("ServerOAuth2Config");
                    if (_instance == null)
                    {
                        Debug.LogError("ServerOAuth2Config를 찾을 수 없습니다. Resources 폴더에 ServerOAuth2Config.asset 파일을 생성하세요.");
                        _instance = CreateDefaultInstance();
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 서버 URL (NetworkConfig에서 가져옴)
        /// </summary>
        public string ServerUrl => ProjectVG.Infrastructure.Network.Configs.NetworkConfig.HttpServerAddress;
       
        
        /// <summary>
        /// 현재 플랫폼의 리다이렉트 URI
        /// </summary>
        public string GetCurrentPlatformRedirectUri()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return webGLRedirectUri;
#elif UNITY_ANDROID && !UNITY_EDITOR
            return androidRedirectUri;
#elif UNITY_IOS && !UNITY_EDITOR
            return iosRedirectUri;
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
            return windowsRedirectUri;
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            return macosRedirectUri;
#else
            // 에디터에서는 Windows 설정 사용
            return windowsRedirectUri;
#endif
        }
        
        /// <summary>
        /// PKCE Code Verifier 길이
        /// </summary>
        public int PKCECodeVerifierLength => pkceCodeVerifierLength;
        
        /// <summary>
        /// State 길이
        /// </summary>
        public int StateLength => stateLength;
        
        /// <summary>
        /// 타임아웃 (초)
        /// </summary>
        public float TimeoutSeconds => timeoutSeconds;
        
        /// <summary>
        /// 설정 유효성 검사
        /// </summary>
        public bool IsValid()
        {
            // NetworkConfig 유효성 검사
            var networkConfig = ProjectVG.Infrastructure.Network.Configs.NetworkConfig.Instance;
            if (networkConfig == null)
            {
                Debug.LogError("[ServerOAuth2Config] NetworkConfig를 찾을 수 없습니다.");
                return false;
            }
            
            // 기본 필수 값 검사
            var hasRequiredFields = !string.IsNullOrEmpty(GetCurrentPlatformRedirectUri());
            
            // PKCE 설정 검사
            var hasValidPKCE = pkceCodeVerifierLength >= 43 && pkceCodeVerifierLength <= 128 &&
                              stateLength >= 16 && stateLength <= 64;
            
            // 타임아웃 검사
            var hasValidTimeout = timeoutSeconds > 0;
            
            var isValid = hasRequiredFields && hasValidPKCE && hasValidTimeout;
            
            if (!isValid)
            {
                Debug.LogError($"[ServerOAuth2Config] 설정 유효성 검사 실패:");
                
                if (string.IsNullOrEmpty(GetCurrentPlatformRedirectUri()))
                    Debug.LogError($"  - redirectUri: 비어있음");
                else if (GetCurrentPlatformRedirectUri().Contains("your-domain"))
                    Debug.LogError($"  - redirectUri: '{GetCurrentPlatformRedirectUri()}' (기본값입니다. 실제 도메인으로 변경하세요)");
                else
                    Debug.LogError($"  - redirectUri: '{GetCurrentPlatformRedirectUri()}'");
                    
                if (!hasValidPKCE)
                {
                    Debug.LogError($"  - pkceCodeVerifierLength: {pkceCodeVerifierLength} (43-128 사이여야 함)");
                    Debug.LogError($"  - stateLength: {stateLength} (16-64 사이여야 함)");
                }
                
                if (!hasValidTimeout)
                    Debug.LogError($"  - timeoutSeconds: {timeoutSeconds} (0보다 커야 함)");
            }
            else
            {
                Debug.Log($"[ServerOAuth2Config] 설정 유효성 검사 통과");
                Debug.Log($"  - 서버: {ServerUrl} (NetworkConfig에서 가져옴)");
                Debug.Log($"  - 플랫폼: {GetCurrentPlatformName()}");
                Debug.Log($"  - 리다이렉트 URI: {GetCurrentPlatformRedirectUri()}");
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 현재 플랫폼 이름
        /// </summary>
        public string GetCurrentPlatformName()
        {
#if UNITY_WEBGL
            return "WebGL";
#elif UNITY_ANDROID
            return "Android";
#elif UNITY_IOS
            return "iOS";
#elif UNITY_STANDALONE_WIN
            return "Windows";
#elif UNITY_STANDALONE_OSX
            return "macOS";
#else
            return "Unknown";
#endif
        }
        
        /// <summary>
        /// 기본 인스턴스 생성
        /// </summary>
        private static ServerOAuth2Config CreateDefaultInstance()
        {
            var instance = CreateInstance<ServerOAuth2Config>();
            
            // JavaScript 클라이언트와 동일한 기본값으로 초기화
            instance.webGLRedirectUri = "http://localhost:3000/auth/callback";
            instance.androidRedirectUri = "com.yourgame://auth/callback";
            instance.iosRedirectUri = "com.yourgame://auth/callback";
            instance.windowsRedirectUri = "http://localhost:3000/auth/callback";
            instance.macosRedirectUri = "http://localhost:3000/auth/callback";
            instance.pkceCodeVerifierLength = 64; // PKCE 표준에 맞게 64바이트
            instance.stateLength = 16;
            instance.timeoutSeconds = 300f;
            
            Debug.LogWarning("기본 ServerOAuth2Config를 생성했습니다. Resources 폴더에 ServerOAuth2Config.asset 파일을 생성하는 것을 권장합니다.");
            
            return instance;
        }
        
#if UNITY_EDITOR
        [Header("개발자 도구")]
        [SerializeField] private bool showDebugInfo = false;
        
        private void OnValidate()
        {
            if (showDebugInfo)
            {
                Debug.Log($"[ServerOAuth2Config] 현재 플랫폼: {GetCurrentPlatformName()}");
                Debug.Log($"[ServerOAuth2Config] 서버 URL: {ServerUrl}");
                Debug.Log($"[ServerOAuth2Config] 리다이렉트 URI: {GetCurrentPlatformRedirectUri()}");
                Debug.Log($"[ServerOAuth2Config] 설정 유효: {IsValid()}");
            }
        }
#endif
    }
}
