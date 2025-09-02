using UnityEngine;
using ProjectVG.Core.Utils;
using ProjectVG.Core.Audio;
using ProjectVG.Core.Input;

namespace ProjectVG.Core.Settings
{
    /// <summary>
    /// 설정 시스템 전체를 통합 관리하는 매니저
    /// 모든 설정 관련 컴포넌트들의 초기화 순서와 상호작용을 조율합니다.
    /// </summary>
    public class SettingsSystemManager : Singleton<SettingsSystemManager>
    {
        [Header("System References")]
        [SerializeField] private SettingsManager _settingsManager;
        [SerializeField] private PauseMenuManager _pauseMenuManager;
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private PlatformInputManager _inputManager;
        [SerializeField] private AudioSettingsIntegration _audioIntegration;

        [Header("Initialization Settings")]
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private float _initializationDelay = 0.5f;

        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugLogs = true;

        private bool _isInitialized = false;
        private int _initializedComponentCount = 0;
        private readonly int _totalComponentCount = 5; // 관리하는 컴포넌트 총 개수

        protected override void Awake()
        {
            base.Awake();
            
            if (_autoInitialize)
            {
                Invoke(nameof(InitializeSystem), _initializationDelay);
            }
        }

        private void Start()
        {
            if (!_autoInitialize)
            {
                InitializeSystem();
            }
        }

        public void InitializeSystem()
        {
            if (_isInitialized)
            {
                DebugLog("시스템이 이미 초기화되었습니다.");
                return;
            }

            DebugLog("설정 시스템 초기화 시작...");

            // 1단계: 코어 매니저들 찾기 및 초기화
            FindAndInitializeManagers();

            // 2단계: 통합 컴포넌트들 초기화
            InitializeIntegrationComponents();

            // 3단계: 이벤트 연결
            SetupSystemEvents();

            // 4단계: 초기 설정 적용
            ApplyInitialSettings();

            _isInitialized = true;
            DebugLog("설정 시스템 초기화 완료!");
        }

        private void FindAndInitializeManagers()
        {
            // SettingsManager 찾기 및 초기화
            if (_settingsManager == null)
                _settingsManager = SettingsManager.Instance;
            CheckComponentInitialization(_settingsManager, "SettingsManager");

            // PauseMenuManager 찾기 및 초기화  
            if (_pauseMenuManager == null)
                _pauseMenuManager = PauseMenuManager.Instance;
            CheckComponentInitialization(_pauseMenuManager, "PauseMenuManager");

            // AudioManager 찾기 및 초기화
            if (_audioManager == null)
                _audioManager = AudioManager.Instance;
            CheckComponentInitialization(_audioManager, "AudioManager");

            // PlatformInputManager 찾기 및 초기화
            if (_inputManager == null)
                _inputManager = PlatformInputManager.Instance;
            CheckComponentInitialization(_inputManager, "PlatformInputManager");
        }

        private void InitializeIntegrationComponents()
        {
            // AudioSettingsIntegration 찾기 및 초기화
            if (_audioIntegration == null)
                _audioIntegration = AudioSettingsIntegration.Instance;
            CheckComponentInitialization(_audioIntegration, "AudioSettingsIntegration");
        }

        private void CheckComponentInitialization(MonoBehaviour component, string componentName)
        {
            if (component != null)
            {
                _initializedComponentCount++;
                DebugLog($"{componentName} 초기화 완료 ({_initializedComponentCount}/{_totalComponentCount})");
            }
            else
            {
                Debug.LogWarning($"[SettingsSystemManager] {componentName}를 찾을 수 없습니다!");
            }
        }

        private void SetupSystemEvents()
        {
            if (_settingsManager != null)
            {
                _settingsManager.OnSettingsLoaded += OnSettingsLoaded;
                _settingsManager.OnUnsavedChangesChanged += OnUnsavedChangesChanged;
            }

            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.OnPauseStateChanged += OnPauseStateChanged;
                _pauseMenuManager.OnSettingsStateChanged += OnSettingsStateChanged;
            }

            if (_inputManager != null)
            {
                // 입력 이벤트는 PauseMenuManager가 직접 처리하므로 여기서는 설정하지 않음
            }

            DebugLog("시스템 이벤트 연결 완료");
        }

        private void ApplyInitialSettings()
        {
            // 초기 설정 적용
            if (_audioIntegration != null)
            {
                _audioIntegration.ApplyAudioSettings();
            }

            // 그래픽 설정 적용 (추후 구현)
            ApplyGraphicsSettings();

            DebugLog("초기 설정 적용 완료");
        }

