using UnityEngine;
using ProjectVG.Infrastructure.Auth.Services;
using ProjectVG.Infrastructure.Auth.Utils;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Auth.Examples
{
    /// <summary>
    /// Guest 로그인 사용 예제
    /// </summary>
    public class GuestLoginExample : MonoBehaviour
    {
        [Header("Debug UI")]
        public bool showDebugUI = true;
        
        private GuestAuthService _guestAuthService;
        private TokenManager _tokenManager;
        private bool _isLoggingIn = false;

        private void Start()
        {
            InitializeServices();
            SetupEventHandlers();
            
            // 앱 시작 시 자동 게스트 로그인 시도
            CheckAutoGuestLogin();
        }

        private void InitializeServices()
        {
            _guestAuthService = GuestAuthService.Instance;
            _tokenManager = TokenManager.Instance;
            
            Debug.Log("[GuestLoginExample] 서비스 초기화 완료");
        }

        private void SetupEventHandlers()
        {
            // Guest 로그인 이벤트 구독
            _guestAuthService.OnGuestLoginSuccess += HandleGuestLoginSuccess;
            _guestAuthService.OnGuestLoginFailed += HandleGuestLoginFailed;
            
            // Token 이벤트 구독
            _tokenManager.OnTokensUpdated += HandleTokensUpdated;
            _tokenManager.OnTokensExpired += HandleTokensExpired;
        }

        private async void CheckAutoGuestLogin()
        {
            // 이미 유효한 토큰이 있으면 자동 로그인 스킵
            if (_tokenManager.HasValidTokens)
            {
                Debug.Log("[GuestLoginExample] 유효한 토큰 존재 - 자동 로그인 스킵");
                return;
            }

            // RefreshToken이 있으면 자동 갱신 대기
            if (_tokenManager.HasRefreshToken && !_tokenManager.IsRefreshTokenExpired())
            {
                Debug.Log("[GuestLoginExample] RefreshToken 존재 - 자동 갱신 대기");
                return;
            }

            // Guest 로그인 가능하면 자동 로그인 시도
            if (_guestAuthService.CanLoginAsGuest())
            {
                Debug.Log("[GuestLoginExample] 자동 Guest 로그인 시도");
                await PerformGuestLoginAsync();
            }
        }

        /// <summary>
        /// Guest 로그인 수행
        /// </summary>
        public async void PerformGuestLogin()
        {
            await PerformGuestLoginAsync();
        }

        private async System.Threading.Tasks.Task PerformGuestLoginAsync()
        {
            if (_isLoggingIn)
            {
                Debug.LogWarning("[GuestLoginExample] 이미 로그인 진행 중입니다.");
                return;
            }

            _isLoggingIn = true;

            try
            {
                Debug.Log("[GuestLoginExample] Guest 로그인 시작");
                
                // 디바이스 정보 출력
                Debug.Log($"[GuestLoginExample] 디바이스 정보: {DeviceIdProvider.GetPlatformInfo()}");
                Debug.Log($"[GuestLoginExample] 디바이스 ID: {MaskString(_guestAuthService.GetCurrentDeviceId())}");

                // Guest 로그인 수행
                bool success = await _guestAuthService.LoginAsGuestAsync();

                if (success)
                {
                    Debug.Log("[GuestLoginExample] Guest 로그인 성공");
                }
                else
                {
                    Debug.LogError("[GuestLoginExample] Guest 로그인 실패");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GuestLoginExample] Guest 로그인 중 오류: {ex.Message}");
            }
            finally
            {
                _isLoggingIn = false;
            }
        }


        /// <summary>
        /// Guest 로그인 상태 확인
        /// </summary>
        public void CheckGuestLoginStatus()
        {
            var status = _guestAuthService.GetGuestLoginStatus();
            Debug.Log("=== Guest Login Status ===");
            Debug.Log(status.GetDebugInfo());
            Debug.Log("==========================");
        }

        /// <summary>
        /// 모든 토큰 삭제 (테스트용)
        /// </summary>
        public void ClearAllTokens()
        {
            _tokenManager.ClearTokens();
            Debug.Log("[GuestLoginExample] 모든 토큰 삭제 완료");
        }

        /// <summary>
        /// 디바이스 ID 리셋 (테스트용)
        /// </summary>
        public void ResetDeviceId()
        {
            _guestAuthService.ResetDeviceId();
            Debug.Log("[GuestLoginExample] 디바이스 ID 리셋 완료");
        }

        #region Event Handlers

        private void HandleGuestLoginSuccess(TokenSet tokenSet)
        {
            Debug.Log("[GuestLoginExample] Guest 로그인 성공 이벤트 수신");
            Debug.Log($"[GuestLoginExample] AccessToken 만료: {tokenSet.AccessToken.ExpiresAt}");
        }

        private void HandleGuestLoginFailed(string error)
        {
            Debug.LogError($"[GuestLoginExample] Guest 로그인 실패 이벤트 수신: {error}");
        }

        private void HandleTokensUpdated(TokenSet tokenSet)
        {
            Debug.Log("[GuestLoginExample] 토큰 업데이트 이벤트 수신");
        }

        private void HandleTokensExpired()
        {
            Debug.Log("[GuestLoginExample] 토큰 만료 이벤트 수신 - 자동 갱신 시도");
        }

        #endregion

        #region Debug UI

        private void OnGUI()
        {
            if (!showDebugUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 600));
            GUILayout.Label("=== Guest Login Debug UI ===", GUI.skin.box);

            // 로그인 버튼
            GUI.enabled = !_isLoggingIn && _guestAuthService.CanLoginAsGuest();
            if (GUILayout.Button(_isLoggingIn ? "로그인 중..." : "Guest 로그인"))
            {
                PerformGuestLogin();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            if (GUILayout.Button("Guest 로그인 상태 확인"))
            {
                CheckGuestLoginStatus();
            }

            GUILayout.Space(10);

            // 테스트 버튼들
            GUILayout.Label("=== 테스트 기능 ===", GUI.skin.box);
            
            if (GUILayout.Button("모든 토큰 삭제"))
            {
                ClearAllTokens();
            }

            if (GUILayout.Button("디바이스 ID 리셋"))
            {
                ResetDeviceId();
            }

            GUILayout.Space(10);

            // 현재 상태 표시
            GUILayout.Label("=== 현재 상태 ===", GUI.skin.box);
            GUILayout.Label($"로그인 중: {_isLoggingIn}");
            GUILayout.Label($"유효한 토큰: {_tokenManager?.HasValidTokens ?? false}");
            GUILayout.Label($"RefreshToken: {_tokenManager?.HasRefreshToken ?? false}");
            GUILayout.Label($"Guest 로그인 가능: {_guestAuthService?.CanLoginAsGuest() ?? false}");

            GUILayout.EndArea();
        }

        #endregion

        private string MaskString(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 8)
                return "***";
            return $"{input.Substring(0, 4)}****{input.Substring(input.Length - 4)}";
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_guestAuthService != null)
            {
                _guestAuthService.OnGuestLoginSuccess -= HandleGuestLoginSuccess;
                _guestAuthService.OnGuestLoginFailed -= HandleGuestLoginFailed;
            }

            if (_tokenManager != null)
            {
                _tokenManager.OnTokensUpdated -= HandleTokensUpdated;
                _tokenManager.OnTokensExpired -= HandleTokensExpired;
            }
        }
    }
}