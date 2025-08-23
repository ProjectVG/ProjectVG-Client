using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Auth.Storage;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.Core
{
    public class TokenStorage : ITokenStorage
    {
        private const string REFRESH_TOKEN_KEY = "auth_refresh_token";
        private const string ACCESS_TOKEN_MEMORY_KEY = "memory_access_token";
        
        private readonly ISecureStorage _secureStorage;
        private AccessToken _memoryAccessToken;
        
        public bool HasValidAccessToken => _memoryAccessToken != null && !_memoryAccessToken.IsExpired;
        public bool HasStoredRefreshToken { get; private set; }
        
        public TokenStorage()
        {
            _secureStorage = SecureStorageFactory.CreatePlatformStorage();
            
            if (!_secureStorage.IsAvailable)
            {
                Debug.LogWarning($"[TokenStorage] {_secureStorage.PlatformName} 보안 저장소를 사용할 수 없습니다.");
            }
            else
            {
                Debug.Log($"[TokenStorage] {_secureStorage.PlatformName} 보안 저장소 초기화 완료");
            }
        }
        
        #region Refresh Token Methods
        
        public async UniTask StoreRefreshTokenAsync(RefreshToken token)
        {
            if (token == null)
            {
                Debug.LogWarning("[TokenStorage] null 토큰을 저장하려고 시도했습니다.");
                return;
            }
            
            try
            {
                var json = JsonConvert.SerializeObject(token);
                var success = await _secureStorage.StoreAsync(REFRESH_TOKEN_KEY, json);
                
                if (success)
                {
                    HasStoredRefreshToken = true;
                    Debug.Log("[TokenStorage] Refresh 토큰 저장 완료");
                }
                else
                {
                    Debug.LogError("[TokenStorage] Refresh 토큰 저장 실패");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenStorage] Refresh 토큰 저장 중 오류: {ex.Message}");
                throw;
            }
        }
        
        public async UniTask<RefreshToken> LoadRefreshTokenAsync()
        {
            try
            {
                var json = await _secureStorage.LoadAsync(REFRESH_TOKEN_KEY);
                
                if (string.IsNullOrEmpty(json))
                {
                    HasStoredRefreshToken = false;
                    return null;
                }
                
                var token = JsonConvert.DeserializeObject<RefreshToken>(json);
                HasStoredRefreshToken = token != null && !token.IsExpired;
                
                if (HasStoredRefreshToken)
                {
                    Debug.Log("[TokenStorage] Refresh 토큰 로드 완료");
                }
                else
                {
                    Debug.Log("[TokenStorage] 저장된 Refresh 토큰이 만료되었습니다.");
                    await ClearRefreshTokenAsync();
                }
                
                return HasStoredRefreshToken ? token : null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenStorage] Refresh 토큰 로드 중 오류: {ex.Message}");
                HasStoredRefreshToken = false;
                return null;
            }
        }
        
        public async UniTask ClearRefreshTokenAsync()
        {
            try
            {
                var success = await _secureStorage.DeleteAsync(REFRESH_TOKEN_KEY);
                HasStoredRefreshToken = false;
                
                if (success)
                {
                    Debug.Log("[TokenStorage] Refresh 토큰 삭제 완료");
                }
                else
                {
                    Debug.LogWarning("[TokenStorage] Refresh 토큰 삭제 실패 (토큰이 존재하지 않을 수 있음)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenStorage] Refresh 토큰 삭제 중 오류: {ex.Message}");
                HasStoredRefreshToken = false;
            }
        }
        
        #endregion
        
        #region Access Token Methods
        
        public void StoreAccessTokenInMemory(AccessToken token)
        {
            if (token == null)
            {
                Debug.LogWarning("[TokenStorage] null Access 토큰을 저장하려고 시도했습니다.");
                return;
            }
            
            _memoryAccessToken = token;
            Debug.Log($"[TokenStorage] Access 토큰 메모리 저장 완료 (만료: {token.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC)");
        }
        
        public AccessToken GetAccessTokenFromMemory()
        {
            if (_memoryAccessToken == null)
            {
                return null;
            }
            
            if (_memoryAccessToken.IsExpired)
            {
                Debug.Log("[TokenStorage] 메모리의 Access 토큰이 만료되었습니다.");
                ClearAccessTokenFromMemory();
                return null;
            }
            
            return _memoryAccessToken;
        }
        
        public void ClearAccessTokenFromMemory()
        {
            if (_memoryAccessToken != null)
            {
                _memoryAccessToken = null;
                Debug.Log("[TokenStorage] Access 토큰 메모리에서 삭제 완료");
            }
        }
        
        #endregion
    }
}
