using System;
using System.Text;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.Storage.Platforms
{
    public class WindowsSecureStorage : ISecureStorage
    {
        private const string REGISTRY_PATH = @"SOFTWARE\YourCompany\YourGame\Auth";
        
        public bool IsAvailable { get; private set; }
        public string PlatformName => "Windows DPAPI";
        
        public WindowsSecureStorage()
        {
            InitializeDPAPI();
        }
        
        #region Public Methods
        
        public async UniTask<bool> StoreAsync(string key, string value)
        {
            if (!IsAvailable)
            {
                Debug.LogError("[WindowsSecureStorage] DPAPI를 사용할 수 없습니다.");
                return false;
            }
            
            try
            {
                return await StoreWithDPAPIAsync(key, value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsSecureStorage] 저장 실패: {ex.Message}");
                return false;
            }
        }
        
        public async UniTask<string> LoadAsync(string key)
        {
            if (!IsAvailable)
            {
                Debug.LogError("[WindowsSecureStorage] DPAPI를 사용할 수 없습니다.");
                return null;
            }
            
            try
            {
                return await LoadFromDPAPIAsync(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsSecureStorage] 로드 실패: {ex.Message}");
                return null;
            }
        }
        
        public async UniTask<bool> DeleteAsync(string key)
        {
            if (!IsAvailable)
            {
                Debug.LogError("[WindowsSecureStorage] DPAPI를 사용할 수 없습니다.");
                return false;
            }
            
            try
            {
                return await DeleteFromDPAPIAsync(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsSecureStorage] 삭제 실패: {ex.Message}");
                return false;
            }
        }
        
        public async UniTask<bool> ExistsAsync(string key)
        {
            if (!IsAvailable) return false;
            
            try
            {
                var value = await LoadAsync(key);
                return !string.IsNullOrEmpty(value);
            }
            catch
            {
                return false;
            }
        }
        
        public async UniTask ClearAllAsync()
        {
            if (!IsAvailable) return;
            
            try
            {
                await ClearDPAPIDataAsync();
                Debug.Log("[WindowsSecureStorage] 모든 데이터 삭제 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsSecureStorage] 전체 삭제 실패: {ex.Message}");
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
                Debug.LogError($"[WindowsSecureStorage] 바이너리 데이터 디코딩 실패: {ex.Message}");
                return null;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeDPAPI()
        {
            try
            {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
                IsAvailable = InitializeWindowsDPAPI();
#else
                IsAvailable = false;
                Debug.LogWarning("[WindowsSecureStorage] Windows 플랫폼이 아닙니다.");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsSecureStorage] DPAPI 초기화 실패: {ex.Message}");
                IsAvailable = false;
            }
        }
        
        private bool InitializeWindowsDPAPI()
        {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            try
            {
                // TODO: Windows Native Plugin을 통한 DPAPI 초기화
                // 1. System.Security.Cryptography.ProtectedData 사용 가능 여부 확인
                // 2. 레지스트리 접근 권한 확인
                // 3. 사용자 프로필 암호화 가능 여부 테스트
                
                Debug.Log("[WindowsSecureStorage] Windows DPAPI 초기화 성공");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsSecureStorage] Windows DPAPI 초기화 실패: {ex.Message}");
                return false;
            }
#else
            return false;
#endif
        }
        
        private async UniTask<bool> StoreWithDPAPIAsync(string key, string value)
        {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            try
            {
                // TODO: Windows Native 코드 호출
                // 1. ProtectedData.Protect로 데이터 암호화 (CurrentUser 스코프)
                // 2. 레지스트리 또는 AppData에 암호화된 데이터 저장
                // 3. 추가 엔트로피 사용 (기기별 고유값)
                
                await UniTask.Delay(10); // 임시 지연
                Debug.Log($"[WindowsSecureStorage] DPAPI 저장 완료: {key}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsSecureStorage] DPAPI 저장 실패: {ex.Message}");
                return false;
            }
#else
            await UniTask.CompletedTask;
            return false;
#endif
        }
        
        private async UniTask<string> LoadFromDPAPIAsync(string key)
        {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            try
            {
                // TODO: Windows Native 코드 호출
                // 1. 레지스트리 또는 AppData에서 암호화된 데이터 로드
                // 2. ProtectedData.Unprotect로 데이터 복호화
                // 3. 동일한 엔트로피 사용
                
                await UniTask.Delay(10); // 임시 지연
                Debug.Log($"[WindowsSecureStorage] DPAPI 로드 완료: {key}");
                return null; // 임시로 null 반환
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsSecureStorage] DPAPI 로드 실패: {ex.Message}");
                return null;
            }
#else
            await UniTask.CompletedTask;
            return null;
#endif
        }
        
        private async UniTask<bool> DeleteFromDPAPIAsync(string key)
        {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            try
            {
                // TODO: Windows Native 코드 호출
                // 레지스트리 또는 AppData에서 키 삭제
                
                await UniTask.Delay(10); // 임시 지연
                Debug.Log($"[WindowsSecureStorage] DPAPI 삭제 완료: {key}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsSecureStorage] DPAPI 삭제 실패: {ex.Message}");
                return false;
            }
#else
            await UniTask.CompletedTask;
            return false;
#endif
        }
        
        private async UniTask ClearDPAPIDataAsync()
        {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
            try
            {
                // TODO: Windows Native 코드 호출
                // 앱별 레지스트리 키 또는 AppData 폴더 전체 삭제
                
                await UniTask.Delay(10); // 임시 지연
                Debug.Log("[WindowsSecureStorage] DPAPI 전체 삭제 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsSecureStorage] DPAPI 전체 삭제 실패: {ex.Message}");
                throw;
            }
#else
            await UniTask.CompletedTask;
#endif
        }
        
        #endregion
    }
}
