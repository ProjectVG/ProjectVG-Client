using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.OAuth2;
using ProjectVG.Infrastructure.Auth.OAuth2.Config;
using ProjectVG.Infrastructure.Auth.Models;
using System;

namespace ProjectVG.Infrastructure.Auth.Examples
{
    /// <summary>
    /// 서버 OAuth2 로그인 예제
    /// </summary>
    public class ServerOAuth2Example : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button checkButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI userInfoText;
        [SerializeField] private TextMeshProUGUI debugText;
        
        [Header("설정")]
        [SerializeField] private ServerOAuth2Config oauth2Config;
        
        // OAuth2 설정 자동 로드
        private ServerOAuth2Config OAuth2Config => oauth2Config ?? ServerOAuth2Config.Instance;
        
        private ServerOAuth2Provider _oauth2Provider;
        private TokenManager _tokenManager;
        private TokenRefreshService _refreshService;
        private TokenSet _currentTokenSet;
        private bool _isLoggedIn = false;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeOAuth2Provider();
            InitializeTokenServices();
            SetupEventHandlers();
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            // 이벤트 핸들러 해제
            if (loginButton != null) loginButton.onClick.RemoveAllListeners();
            if (logoutButton != null) logoutButton.onClick.RemoveAllListeners();
            if (refreshButton != null) refreshButton.onClick.RemoveAllListeners();
            if (clearButton != null) clearButton.onClick.RemoveAllListeners();
            if (checkButton != null) checkButton.onClick.RemoveAllListeners();
            
            // Token 서비스 이벤트 해제
            if (_tokenManager != null)
            {
                _tokenManager.OnTokensUpdated -= OnTokensUpdated;
                _tokenManager.OnTokensExpired -= OnTokensExpired;
                _tokenManager.OnTokensCleared -= OnTokensCleared;
            }
            
            if (_refreshService != null)
            {
                _refreshService.OnTokenRefreshed -= OnTokenRefreshed;
                _refreshService.OnTokenRefreshFailed -= OnTokenRefreshFailed;
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeOAuth2Provider()
        {
            var config = OAuth2Config;
            
            if (config == null)
            {
                Debug.LogError("[ServerOAuth2Example] OAuth2 설정을 로드할 수 없습니다.");
                ShowStatus("OAuth2 설정을 로드할 수 없습니다.", Color.red);
                return;
            }
            
            // HttpApiClient 초기화 상태 확인
            var httpClient = ProjectVG.Infrastructure.Network.Http.HttpApiClient.Instance;
            if (httpClient == null)
            {
                Debug.LogError("[ServerOAuth2Example] HttpApiClient.Instance가 null입니다.");
                ShowStatus("HttpApiClient가 초기화되지 않았습니다.", Color.red);
                return;
            }
            
            Debug.Log($"[ServerOAuth2Example] HttpApiClient 초기화 상태: {httpClient.IsInitialized}");
            
            if (!httpClient.IsInitialized)
            {
                Debug.LogError("[ServerOAuth2Example] HttpApiClient가 초기화되지 않았습니다.");
                ShowStatus("HttpApiClient가 초기화되지 않았습니다.", Color.red);
                return;
            }
            
            _oauth2Provider = new ServerOAuth2Provider(config);
            
            if (!_oauth2Provider.IsConfigured)
            {
                Debug.LogError("[ServerOAuth2Example] OAuth2 설정이 유효하지 않습니다.");
                ShowStatus("OAuth2 설정이 유효하지 않습니다.", Color.red);
                return;
            }
            
            Debug.Log("[ServerOAuth2Example] OAuth2 Provider 초기화 완료");
            Debug.Log(_oauth2Provider.GetDebugInfo());
        }
        
        private void InitializeTokenServices()
        {
            _tokenManager = TokenManager.Instance;
            _refreshService = TokenRefreshService.Instance;
            
            // 이벤트 구독
            _tokenManager.OnTokensUpdated += OnTokensUpdated;
            _tokenManager.OnTokensExpired += OnTokensExpired;
            _tokenManager.OnTokensCleared += OnTokensCleared;
            _refreshService.OnTokenRefreshed += OnTokenRefreshed;
            _refreshService.OnTokenRefreshFailed += OnTokenRefreshFailed;
            
            Debug.Log("[ServerOAuth2Example] Token 서비스 초기화 완료");
        }
        
        private void SetupEventHandlers()
        {
            if (loginButton != null)
                loginButton.onClick.AddListener(() => _ = OnLoginButtonClicked());
            
            if (logoutButton != null)
                logoutButton.onClick.AddListener(() => _ = OnLogoutButtonClicked());
            
            if (refreshButton != null)
                refreshButton.onClick.AddListener(() => _ = OnRefreshButtonClicked());
            
            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearButtonClicked);
            
            if (checkButton != null)
                checkButton.onClick.AddListener(() => _ = OnCheckButtonClicked());
        }
        
