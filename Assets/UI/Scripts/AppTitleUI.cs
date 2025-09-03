using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Core.Managers;
using Cysharp.Threading.Tasks;

public class AppTitleUI : MonoBehaviour
{
    [Header("패널 참조")]
    [SerializeField] private GameObject _processInfoPanel;      // 프로세스 정보 패널
    [SerializeField] private GameObject _updatePanel;           // 업데이트 패널  
    [SerializeField] private GameObject _startPanel;            // 시작 패널
    [SerializeField] private GameObject _loginButtonPanel;      // 로그인 버튼 패널
    
    [Header("프로세스 정보 패널 UI")]
    [SerializeField] private TextMeshProUGUI _processMessageText;     // 현재 작업 메시지
    [SerializeField] private Slider _progressBar;                     // 진행률 바
    [SerializeField] private TextMeshProUGUI _progressPercentText;    // 진행률 퍼센트 텍스트
    
    [Header("업데이트 패널 UI")]
    [SerializeField] private TextMeshProUGUI _updateInfoText;         // 업데이트 정보
    [SerializeField] private Button _updateButton;                    // 업데이트 버튼
    [SerializeField] private Button _skipUpdateButton;               // 업데이트 스킵 버튼
    
    [Header("시작 패널 UI")]
    [SerializeField] private TextMeshProUGUI _welcomeText;            // 환영 메시지
    [SerializeField] private Button _startAppButton;                  // 앱 시작 버튼
    
    [Header("로그인 패널 UI")]
    [SerializeField] private TextMeshProUGUI _loginMessageText;       // 로그인 메시지
    [SerializeField] private Button _guestLoginButton;               // 게스트 로그인 버튼
    [SerializeField] private Button _oauth2LoginButton;              // OAuth2 로그인 버튼
    
    [Header("설정")]
    [SerializeField] private float _panelTransitionTime = 0.3f;      // 패널 전환 시간
    
    private AppStartManager _appStartManager;
    private AppStartState _currentDisplayState = AppStartState.Initializing;
    
    void Start()
    {
        InitializeUI();
        SetupEventListeners();
    }
    
    void OnDestroy()
    {
        RemoveEventListeners();
    }
    
    #region 초기화
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // AppStartManager 참조 설정
        _appStartManager = AppStartManager.Instance;
        
        // 모든 패널 비활성화
        HideAllPanels();
        
        // 프로세스 정보 패널을 기본으로 표시
        ShowProcessInfoPanel("앱 초기화 중...", 0f);
        
