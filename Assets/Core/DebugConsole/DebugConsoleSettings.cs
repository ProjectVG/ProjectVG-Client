using UnityEngine;

namespace ProjectVG.Core.Utils
{
    [CreateAssetMenu(fileName = "DebugConsoleSettings", menuName = "ProjectVG/Debug Console Settings")]
    public class DebugConsoleSettings : ScriptableObject
    {
        [Header("Console Settings")]
        [SerializeField] private int _maxLogLines = 1000;
        [SerializeField] private bool _autoScroll = true;
        [SerializeField] private bool _showTimestamp = true;
        [SerializeField] private bool _showLogType = true;
        [SerializeField] private bool _autoClearOldLogs = true;
        [SerializeField] private float _logRetentionTime = 300f;
        [SerializeField] private int _maxVisibleLogs = 500;
        [SerializeField] private bool _logInBackground = true;
        
        [Header("Input Settings")]
        [SerializeField] private bool _enableMobileInput = true;
        [SerializeField] private int _mobileTouchCount = 3;
        
        [Header("Pooling Settings")]
        [SerializeField] private int _poolSize = 100;
        [SerializeField] private bool _useObjectPooling = true;
        [SerializeField] private bool _initializePoolOnStart = false;
        
        [Header("Filter Settings")]
        [SerializeField] private bool _enableFiltering = true;
        [SerializeField] private string _defaultFilterKeyword = "";
        
        [Header("UI Settings")]
        [SerializeField] private float _fontSize = 12f;
        [SerializeField] private Color _logColor = Color.white;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;
        
        // Properties
        public int MaxLogLines => _maxLogLines;
        public bool AutoScroll => _autoScroll;
        public bool ShowTimestamp => _showTimestamp;
        public bool ShowLogType => _showLogType;
        public bool AutoClearOldLogs => _autoClearOldLogs;
        public float LogRetentionTime => _logRetentionTime;
        public int MaxVisibleLogs => _maxVisibleLogs;
        public bool LogInBackground => _logInBackground;
        public bool EnableFiltering => _enableFiltering;
        public string DefaultFilterKeyword => _defaultFilterKeyword;
        public float FontSize => _fontSize;
        public Color LogColor => _logColor;
        public Color WarningColor => _warningColor;
        public Color ErrorColor => _errorColor;
        public int PoolSize => _poolSize;
        public bool UseObjectPooling => _useObjectPooling;
        public bool InitializePoolOnStart => _initializePoolOnStart;
        public bool EnableMobileInput => _enableMobileInput;
        public int MobileTouchCount => _mobileTouchCount;
    }
} 