using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Core.Managers;

/// <summary>
/// 프로세스 정보 표시 패널 (진행률 바, 메시지)
/// </summary>
public class ProcessInfoPanelUI : BasePanelUI
{
    [Header("Process Info Panel UI")]
    [SerializeField] private TextMeshProUGUI _processMessageText;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TextMeshProUGUI _progressPercentText;
    
    protected override void SetupEventListeners()
    {
        if (_appTitleUI?.AppStartManager != null)
        {
            _appTitleUI.AppStartManager.OnStateChanged += HandleAppStartStateChanged;
        }
    }
    
    protected override void RemoveEventListeners()
    {
        if (_appTitleUI?.AppStartManager != null)
        {
            _appTitleUI.AppStartManager.OnStateChanged -= HandleAppStartStateChanged;
        }
    }
    
    /// <summary>
    /// 프로세스 정보를 업데이트합니다
    /// </summary>
    public void UpdateProcessInfo(string message, float progress)
    {
        if (_processMessageText != null)
            _processMessageText.text = message;
            
        if (_progressBar != null)
            _progressBar.value = Mathf.Clamp01(progress);
            
        if (_progressPercentText != null)
            _progressPercentText.text = $"{(progress * 100):F0}%";
    }
    
    private void HandleAppStartStateChanged(AppStartInfo appStartInfo)
    {
        switch (appStartInfo.state)
        {
            case AppStartState.Initializing:
            case AppStartState.CheckingManagers:
            case AppStartState.CheckingServerConnection:
            case AppStartState.AttemptingAutoLogin:
            case AppStartState.LoginSuccessful:
                if (!appStartInfo.hasError)
                {
                    UpdateProcessInfo(appStartInfo.message, appStartInfo.progress);
                }
                break;
                
            case AppStartState.CheckingUpdates:
                if (!appStartInfo.hasError)
                {
                    UpdateProcessInfo(appStartInfo.message, appStartInfo.progress);
                }
                break;
        }
        
        if (appStartInfo.hasError)
        {
            Debug.LogError($"[ProcessInfoPanelUI] 에러 상태: {appStartInfo.state} - {appStartInfo.errorMessage}");
        }
    }
}