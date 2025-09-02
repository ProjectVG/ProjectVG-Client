using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Core.Settings;

namespace ProjectVG.UI
{
    public class BoolSettingUI : MonoBehaviour, ISettingUI
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private Toggle _valueToggle;
        [SerializeField] private TextMeshProUGUI _valueLabel;
        [SerializeField] private Button _resetButton;

        [Header("Display Settings")]
        [SerializeField] private string _onText = "켜기";
        [SerializeField] private string _offText = "끄기";
        [SerializeField] private bool _showValueLabel = true;

        private BoolSettingEntry _setting;
        private SettingsMenuUI _settingsMenuUI;

        private void Awake()
        {
            SetupComponents();
            SetupEvents();
        }

        private void SetupComponents()
        {
            if (_valueToggle == null)
                _valueToggle = GetComponentInChildren<Toggle>();
            if (_nameLabel == null)
                _nameLabel = GetComponentInChildren<TextMeshProUGUI>();
            if (_resetButton == null)
                _resetButton = GetComponentInChildren<Button>();
        }

        private void SetupEvents()
        {
            if (_valueToggle != null)
            {
                _valueToggle.onValueChanged.AddListener(OnToggleValueChanged);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(OnResetButtonClick);
            }
        }

        public void Initialize(BaseSettingEntry setting)
        {
            if (setting is not BoolSettingEntry boolSetting)
            {
                Debug.LogError($"[BoolSettingUI] 잘못된 설정 타입: {setting.GetType()}");
                return;
            }

            _setting = boolSetting;
            _settingsMenuUI = GetComponentInParent<SettingsMenuUI>();

            UpdateUI();
            RefreshValue();

            // 설정 변경 이벤트 구독
            _setting.OnValueChanged += OnSettingValueChanged;
        }

        private void UpdateUI()
        {
            if (_setting == null) return;

            // 이름과 설명 설정
            if (_nameLabel != null)
                _nameLabel.text = _setting.DisplayName;

            if (_descriptionLabel != null)
                _descriptionLabel.text = _setting.Description;

            // 토글 초기값 설정
            if (_valueToggle != null)
            {
                _valueToggle.isOn = _setting.Value;
            }

            // 값 레이블 표시 여부
            if (_valueLabel != null)
            {
                _valueLabel.gameObject.SetActive(_showValueLabel);
            }
        }

        public void RefreshValue()
        {
            if (_setting == null) return;

            if (_valueToggle != null && _valueToggle.isOn != _setting.Value)
            {
                _valueToggle.isOn = _setting.Value;
            }

            UpdateValueLabel();
        }

        private void UpdateValueLabel()
        {
            if (_valueLabel == null || _setting == null || !_showValueLabel) return;

            _valueLabel.text = _setting.Value ? _onText : _offText;
            _valueLabel.color = _setting.Value ? Color.green : Color.red;
        }

        private void OnToggleValueChanged(bool value)
        {
            if (_setting == null) return;

            _setting.SetBoolValue(value);
            UpdateValueLabel();

            // 설정 메뉴에 변경사항 알림
            if (_settingsMenuUI != null)
            {
                _settingsMenuUI.OnSettingValueChanged();
            }
        }

        private void OnResetButtonClick()
        {
            if (_setting == null) return;

            _setting.ResetToDefault();
            RefreshValue();

            if (_settingsMenuUI != null)
            {
                _settingsMenuUI.OnSettingValueChanged();
            }
        }

        private void OnSettingValueChanged(BaseSettingEntry setting)
        {
            RefreshValue();
        }

        private void OnDestroy()
        {
            if (_setting != null)
            {
                _setting.OnValueChanged -= OnSettingValueChanged;
            }

            if (_valueToggle != null)
            {
                _valueToggle.onValueChanged.RemoveAllListeners();
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveAllListeners();
            }
        }
    }
}