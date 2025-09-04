using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Core.Managers;
using Cysharp.Threading.Tasks;

/// <summary>
/// 업데이트 정보 및 버튼 관리 패널
/// </summary>
public class UpdatePanelUI : BasePanelUI
{
    [Header("Update Panel UI")]
    [SerializeField] private TextMeshProUGUI _updateInfoText;
    [SerializeField] private Button _updateButton;
    [SerializeField] private Button _skipUpdateButton;
    
    protected override void SetupEventListeners()
    {
        if (_updateButton != null)
            _updateButton.onClick.AddListener(OnUpdateButtonClicked);
            
        if (_skipUpdateButton != null)
            _skipUpdateButton.onClick.AddListener(OnSkipUpdateButtonClicked);
            
        if (_appTitleUI?.AppStartManager != null)
        {
            _appTitleUI.AppStartManager.OnUpdateCheckCompleted += HandleUpdateCheckCompleted;
            _appTitleUI.AppStartManager.OnStateChanged += HandleAppStartStateChanged;
        }
    }
    
    protected override void RemoveEventListeners()
    {
        if (_updateButton != null)
            _updateButton.onClick.RemoveListener(OnUpdateButtonClicked);
            
        if (_skipUpdateButton != null)
            _skipUpdateButton.onClick.RemoveListener(OnSkipUpdateButtonClicked);
            
        if (_appTitleUI?.AppStartManager != null)
        {
            _appTitleUI.AppStartManager.OnUpdateCheckCompleted -= HandleUpdateCheckCompleted;
            _appTitleUI.AppStartManager.OnStateChanged -= HandleAppStartStateChanged;
        }
    }
    
    /// <summary>
    /// 업데이트 정보를 설정합니다
    /// </summary>
    public void SetUpdateInfo(string updateInfo)
    {
        if (_updateInfoText != null)
            _updateInfoText.text = updateInfo;
    }
    
    private void OnUpdateButtonClicked()
    {
        Debug.Log("[UpdatePanelUI] 업데이트 버튼 클릭 - 업데이트 기능은 나중에 구현 예정");
        OnSkipUpdateButtonClicked();
    }
    
    private void OnSkipUpdateButtonClicked()
    {
        Debug.Log("[UpdatePanelUI] 업데이트 스킵");
        
        if (_appTitleUI != null)
        {
            _appTitleUI.ShowProcessInfoPanel("업데이트를 스킵했습니다. 계속 진행합니다...", 0.5f);
            
            if (_appTitleUI.AppStartManager != null)
            {
                _appTitleUI.AppStartManager.TryLoginAsync().Forget();
            }
        }
    }
    
    private void HandleUpdateCheckCompleted(bool updateRequired)
    {
        if (updateRequired)
        {
            SetUpdateInfo("새로운 업데이트가 있습니다. 업데이트를 진행하시겠습니까?");
        }
    }
    
    private void HandleAppStartStateChanged(AppStartInfo appStartInfo)
    {
        if (appStartInfo.state == AppStartState.CheckingUpdates)
        {
            if (appStartInfo.hasError && !string.IsNullOrEmpty(appStartInfo.errorMessage))
            {
                SetUpdateInfo(appStartInfo.errorMessage);
            }
        }
    }
}