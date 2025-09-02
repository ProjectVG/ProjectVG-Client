using UnityEngine;
using ProjectVG.Core.Audio;
using ProjectVG.Core.Utils;

namespace ProjectVG.Core.Settings
{
    public class AudioSettingsIntegration : Singleton<AudioSettingsIntegration>
    {
        private SettingsManager _settingsManager;
        private AudioManager _audioManager;
        private bool _isInitialized = false;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _settingsManager = SettingsManager.Instance;
            _audioManager = AudioManager.Instance;

            if (_settingsManager == null)
            {
                Debug.LogWarning("[AudioSettingsIntegration] SettingsManager를 찾을 수 없습니다.");
                return;
            }

            if (_audioManager == null)
            {
                Debug.LogWarning("[AudioSettingsIntegration] AudioManager를 찾을 수 없습니다.");
                return;
            }

            // SettingsManager 초기화 대기
            if (_settingsManager.IsInitialized)
            {
                SetupIntegration();
            }
            else
            {
                _settingsManager.OnSettingsLoaded += SetupIntegration;
            }

            // AudioManager 초기화 대기
            if (_audioManager.IsInitialized)
            {
                ApplyCurrentAudioSettings();
            }
            else
            {
                _audioManager.OnInitialized += ApplyCurrentAudioSettings;
            }

            _isInitialized = true;
            Debug.Log("[AudioSettingsIntegration] 초기화 완료");
        }

        private void SetupIntegration()
        {
            if (_settingsManager == null) return;

            // 오디오 설정 변경 이벤트 구독
            _settingsManager.OnCategorySettingsChanged += OnCategorySettingsChanged;

            // 개별 오디오 설정 이벤트 구독
            SubscribeToAudioSettings();

            Debug.Log("[AudioSettingsIntegration] 설정 연동 완료");
        }

        private void SubscribeToAudioSettings()
        {
            var audioGroup = _settingsManager.GetSettingGroup(SettingCategory.Audio);
            if (audioGroup == null) return;

            foreach (var setting in audioGroup.Settings)
            {
                setting.OnValueChanged += OnAudioSettingChanged;
            }
        }

        private void OnCategorySettingsChanged(SettingCategory category)
        {
            if (category == SettingCategory.Audio)
            {
                ApplyCurrentAudioSettings();
            }
        }

        private void OnAudioSettingChanged(BaseSettingEntry setting)
        {
            ApplyAudioSetting(setting);
        }

        private void ApplyCurrentAudioSettings()
        {
            if (_settingsManager == null || _audioManager == null) return;

            // 모든 오디오 설정 적용
            var audioGroup = _settingsManager.GetSettingGroup(SettingCategory.Audio);
            if (audioGroup == null) return;

            foreach (var setting in audioGroup.Settings)
            {
                ApplyAudioSetting(setting);
            }

            Debug.Log("[AudioSettingsIntegration] 현재 오디오 설정 적용 완료");
        }

        private void ApplyAudioSetting(BaseSettingEntry setting)
        {
            if (_audioManager == null || setting == null) return;

            try
            {
                switch (setting.Key)
                {
                    case "Audio_MasterVolume":
                        if (setting is FloatSettingEntry masterVolume)
                        {
                            _audioManager.SetMasterVolume(masterVolume.Value);
                        }
                        break;

                    case "Audio_BGMVolume":
                        if (setting is FloatSettingEntry bgmVolume)
                        {
                            _audioManager.SetBGMVolume(bgmVolume.Value);
                        }
                        break;

                    case "Audio_SFXVolume":
                        if (setting is FloatSettingEntry sfxVolume)
                        {
                            _audioManager.SetSFXVolume(sfxVolume.Value);
                        }
                        break;

                    case "Audio_VoiceVolume":
                        if (setting is FloatSettingEntry voiceVolume)
                        {
                            _audioManager.SetVoiceVolume(voiceVolume.Value);
                        }
                        break;

                    case "Audio_UIVolume":
                        if (setting is FloatSettingEntry uiVolume)
                        {
                            _audioManager.SetUIVolume(uiVolume.Value);
                        }
                        break;

                    default:
                        Debug.LogWarning($"[AudioSettingsIntegration] 알 수 없는 오디오 설정: {setting.Key}");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AudioSettingsIntegration] 오디오 설정 적용 실패 ({setting.Key}): {ex.Message}");
            }
        }

