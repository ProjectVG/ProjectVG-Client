using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectVG.Core.Utils
{
    public class InGameDebugConsole : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject? _consolePanel;
        [SerializeField] private ScrollRect? _scrollRect;
        [SerializeField] private Transform? _logContentParent;
        [SerializeField] private GameObject? _logEntryPrefab;
        [SerializeField] private Button? _clearButton;
        [SerializeField] private Button? _toggleButton;
        [SerializeField] private TMP_InputField? _filterInput;
        
        [Header("Settings")]
        [SerializeField] private DebugConsoleSettings? _settings;
        
        private string _filterKeyword = "";
        
        private List<LogEntry> _logEntries = new List<LogEntry>();
        private List<GameObject> _logEntryObjects = new List<GameObject>();
        private Queue<GameObject> _objectPool = new Queue<GameObject>();
        private bool _isConsoleVisible = false;
        private float _lastCleanupTime = 0f;
        private bool _isProcessingLogs = false;
        private Coroutine? _backgroundProcessingCoroutine;
        
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
            
            if (_backgroundProcessingCoroutine != null)
            {
                StopCoroutine(_backgroundProcessingCoroutine);
            }
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                ToggleConsole();
            }
            
            if (_settings?.EnableMobileInput == true && Input.touchCount == _settings.MobileTouchCount)
            {
                bool allTouchesBegan = true;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.GetTouch(i).phase != TouchPhase.Began)
                    {
                        allTouchesBegan = false;
                        break;
                    }
                }
                
                if (allTouchesBegan)
                {
                    ToggleConsole();
                }
            }
            
            if (_settings?.AutoClearOldLogs == true && Time.time - _lastCleanupTime > 30f)
            {
                CleanupOldLogs();
                _lastCleanupTime = Time.time;
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
            if (_clearButton != null)
            {
                _clearButton.onClick.AddListener(ClearLogs);
            }
            
            if (_toggleButton != null)
            {
                _toggleButton.onClick.AddListener(ToggleConsole);
            }
            
            if (_filterInput != null)
            {
                _filterInput.onValueChanged.AddListener(OnFilterChanged);
            }
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
            
            if (_backgroundProcessingCoroutine != null)
            {
                StopCoroutine(_backgroundProcessingCoroutine);
            }
            _backgroundProcessingCoroutine = StartCoroutine(ProcessLogsInBackground());
        }
        
        private void UpdateLogDisplay()
        {
            if (_logContentParent == null) return;
                

            if (_isProcessingLogs)
            {
                StartCoroutine(UpdateLogDisplayWhenReady());
                return;
            }
            
            ClearLogEntryObjects();
            
            for (int i = 0; i < _logEntries.Count; i++)
            {
                var entry = _logEntries[i];
                
                if (_settings?.EnableFiltering == true && !string.IsNullOrEmpty(_filterKeyword))
                {
                    if (!entry.message.Contains(_filterKeyword, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                
                CreateLogEntryObject(entry);
            }
            
            if (_settings?.AutoScroll == true && _scrollRect != null)
            {
                StartCoroutine(ScrollToBottomCoroutine());
            }
        }
        
        private System.Collections.IEnumerator UpdateLogDisplayWhenReady()
        {
            while (_isProcessingLogs)
            {
                yield return null;
            }
            
            UpdateLogDisplay();
        }
        
        private System.Collections.IEnumerator ProcessLogsInBackground()
        {
            _isProcessingLogs = true;
            
            yield return new WaitForEndOfFrame();
            
            if (_settings?.AutoClearOldLogs == true)
            {
                CleanupOldLogs();
            }
            
            if (_settings?.UseObjectPooling == true)
            {
                ManageObjectPool();
            }
            
            yield return new WaitForEndOfFrame();
            
            _isProcessingLogs = false;
            
            if (_isConsoleVisible)
            {
                UpdateLogDisplay();
            }
        }
        
        private void ManageObjectPool()
        {
            if (_objectPool.Count > _settings?.PoolSize)
            {
                int excessCount = _objectPool.Count - _settings.PoolSize;
                for (int i = 0; i < excessCount && _objectPool.Count > 0; i++)
                {
                    var obj = _objectPool.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }
        }
        
        private void CreateLogEntryObject(LogEntry entry)
        {
            if (_logEntryPrefab == null || _logContentParent == null) return;
            
            GameObject logEntryObj;
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
        
        private GameObject GetPooledObject()
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
            _isConsoleVisible = !_isConsoleVisible;
            
            if (_consolePanel != null)
            {
                _consolePanel.SetActive(_isConsoleVisible);
            }
            
            if (_isConsoleVisible)
            {
                if (_settings?.UseObjectPooling == true && _objectPool.Count == 0)
                {
                    InitializeObjectPool();
                }
                StartCoroutine(UpdateConsoleWhenReady());
            }
        }
        
        private System.Collections.IEnumerator UpdateConsoleWhenReady()
        {
            while (_isProcessingLogs)
            {
                yield return null;
            }
            
            UpdateLogDisplay();
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
        
        private void OnFilterChanged(string filterText)
        {
            _filterKeyword = filterText;
            if (_isConsoleVisible)
            {
                StartCoroutine(UpdateFilterWhenReady());
            }
        }
        
        public void SetFilter(string keyword)
        {
            _filterKeyword = keyword;
            if (_filterInput != null)
            {
                _filterInput.text = keyword;
            }
            if (_isConsoleVisible)
            {
                StartCoroutine(UpdateFilterWhenReady());
            }
        }
        
        private System.Collections.IEnumerator UpdateFilterWhenReady()
        {
            while (_isProcessingLogs)
            {
                yield return null;
            }
            
            UpdateLogDisplay();
        }
        
        private void CleanupOldLogs()
        {
            if (_settings == null) return;
            
            var cutoffTime = DateTime.Now.AddSeconds(-_settings.LogRetentionTime);
            int removedCount = 0;
            
            for (int i = _logEntries.Count - 1; i >= 0; i--)
            {
                if (_logEntries[i].timestamp < cutoffTime)
                {
                    _logEntries.RemoveAt(i);
                    removedCount++;
                }
            }
            
        }
        
        public List<LogEntry> GetLogEntries()
        {
            return new List<LogEntry>(_logEntries);
        }
        

        public void ScrollToTop()
        {
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }
        
        public void ScrollToBottom()
        {
            if (_scrollRect != null)
            {
                StartCoroutine(ScrollToBottomCoroutine());
            }
        }
        
        public float GetScrollPosition()
        {
            return _scrollRect?.verticalNormalizedPosition ?? 0f;
        }
        
        public bool IsAtBottom()
        {
            if (_scrollRect == null) return false;
            return _scrollRect.verticalNormalizedPosition <= 0.01f;
        }
        
        public void ValidateScrollRect()
        {
            if (_scrollRect == null)
            {
                return;
            }
        }
        
        public void ForceScrollUpdate()
        {
            if (_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        public void ForceEnableScrollbar()
        {
            if (_scrollRect != null)
            {
                _scrollRect.vertical = true;
                
                if (_scrollRect.verticalScrollbar != null)
                {
                    _scrollRect.verticalScrollbar.gameObject.SetActive(true);
                    _scrollRect.verticalScrollbar.interactable = true;
                }
                
                if (_scrollRect.content != null && _scrollRect.viewport != null)
                {
                    var contentRect = _scrollRect.content.GetComponent<RectTransform>();
                    var viewportRect = _scrollRect.viewport.GetComponent<RectTransform>();
                    
                    float minContentHeight = viewportRect.sizeDelta.y + 100f;
                    if (contentRect.sizeDelta.y < minContentHeight)
                    {
                        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, minContentHeight);
                    }
                }
            }
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