        #endregion
        
        #region UI Event Handlers
        
        private async UniTaskVoid OnLoginButtonClicked()
        {
            if (_isLoggedIn)
            {
                ShowStatus("이미 로그인되어 있습니다.", Color.yellow);
                return;
            }
            
            if (_oauth2Provider == null)
            {
                ShowStatus("OAuth2 Provider가 초기화되지 않았습니다.", Color.red);
                return;
            }
            
            try
            {
                ShowStatus("서버 OAuth2 로그인 시작...", Color.blue);
                SetButtonsEnabled(false);
                
                // 전체 OAuth2 로그인 플로우 실행
                _currentTokenSet = await _oauth2Provider.LoginWithServerOAuth2Async();
                
                if (_currentTokenSet?.HasRefreshToken() == true)
                {
                    _isLoggedIn = true;
                    ShowStatus("서버 OAuth2 로그인 성공!", Color.green);
                    
                    // 상세 토큰 정보 로그
                    LogDetailedTokenInfo();
                    
                    // 토큰 정보 표시
                    DisplayTokenInfo();
                    
                    // 자동 토큰 갱신 시작
                    _ = StartAutoTokenRefresh();
                }
                else
                {
                    ShowStatus("로그인 실패: 유효하지 않은 토큰", Color.red);
                }
            }
            catch (System.Exception ex)
            {
                ShowStatus($"로그인 실패: {ex.Message}", Color.red);
                Debug.LogError($"[ServerOAuth2Example] 로그인 오류: {ex}");
            }
            finally
            {
                SetButtonsEnabled(true);
                UpdateUI();
            }
        }
        
        private async UniTaskVoid OnLogoutButtonClicked()
        {
            if (!_isLoggedIn)
            {
                ShowStatus("로그인되어 있지 않습니다.", Color.yellow);
                return;
            }
            
            try
            {
                ShowStatus("로그아웃 중...", Color.blue);
                
                // TokenManager에서 토큰 정리
                _tokenManager.ClearTokens();
                
                // 토큰 정리
                _currentTokenSet?.Clear();
                _currentTokenSet = null;
                _isLoggedIn = false;
                
                // UI 초기화
                ClearUserInfo();
                ShowStatus("로그아웃 완료", Color.green);
            }
            catch (System.Exception ex)
            {
                ShowStatus($"로그아웃 실패: {ex.Message}", Color.red);
                Debug.LogError($"[ServerOAuth2Example] 로그아웃 오류: {ex}");
            }
            finally
            {
                UpdateUI();
            }
        }
        
        #endregion
        
        #region Token Management
        
