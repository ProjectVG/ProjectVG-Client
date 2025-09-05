#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectVG.Core.Utils
{
    public class GameDebugConsoleManager : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject? _consolePanel;
        [SerializeField] private ScrollRect? _scrollRect;
        [SerializeField] private Transform? _logContentParent;
        [SerializeField] private GameObject? _logEntryPrefab;

        [Header("Settings")]
        [SerializeField] private DebugConsoleSettings? _settings;
        
        private List<LogEntry> _logEntries = new List<LogEntry>();
        private List<GameObject> _logEntryObjects = new List<GameObject>();
        private Queue<GameObject> _objectPool = new Queue<GameObject>();
        private bool _isConsoleVisible = false;
        
        [System.Serializable]
        public class LogEntry
        {
            public string message;
            public string stackTrace;
            public LogType logType;
            public DateTime timestamp;
            
            public LogEntry(string message, string stackTrace, LogType logType)
            {
                this.message = message;
                this.stackTrace = stackTrace;
                this.logType = logType;
                this.timestamp = DateTime.Now;
            }
        }
        
        void Awake()
        {
            InitializeConsole();
        }
        
        void Start()
        {
            Application.logMessageReceived += OnLogMessageReceived;
            SetupUI();
        }
        
        void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                ToggleConsole();
            }
        }
        
        private void InitializeConsole()
        {
            if (_consolePanel == null)
            {
                return;
            }
            
            _consolePanel.SetActive(false);
            _isConsoleVisible = false;
            
            SetupLayoutGroup();
            
            if (_settings?.UseObjectPooling == true && _settings?.InitializePoolOnStart == true)
            {
                InitializeObjectPool();
            }
        }
        
        private void SetupUI()
        {
            SetupLayoutGroup();
        }
        
        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (logString.Contains("[DEBUG_CONSOLE]") || logString.Contains("[TEST]"))
                return;
            
            if (_settings?.LogInBackground == false && !_isConsoleVisible)
                return;
            
            if (!_isConsoleVisible)
            {
                var logEntry = new LogEntry(logString, "", type);
                _logEntries.Add(logEntry);
                
                if (_settings != null && _logEntries.Count > _settings.MaxLogLines)
                {
                    _logEntries.RemoveAt(0);
                }
                return;
            }
            
            var entry = new LogEntry(logString, stackTrace, type);
            _logEntries.Add(entry);
            
            if (_settings != null && _logEntries.Count > _settings.MaxLogLines)
            {
                _logEntries.RemoveAt(0);
            }
            
            UpdateLogDisplay();
        }
        
        private void UpdateLogDisplay()
        {
            if (_logContentParent == null) return;
            
            ClearLogEntryObjects();
            
            for (int i = 0; i < _logEntries.Count; i++)
            {
                var entry = _logEntries[i];
                CreateLogEntryObject(entry);
            }
            
            if (_settings?.AutoScroll == true && _scrollRect != null)
            {
                StartCoroutine(ScrollToBottomCoroutine());
            }
        }
        
        private void CreateLogEntryObject(LogEntry entry)
        {
            if (_logEntryPrefab == null || _logContentParent == null) return;
            
            GameObject? logEntryObj;
            if (_settings?.UseObjectPooling == true)
            {
                logEntryObj = GetPooledObject();
            }
            else
            {
                logEntryObj = Instantiate(_logEntryPrefab, _logContentParent);
            }
            
            if (logEntryObj == null) return;
            
            logEntryObj.transform.SetAsLastSibling();
            _logEntryObjects.Add(logEntryObj);
            
            var logText = logEntryObj.GetComponentInChildren<TextMeshProUGUI>();
            if (logText != null)
            {
                string timestamp = _settings?.ShowTimestamp == true ? $"[{entry.timestamp:HH:mm:ss}] " : "";
                string logType = _settings?.ShowLogType == true ? $"[{entry.logType}] " : "";
                
                string logMessage = $"{timestamp}{logType}{entry.message}";
                
                if (!string.IsNullOrEmpty(entry.stackTrace))
                {
                    logMessage += $"\nStack Trace: {entry.stackTrace}";
                }
                
                logText.text = logMessage;
                
                if (_settings != null)
                {
                    logText.fontSize = _settings.FontSize;
                }
                
                SetLogEntryColor(logText, entry.logType);
                
                var contentSizeFitter = logEntryObj.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter == null)
                {
                    contentSizeFitter = logEntryObj.AddComponent<ContentSizeFitter>();
                }
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
        
        private void SetLogEntryColor(TextMeshProUGUI logText, LogType logType)
        {
            if (_settings == null) return;
            
            switch (logType)
            {
                case LogType.Log:
                    logText.color = _settings.LogColor;
                    break;
                case LogType.Warning:
                    logText.color = _settings.WarningColor;
                    break;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    logText.color = _settings.ErrorColor;
                    break;
                default:
                    logText.color = _settings.LogColor;
                    break;
            }
        }
        
        private void InitializeObjectPool()
        {
            if (_logEntryPrefab == null || _settings == null) return;
            
            for (int i = 0; i < _settings.PoolSize; i++)
            {
                GameObject pooledObject = Instantiate(_logEntryPrefab, _logContentParent);
                pooledObject.SetActive(false);
                _objectPool.Enqueue(pooledObject);
            }
        }
        
        private GameObject? GetPooledObject()
        {
            if (_objectPool.Count > 0)
            {
                GameObject obj = _objectPool.Dequeue();
                obj.SetActive(true);
                return obj;
            }
            
            if (_logEntryPrefab != null && _logContentParent != null)
            {
                return Instantiate(_logEntryPrefab, _logContentParent);
            }
            
            return null;
        }
        
        private void ReturnToPool(GameObject obj)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                _objectPool.Enqueue(obj);
            }
        }
        
        private void ClearLogEntryObjects()
        {
            if (_settings?.UseObjectPooling == true)
            {
                foreach (var obj in _logEntryObjects)
                {
                    if (obj != null)
                    {
                        ReturnToPool(obj);
                    }
                }
            }
            else
            {
                foreach (var obj in _logEntryObjects)
                {
                    if (obj != null)
                    {
                        DestroyImmediate(obj);
                    }
                }
            }
            
            _logEntryObjects.Clear();
        }
        
        private System.Collections.IEnumerator ScrollToBottomCoroutine()
        {
            yield return null;
            
            Canvas.ForceUpdateCanvases();
            
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 0f;
                
                yield return new WaitForEndOfFrame();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        public void ToggleConsole()
        {
            Debug.Log($"[DEBUG_CONSOLE] ToggleConsole called. Current state: {_isConsoleVisible}");
            
            _isConsoleVisible = !_isConsoleVisible;
            
            if (_consolePanel != null)
            {
                _consolePanel.SetActive(_isConsoleVisible);
                Debug.Log($"[DEBUG_CONSOLE] Console panel set to: {_isConsoleVisible}");
            }
            else
            {
                Debug.LogWarning("[DEBUG_CONSOLE] _consolePanel is null!");
            }
            
            if (_isConsoleVisible)
            {
                if (_settings?.UseObjectPooling == true && _objectPool.Count == 0)
                {
                    InitializeObjectPool();
                }
                
                UpdateLogDisplay();
            }
        }
        
        public void ClearLogs()
        {
            _logEntries.Clear();
            ClearLogEntryObjects();
            
            if (_settings?.UseObjectPooling == true)
            {
                foreach (var pooledObj in _objectPool)
                {
                    if (pooledObj != null)
                    {
                        pooledObj.SetActive(false);
                    }
                }
            }
        }
        
        
        
        public List<LogEntry> GetLogEntries()
        {
            return new List<LogEntry>(_logEntries);
        }
        

        
        public void SetupLayoutGroup()
        {
            if (_logContentParent == null) return;
            
            var verticalLayoutGroup = _logContentParent.GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup == null)
            {
                verticalLayoutGroup = _logContentParent.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            
            verticalLayoutGroup.spacing = 2f;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.padding = new RectOffset(5, 5, 5, 5);
            
            var contentSizeFitter = _logContentParent.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
            {
                contentSizeFitter = _logContentParent.gameObject.AddComponent<ContentSizeFitter>();
            }
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        
    }
} 