using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Core.Settings;

namespace ProjectVG.UI
{
    public class SettingsMenuUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _defaultsButton;

        [Header("Category Tabs")]
        [SerializeField] private Transform _tabContainer;
        [SerializeField] private Button _tabButtonPrefab;
        [SerializeField] private List<SettingsCategoryTab> _categoryTabs = new List<SettingsCategoryTab>();

        [Header("Settings Content")]
        [SerializeField] private Transform _settingsContainer;
        [SerializeField] private ScrollRect _settingsScrollRect;

        [Header("Setting UI Prefabs")]
        [SerializeField] private GameObject _floatSettingPrefab;
        [SerializeField] private GameObject _boolSettingPrefab;
        [SerializeField] private GameObject _intSettingPrefab;
        [SerializeField] private GameObject _stringSettingPrefab;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private bool _enableFadeAnimation = true;
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.2f;

        private SettingsManager _settingsManager;
        private PauseMenuManager _pauseMenuManager;
        private SettingCategory _currentCategory = SettingCategory.Audio;
        private Dictionary<SettingCategory, List<GameObject>> _categorySettingUIs = new Dictionary<SettingCategory, List<GameObject>>();
        private bool _isAnimating = false;
        private bool _hasUnsavedChanges = false;

        [System.Serializable]
        public class SettingsCategoryTab
        {
            public SettingCategory category;
            public Button tabButton;
            public TextMeshProUGUI tabLabel;
            public GameObject tabContent;
        }

        private void Awake()
        {
            SetupComponents();
            SetupButtons();
        }

        private void Start()
        {
            _settingsManager = SettingsManager.Instance;
            _pauseMenuManager = PauseMenuManager.Instance;

            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.SetSettingsMenuUI(gameObject);
            }