        private void ApplyGraphicsSettings()
        {
            if (_settingsManager == null) return;

            var graphicsGroup = _settingsManager.GetSettingGroup(SettingCategory.Graphics);
            if (graphicsGroup == null) return;

            // 해상도 설정
            var widthSetting = graphicsGroup.GetSetting<IntSettingEntry>("Graphics_ResolutionWidth");
            var heightSetting = graphicsGroup.GetSetting<IntSettingEntry>("Graphics_ResolutionHeight");
            var fullscreenSetting = graphicsGroup.GetSetting<BoolSettingEntry>("Graphics_Fullscreen");

            if (widthSetting != null && heightSetting != null && fullscreenSetting != null)
            {
                Screen.SetResolution(widthSetting.Value, heightSetting.Value, fullscreenSetting.Value);
                DebugLog($"해상도 설정 적용: {widthSetting.Value}x{heightSetting.Value}, 전체화면: {fullscreenSetting.Value}");
            }

            // VSync 설정
            var vsyncSetting = graphicsGroup.GetSetting<BoolSettingEntry>("Graphics_VSync");
            if (vsyncSetting != null)
            {
                QualitySettings.vSyncCount = vsyncSetting.Value ? 1 : 0;
                DebugLog($"VSync 설정 적용: {vsyncSetting.Value}");
            }

            // 목표 프레임레이트 설정
            var frameRateSetting = graphicsGroup.GetSetting<IntSettingEntry>("Graphics_TargetFrameRate");
            if (frameRateSetting != null)
            {
                Application.targetFrameRate = frameRateSetting.Value;
                DebugLog($"목표 프레임레이트 설정 적용: {frameRateSetting.Value}");
            }
        }

        // 이벤트 핸들러들
        private void OnSettingsLoaded()
        {
            DebugLog("설정 로드 완료 이벤트 수신");
            ApplyInitialSettings();
        }

        private void OnUnsavedChangesChanged()
        {
            bool hasChanges = _settingsManager?.HasUnsavedChanges ?? false;
            DebugLog($"저장되지 않은 변경사항: {hasChanges}");
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            DebugLog($"일시정지 상태 변경: {isPaused}");
            
            // 일시정지 상태에 따른 추가 처리 가능
            if (isPaused)
            {
                // 일시정지 시 필요한 작업
            }
            else
            {
                // 재개 시 필요한 작업
            }
        }

        private void OnSettingsStateChanged(bool isSettingsOpen)
        {
            DebugLog($"설정 메뉴 상태 변경: {isSettingsOpen}");
        }

        // 공용 메서드들
        public void SaveAllSettings()
        {
            if (_settingsManager != null)
            {
                _settingsManager.SaveAllSettings();
                DebugLog("모든 설정 저장 완료");
            }
        }

        public void ResetAllSettings()
        {
            if (_settingsManager != null)
            {
                _settingsManager.ResetAllToDefaults();
                ApplyInitialSettings();
                DebugLog("모든 설정 초기화 완료");
            }
        }

        public void TogglePauseMenu()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.TogglePause();
            }
        }

        // 시스템 상태 확인
        public bool IsSystemReady()
        {
            return _isInitialized && _initializedComponentCount >= _totalComponentCount;
        }

        public SystemHealthInfo GetSystemHealthInfo()
        {
            return new SystemHealthInfo
            {
                IsInitialized = _isInitialized,
                InitializedComponents = _initializedComponentCount,
                TotalComponents = _totalComponentCount,
                SettingsManagerReady = _settingsManager != null && _settingsManager.IsInitialized,
                AudioManagerReady = _audioManager != null && _audioManager.IsInitialized,
                PauseMenuReady = _pauseMenuManager != null,
                InputManagerReady = _inputManager != null,
                AudioIntegrationReady = _audioIntegration != null
            };
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[SettingsSystemManager] {message}");
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_settingsManager != null)
            {
                _settingsManager.OnSettingsLoaded -= OnSettingsLoaded;
                _settingsManager.OnUnsavedChangesChanged -= OnUnsavedChangesChanged;
            }

            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.OnPauseStateChanged -= OnPauseStateChanged;
                _pauseMenuManager.OnSettingsStateChanged -= OnSettingsStateChanged;
            }

            DebugLog("시스템 매니저 정리 완료");
        }

        // 개발자용 디버그 메서드들
        [ContextMenu("Initialize System")]
        private void DebugInitializeSystem()
        {
            InitializeSystem();
        }

        [ContextMenu("Save All Settings")]
        private void DebugSaveSettings()
        {
            SaveAllSettings();
        }

        [ContextMenu("Reset All Settings")]
        private void DebugResetSettings()
        {
            ResetAllSettings();
        }

        [ContextMenu("Show System Health")]
        private void DebugShowSystemHealth()
        {
            var health = GetSystemHealthInfo();
            Debug.Log($"시스템 상태:\n{health}");
        }

        [System.Serializable]
        public struct SystemHealthInfo
        {
            public bool IsInitialized;
            public int InitializedComponents;
            public int TotalComponents;
            public bool SettingsManagerReady;
            public bool AudioManagerReady;
            public bool PauseMenuReady;
            public bool InputManagerReady;
            public bool AudioIntegrationReady;

            public override string ToString()
            {
                return $"초기화됨: {IsInitialized}\n" +
                       $"컴포넌트: {InitializedComponents}/{TotalComponents}\n" +
                       $"SettingsManager: {SettingsManagerReady}\n" +
                       $"AudioManager: {AudioManagerReady}\n" +
                       $"PauseMenu: {PauseMenuReady}\n" +
                       $"InputManager: {InputManagerReady}\n" +
                       $"AudioIntegration: {AudioIntegrationReady}";
            }
        }
    }
}