using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectVG.Core.Utils;

namespace ProjectVG.Core.Settings
{
    public enum SettingCategory
    {
        Audio,
        Graphics,
        Gameplay,
        Accessibility
    }

    [Serializable]
    public class SettingGroup
    {
        [SerializeField] private SettingCategory _category;
        [SerializeField] private string _displayName;
        [SerializeField] private List<BaseSettingEntry> _settings = new List<BaseSettingEntry>();

        public SettingCategory Category => _category;
        public string DisplayName => _displayName;
        public IReadOnlyList<BaseSettingEntry> Settings => _settings;

        public SettingGroup(SettingCategory category, string displayName)
        {
            _category = category;
            _displayName = displayName;
        }

        public void AddSetting(BaseSettingEntry setting)
        {
            if (!_settings.Contains(setting))
            {
                _settings.Add(setting);
            }
        }

        public T GetSetting<T>(string key) where T : BaseSettingEntry
        {
            return _settings.FirstOrDefault(s => s.Key == key) as T;
        }
    }

    public class SettingsManager : Singleton<SettingsManager>
    {
        private Dictionary<SettingCategory, SettingGroup> _settingGroups = new Dictionary<SettingCategory, SettingGroup>();
        private bool _isInitialized = false;
        private bool _hasUnsavedChanges = false;

        public bool IsInitialized => _isInitialized;
        public bool HasUnsavedChanges => _hasUnsavedChanges;

        public event Action OnSettingsLoaded;
        public event Action<SettingCategory> OnCategorySettingsChanged;
        public event Action OnUnsavedChangesChanged;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            CreateDefaultSettings();
            LoadAllSettings();
            
            _isInitialized = true;
            OnSettingsLoaded?.Invoke();
            
            Debug.Log("[SettingsManager] 초기화 완료");
        }

        private void CreateDefaultSettings()
        {
            CreateAudioSettings();
            CreateGraphicsSettings();
            CreateGameplaySettings();
            CreateAccessibilitySettings();
        }

        private void CreateAudioSettings()
        {
            var audioGroup = new SettingGroup(SettingCategory.Audio, "오디오");
            
            var masterVolume = new FloatSettingEntry("Audio_MasterVolume", "마스터 볼륨", "전체 오디오 볼륨", 1.0f, 0f, 1f);
            var bgmVolume = new FloatSettingEntry("Audio_BGMVolume", "배경음악 볼륨", "배경음악 볼륨", 1.0f, 0f, 1f);
            var sfxVolume = new FloatSettingEntry("Audio_SFXVolume", "효과음 볼륨", "효과음 볼륨", 1.0f, 0f, 1f);
            var voiceVolume = new FloatSettingEntry("Audio_VoiceVolume", "음성 볼륨", "음성 볼륨", 1.0f, 0f, 1f);
            var uiVolume = new FloatSettingEntry("Audio_UIVolume", "UI 음성 볼륨", "UI 효과음 볼륨", 1.0f, 0f, 1f);

            audioGroup.AddSetting(masterVolume);
            audioGroup.AddSetting(bgmVolume);
            audioGroup.AddSetting(sfxVolume);
            audioGroup.AddSetting(voiceVolume);
            audioGroup.AddSetting(uiVolume);

            _settingGroups[SettingCategory.Audio] = audioGroup;

            // 설정 변경 이벤트 구독
            SubscribeToSettingEvents(audioGroup);
        }

        private void CreateGraphicsSettings()
        {
            var graphicsGroup = new SettingGroup(SettingCategory.Graphics, "그래픽");

            var resolutionWidth = new IntSettingEntry("Graphics_ResolutionWidth", "해상도 너비", "화면 너비", Screen.width, 800, 3840);
            var resolutionHeight = new IntSettingEntry("Graphics_ResolutionHeight", "해상도 높이", "화면 높이", Screen.height, 600, 2160);
            var fullscreen = new BoolSettingEntry("Graphics_Fullscreen", "전체화면", "전체화면 모드", true);
            var vsync = new BoolSettingEntry("Graphics_VSync", "수직 동기화", "화면 깜빡임 방지", true);
            var targetFrameRate = new IntSettingEntry("Graphics_TargetFrameRate", "목표 프레임레이트", "초당 프레임 수", 60, 30, 120);

            graphicsGroup.AddSetting(resolutionWidth);
            graphicsGroup.AddSetting(resolutionHeight);
            graphicsGroup.AddSetting(fullscreen);
            graphicsGroup.AddSetting(vsync);
            graphicsGroup.AddSetting(targetFrameRate);

            _settingGroups[SettingCategory.Graphics] = graphicsGroup;
            SubscribeToSettingEvents(graphicsGroup);
        }

        private void CreateGameplaySettings()
        {
            var gameplayGroup = new SettingGroup(SettingCategory.Gameplay, "게임플레이");

            var language = new StringSettingEntry("Gameplay_Language", "언어", "게임 언어", "Korean");
            var autoSave = new BoolSettingEntry("Gameplay_AutoSave", "자동 저장", "자동으로 진행상황 저장", true);

            gameplayGroup.AddSetting(language);
            gameplayGroup.AddSetting(autoSave);

            _settingGroups[SettingCategory.Gameplay] = gameplayGroup;
            SubscribeToSettingEvents(gameplayGroup);
        }