        // 버튼 이벤트 설정
        SetupButtonEvents();
    }
    
    /// <summary>
    /// 버튼 이벤트 설정
    /// </summary>
    private void SetupButtonEvents()
    {
        // 업데이트 패널 버튼
        if (_updateButton != null)
            _updateButton.onClick.AddListener(OnUpdateButtonClicked);
        if (_skipUpdateButton != null)
            _skipUpdateButton.onClick.AddListener(OnSkipUpdateButtonClicked);
        
        // 시작 패널 버튼
        if (_startAppButton != null)
            _startAppButton.onClick.AddListener(OnStartAppButtonClicked);
        
        // 로그인 패널 버튼
        if (_guestLoginButton != null)
            _guestLoginButton.onClick.AddListener(OnGuestLoginButtonClicked);
        if (_oauth2LoginButton != null)
            _oauth2LoginButton.onClick.AddListener(OnOAuth2LoginButtonClicked);
    }
    
    /// <summary>
    /// 이벤트 리스너 설정
    /// </summary>
    private void SetupEventListeners()
    {
        if (_appStartManager != null)
        {
            _appStartManager.OnStateChanged += HandleAppStartStateChanged;
            _appStartManager.OnLoginRequired += HandleLoginRequired;
            _appStartManager.OnAppStartReady += HandleAppStartReady;
            _appStartManager.OnUpdateCheckCompleted += HandleUpdateCheckCompleted;
        }
    }
    
    /// <summary>
    /// 이벤트 리스너 제거
    /// </summary>
    private void RemoveEventListeners()
    {
        if (_appStartManager != null)
        {
            _appStartManager.OnStateChanged -= HandleAppStartStateChanged;
            _appStartManager.OnLoginRequired -= HandleLoginRequired;
            _appStartManager.OnAppStartReady -= HandleAppStartReady;
            _appStartManager.OnUpdateCheckCompleted -= HandleUpdateCheckCompleted;
        }
    }
    
    #endregion
    
    #region 패널 관리
    
    /// <summary>
    /// 모든 패널을 숨깁니다
    /// </summary>
    private void HideAllPanels()
    {
        if (_processInfoPanel != null) _processInfoPanel.SetActive(false);
        if (_updatePanel != null) _updatePanel.SetActive(false);
        if (_startPanel != null) _startPanel.SetActive(false);
        if (_loginButtonPanel != null) _loginButtonPanel.SetActive(false);
    }
    
    /// <summary>
    /// 프로세스 정보 패널을 표시합니다
    /// </summary>
    private void ShowProcessInfoPanel(string message, float progress)
    {
        HideAllPanels();
        
        if (_processInfoPanel != null)
        {
            _processInfoPanel.SetActive(true);
            
            // 메시지 업데이트
            if (_processMessageText != null)
                _processMessageText.text = message;
            
            // 진행률 업데이트
            if (_progressBar != null)
                _progressBar.value = progress;
            
            if (_progressPercentText != null)
                _progressPercentText.text = $"{(progress * 100):F0}%";
        }
    }
    
    /// <summary>
    /// 업데이트 패널을 표시합니다
    /// </summary>
    private void ShowUpdatePanel(string updateInfo)
    {
        HideAllPanels();
        
        if (_updatePanel != null)
        {
            _updatePanel.SetActive(true);
            
            if (_updateInfoText != null)
                _updateInfoText.text = updateInfo;
        }
    }
    
    /// <summary>
    /// 시작 패널을 표시합니다
    /// </summary>
    private void ShowStartPanel(string welcomeMessage = "환영합니다! 앱을 시작할 준비가 완료되었습니다.")
    {
        HideAllPanels();
        
        if (_startPanel != null)
        {
            _startPanel.SetActive(true);
            
            if (_welcomeText != null)
                _welcomeText.text = welcomeMessage;
        }
    }
    
    /// <summary>
    /// 로그인 버튼 패널을 표시합니다
    /// </summary>
    private void ShowLoginButtonPanel(string loginMessage = "로그인이 필요합니다.")
    {
        HideAllPanels();
        
        if (_loginButtonPanel != null)
        {
            _loginButtonPanel.SetActive(true);
            
            if (_loginMessageText != null)
                _loginMessageText.text = loginMessage;
        }
    }
    
    #endregion
    
    #region 이벤트 핸들러
    
    /// <summary>
    /// AppStart 상태 변경 처리
    /// </summary>
    private void HandleAppStartStateChanged(AppStartInfo appStartInfo)
    {
        _currentDisplayState = appStartInfo.state;
        
        switch (appStartInfo.state)
        {
            case AppStartState.Initializing:
            case AppStartState.CheckingManagers:
            case AppStartState.CheckingServerConnection:
            case AppStartState.AttemptingAutoLogin:
                ShowProcessInfoPanel(appStartInfo.message, appStartInfo.progress);
                break;
                
            case AppStartState.CheckingUpdates:
                // 업데이트가 필요한 경우에만 업데이트 패널 표시
                // 아니면 프로세스 정보 패널 유지
                if (appStartInfo.hasError && !string.IsNullOrEmpty(appStartInfo.errorMessage))
                {
                    // 업데이트 필요한 경우 (에러가 아님, 단지 업데이트 정보)
                    ShowUpdatePanel(appStartInfo.errorMessage);
                }
                else
                {
                    ShowProcessInfoPanel(appStartInfo.message, appStartInfo.progress);
                }
                break;
                
            case AppStartState.LoginRequired:
                ShowLoginButtonPanel(appStartInfo.message);
                break;
                
            case AppStartState.LoginSuccessful:
                ShowProcessInfoPanel("로그인 성공! 앱을 시작합니다...", appStartInfo.progress);
                break;
                
            case AppStartState.Ready:
                ShowStartPanel();
                break;
        }
        
        // 에러가 있는 경우 로그 출력
        if (appStartInfo.hasError)
        {
            Debug.LogError($"[AppTitleUI] 에러 상태: {appStartInfo.state} - {appStartInfo.errorMessage}");
        }
    }
    
    /// <summary>
    /// 로그인 필요 이벤트 처리
    /// </summary>
    private void HandleLoginRequired()
    {
        ShowLoginButtonPanel("로그인이 필요합니다. 로그인 방법을 선택해주세요.");
    }
    
    /// <summary>
    /// 앱 시작 준비 완료 이벤트 처리
    /// </summary>
    private void HandleAppStartReady()
    {
        ShowStartPanel("모든 준비가 완료되었습니다! 앱을 시작하세요.");
    }
    
    /// <summary>
    /// 업데이트 확인 완료 이벤트 처리
    /// </summary>
    private void HandleUpdateCheckCompleted(bool updateRequired)
    {
        if (updateRequired)
        {
            ShowUpdatePanel("새로운 업데이트가 있습니다. 업데이트를 진행하시겠습니까?");
        }
    }
    
    #endregion
    
    #region 버튼 이벤트 핸들러
    
    /// <summary>
    /// 업데이트 버튼 클릭 처리
    /// </summary>
    private void OnUpdateButtonClicked()
    {
        Debug.Log("[AppTitleUI] 업데이트 버튼 클릭 - 업데이트 기능은 나중에 구현 예정");
        // 현재는 업데이트를 스킵하고 계속 진행
        OnSkipUpdateButtonClicked();
    }
    
    /// <summary>
    /// 업데이트 스킵 버튼 클릭 처리
    /// </summary>
    private void OnSkipUpdateButtonClicked()
    {
        Debug.Log("[AppTitleUI] 업데이트 스킵");
        // AppStartManager의 프로세스를 재개 (현재는 간단히 프로세스 정보 패널로 전환)
        ShowProcessInfoPanel("업데이트를 스킵했습니다. 계속 진행합니다...", 0.5f);
        
        // 실제로는 AppStartManager에 업데이트 스킵 신호를 보내야 함
        // 현재는 간단히 자동 로그인으로 진행
        if (_appStartManager != null)
        {
            _appStartManager.TryLoginAsync().Forget();
        }
    }
    
    /// <summary>
    /// 앱 시작 버튼 클릭 처리
    /// </summary>
    private void OnStartAppButtonClicked()
    {
        Debug.Log("[AppTitleUI] 앱 시작 버튼 클릭");
        
        // 메인 씬으로 전환
        if (SystemManager.Instance != null)
        {
            SystemManager.Instance.TransitionToMainSceneAsync().Forget();
        }
    }
    
    /// <summary>
    /// 게스트 로그인 버튼 클릭 처리
    /// </summary>
    private async void OnGuestLoginButtonClicked()
    {
        Debug.Log("[AppTitleUI] 게스트 로그인 버튼 클릭");
        
        ShowProcessInfoPanel("게스트로 로그인 중...", 0.7f);
        
        var authManager = ProjectVG.Infrastructure.Auth.AuthManager.Instance;
        if (authManager != null)
        {
            bool loginSuccess = await authManager.LoginAsGuestAsync();
            
            if (loginSuccess)
            {
                ShowProcessInfoPanel("게스트 로그인 성공!", 1.0f);
                await UniTask.Delay(1000);
                ShowStartPanel("게스트로 로그인했습니다. 앱을 시작하세요!");
            }
            else
            {
                ShowLoginButtonPanel("게스트 로그인에 실패했습니다. 다시 시도해주세요.");
            }
        }
    }
    
    /// <summary>
    /// OAuth2 로그인 버튼 클릭 처리  
    /// </summary>
    private async void OnOAuth2LoginButtonClicked()
    {
        Debug.Log("[AppTitleUI] OAuth2 로그인 버튼 클릭");
        
        ShowProcessInfoPanel("OAuth2 로그인 중...", 0.7f);
        
        var authManager = ProjectVG.Infrastructure.Auth.AuthManager.Instance;
        if (authManager != null)
        {
            bool loginSuccess = await authManager.LoginWithOAuth2Async();
            
            if (loginSuccess)
            {
                ShowProcessInfoPanel("OAuth2 로그인 성공!", 1.0f);
                await UniTask.Delay(1000);
                ShowStartPanel("OAuth2 로그인했습니다. 앱을 시작하세요!");
            }
            else
            {
                ShowLoginButtonPanel("OAuth2 로그인에 실패했습니다. 다시 시도해주세요.");
            }
        }
    }
    
    #endregion
}
