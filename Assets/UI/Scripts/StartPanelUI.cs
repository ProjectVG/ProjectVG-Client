using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Core.Managers;
using Cysharp.Threading.Tasks;

/// <summary>
/// 앱 시작 패널 (환영 메시지, 시작 버튼)
/// </summary>
public class StartPanelUI : BasePanelUI
{
    [Header("Start Panel UI")]
    [SerializeField] private TextMeshProUGUI _welcomeText;
    [SerializeField] private Button _startAppButton;
    
    protected override void SetupEventListeners()
    {
        if (_startAppButton != null)
            _startAppButton.onClick.AddListener(OnStartAppButtonClicked);
            
        if (_appTitleUI?.AppStartManager != null)
        {
            _appTitleUI.AppStartManager.OnAppStartReady += HandleAppStartReady;
        }
    }
    
    protected override void RemoveEventListeners()
    {
        if (_startAppButton != null)
            _startAppButton.onClick.RemoveListener(OnStartAppButtonClicked);
            
        if (_appTitleUI?.AppStartManager != null)
        {
            _appTitleUI.AppStartManager.OnAppStartReady -= HandleAppStartReady;
        }
    }
    
    /// <summary>
    /// 환영 메시지를 설정합니다
    /// </summary>
    public void SetWelcomeMessage(string message)
    {
        if (_welcomeText != null)
            _welcomeText.text = message;
    }
    
    protected override void OnPanelShown()
    {
        SetWelcomeMessage("환영합니다! 앱을 시작할 준비가 완료되었습니다.");
    }
    
    private void OnStartAppButtonClicked()
    {
        Debug.Log("[StartPanelUI] 앱 시작 버튼 클릭");
        
        if (SystemManager.Instance != null)
        {
            SystemManager.Instance.TransitionToMainSceneAsync().Forget();
        }
    }
    
    private void HandleAppStartReady()
    {
        SetWelcomeMessage("모든 준비가 완료되었습니다! 앱을 시작하세요.");
    }
}