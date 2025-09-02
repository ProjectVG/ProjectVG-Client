using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Core.Settings;

namespace ProjectVG.UI
{
    public class FloatSettingUI : MonoBehaviour, ISettingUI
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private Slider _valueSlider;
        [SerializeField] private TextMeshProUGUI _valueLabel;
        [SerializeField] private Button _resetButton;

        [Header("Display Settings")]
        [SerializeField] private string _valueFormat = "F2";
        [SerializeField] private string _valueSuffix = "";
        [SerializeField] private bool _showPercentage = false;

        private FloatSettingEntry _setting;
        private SettingsMenuUI _settingsMenuUI;

        private void Awake()
        {
            SetupComponents();
            SetupEvents();
        }

        private void SetupComponents()
        {
            if (_valueSlider == null)
                _valueSlider = GetComponentInChildren<Slider>();
            if (_nameLabel == null)
                _nameLabel = GetComponentInChildren<TextMeshProUGUI>();
            if (_resetButton == null)
                _resetButton = GetComponentInChildren<Button>();

            // 슬라이더 기본 설정
            if (_valueSlider != null)
            {
                _valueSlider.wholeNumbers = false;
                _valueSlider.minValue = 0f;
                _valueSlider.maxValue = 1f;
            }
        }

        private void SetupEvents()
        {
            if (_valueSlider != null)
            {
                _valueSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(OnResetButtonClick);
            }
        }

        public void Initialize(BaseSettingEntry setting)
        {
            if (setting is not FloatSettingEntry floatSetting)
            {
                Debug.LogError($"[FloatSettingUI] 잘못된 설정 타입: {setting.GetType()}");
                return;
            }

            _setting = floatSetting;
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

            // 슬라이더 범위 설정
            if (_valueSlider != null)
            {
                _valueSlider.minValue = _setting.MinValue;
                _valueSlider.maxValue = _setting.MaxValue;
                _valueSlider.value = _setting.Value;
            }

            // 퍼센티지 표시 여부 결정
            if (_setting.Key.Contains("Volume") || _setting.Key.Contains("volume"))
            {
                _showPercentage = true;
                _valueSuffix = "%";
                _valueFormat = "F0";
            }
        }

        public void RefreshValue()
        {
            if (_setting == null) return;

            if (_valueSlider != null && !Mathf.Approximately(_valueSlider.value, _setting.Value))
            {
                _valueSlider.value = _setting.Value;
            }

            UpdateValueLabel();
        }

        private void UpdateValueLabel()
        {
            if (_valueLabel == null || _setting == null) return;

            float displayValue = _setting.Value;
            
            if (_showPercentage)
            {
                displayValue *= 100f;
            }

            string formattedValue = displayValue.ToString(_valueFormat);
            _valueLabel.text = formattedValue + _valueSuffix;
        }

        private void OnSliderValueChanged(float value)
        {
            if (_setting == null) return;

            _setting.SetFloatValue(value);
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

            if (_valueSlider != null)
            {
                _valueSlider.onValueChanged.RemoveAllListeners();
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveAllListeners();
            }
        }
    }
}