            InitializeSettingsUI();
            gameObject.SetActive(false);
        }

        private void SetupComponents()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void SetupButtons()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(OnBackButtonClick);
            }

            if (_applyButton != null)
            {
                _applyButton.onClick.RemoveAllListeners();
                _applyButton.onClick.AddListener(OnApplyButtonClick);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveAllListeners();
                _resetButton.onClick.AddListener(OnResetButtonClick);
            }

            if (_defaultsButton != null)
            {
                _defaultsButton.onClick.RemoveAllListeners();
                _defaultsButton.onClick.AddListener(OnDefaultsButtonClick);
            }
        }

        private void InitializeSettingsUI()
        {
            if (_settingsManager == null || !_settingsManager.IsInitialized) return;

            CreateCategoryTabs();
            CreateSettingUIs();
            ShowCategory(_currentCategory);
        }

        private void CreateCategoryTabs()
        {
            if (_tabContainer == null || _tabButtonPrefab == null) return;

            _categoryTabs.Clear();

            foreach (SettingCategory category in System.Enum.GetValues(typeof(SettingCategory)))
            {
                var group = _settingsManager.GetSettingGroup(category);
                if (group == null) continue;

                GameObject tabButtonObj = Instantiate(_tabButtonPrefab.gameObject, _tabContainer);
                Button tabButton = tabButtonObj.GetComponent<Button>();
                TextMeshProUGUI tabLabel = tabButtonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (tabLabel != null)
                {
                    tabLabel.text = group.DisplayName;
                }

                SettingCategory currentCat = category; // Capture for closure
                tabButton.onClick.AddListener(() => OnCategoryTabClick(currentCat));

                var categoryTab = new SettingsCategoryTab
                {
                    category = category,
                    tabButton = tabButton,
                    tabLabel = tabLabel,
                    tabContent = null
                };

                _categoryTabs.Add(categoryTab);
            }
        }

        private void CreateSettingUIs()
        {
            if (_settingsContainer == null) return;

            _categorySettingUIs.Clear();

            foreach (var group in _settingsManager.GetAllGroups())
            {
                var categoryUIs = new List<GameObject>();

                foreach (var setting in group.Settings)
                {
                    GameObject settingUI = CreateSettingUI(setting);
                    if (settingUI != null)
                    {
                        settingUI.transform.SetParent(_settingsContainer, false);
                        settingUI.SetActive(false);
                        categoryUIs.Add(settingUI);
                    }
                }

                _categorySettingUIs[group.Category] = categoryUIs;
            }
        }

        private GameObject CreateSettingUI(BaseSettingEntry setting)
        {
            GameObject prefab = GetSettingPrefab(setting.SettingType);
            if (prefab == null) return null;

            GameObject settingUI = Instantiate(prefab);
            ISettingUI settingUIComponent = settingUI.GetComponent<ISettingUI>();

            if (settingUIComponent != null)
            {
                settingUIComponent.Initialize(setting);
            }

            return settingUI;
        }

        private GameObject GetSettingPrefab(SettingType settingType)
        {
            return settingType switch
            {
                SettingType.Float => _floatSettingPrefab,
                SettingType.Bool => _boolSettingPrefab,
                SettingType.Int => _intSettingPrefab,
                SettingType.String => _stringSettingPrefab,
                _ => null
            };
        }

        private void OnCategoryTabClick(SettingCategory category)
        {
            ShowCategory(category);
        }

        private void ShowCategory(SettingCategory category)
        {
            _currentCategory = category;

            // 모든 설정 UI 숨기기
            foreach (var kvp in _categorySettingUIs)
            {
                foreach (var ui in kvp.Value)
                {
                    ui.SetActive(false);
                }
            }

            // 선택된 카테고리의 설정 UI 표시
            if (_categorySettingUIs.TryGetValue(category, out var categoryUIs))
            {
                foreach (var ui in categoryUIs)
                {
                    ui.SetActive(true);
                }
            }

            // 탭 버튼 상태 업데이트
            UpdateTabButtonStates();

            // 스크롤 위치 초기화
            if (_settingsScrollRect != null)
            {
                _settingsScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void UpdateTabButtonStates()
        {
            foreach (var tab in _categoryTabs)
            {
                bool isSelected = tab.category == _currentCategory;
                
                // 버튼 색상이나 스타일 변경
                if (tab.tabButton != null)
                {
                    var colors = tab.tabButton.colors;
                    colors.normalColor = isSelected ? Color.white : Color.gray;
                    tab.tabButton.colors = colors;
                }

                if (tab.tabLabel != null)
                {
                    tab.tabLabel.color = isSelected ? Color.black : Color.white;
                }
            }
        }

        private void OnEnable()
        {
            if (_enableFadeAnimation && !_isAnimating)
            {
                PlayFadeInAnimation();
            }

            UpdateApplyButtonState();
        }

        private void OnBackButtonClick()
        {
            if (_hasUnsavedChanges)
            {
                // 저장하지 않은 변경사항이 있으면 확인
                ShowUnsavedChangesDialog();
            }
            else
            {
                if (_pauseMenuManager != null)
                {
                    _pauseMenuManager.OnSettingsBackButtonClick();
                }
            }
        }

        private void OnApplyButtonClick()
        {
            if (_settingsManager != null)
            {
                _settingsManager.SaveAllSettings();
                _hasUnsavedChanges = false;
                UpdateApplyButtonState();
            }
        }

        private void OnResetButtonClick()
        {
            if (_settingsManager != null)
            {
                _settingsManager.ResetCategoryToDefaults(_currentCategory);
                _hasUnsavedChanges = true;
                UpdateApplyButtonState();
            }
        }

        private void OnDefaultsButtonClick()
        {
            if (_settingsManager != null)
            {
                _settingsManager.ResetAllToDefaults();
                _hasUnsavedChanges = true;
                UpdateApplyButtonState();
            }
        }

        private void ShowUnsavedChangesDialog()
        {
            // 간단한 확인 대화상자 (나중에 더 예쁜 UI로 교체 가능)
            Debug.Log("[SettingsMenuUI] 저장하지 않은 변경사항이 있습니다. 그래도 나가시겠습니까?");
            
            // 임시로 그냥 나가기 (실제로는 다이얼로그 표시 후 사용자 선택에 따라 처리)
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.OnSettingsBackButtonClick();
            }
        }

        private void UpdateApplyButtonState()
        {
            if (_applyButton != null)
            {
                _applyButton.interactable = _hasUnsavedChanges;
                
                var colors = _applyButton.colors;
                colors.normalColor = _hasUnsavedChanges ? Color.green : Color.gray;
                _applyButton.colors = colors;
            }
        }

        private void PlayFadeInAnimation()
        {
            if (_canvasGroup == null) return;

            _isAnimating = true;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;

            StartCoroutine(FadeCoroutine(0f, 1f, _fadeInDuration, () =>
            {
                _canvasGroup.interactable = true;
                _isAnimating = false;
            }));
        }

        public void PlayFadeOutAnimation(System.Action onComplete = null)
        {
            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            _isAnimating = true;
            _canvasGroup.interactable = false;

            StartCoroutine(FadeCoroutine(1f, 0f, _fadeOutDuration, () =>
            {
                _canvasGroup.blocksRaycasts = false;
                _isAnimating = false;
                onComplete?.Invoke();
            }));
        }

        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / duration;
                progress = Mathf.SmoothStep(0f, 1f, progress);
                
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                yield return null;
            }
            
            _canvasGroup.alpha = endAlpha;
            onComplete?.Invoke();
        }

        // 설정 값 변경 시 호출되는 메서드
        public void OnSettingValueChanged()
        {
            _hasUnsavedChanges = true;
            UpdateApplyButtonState();
        }

        private void OnDestroy()
        {
            // 이벤트 정리
            if (_backButton != null)
                _backButton.onClick.RemoveAllListeners();
            if (_applyButton != null)
                _applyButton.onClick.RemoveAllListeners();
            if (_resetButton != null)
                _resetButton.onClick.RemoveAllListeners();
            if (_defaultsButton != null)
                _defaultsButton.onClick.RemoveAllListeners();

            foreach (var tab in _categoryTabs)
            {
                if (tab.tabButton != null)
                    tab.tabButton.onClick.RemoveAllListeners();
            }
        }
    }

    // 설정 UI 컴포넌트들이 구현해야 하는 인터페이스
    public interface ISettingUI
    {
        void Initialize(BaseSettingEntry setting);
        void RefreshValue();
    }
}