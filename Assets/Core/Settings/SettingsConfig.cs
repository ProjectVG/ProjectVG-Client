using System.Collections.Generic;
using UnityEngine;

namespace ProjectVG.Core.Settings
{
    [CreateAssetMenu(fileName = "SettingsConfig", menuName = "ProjectVG/Settings/Settings Config")]
    public class SettingsConfig : ScriptableObject
    {
        [Header("Audio Settings")]
        public AudioSettingsData audioSettings;

        [Header("Graphics Settings")]
        public GraphicsSettingsData graphicsSettings;

        [Header("Gameplay Settings")]
        public GameplaySettingsData gameplaySettings;

        [Header("Accessibility Settings")]
        public AccessibilitySettingsData accessibilitySettings;

        private void OnValidate()
        {
            if (audioSettings == null)
                audioSettings = new AudioSettingsData();
            if (graphicsSettings == null)
                graphicsSettings = new GraphicsSettingsData();
            if (gameplaySettings == null)
                gameplaySettings = new GameplaySettingsData();
            if (accessibilitySettings == null)
                accessibilitySettings = new AccessibilitySettingsData();
        }
    }

    [System.Serializable]
    public class AudioSettingsData
    {
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float defaultMasterVolume = 1.0f;
        [Range(0f, 1f)] public float defaultBGMVolume = 1.0f;
        [Range(0f, 1f)] public float defaultSFXVolume = 1.0f;
        [Range(0f, 1f)] public float defaultVoiceVolume = 1.0f;
        [Range(0f, 1f)] public float defaultUIVolume = 1.0f;

        [Header("Volume Ranges")]
        [Range(0f, 1f)] public float minVolume = 0.0f;
        [Range(0f, 1f)] public float maxVolume = 1.0f;

        [Header("Audio Features")]
        public bool enableAudioMixerGroups = true;
        public bool enableVolumeNormalization = true;
    }

    [System.Serializable]
    public class GraphicsSettingsData
    {
        [Header("Resolution Settings")]
        public Vector2Int defaultResolution = new Vector2Int(1920, 1080);
        public bool defaultFullscreen = true;
        public List<Vector2Int> supportedResolutions = new List<Vector2Int>
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160)
        };

        [Header("Performance Settings")]
        public bool defaultVSync = true;
        public int defaultTargetFrameRate = 60;
        public List<int> supportedFrameRates = new List<int> { 30, 60, 90, 120 };

        [Header("Quality Settings")]
        public int defaultQualityLevel = 2;
        public bool enableAntiAliasing = true;
        public bool enableHDR = false;
    }

    [System.Serializable]
    public class GameplaySettingsData
    {
        [Header("Language Settings")]
        public string defaultLanguage = "Korean";
        public List<string> supportedLanguages = new List<string> { "Korean", "English", "Japanese" };

        [Header("Game Features")]
        public bool defaultAutoSave = true;
        public bool defaultShowTutorial = true;
        public bool defaultShowSubtitles = true;

        [Header("Input Settings")]
        public float defaultInputSensitivity = 1.0f;
        [Range(0.1f, 3.0f)] public float minSensitivity = 0.1f;
        [Range(0.1f, 3.0f)] public float maxSensitivity = 3.0f;
    }

    [System.Serializable]
    public class AccessibilitySettingsData
    {
        [Header("Visual Accessibility")]
        [Range(0.8f, 2.0f)] public float defaultFontSize = 1.0f;
        [Range(0.8f, 2.0f)] public float minFontSize = 0.8f;
        [Range(0.8f, 2.0f)] public float maxFontSize = 2.0f;

        public bool defaultHighContrast = false;
        public bool defaultColorBlindSupport = false;

        [Header("Audio Accessibility")]
        public bool defaultClosedCaptions = false;
        public bool defaultAudioDescriptions = false;

        [Header("Interaction Accessibility")]
        public bool defaultReducedMotion = false;
        public float defaultHoldDuration = 0.5f;
        [Range(0.1f, 2.0f)] public float minHoldDuration = 0.1f;
        [Range(0.1f, 2.0f)] public float maxHoldDuration = 2.0f;
    }

    // 설정 프리셋 데이터
    [System.Serializable]
    public class SettingsPreset
    {
        public string presetName;
        public string description;
        public AudioSettingsData audioPreset;
        public GraphicsSettingsData graphicsPreset;
        public GameplaySettingsData gameplayPreset;
        public AccessibilitySettingsData accessibilityPreset;
    }

    [CreateAssetMenu(fileName = "SettingsPresets", menuName = "ProjectVG/Settings/Settings Presets")]
    public class SettingsPresetsConfig : ScriptableObject
    {
        [Header("Predefined Presets")]
        public List<SettingsPreset> presets = new List<SettingsPreset>();

        [Header("Default Presets")]
        public SettingsPreset lowPerformancePreset;
        public SettingsPreset balancedPreset;
        public SettingsPreset highPerformancePreset;
        public SettingsPreset accessibilityPreset;

        private void OnValidate()
        {
            // 기본 프리셋이 없으면 생성
            if (lowPerformancePreset == null)
                lowPerformancePreset = CreateLowPerformancePreset();
            if (balancedPreset == null)
                balancedPreset = CreateBalancedPreset();
            if (highPerformancePreset == null)
                highPerformancePreset = CreateHighPerformancePreset();
            if (accessibilityPreset == null)
                accessibilityPreset = CreateAccessibilityPreset();
        }

        private SettingsPreset CreateLowPerformancePreset()
        {
            return new SettingsPreset
            {
                presetName = "저성능",
                description = "저사양 기기에 최적화된 설정",
                graphicsPreset = new GraphicsSettingsData
                {
                    defaultResolution = new Vector2Int(1280, 720),
                    defaultFullscreen = true,
                    defaultVSync = false,
                    defaultTargetFrameRate = 30,
                    defaultQualityLevel = 0,
                    enableAntiAliasing = false,
                    enableHDR = false
                }
            };
        }

        private SettingsPreset CreateBalancedPreset()
        {
            return new SettingsPreset
            {
                presetName = "균형",
                description = "성능과 품질의 균형을 맞춘 설정",
                graphicsPreset = new GraphicsSettingsData
                {
                    defaultResolution = new Vector2Int(1920, 1080),
                    defaultFullscreen = true,
                    defaultVSync = true,
                    defaultTargetFrameRate = 60,
                    defaultQualityLevel = 2,
                    enableAntiAliasing = true,
                    enableHDR = false
                }
            };
        }

        private SettingsPreset CreateHighPerformancePreset()
        {
            return new SettingsPreset
            {
                presetName = "고성능",
                description = "최고 품질의 그래픽 설정",
                graphicsPreset = new GraphicsSettingsData
                {
                    defaultResolution = new Vector2Int(2560, 1440),
                    defaultFullscreen = true,
                    defaultVSync = true,
                    defaultTargetFrameRate = 60,
                    defaultQualityLevel = 5,
                    enableAntiAliasing = true,
                    enableHDR = true
                }
            };
        }

        private SettingsPreset CreateAccessibilityPreset()
        {
            return new SettingsPreset
            {
                presetName = "접근성",
                description = "접근성을 향상시킨 설정",
                accessibilityPreset = new AccessibilitySettingsData
                {
                    defaultFontSize = 1.5f,
                    defaultHighContrast = true,
                    defaultColorBlindSupport = true,
                    defaultClosedCaptions = true,
                    defaultAudioDescriptions = true,
                    defaultReducedMotion = true,
                    defaultHoldDuration = 1.0f
                }
            };
        }
    }
}