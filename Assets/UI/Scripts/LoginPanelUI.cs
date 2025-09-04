using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Infrastructure.Auth;
using Cysharp.Threading.Tasks;

/// <summary>
/// 로그인 패널 (게스트 로그인, OAuth2 로그인 버튼)
/// </summary>
public class LoginPanelUI : BasePanelUI
{
    [Header("Login Panel UI")]
    [SerializeField] private TextMeshProUGUI _loginMessageText;
    [SerializeField] private Button _guestLoginButton;
    [SerializeField] private Button _oauth2LoginButton;
    
    private AuthManager _authManager;
    
    protected override void SetupEventListeners()
    {
        if (_guestLoginButton != null)
            _guestLoginButton.onClick.AddListener(OnGuestLoginButtonClicked);
            
        if (_oauth2LoginButton != null)
            _oauth2LoginButton.onClick.AddListener(OnOAuth2LoginButtonClicked);
            
        if (_appTitleUI?.AppStartManager != null)
        {
            _appTitleUI.AppStartManager.OnLoginRequired += HandleLoginRequired;
        }
    }
    
    protected override void RemoveEventListeners()
    {
        if (_guestLoginButton != null)
            _guestLoginButton.onClick.RemoveListener(OnGuestLoginButtonClicked);
            
        if (_oauth2LoginButton != null)
            _oauth2LoginButton.onClick.RemoveListener(OnOAuth2LoginButtonClicked);
            
        if (_appTitleUI?.AppStartManager != null)
        {
            _appTitleUI.AppStartManager.OnLoginRequired -= HandleLoginRequired;
        }
    }
    
    public override void Initialize(AppTitleUI appTitleUI)
    {
        base.Initialize(appTitleUI);
        _authManager = AuthManager.Instance;
    }
    
    /// <summary>
    /// 로그인 메시지를 설정합니다
    /// </summary>
    public void SetLoginMessage(string message)
    {
        if (_loginMessageText != null)
            _loginMessageText.text = message;
    }
    
    protected override void OnPanelShown()
    {
        SetLoginMessage("로그인이 필요합니다. 로그인 방법을 선택해주세요.");
    }
    
    private async void OnGuestLoginButtonClicked()
    {
        Debug.Log("[LoginPanelUI] 게스트 로그인 버튼 클릭");
        
        if (_appTitleUI != null)
        {
            _appTitleUI.ShowProcessInfoPanel("게스트로 로그인 중...", 0.7f);
        }
        
        if (_authManager != null)
        {
            bool loginSuccess = await _authManager.LoginAsGuestAsync();
            
            if (loginSuccess)
            {
                if (_appTitleUI != null)
                {
                    _appTitleUI.ShowProcessInfoPanel("게스트 로그인 성공!", 1.0f);
                    await UniTask.Delay(1000);
                    _appTitleUI.ShowStartPanel("게스트로 로그인했습니다. 앱을 시작하세요!");
                }
            }
            else
            {
                SetLoginMessage("게스트 로그인에 실패했습니다. 다시 시도해주세요.");
            }
        }
    }
    
    private async void OnOAuth2LoginButtonClicked()
    {
        Debug.Log("[LoginPanelUI] OAuth2 로그인 버튼 클릭");
        
        if (_appTitleUI != null)
        {
            _appTitleUI.ShowProcessInfoPanel("OAuth2 로그인 중...", 0.7f);
        }
        
        if (_authManager != null)
        {
            bool loginSuccess = await _authManager.LoginWithOAuth2Async();
            
            if (loginSuccess)
            {
                if (_appTitleUI != null)
                {
                    _appTitleUI.ShowProcessInfoPanel("OAuth2 로그인 성공!", 1.0f);
                    await UniTask.Delay(1000);
                    _appTitleUI.ShowStartPanel("OAuth2 로그인했습니다. 앱을 시작하세요!");
                }
            }
            else
            {
                SetLoginMessage("OAuth2 로그인에 실패했습니다. 다시 시도해주세요.");
            }
        }
    }
    
    private void HandleLoginRequired()
    {
        SetLoginMessage("로그인이 필요합니다. 로그인 방법을 선택해주세요.");
    }
}