        private void CreateAccessibilitySettings()
        {
            var accessibilityGroup = new SettingGroup(SettingCategory.Accessibility, "접근성");

            var fontSize = new FloatSettingEntry("Accessibility_FontSize", "폰트 크기", "텍스트 크기 배율", 1.0f, 0.8f, 1.5f);
            var highContrast = new BoolSettingEntry("Accessibility_HighContrast", "높은 대비", "색상 대비 향상", false);

            accessibilityGroup.AddSetting(fontSize);
            accessibilityGroup.AddSetting(highContrast);

            _settingGroups[SettingCategory.Accessibility] = accessibilityGroup;
            SubscribeToSettingEvents(accessibilityGroup);
        }

        private void SubscribeToSettingEvents(SettingGroup group)
        {
            foreach (var setting in group.Settings)
            {
                setting.OnValueChanged += OnSettingValueChanged;
            }
        }

        private void OnSettingValueChanged(BaseSettingEntry setting)
        {
            _hasUnsavedChanges = true;
            OnUnsavedChangesChanged?.Invoke();

            // 해당 카테고리의 설정 변경 알림
            var category = GetCategoryForSetting(setting);
            OnCategorySettingsChanged?.Invoke(category);

            Debug.Log($"[SettingsManager] 설정 변경됨: {setting.Key} = {setting.GetValue()}");
        }

        private SettingCategory GetCategoryForSetting(BaseSettingEntry setting)
        {
            foreach (var kvp in _settingGroups)
            {
                if (kvp.Value.Settings.Contains(setting))
                {
                    return kvp.Key;
                }
            }
            return SettingCategory.Gameplay;
        }

        public void LoadAllSettings()
        {
            foreach (var group in _settingGroups.Values)
            {
                foreach (var setting in group.Settings)
                {
                    setting.LoadFromPlayerPrefs();
                }
            }

            _hasUnsavedChanges = false;
            OnUnsavedChangesChanged?.Invoke();
            
            Debug.Log("[SettingsManager] 모든 설정 로드 완료");
        }

        public void SaveAllSettings()
        {
            foreach (var group in _settingGroups.Values)
            {
                foreach (var setting in group.Settings)
                {
                    setting.SaveToPlayerPrefs();
                }
            }

            PlayerPrefs.Save();
            _hasUnsavedChanges = false;
            OnUnsavedChangesChanged?.Invoke();
            
            Debug.Log("[SettingsManager] 모든 설정 저장 완료");
        }

        public void ResetCategoryToDefaults(SettingCategory category)
        {
            if (_settingGroups.TryGetValue(category, out var group))
            {
                foreach (var setting in group.Settings)
                {
                    setting.ResetToDefault();
                }
                Debug.Log($"[SettingsManager] {category} 설정을 기본값으로 재설정");
            }
        }

        public void ResetAllToDefaults()
        {
            foreach (var group in _settingGroups.Values)
            {
                foreach (var setting in group.Settings)
                {
                    setting.ResetToDefault();
                }
            }
            Debug.Log("[SettingsManager] 모든 설정을 기본값으로 재설정");
        }

        public SettingGroup GetSettingGroup(SettingCategory category)
        {
            return _settingGroups.TryGetValue(category, out var group) ? group : null;
        }

        public T GetSetting<T>(SettingCategory category, string key) where T : BaseSettingEntry
        {
            var group = GetSettingGroup(category);
            return group?.GetSetting<T>(key);
        }

        public T GetSetting<T>(string key) where T : BaseSettingEntry
        {
            foreach (var group in _settingGroups.Values)
            {
                var setting = group.GetSetting<T>(key);
                if (setting != null)
                    return setting;
            }
            return null;
        }

        public IEnumerable<SettingGroup> GetAllGroups()
        {
            return _settingGroups.Values;
        }

        public float GetFloatSetting(string key, float defaultValue = 0f)
        {
            var setting = GetSetting<FloatSettingEntry>(key);
            return setting?.Value ?? defaultValue;
        }

        public bool GetBoolSetting(string key, bool defaultValue = false)
        {
            var setting = GetSetting<BoolSettingEntry>(key);
            return setting?.Value ?? defaultValue;
        }

        public int GetIntSetting(string key, int defaultValue = 0)
        {
            var setting = GetSetting<IntSettingEntry>(key);
            return setting?.Value ?? defaultValue;
        }

        public string GetStringSetting(string key, string defaultValue = "")
        {
            var setting = GetSetting<StringSettingEntry>(key);
            return setting?.Value ?? defaultValue;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _hasUnsavedChanges)
            {
                SaveAllSettings();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _hasUnsavedChanges)
            {
                SaveAllSettings();
            }
        }

        private void OnDestroy()
        {
            if (_hasUnsavedChanges)
            {
                SaveAllSettings();
            }
        }
    }
}