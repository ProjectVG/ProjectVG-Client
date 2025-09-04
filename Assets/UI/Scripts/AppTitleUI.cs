using UnityEngine;
using ProjectVG.Core.Managers;

public class AppTitleUI : MonoBehaviour
{
    [Header("패널 참조")]
    [SerializeField] private ProcessInfoPanelUI _processInfoPanel;
    [SerializeField] private UpdatePanelUI _updatePanel;
    [SerializeField] private StartPanelUI _startPanel;
    [SerializeField] private LoginPanelUI _loginPanel;
    
    [Header("설정")]
    [SerializeField] private float _panelTransitionTime = 0.3f;
    
    private AppStartManager _appStartManager;
    private AppStartState _currentDisplayState = AppStartState.Initializing;
    
    public AppStartManager AppStartManager => _appStartManager;
    
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
        _appStartManager = AppStartManager.Instance;
        
        InitializePanels();
        HideAllPanels();
        ShowProcessInfoPanel("앱 초기화 중...", 0f);
    }
    
    /// <summary>
    /// 패널 초기화
    /// </summary>
    private void InitializePanels()
    {
        if (_processInfoPanel != null)
            _processInfoPanel.Initialize(this);
            
        if (_updatePanel != null)
            _updatePanel.Initialize(this);
            
        if (_startPanel != null)
            _startPanel.Initialize(this);
            
        if (_loginPanel != null)
            _loginPanel.Initialize(this);
    }
    
    
    /// <summary>
    /// 이벤트 리스너 설정
    /// </summary>
    private void SetupEventListeners()
    {
        if (_appStartManager != null)
        {
            _appStartManager.OnStateChanged += HandleAppStartStateChanged;
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
        }
    }
    
    #endregion
    
    #region 패널 관리
    
    /// <summary>
    /// 모든 패널을 숨깁니다
    /// </summary>
    private void HideAllPanels()
    {
        if (_processInfoPanel != null) _processInfoPanel.HidePanel();
        if (_updatePanel != null) _updatePanel.HidePanel();
        if (_startPanel != null) _startPanel.HidePanel();
        if (_loginPanel != null) _loginPanel.HidePanel();
    }
    
    /// <summary>
    /// 프로세스 정보 패널을 표시합니다
    /// </summary>
    public void ShowProcessInfoPanel(string message, float progress)
    {
        HideAllPanels();
        
        if (_processInfoPanel != null)
        {
            _processInfoPanel.UpdateProcessInfo(message, progress);
            _processInfoPanel.ShowPanel();
        }
    }
    
    /// <summary>
    /// 업데이트 패널을 표시합니다
    /// </summary>
    public void ShowUpdatePanel(string updateInfo)
    {
        HideAllPanels();
        
        if (_updatePanel != null)
        {
            _updatePanel.SetUpdateInfo(updateInfo);
            _updatePanel.ShowPanel();
        }
    }
    
    /// <summary>
    /// 시작 패널을 표시합니다
    /// </summary>
    public void ShowStartPanel(string welcomeMessage = "환영합니다! 앱을 시작할 준비가 완료되었습니다.")
    {
        HideAllPanels();
        
        if (_startPanel != null)
        {
            _startPanel.SetWelcomeMessage(welcomeMessage);
            _startPanel.ShowPanel();
        }
    }
    
    /// <summary>
    /// 로그인 패널을 표시합니다
    /// </summary>
    public void ShowLoginPanel(string loginMessage = "로그인이 필요합니다.")
    {
        HideAllPanels();
        
        if (_loginPanel != null)
        {
            _loginPanel.SetLoginMessage(loginMessage);
            _loginPanel.ShowPanel();
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
                ShowLoginPanel(appStartInfo.message);
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
    
    
    #endregion

}