        // AudioManager의 볼륨 변경을 설정 시스템에 반영
        private void SyncAudioManagerToSettings()
        {
            if (_settingsManager == null || _audioManager == null) return;

            var audioGroup = _settingsManager.GetSettingGroup(SettingCategory.Audio);
            if (audioGroup == null) return;

            // AudioManager의 현재 볼륨을 설정에 동기화
            var masterVolumeSetting = audioGroup.GetSetting<FloatSettingEntry>("Audio_MasterVolume");
            if (masterVolumeSetting != null && !Mathf.Approximately(masterVolumeSetting.Value, _audioManager.MasterVolume))
            {
                masterVolumeSetting.SetFloatValue(_audioManager.MasterVolume);
            }

            var bgmVolumeSetting = audioGroup.GetSetting<FloatSettingEntry>("Audio_BGMVolume");
            if (bgmVolumeSetting != null && !Mathf.Approximately(bgmVolumeSetting.Value, _audioManager.BgmVolume))
            {
                bgmVolumeSetting.SetFloatValue(_audioManager.BgmVolume);
            }

            var sfxVolumeSetting = audioGroup.GetSetting<FloatSettingEntry>("Audio_SFXVolume");
            if (sfxVolumeSetting != null && !Mathf.Approximately(sfxVolumeSetting.Value, _audioManager.SfxVolume))
            {
                sfxVolumeSetting.SetFloatValue(_audioManager.SfxVolume);
            }

            var voiceVolumeSetting = audioGroup.GetSetting<FloatSettingEntry>("Audio_VoiceVolume");
            if (voiceVolumeSetting != null && !Mathf.Approximately(voiceVolumeSetting.Value, _audioManager.VoiceVolume))
            {
                voiceVolumeSetting.SetFloatValue(_audioManager.VoiceVolume);
            }

            var uiVolumeSetting = audioGroup.GetSetting<FloatSettingEntry>("Audio_UIVolume");
            if (uiVolumeSetting != null && !Mathf.Approximately(uiVolumeSetting.Value, _audioManager.UiVolume))
            {
                uiVolumeSetting.SetFloatValue(_audioManager.UiVolume);
            }
        }

        // 외부에서 AudioManager와 설정 동기화를 요청할 때 사용
        public void SynchronizeWithAudioManager()
        {
            SyncAudioManagerToSettings();
        }

        // 오디오 설정을 즉시 적용
        public void ApplyAudioSettings()
        {
            ApplyCurrentAudioSettings();
        }

        // 특정 오디오 설정 값 가져오기
        public float GetAudioSettingValue(string settingKey, float defaultValue = 1.0f)
        {
            if (_settingsManager == null) return defaultValue;

            var setting = _settingsManager.GetSetting<FloatSettingEntry>(settingKey);
            return setting?.Value ?? defaultValue;
        }

        // 특정 오디오 설정 값 설정하기
        public void SetAudioSettingValue(string settingKey, float value)
        {
            if (_settingsManager == null) return;

            var setting = _settingsManager.GetSetting<FloatSettingEntry>(settingKey);
            setting?.SetFloatValue(value);
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_settingsManager != null)
            {
                _settingsManager.OnSettingsLoaded -= SetupIntegration;
                _settingsManager.OnCategorySettingsChanged -= OnCategorySettingsChanged;

                var audioGroup = _settingsManager.GetSettingGroup(SettingCategory.Audio);
                if (audioGroup != null)
                {
                    foreach (var setting in audioGroup.Settings)
                    {
                        setting.OnValueChanged -= OnAudioSettingChanged;
                    }
                }
            }

            if (_audioManager != null)
            {
                _audioManager.OnInitialized -= ApplyCurrentAudioSettings;
            }
        }

        // 개발자용 디버그 메서드
        [ContextMenu("Apply Audio Settings (Debug)")]
        private void DebugApplyAudioSettings()
        {
            ApplyCurrentAudioSettings();
        }

        [ContextMenu("Sync Audio Manager (Debug)")]
        private void DebugSyncAudioManager()
        {
            SyncAudioManagerToSettings();
        }
    }
}