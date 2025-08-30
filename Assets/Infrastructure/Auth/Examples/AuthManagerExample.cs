using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Auth.Examples
{
    /// <summary>
    /// AuthManager 사용 예제
    /// UI 버튼을 통해 다양한 인증 기능을 테스트할 수 있습니다.
    /// </summary>
    public class AuthManagerExample : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button guestLoginButton;
        [SerializeField] private Button oauth2LoginButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button refreshTokenButton;
        [SerializeField] private Button checkStatusButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text debugInfoText;

        private AuthManager _authManager;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeUI();
            SetupAuthManager();
        }

        private void OnDestroy()
        {
            UnsubscribeFromAuthEvents();
        }

        #endregion

        #region UI Setup

        private void InitializeUI()
        {
            // 버튼 이벤트 연결
            if (guestLoginButton != null)
                guestLoginButton.onClick.AddListener(() => OnGuestLoginClicked().Forget());

            if (oauth2LoginButton != null)
                oauth2LoginButton.onClick.AddListener(() => OnOAuth2LoginClicked().Forget());

            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);

            if (refreshTokenButton != null)
                refreshTokenButton.onClick.AddListener(() => OnRefreshTokenClicked().Forget());

            if (checkStatusButton != null)
                checkStatusButton.onClick.AddListener(OnCheckStatusClicked);

            UpdateStatusText("AuthManager 초기화 중...");
        }

        #endregion

        #region AuthManager Setup

        private void SetupAuthManager()
        {
            try
            {
                _authManager = AuthManager.Instance;
                
                // AuthManager 이벤트 구독
                _authManager.OnLoginSuccess += HandleLoginSuccess;
                _authManager.OnLoginFailed += HandleLoginFailed;
                _authManager.OnLoggedOut += HandleLoggedOut;
                _authManager.OnTokenAutoRefreshed += HandleTokenAutoRefreshed;
                _authManager.OnReLoginRequired += HandleReLoginRequired;

                Debug.Log("[AuthManagerExample] AuthManager 설정 완료");
                UpdateStatusText("AuthManager 준비 완료");
                
                // 초기 상태 업데이트
                UpdateUI();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManagerExample] AuthManager 설정 실패: {ex.Message}");
                UpdateStatusText($"초기화 실패: {ex.Message}");
            }
        }

        private void UnsubscribeFromAuthEvents()
        {
            if (_authManager != null)
            {
                _authManager.OnLoginSuccess -= HandleLoginSuccess;
                _authManager.OnLoginFailed -= HandleLoginFailed;
                _authManager.OnLoggedOut -= HandleLoggedOut;
                _authManager.OnTokenAutoRefreshed -= HandleTokenAutoRefreshed;
                _authManager.OnReLoginRequired -= HandleReLoginRequired;
            }
        }

        #endregion

        #region Button Event Handlers

        private async UniTaskVoid OnGuestLoginClicked()
        {
            try
            {
                UpdateStatusText("Guest 로그인 시도 중...");
                SetButtonsEnabled(false);

                bool success = await _authManager.LoginAsGuestAsync();
                
                if (!success)
                {
                    UpdateStatusText("Guest 로그인 실패");
                }
                // 성공 시에는 HandleLoginSuccess 이벤트에서 처리
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManagerExample] Guest 로그인 오류: {ex.Message}");
                UpdateStatusText($"Guest 로그인 오류: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async UniTaskVoid OnOAuth2LoginClicked()
        {
            try
            {
                UpdateStatusText("OAuth2 로그인 시도 중...\n브라우저에서 Google 로그인을 완료해주세요.");
                SetButtonsEnabled(false);

                bool success = await _authManager.LoginWithOAuth2Async();
                
                if (!success)
                {
                    UpdateStatusText("OAuth2 로그인 실패");
                }
                // 성공 시에는 HandleLoginSuccess 이벤트에서 처리
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManagerExample] OAuth2 로그인 오류: {ex.Message}");
                UpdateStatusText($"OAuth2 로그인 오류: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void OnLogoutClicked()
        {
            try
            {
                UpdateStatusText("로그아웃 중...");
                _authManager.Logout();
                // 로그아웃 완료는 HandleLoggedOut 이벤트에서 처리
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManagerExample] 로그아웃 오류: {ex.Message}");
                UpdateStatusText($"로그아웃 오류: {ex.Message}");
            }
        }

        private async UniTaskVoid OnRefreshTokenClicked()
        {
            try
            {
                UpdateStatusText("토큰 갱신 중...");
                SetButtonsEnabled(false);

                bool success = await _authManager.RefreshTokenAsync();
                
                if (success)
                {
                    UpdateStatusText("토큰 갱신 성공");
                }
                else
                {
                    UpdateStatusText("토큰 갱신 실패");
                }
                
                UpdateUI();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManagerExample] 토큰 갱신 오류: {ex.Message}");
                UpdateStatusText($"토큰 갱신 오류: {ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void OnCheckStatusClicked()
        {
            UpdateUI();
            UpdateStatusText("상태 정보를 업데이트했습니다.");
        }

        #endregion

        #region AuthManager Event Handlers

        private void HandleLoginSuccess(TokenSet tokenSet)
        {
            Debug.Log("[AuthManagerExample] 로그인 성공 이벤트 수신");
            UpdateStatusText($"로그인 성공!\n사용자 ID: {_authManager.CurrentUserId}");
            UpdateUI();
            SetButtonsEnabled(true);
        }

        private void HandleLoginFailed(string error)
        {
            Debug.LogError($"[AuthManagerExample] 로그인 실패 이벤트 수신: {error}");
            UpdateStatusText($"로그인 실패: {error}");
            UpdateUI();
            SetButtonsEnabled(true);
        }

        private void HandleLoggedOut()
        {
            Debug.Log("[AuthManagerExample] 로그아웃 이벤트 수신");
            UpdateStatusText("로그아웃 완료");
            UpdateUI();
        }

        private void HandleTokenAutoRefreshed(string newAccessToken)
        {
            Debug.Log("[AuthManagerExample] 토큰 자동 갱신 이벤트 수신");
            UpdateStatusText("토큰이 자동으로 갱신되었습니다.");
            UpdateUI();
        }

        private void HandleReLoginRequired(string reason)
        {
            Debug.LogWarning($"[AuthManagerExample] 재로그인 필요 이벤트 수신: {reason}");
            UpdateStatusText($"재로그인이 필요합니다.\n이유: {reason}");
            UpdateUI();
        }

        #endregion

        #region UI Update Methods

        private void UpdateUI()
        {
            if (_authManager == null) return;

            // 버튼 활성화 상태 업데이트
            bool isLoggedIn = _authManager.IsLoggedIn;
            bool hasValidRefreshToken = _authManager.HasValidRefreshToken;

            if (guestLoginButton != null)
                guestLoginButton.interactable = !isLoggedIn && _authManager.CanLoginAsGuest();

            if (oauth2LoginButton != null)
                oauth2LoginButton.interactable = !isLoggedIn;

            if (logoutButton != null)
                logoutButton.interactable = isLoggedIn;

            if (refreshTokenButton != null)
                refreshTokenButton.interactable = hasValidRefreshToken;

            // 디버그 정보 업데이트
            if (debugInfoText != null)
            {
                debugInfoText.text = _authManager.GetDebugInfo();
            }
        }

        private void UpdateStatusText(string status)
        {
            if (statusText != null)
            {
                statusText.text = $"[{DateTime.Now:HH:mm:ss}] {status}";
            }
            Debug.Log($"[AuthManagerExample] Status: {status}");
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (guestLoginButton != null)
                guestLoginButton.interactable = enabled && !_authManager.IsLoggedIn && _authManager.CanLoginAsGuest();

            if (oauth2LoginButton != null)
                oauth2LoginButton.interactable = enabled && !_authManager.IsLoggedIn;

            if (logoutButton != null)
                logoutButton.interactable = enabled && _authManager.IsLoggedIn;

            if (refreshTokenButton != null)
                refreshTokenButton.interactable = enabled && _authManager.HasValidRefreshToken;

            if (checkStatusButton != null)
                checkStatusButton.interactable = enabled;
        }

        #endregion

        #region Test Methods (Unity Inspector에서 호출 가능)

        [ContextMenu("Test Guest Login")]
        public void TestGuestLogin()
        {
            OnGuestLoginClicked().Forget();
        }

        [ContextMenu("Test OAuth2 Login")]
        public void TestOAuth2Login()
        {
            OnOAuth2LoginClicked().Forget();
        }

        [ContextMenu("Test Logout")]
        public void TestLogout()
        {
            OnLogoutClicked();
        }

        [ContextMenu("Test Token Refresh")]
        public void TestTokenRefresh()
        {
            OnRefreshTokenClicked().Forget();
        }

        [ContextMenu("Show Debug Info")]
        public void ShowDebugInfo()
        {
            if (_authManager != null)
            {
                Debug.Log(_authManager.GetDebugInfo());
            }
        }

        [ContextMenu("Test Auto Login Simulation")]
        public async void TestAutoLoginSimulation()
        {
            try
            {
                Debug.Log("[AuthManagerExample] 자동 로그인 시뮬레이션 시작");
                
                // 현재 토큰 상태 확인
                if (_authManager.IsLoggedIn)
                {
                    Debug.Log("[AuthManagerExample] 이미 로그인된 상태입니다.");
                    return;
                }

                // Refresh Token으로 자동 로그인 시도
                if (_authManager.HasValidRefreshToken)
                {
                    UpdateStatusText("자동 로그인 시뮬레이션 중...");
                    bool success = await _authManager.RefreshTokenAsync();
                    
                    if (success)
                    {
                        UpdateStatusText("자동 로그인 시뮬레이션 성공");
                        Debug.Log("[AuthManagerExample] 자동 로그인 시뮬레이션 성공");
                    }
                    else
                    {
                        UpdateStatusText("자동 로그인 시뮬레이션 실패 - 재로그인 필요");
                        Debug.Log("[AuthManagerExample] 자동 로그인 시뮬레이션 실패");
                    }
                }
                else
                {
                    UpdateStatusText("유효한 Refresh Token이 없어 자동 로그인 불가");
                    Debug.Log("[AuthManagerExample] 유효한 Refresh Token이 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManagerExample] 자동 로그인 시뮬레이션 오류: {ex.Message}");
                UpdateStatusText($"자동 로그인 시뮬레이션 오류: {ex.Message}");
            }
            finally
            {
                UpdateUI();
            }
        }

        #endregion
    }
}