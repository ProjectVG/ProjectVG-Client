using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.Storage.Platforms
{
    public class FallbackSecureStorage : ISecureStorage
    {
        public bool IsAvailable => true;
        public string PlatformName => "Fallback (PlayerPrefs)";
        
        public FallbackSecureStorage()
        {
            Debug.LogWarning("[FallbackSecureStorage] 플랫폼별 보안 저장소를 사용할 수 없어 PlayerPrefs를 사용합니다. 보안성이 낮습니다.");
        }
        
        #region Public Methods
        
        public async UniTask<bool> StoreAsync(string key, string value)
        {
            try
            {
                PlayerPrefs.SetString($"auth_{key}", value);
                PlayerPrefs.Save();
                
                await UniTask.Delay(1);
                Debug.LogWarning($"[FallbackSecureStorage] PlayerPrefs에 저장 (보안 취약): {key}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FallbackSecureStorage] 저장 실패: {ex.Message}");
                return false;
            }
        }
        
        public async UniTask<string> LoadAsync(string key)
        {
            try
            {
                var value = PlayerPrefs.GetString($"auth_{key}", null);
                
                await UniTask.Delay(1);
                if (!string.IsNullOrEmpty(value))
                {
                    Debug.LogWarning($"[FallbackSecureStorage] PlayerPrefs에서 로드 (보안 취약): {key}");
                }
                
                return value;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FallbackSecureStorage] 로드 실패: {ex.Message}");
                return null;
            }
        }
        
        public async UniTask<bool> DeleteAsync(string key)
        {
            try
            {
                PlayerPrefs.DeleteKey($"auth_{key}");
                PlayerPrefs.Save();
                
                await UniTask.Delay(1);
                Debug.LogWarning($"[FallbackSecureStorage] PlayerPrefs에서 삭제: {key}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FallbackSecureStorage] 삭제 실패: {ex.Message}");
                return false;
            }
        }
        
        public async UniTask<bool> ExistsAsync(string key)
        {
            try
            {
                var exists = PlayerPrefs.HasKey($"auth_{key}");
                await UniTask.Delay(1);
                return exists;
            }
            catch
            {
                return false;
            }
        }
        
        public async UniTask ClearAllAsync()
        {
            try
            {
                var keysToDelete = new System.Collections.Generic.List<string>();
                
                // PlayerPrefs에는 모든 키를 나열하는 기능이 없으므로
                // 알려진 인증 관련 키들만 삭제
                keysToDelete.Add("auth_refresh_token");
                keysToDelete.Add("auth_auth_state");
                keysToDelete.Add("auth_device_id");
                
                foreach (var key in keysToDelete)
                {
                    if (PlayerPrefs.HasKey(key))
                    {
                        PlayerPrefs.DeleteKey(key);
                    }
                }
                
                PlayerPrefs.Save();
                
                await UniTask.Delay(1);
                Debug.LogWarning("[FallbackSecureStorage] PlayerPrefs에서 인증 데이터 삭제 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FallbackSecureStorage] 전체 삭제 실패: {ex.Message}");
            }
        }
        
        public async UniTask<bool> StoreEncryptedAsync(string key, byte[] data)
        {
            if (data == null) return false;
            
            var base64Data = Convert.ToBase64String(data);
            return await StoreAsync(key, base64Data);
        }
        
        public async UniTask<byte[]> LoadEncryptedAsync(string key)
        {
            var base64Data = await LoadAsync(key);
            if (string.IsNullOrEmpty(base64Data)) return null;
            
            try
            {
                return Convert.FromBase64String(base64Data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FallbackSecureStorage] 바이너리 데이터 디코딩 실패: {ex.Message}");
                return null;
            }
        }
        
        #endregion
    }
}
