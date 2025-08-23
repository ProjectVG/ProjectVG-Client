using UnityEngine;

namespace ProjectVG.Infrastructure.Auth.Storage
{
    public static class SecureStorageFactory
    {
        public static ISecureStorage CreatePlatformStorage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new Platforms.WebGLCookieStorage();
#elif UNITY_ANDROID && !UNITY_EDITOR
            return new Platforms.AndroidSecureStorage();
#elif UNITY_IOS && !UNITY_EDITOR
            return new Platforms.IOSSecureStorage();
#elif (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            return new Platforms.WindowsSecureStorage();
#elif (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX) && !UNITY_WEBGL
            return new Platforms.MacOSSecureStorage();
#else
            Debug.LogWarning("[SecureStorageFactory] 지원되지 않는 플랫폼입니다. 기본 저장소를 사용합니다.");
            return new Platforms.FallbackSecureStorage();
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
