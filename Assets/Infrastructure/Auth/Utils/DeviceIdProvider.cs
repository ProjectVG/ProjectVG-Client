using System;
using UnityEngine;

namespace ProjectVG.Infrastructure.Auth.Utils
{
    /// <summary>
    /// 디바이스 고유 ID 제공자
    /// 플랫폼별로 디바이스 고유 식별자를 생성/관리
    /// </summary>
    public static class DeviceIdProvider
    {
        private const string DEVICE_ID_KEY = "projectvg_device_id";
        private static string _cachedDeviceId;

        /// <summary>
        /// 디바이스 고유 ID 반환
        /// </summary>
        /// <returns>디바이스 고유 ID</returns>
        public static string GetDeviceId()
        {
            if (!string.IsNullOrEmpty(_cachedDeviceId))
            {
                return _cachedDeviceId;
            }

            // PlayerPrefs에서 저장된 ID 확인
            if (PlayerPrefs.HasKey(DEVICE_ID_KEY))
            {
                _cachedDeviceId = PlayerPrefs.GetString(DEVICE_ID_KEY);
                Debug.Log($"[DeviceIdProvider] 저장된 디바이스 ID 로드: {MaskDeviceId(_cachedDeviceId)}");
                return _cachedDeviceId;
            }

            // 새로운 디바이스 ID 생성
            _cachedDeviceId = GenerateDeviceId();
            
            // PlayerPrefs에 저장
            PlayerPrefs.SetString(DEVICE_ID_KEY, _cachedDeviceId);
            PlayerPrefs.Save();

            Debug.Log($"[DeviceIdProvider] 새로운 디바이스 ID 생성: {MaskDeviceId(_cachedDeviceId)}");
            return _cachedDeviceId;
        }

        /// <summary>
        /// 플랫폼별 디바이스 ID 생성
        /// </summary>
        private static string GenerateDeviceId()
        {
            string platformPrefix;
            string platformId;

#if UNITY_ANDROID && !UNITY_EDITOR
            platformPrefix = "android";
            platformId = GetAndroidDeviceId();
#elif UNITY_IOS && !UNITY_EDITOR
            platformPrefix = "ios";
            platformId = GetIOSDeviceId();
#elif UNITY_WEBGL && !UNITY_EDITOR
            platformPrefix = "webgl";
            platformId = GetWebGLDeviceId();
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
            platformPrefix = "windows";
            platformId = GetWindowsDeviceId();
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            platformPrefix = "macos";
            platformId = GetMacOSDeviceId();
#elif UNITY_STANDALONE_LINUX && !UNITY_EDITOR
            platformPrefix = "linux";
            platformId = GetLinuxDeviceId();
#else
            // Unity Editor 또는 알 수 없는 플랫폼
            platformPrefix = "editor";
            platformId = GetEditorDeviceId();
#endif

            // 플랫폼 접두사 + 하이픈 + 디바이스 ID + 타임스탬프
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"{platformPrefix}-{platformId}-{timestamp}";
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static string GetAndroidDeviceId()
        {
            try
            {
                // Android Device ID 사용
                using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver"))
                using (var settingsSecure = new AndroidJavaClass("android.provider.Settings$Secure"))
                {
                    string androidId = settingsSecure.CallStatic<string>("getString", contentResolver, "android_id");
                    if (!string.IsNullOrEmpty(androidId))
                    {
                        return androidId;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceIdProvider] Android ID 생성 실패: {ex.Message}");
            }
            
            // Fallback: SystemInfo.deviceUniqueIdentifier
            return SystemInfo.deviceUniqueIdentifier;
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        private static string GetIOSDeviceId()
        {
            // iOS는 IDFV (Identifier for Vendor) 사용
            // SystemInfo.deviceUniqueIdentifier가 IDFV를 반환함
            return SystemInfo.deviceUniqueIdentifier;
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        private static string GetWebGLDeviceId()
        {
            // WebGL에서는 브라우저 기반 ID 생성
            string browserId = SystemInfo.deviceUniqueIdentifier;
            
            // 추가로 브라우저 정보 포함
            string userAgent = Application.platform.ToString();
            string screenInfo = $"{Screen.width}x{Screen.height}";
            
            return $"{browserId}-{userAgent.GetHashCode()}-{screenInfo.GetHashCode()}";
        }
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static string GetWindowsDeviceId()
        {
            try
            {
                // Windows Machine GUID 사용
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                if (key != null)
                {
                    var machineGuid = key.GetValue("MachineGuid")?.ToString();
                    if (!string.IsNullOrEmpty(machineGuid))
                    {
                        return machineGuid;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceIdProvider] Windows Machine GUID 생성 실패: {ex.Message}");
            }
            
            // Fallback
            return SystemInfo.deviceUniqueIdentifier;
        }
#endif

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        private static string GetMacOSDeviceId()
        {
            // macOS는 SystemInfo.deviceUniqueIdentifier 사용
            return SystemInfo.deviceUniqueIdentifier;
        }
#endif

#if UNITY_STANDALONE_LINUX && !UNITY_EDITOR
        private static string GetLinuxDeviceId()
        {
            // Linux는 SystemInfo.deviceUniqueIdentifier 사용
            return SystemInfo.deviceUniqueIdentifier;
        }
#endif

        private static string GetEditorDeviceId()
        {
            // Unity Editor에서는 고정된 ID + 랜덤 요소 사용
            string editorId = SystemInfo.deviceUniqueIdentifier;
            
            // Editor에서는 개발 편의성을 위해 단순한 ID 생성
            if (string.IsNullOrEmpty(editorId) || editorId == "n/a")
            {
                editorId = $"editor-{Environment.MachineName}-{Environment.UserName}";
            }
            
            return editorId;
        }

        /// <summary>
        /// 디바이스 ID 마스킹 (로깅용)
        /// </summary>
        private static string MaskDeviceId(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId) || deviceId.Length < 8)
            {
                return "***";
            }

            // 앞 4자리와 뒤 4자리만 표시
            return $"{deviceId.Substring(0, 4)}****{deviceId.Substring(deviceId.Length - 4)}";
        }

        /// <summary>
        /// 디바이스 ID 초기화 (테스트용)
        /// </summary>
        public static void ClearDeviceId()
        {
            _cachedDeviceId = null;
            PlayerPrefs.DeleteKey(DEVICE_ID_KEY);
            PlayerPrefs.Save();
            Debug.Log("[DeviceIdProvider] 디바이스 ID 초기화 완료");
        }

        /// <summary>
        /// 현재 플랫폼 정보 반환
        /// </summary>
        public static string GetPlatformInfo()
        {
            return $"Platform: {Application.platform}, " +
                   $"OS: {SystemInfo.operatingSystem}, " +
                   $"Device: {SystemInfo.deviceModel}";
        }

        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public static string GetDebugInfo()
        {
            var info = "DeviceIdProvider Debug Info:\n";
            info += $"Cached Device ID: {MaskDeviceId(_cachedDeviceId)}\n";
            info += $"Has Stored ID: {PlayerPrefs.HasKey(DEVICE_ID_KEY)}\n";
            info += $"{GetPlatformInfo()}\n";
            info += $"System Device ID: {MaskDeviceId(SystemInfo.deviceUniqueIdentifier)}\n";
            
            return info;
        }
    }
}