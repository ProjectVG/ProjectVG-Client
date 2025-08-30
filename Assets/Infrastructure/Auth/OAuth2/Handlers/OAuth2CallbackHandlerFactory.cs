using UnityEngine;

namespace ProjectVG.Infrastructure.Auth.OAuth2.Handlers
{
    /// <summary>
    /// OAuth2 콜백 핸들러 팩토리
    /// 플랫폼에 맞는 콜백 핸들러를 생성
    /// </summary>
    public static class OAuth2CallbackHandlerFactory
    {
        /// <summary>
        /// 현재 플랫폼에 맞는 콜백 핸들러 생성
        /// </summary>
        /// <returns>플랫폼별 콜백 핸들러</returns>
        public static IOAuth2CallbackHandler CreateHandler()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[OAuth2CallbackHandlerFactory] WebGL 콜백 핸들러 생성");
            return new WebGLCallbackHandler();
#elif UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("[OAuth2CallbackHandlerFactory] Android 콜백 핸들러 생성");
            return new MobileCallbackHandler();
#elif UNITY_IOS && !UNITY_EDITOR
            Debug.Log("[OAuth2CallbackHandlerFactory] iOS 콜백 핸들러 생성");
            return new MobileCallbackHandler();
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
            Debug.Log("[OAuth2CallbackHandlerFactory] Windows 콜백 핸들러 생성");
            return new DesktopCallbackHandler();
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            Debug.Log("[OAuth2CallbackHandlerFactory] macOS 콜백 핸들러 생성");
            return new DesktopCallbackHandler();
#else
            // 에디터에서는 데스크톱 핸들러 사용
            Debug.Log("[OAuth2CallbackHandlerFactory] 에디터용 데스크톱 콜백 핸들러 생성");
            return new DesktopCallbackHandler();
#endif
        }
        
        /// <summary>
        /// 현재 플랫폼 이름 반환
        /// </summary>
        /// <returns>플랫폼 이름</returns>
        public static string GetCurrentPlatformName()
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
            return "Editor";
#endif
        }
        
        /// <summary>
        /// 현재 플랫폼이 지원되는지 확인
        /// </summary>
        /// <returns>지원 여부</returns>
        public static bool IsPlatformSupported()
        {
            var handler = CreateHandler();
            return handler.IsSupported;
        }
    }
}
