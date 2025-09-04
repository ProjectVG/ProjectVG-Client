using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 패널 UI의 기본 인터페이스
/// </summary>
public interface IPanelManager
{
    /// <summary>
    /// 패널을 표시합니다
    /// </summary>
    void ShowPanel();
    
    /// <summary>
    /// 패널을 숨깁니다
    /// </summary>
    void HidePanel();
    
    /// <summary>
    /// 패널이 현재 활성화되어 있는지 확인합니다
    /// </summary>
    bool IsActive { get; }
}

/// <summary>
/// 모든 패널 UI의 기본 클래스
/// </summary>
public abstract class BasePanelUI : MonoBehaviour, IPanelManager
{
    [Header("Panel Settings")]
    [SerializeField] protected GameObject _panelRoot;
    [SerializeField] protected float _transitionTime = 0.3f;
    
    protected AppTitleUI _appTitleUI;
    
    public bool IsActive => _panelRoot != null && _panelRoot.activeInHierarchy;
    
    protected virtual void Awake()
    {
        if (_panelRoot == null)
            _panelRoot = gameObject;
    }
    
    /// <summary>
    /// AppTitleUI 참조를 주입받습니다
    /// </summary>
    public virtual void Initialize(AppTitleUI appTitleUI)
    {
        _appTitleUI = appTitleUI;
        SetupEventListeners();
    }
    
    /// <summary>
    /// 패널을 표시합니다
    /// </summary>
    public virtual void ShowPanel()
    {
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(true);
            OnPanelShown();
        }
    }
    
    /// <summary>
    /// 패널을 숨깁니다
    /// </summary>
    public virtual void HidePanel()
    {
        if (_panelRoot != null)
        {
            OnPanelHiding();
            _panelRoot.SetActive(false);
        }
    }
    
    /// <summary>
    /// 이벤트 리스너를 설정합니다
    /// </summary>
    protected virtual void SetupEventListeners() { }
    
    /// <summary>
    /// 이벤트 리스너를 제거합니다
    /// </summary>
    protected virtual void RemoveEventListeners() { }
    
    /// <summary>
    /// 패널이 표시될 때 호출됩니다
    /// </summary>
    protected virtual void OnPanelShown() { }
    
    /// <summary>
    /// 패널이 숨겨지기 전에 호출됩니다
    /// </summary>
    protected virtual void OnPanelHiding() { }
    
    protected virtual void OnDestroy()
    {
        RemoveEventListeners();
    }
}