        private async UniTaskVoid StartAutoTokenRefresh()
        {
            while (_isLoggedIn && _currentTokenSet?.HasRefreshToken() == true)
            {
                try
                {
                    // 토큰 갱신이 필요한지 확인
                    if (_currentTokenSet.NeedsRefresh())
                    {
                        Debug.Log("[ServerOAuth2Example] 토큰 갱신 시작");
                        
                         // 현재는 전체 재로그인 플로우 실행
                         var newTokenSet = await _oauth2Provider.LoginWithServerOAuth2Async();
                        
                        if (newTokenSet?.HasRefreshToken() == true)
                        {
                            _currentTokenSet = newTokenSet;
                            Debug.Log("[ServerOAuth2Example] 토큰 갱신 성공");
                            DisplayTokenInfo();
                        }
                        else
                        {
                            Debug.LogWarning("[ServerOAuth2Example] 토큰 갱신 실패");
                            break;
                        }
                    }
                    
                    // 1분 대기
                    await UniTask.Delay(60000);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ServerOAuth2Example] 토큰 갱신 오류: {ex.Message}");
                    break;
                }
            }
        }
        
        #endregion
        
        #region Token Management Buttons
        
        private async UniTaskVoid OnRefreshButtonClicked()
        {
            try
            {
                ShowStatus("토큰 갱신 시작...", Color.yellow);
                
                var success = await _refreshService.ForceRefreshAsync();
                
                if (success)
                {
                    ShowStatus("토큰 갱신 성공!", Color.green);
                    // 갱신된 토큰으로 현재 토큰 세트 업데이트
                    _currentTokenSet = _tokenManager.LoadTokens();
                }
                else
                {
                    ShowStatus("토큰 갱신 실패", Color.red);
                }
                
                UpdateUI();
            }
            catch (System.Exception ex)
            {
                ShowStatus($"토큰 갱신 오류: {ex.Message}", Color.red);
                Debug.LogError($"[ServerOAuth2Example] 토큰 갱신 실패: {ex.Message}");
            }
        }
        
        private void OnClearButtonClicked()
        {
            try
            {
                _tokenManager.ClearTokens();
                _currentTokenSet = null;
                _isLoggedIn = false;
                ShowStatus("토큰 삭제 완료", Color.blue);
                UpdateUI();
            }
            catch (System.Exception ex)
            {
                ShowStatus($"토큰 삭제 오류: {ex.Message}", Color.red);
                Debug.LogError($"[ServerOAuth2Example] 토큰 삭제 실패: {ex.Message}");
            }
        }
        
        private async UniTaskVoid OnCheckButtonClicked()
        {
            try
            {
                ShowStatus("토큰 상태 확인 중...", Color.yellow);
                
                var hasValidToken = await _refreshService.EnsureValidTokenAsync();
                
                if (hasValidToken)
                {
                    ShowStatus("유효한 토큰이 있습니다.", Color.green);
                    // 유효한 토큰으로 현재 토큰 세트 업데이트
                    _currentTokenSet = _tokenManager.LoadTokens();
                }
                else
                {
                    ShowStatus("유효한 토큰이 없습니다.", Color.red);
                }
                
                UpdateUI();
            }
            catch (System.Exception ex)
            {
                ShowStatus($"토큰 확인 오류: {ex.Message}", Color.red);
                Debug.LogError($"[ServerOAuth2Example] 토큰 확인 실패: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Token Events
        
        private void OnTokensUpdated(TokenSet tokenSet)
        {
            Debug.Log("[ServerOAuth2Example] 토큰 업데이트됨");
            _currentTokenSet = tokenSet;
            UpdateUI();
        }
        
        private void OnTokensExpired()
        {
            Debug.Log("[ServerOAuth2Example] 토큰 만료됨");
            ShowStatus("토큰이 만료되었습니다.", Color.red);
            UpdateUI();
        }
        
        private void OnTokensCleared()
        {
            Debug.Log("[ServerOAuth2Example] 토큰 삭제됨");
            _currentTokenSet = null;
            _isLoggedIn = false;
            ShowStatus("토큰이 삭제되었습니다.", Color.blue);
            UpdateUI();
        }
        
        private void OnTokenRefreshed(string newAccessToken)
        {
            Debug.Log("[ServerOAuth2Example] 토큰 갱신 성공");
            ShowStatus("토큰 갱신 성공!", Color.green);
            UpdateUI();
        }
        
        private void OnTokenRefreshFailed(string error)
        {
            Debug.Log($"[ServerOAuth2Example] 토큰 갱신 실패: {error}");
            ShowStatus($"토큰 갱신 실패: {error}", Color.red);
            UpdateUI();
        }
        
        #endregion
        
        #region UI Management
        
        private void UpdateUI()
        {
            if (loginButton != null)
                loginButton.interactable = !_isLoggedIn;
            
            if (logoutButton != null)
                logoutButton.interactable = _isLoggedIn;
            
            if (refreshButton != null)
                refreshButton.interactable = _isLoggedIn;
            
            if (clearButton != null)
                clearButton.interactable = _isLoggedIn;
            
            if (checkButton != null)
                checkButton.interactable = true; // 항상 활성화
        }
        
        private void SetButtonsEnabled(bool enabled)
        {
            if (loginButton != null) loginButton.interactable = enabled && !_isLoggedIn;
            if (logoutButton != null) logoutButton.interactable = enabled && _isLoggedIn;
            if (refreshButton != null) refreshButton.interactable = enabled && _isLoggedIn;
            if (clearButton != null) clearButton.interactable = enabled && _isLoggedIn;
            if (checkButton != null) checkButton.interactable = enabled;
        }
        
        private void ShowStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
            
            Debug.Log($"[ServerOAuth2Example] {message}");
        }
        
        private void DisplayTokenInfo()
        {
            if (userInfoText == null || _currentTokenSet == null)
                return;
            
            var info = $"토큰 정보:\n";
            info += $"Access Token: {_currentTokenSet.AccessToken?.Token?.Substring(0, Math.Min(20, _currentTokenSet.AccessToken.Token.Length))}...\n";
            info += $"Access Token 만료: {_currentTokenSet.AccessToken?.ExpiresAt:yyyy-MM-dd HH:mm:ss}\n";
            info += $"Refresh Token: {(string.IsNullOrEmpty(_currentTokenSet.RefreshToken?.Token) ? "없음" : "있음")}\n";
            info += $"갱신 필요: {_currentTokenSet.NeedsRefresh()}";
            
            userInfoText.text = info;
        }
        
        /// <summary>
        /// 상세 토큰 정보를 콘솔에 로그
        /// </summary>
        private void LogDetailedTokenInfo()
        {
            if (_currentTokenSet == null)
            {
                Debug.LogError("[ServerOAuth2Example] 토큰 세트가 null입니다.");
                return;
            }
            
            Debug.Log("=== 🎉 OAuth2 로그인 성공 - 토큰 정보 ===");
            
            // Access Token 정보
            if (_currentTokenSet.AccessToken != null)
            {
                Debug.Log($"✅ Access Token: {_currentTokenSet.AccessToken.Token}");
                Debug.Log($"   - 만료 시간: {_currentTokenSet.AccessToken.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
                Debug.Log($"   - 만료까지: {(_currentTokenSet.AccessToken.ExpiresAt - DateTime.UtcNow).TotalMinutes:F1}분");
            }
            else
            {
                Debug.LogError("❌ Access Token이 null입니다.");
            }
            
            // Refresh Token 정보
            if (_currentTokenSet.RefreshToken != null)
            {
                Debug.Log($"✅ Refresh Token: {_currentTokenSet.RefreshToken.Token}");
                Debug.Log($"   - 만료 시간: {_currentTokenSet.RefreshToken.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
                Debug.Log($"   - 만료까지: {(_currentTokenSet.RefreshToken.ExpiresAt - DateTime.UtcNow).TotalDays:F1}일");
                Debug.Log($"   - 디바이스 ID: {_currentTokenSet.RefreshToken.DeviceId}");
            }
            else
            {
                Debug.LogWarning("⚠️ Refresh Token이 null입니다.");
            }
            
            // 토큰 세트 상태
            Debug.Log($"📊 토큰 세트 상태:");
            Debug.Log($"   - 갱신 필요: {_currentTokenSet.NeedsRefresh()}");
            Debug.Log($"   - Refresh Token 보유: {_currentTokenSet.HasRefreshToken()}");
            
            Debug.Log("=== 토큰 정보 끝 ===");
        }
        
        private void ClearUserInfo()
        {
            if (userInfoText != null)
            {
                userInfoText.text = "로그인하여 토큰 정보를 확인하세요.";
            }
            
            if (debugText != null)
            {
                debugText.text = "토큰 정보가 없습니다.";
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 현재 토큰 세트 조회
        /// </summary>
        public TokenSet GetCurrentTokenSet()
        {
            return _currentTokenSet;
        }
      
        
        #endregion
    }
}
