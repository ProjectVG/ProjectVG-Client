using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Auth.OAuth2;
using ProjectVG.Infrastructure.Auth.Storage;

namespace ProjectVG.Infrastructure.Auth.Core
{
    public class AuthManager : Singleton<AuthManager>, IAuthManager
    {
        [Header("Authentication Configuration")]
        
        private ITokenStorage _tokenStorage;
        private IOAuth2Client _oauth2Client;
        private IRefreshScheduler _refreshScheduler;
        private AuthState _currentAuthState;
        
        public bool IsAuthenticated => _currentAuthState?.IsAuthenticated ?? false;
        public bool IsInitialized { get; private set; }
        
        public event Action<bool> OnAuthStateChanged;
        public event Action<string> OnAuthError;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion
        
        #region Public Methods
        
        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;
            
            try
            {
                InitializeDependencies();
                await RestoreAuthenticationStateAsync();
                IsInitialized = true;
                
                Debug.Log("[AuthManager] 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 초기화 실패: {ex.Message}");
                OnAuthError?.Invoke($"인증 초기화 실패: {ex.Message}");
                throw;
            }
        }
        
        public async UniTask<bool> LoginAsync()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[AuthManager] 초기화되지 않았습니다.");
                return false;
            }
            
            try
            {
                return await _oauth2Client.StartAuthorizationAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 로그인 실패: {ex.Message}");
                OnAuthError?.Invoke($"로그인 실패: {ex.Message}");
                return false;
            }
        }
        
        public async UniTask LogoutAsync()
        {
            try
            {
                await ClearAllTokensAsync();
                _currentAuthState?.MarkAsUnauthenticated();
                OnAuthStateChanged?.Invoke(false);
                
                Debug.Log("[AuthManager] 로그아웃 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 로그아웃 실패: {ex.Message}");
                OnAuthError?.Invoke($"로그아웃 실패: {ex.Message}");
            }
        }
        
        public async UniTask<string> GetValidAccessTokenAsync()
        {
            if (!IsAuthenticated) return null;
            
            var accessToken = _tokenStorage.GetAccessTokenFromMemory();
            if (accessToken != null && !accessToken.IsExpiringSoon)
            {
                return accessToken.Token;
            }
            
            if (await RefreshTokenAsync())
            {
                accessToken = _tokenStorage.GetAccessTokenFromMemory();
                return accessToken?.Token;
            }
            
            return null;
        }
        
        public async UniTask<bool> RefreshTokenAsync()
        {
            try
            {
                var refreshToken = await _tokenStorage.LoadRefreshTokenAsync();
                if (refreshToken == null || refreshToken.IsExpired)
                {
                    await LogoutAsync();
                    return false;
                }
                
                var tokenSet = await _oauth2Client.RefreshTokenAsync(refreshToken);
                if (tokenSet == null)
                {
                    await LogoutAsync();
                    return false;
                }
                
                await StoreTokenSetAsync(tokenSet);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] 토큰 갱신 실패: {ex.Message}");
                await LogoutAsync();
                return false;
            }
        }
        
        public void ClearAuthState()
        {
            _currentAuthState?.MarkAsUnauthenticated();
            _tokenStorage?.ClearAccessTokenFromMemory();
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeDependencies()
        {
            _tokenStorage = new TokenStorage();
            _oauth2Client = new OAuth2Client();
            _refreshScheduler = new RefreshScheduler();
            _currentAuthState = new AuthState();
            
            _refreshScheduler.OnRefreshRequired += async () => await RefreshTokenAsync();
            _refreshScheduler.OnRefreshFailed += (ex) => OnAuthError?.Invoke($"자동 갱신 실패: {ex.Message}");
        }
        
        private async UniTask RestoreAuthenticationStateAsync()
        {
            try
            {
                var refreshToken = await _tokenStorage.LoadRefreshTokenAsync();
                if (refreshToken != null && !refreshToken.IsExpired)
                {
                    if (await RefreshTokenAsync())
                    {
                        _currentAuthState.MarkAsAuthenticated("restored_user");
                        OnAuthStateChanged?.Invoke(true);
                        
                        var accessToken = _tokenStorage.GetAccessTokenFromMemory();
                        if (accessToken != null)
                        {
                            _refreshScheduler.ScheduleRefresh(accessToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AuthManager] 인증 상태 복원 실패: {ex.Message}");
            }
        }
        
        private async UniTask StoreTokenSetAsync(TokenSet tokenSet)
        {
            if (tokenSet?.AccessToken != null)
            {
                _tokenStorage.StoreAccessTokenInMemory(tokenSet.AccessToken);
                _refreshScheduler.ScheduleRefresh(tokenSet.AccessToken);
            }
            
            if (tokenSet?.RefreshToken != null)
            {
                await _tokenStorage.StoreRefreshTokenAsync(tokenSet.RefreshToken);
            }
        }
        
        private async UniTask ClearAllTokensAsync()
        {
            _tokenStorage.ClearAccessTokenFromMemory();
            await _tokenStorage.ClearRefreshTokenAsync();
            _refreshScheduler.CancelRefresh();
        }
        
        private void Shutdown()
        {
            _refreshScheduler?.CancelRefresh();
            IsInitialized = false;
        }
        
        #endregion
    }
}
