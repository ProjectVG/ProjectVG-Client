using UnityEngine;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    public static class OAuth2CallbackHandlerFactory
    {
        public static IOAuth2CallbackHandler CreatePlatformHandler()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGLCallbackHandler();
#elif UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidCallbackHandler();
#elif UNITY_IOS && !UNITY_EDITOR
            return new IOSCallbackHandler();
#elif (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            return new WindowsCallbackHandler();
#elif (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX) && !UNITY_WEBGL
            return new MacOSCallbackHandler();
#else
            Debug.LogWarning("[OAuth2CallbackHandlerFactory] 지원되지 않는 플랫폼입니다. 기본 핸들러를 사용합니다.");
            return new FallbackCallbackHandler();
#endif
        }
        
        public static bool IsPlatformSupported()
        {
#if UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            return true;
#else
            return false;
#endif
        }
        
        public static string GetPlatformName()
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
    }
}
