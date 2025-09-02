using System;
using UnityEngine;

namespace ProjectVG.Core.Settings
{
    public enum SettingType
    {
        Float,
        Int,
        Bool,
        String,
        Enum
    }

    [Serializable]
    public abstract class BaseSettingEntry
    {
        [SerializeField] protected string _key;
        [SerializeField] protected string _displayName;
        [SerializeField] protected string _description;
        [SerializeField] protected SettingType _settingType;
        
        public string Key => _key;
        public string DisplayName => _displayName;
        public string Description => _description;
        public SettingType SettingType => _settingType;
        
        public event Action<BaseSettingEntry> OnValueChanged;

        protected BaseSettingEntry(string key, string displayName, string description, SettingType settingType)
        {
            _key = key;
            _displayName = displayName;
            _description = description;
            _settingType = settingType;
        }

        public abstract object GetValue();
        public abstract void SetValue(object value);
        public abstract object GetDefaultValue();
        public abstract void ResetToDefault();
        public abstract void LoadFromPlayerPrefs();
        public abstract void SaveToPlayerPrefs();

        protected virtual void NotifyValueChanged()
        {
            OnValueChanged?.Invoke(this);
        }
    }

    [Serializable]
    public class FloatSettingEntry : BaseSettingEntry
    {
        [SerializeField] private float _value;
        [SerializeField] private float _defaultValue;
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue;

        public float Value => _value;
        public float DefaultValue => _defaultValue;
        public float MinValue => _minValue;
        public float MaxValue => _maxValue;

        public FloatSettingEntry(string key, string displayName, string description, 
                                float defaultValue, float minValue = 0f, float maxValue = 1f)
            : base(key, displayName, description, SettingType.Float)
        {
            _defaultValue = defaultValue;
            _value = defaultValue;
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public override object GetValue() => _value;
        public override object GetDefaultValue() => _defaultValue;

        public override void SetValue(object value)
        {
            if (value is float floatValue)
            {
                SetFloatValue(floatValue);
            }
        }

        public void SetFloatValue(float value)
        {
            float clampedValue = Mathf.Clamp(value, _minValue, _maxValue);
            if (Mathf.Approximately(_value, clampedValue)) return;
            
            _value = clampedValue;
            NotifyValueChanged();
        }

        public override void ResetToDefault()
        {
            SetFloatValue(_defaultValue);
        }

        public override void LoadFromPlayerPrefs()
        {
            _value = PlayerPrefs.GetFloat(_key, _defaultValue);
        }

        public override void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetFloat(_key, _value);
        }
    }

    [Serializable]
    public class BoolSettingEntry : BaseSettingEntry
    {
        [SerializeField] private bool _value;
        [SerializeField] private bool _defaultValue;

        public bool Value => _value;
        public bool DefaultValue => _defaultValue;

        public BoolSettingEntry(string key, string displayName, string description, bool defaultValue)
            : base(key, displayName, description, SettingType.Bool)
        {
            _defaultValue = defaultValue;
            _value = defaultValue;
        }

        public override object GetValue() => _value;
        public override object GetDefaultValue() => _defaultValue;

        public override void SetValue(object value)
        {
            if (value is bool boolValue)
            {
                SetBoolValue(boolValue);
            }
        }

        public void SetBoolValue(bool value)
        {
            if (_value == value) return;
            
            _value = value;
            NotifyValueChanged();
        }

        public override void ResetToDefault()
        {
            SetBoolValue(_defaultValue);
        }

        public override void LoadFromPlayerPrefs()
        {
            _value = PlayerPrefs.GetInt(_key, _defaultValue ? 1 : 0) == 1;
        }

        public override void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetInt(_key, _value ? 1 : 0);
        }
    }

    [Serializable]
    public class IntSettingEntry : BaseSettingEntry
    {
        [SerializeField] private int _value;
        [SerializeField] private int _defaultValue;
        [SerializeField] private int _minValue;
        [SerializeField] private int _maxValue;

        public int Value => _value;
        public int DefaultValue => _defaultValue;
        public int MinValue => _minValue;
        public int MaxValue => _maxValue;

        public IntSettingEntry(string key, string displayName, string description, 
                               int defaultValue, int minValue = 0, int maxValue = 100)
            : base(key, displayName, description, SettingType.Int)
        {
            _defaultValue = defaultValue;
            _value = defaultValue;
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public override object GetValue() => _value;
        public override object GetDefaultValue() => _defaultValue;

        public override void SetValue(object value)
        {
            if (value is int intValue)
            {
                SetIntValue(intValue);
            }
        }

        public void SetIntValue(int value)
        {
            int clampedValue = Mathf.Clamp(value, _minValue, _maxValue);
            if (_value == clampedValue) return;
            
            _value = clampedValue;
            NotifyValueChanged();
        }

        public override void ResetToDefault()
        {
            SetIntValue(_defaultValue);
        }

        public override void LoadFromPlayerPrefs()
        {
            _value = PlayerPrefs.GetInt(_key, _defaultValue);
        }

        public override void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetInt(_key, _value);
        }
    }

    [Serializable]
    public class StringSettingEntry : BaseSettingEntry
    {
        [SerializeField] private string _value;
        [SerializeField] private string _defaultValue;

        public string Value => _value;
        public string DefaultValue => _defaultValue;

        public StringSettingEntry(string key, string displayName, string description, string defaultValue)
            : base(key, displayName, description, SettingType.String)
        {
            _defaultValue = defaultValue;
            _value = defaultValue;
        }

        public override object GetValue() => _value;
        public override object GetDefaultValue() => _defaultValue;

        public override void SetValue(object value)
        {
            if (value is string stringValue)
            {
                SetStringValue(stringValue);
            }
        }

        public void SetStringValue(string value)
        {
            if (_value == value) return;
            
            _value = value;
            NotifyValueChanged();
        }

        public override void ResetToDefault()
        {
            SetStringValue(_defaultValue);
        }

        public override void LoadFromPlayerPrefs()
        {
            _value = PlayerPrefs.GetString(_key, _defaultValue);
        }

        public override void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetString(_key, _value);
        }
    }
}