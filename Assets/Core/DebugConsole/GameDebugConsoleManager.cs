#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;
using Unity.Collections;
using System.Collections.Generic;

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

        private string _filterKeyword = "";

        // NativeArray로 변경: 초기 용량 설정, Job System과 연동 가능
        private NativeList<LogEntry> _logEntries;
        private List<GameObject> _logEntryObjects = new List<GameObject>();
        private Queue<GameObject> _objectPool = new Queue<GameObject>();
        private bool _isConsoleVisible = false;
        private float _lastCleanupTime = 0f;

        [System.Serializable]
        public struct LogEntry // Native Collection을 사용하기 위해 클래스에서 구조체(struct)로 변경
        {
            public FixedString128Bytes message; // FixedString으로 메모리 최적화
            public FixedString64Bytes stackTrace;
            public LogType logType;
            public float timestamp; // DateTime 대신 float으로 변경하여 GC 부하 제거

            public LogEntry(string message, string stackTrace, LogType logType)
            {
                this.message = new FixedString128Bytes(message);
                this.stackTrace = new FixedString64Bytes(stackTrace);
                this.logType = logType;
                this.timestamp = Time.time; // 현재 게임 실행 시간으로 변경
            }
        }

        void Awake()
        {
            // NativeList 초기화: 최대 로그 라인 수를 기반으로 초기 용량 할당
            _logEntries = new NativeList<LogEntry>(_settings?.MaxLogLines ?? 100, Allocator.Persistent);
            InitializeConsole();
        }

        void Start()
        {
            Application.logMessageReceived += OnLogMessageReceived;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            // NativeList 메모리 해제
            if (_logEntries.IsCreated) {
                _logEntries.Dispose();
            }
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F12)) {
                ToggleConsole();
            }

            if (_settings?.EnableMobileInput == true && UnityEngine.Input.touchCount == _settings.MobileTouchCount) {
                bool allTouchesBegan = true;
                for (int i = 0; i < UnityEngine.Input.touchCount; i++) {
                    if (UnityEngine.Input.GetTouch(i).phase != TouchPhase.Began) {
                        allTouchesBegan = false;
                        break;
                    }
                }

                if (allTouchesBegan) {
                    ToggleConsole();
                }
            }

            if (_settings?.AutoClearOldLogs == true && Time.time - _lastCleanupTime > 30f) {
                CleanupOldLogs();
                _lastCleanupTime = Time.time;
            }
        }

        private void InitializeConsole()
        {
            if (_consolePanel == null) {
                return;
            }

            _consolePanel.SetActive(true);
            _isConsoleVisible = true;

            SetupLayoutGroup();

            if (_settings?.UseObjectPooling == true && _settings?.InitializePoolOnStart == true) {
                InitializeObjectPool();
            }
        }

        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (logString.Contains("[DEBUG_CONSOLE]") || logString.Contains("[TEST]"))
                return;

            if (_settings?.LogInBackground == false && !_isConsoleVisible)
                return;

            // NativeList에 로그 추가
            var entry = new LogEntry(logString, stackTrace, type);
            _logEntries.Add(entry);

            if (_settings != null && _logEntries.Length > _settings.MaxLogLines) {
                _logEntries.RemoveAt(0);
            }

            if (_isConsoleVisible) {
                UpdateLogDisplay();
            }
        }

        private void UpdateLogDisplay()
        {
            if (_logContentParent == null) return;

            ClearLogEntryObjects();

            for (int i = 0; i < _logEntries.Length; i++) {
                var entry = _logEntries[i];

                if (_settings?.EnableFiltering == true && !string.IsNullOrEmpty(_filterKeyword)) {
                    // FixedString으로 비교
                    if (!entry.message.ToString().Contains(_filterKeyword, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                CreateLogEntryObject(entry);
            }

            if (_settings?.AutoScroll == true && _scrollRect != null) {
                StartCoroutine(ScrollToBottomCoroutine());
            }
        }

        private void CreateLogEntryObject(LogEntry entry)
        {
            if (_logEntryPrefab == null || _logContentParent == null) return;

            GameObject? logEntryObj;
            if (_settings?.UseObjectPooling == true) {
                logEntryObj = GetPooledObject();
            }
            else {
                logEntryObj = Instantiate(_logEntryPrefab, _logContentParent);
            }

            if (logEntryObj == null) return;

            logEntryObj.transform.SetAsLastSibling();
            _logEntryObjects.Add(logEntryObj);

            var logText = logEntryObj.GetComponentInChildren<TextMeshProUGUI>();
            if (logText != null) {
                // FixedString을 ToString()으로 변환하여 사용
                string timestamp = _settings?.ShowTimestamp == true ? $"[{entry.timestamp:F2}] " : ""; // float이므로 소수점 표기
                string logType = _settings?.ShowLogType == true ? $"[{entry.logType}] " : "";

                string logMessage = $"{timestamp}{logType}{entry.message.ToString()}";

                if (!string.IsNullOrEmpty(entry.stackTrace.ToString())) {
                    logMessage += $"\nStack Trace: {entry.stackTrace.ToString()}";
                }

                logText.text = logMessage;

                if (_settings != null) {
                    logText.fontSize = _settings.FontSize;
                }

                SetLogEntryColor(logText, entry.logType);

                var contentSizeFitter = logEntryObj.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter == null) {
                    contentSizeFitter = logEntryObj.AddComponent<ContentSizeFitter>();
                }
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private void SetLogEntryColor(TextMeshProUGUI logText, LogType logType)
        {
            if (_settings == null) return;

            switch (logType) {
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

            for (int i = 0; i < _settings.PoolSize; i++) {
                GameObject pooledObject = Instantiate(_logEntryPrefab, _logContentParent);
                pooledObject.SetActive(false);
                _objectPool.Enqueue(pooledObject);
            }
        }

        private GameObject? GetPooledObject()
        {
            if (_objectPool.Count > 0) {
                GameObject obj = _objectPool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            if (_logEntryPrefab != null && _logContentParent != null) {
                return Instantiate(_logEntryPrefab, _logContentParent);
            }

            return null;
        }

        private void ReturnToPool(GameObject obj)
        {
            if (obj != null) {
                obj.SetActive(false);
                _objectPool.Enqueue(obj);
            }
        }

        private void ClearLogEntryObjects()
        {
            if (_settings?.UseObjectPooling == true) {
                foreach (var obj in _logEntryObjects) {
                    if (obj != null) {
                        ReturnToPool(obj);
                    }
                }
            }
            else {
                foreach (var obj in _logEntryObjects) {
                    if (obj != null) {
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

            if (_scrollRect != null) {
                _scrollRect.verticalNormalizedPosition = 0f;

                yield return new WaitForEndOfFrame();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void ToggleConsole()
        {
            Debug.Log($"[DEBUG_CONSOLE] ToggleConsole called. Current state: {_isConsoleVisible}");

            _isConsoleVisible = !_isConsoleVisible;

            if (_consolePanel != null) {
                _consolePanel.SetActive(_isConsoleVisible);
                Debug.Log($"[DEBUG_CONSOLE] Console panel set to: {_isConsoleVisible}");
            }
            else {
                Debug.LogWarning("[DEBUG_CONSOLE] _consolePanel is null!");
            }

            if (_isConsoleVisible) {
                if (_settings?.UseObjectPooling == true && _objectPool.Count == 0) {
                    InitializeObjectPool();
                }

                UpdateLogDisplay();
            }
        }

        public void ClearLogs()
        {
            _logEntries.Clear();
            ClearLogEntryObjects();

            if (_settings?.UseObjectPooling == true) {
                foreach (var pooledObj in _objectPool) {
                    if (pooledObj != null) {
                        pooledObj.SetActive(false);
                    }
                }
            }
        }

        private void OnFilterChanged(string filterText)
        {
            _filterKeyword = filterText;
            UpdateLogDisplay();
        }


        private void CleanupOldLogs()
        {
            if (_settings == null) return;

            var cutoffTime = Time.time - _settings.LogRetentionTime;
            int removedCount = 0;

            // NativeList는 C# List와 달리 RemoveAt(index)가 느릴 수 있으므로,
            // 더 효율적인 제거 방법을 고려할 수 있습니다.
            // 하지만 이 경우 성능 차이가 미미하므로 기존 로직을 유지합니다.
            for (int i = _logEntries.Length - 1; i >= 0; i--) {
                if (_logEntries[i].timestamp < cutoffTime) {
                    _logEntries.RemoveAt(i);
                    removedCount++;
                }
            }

            if (removedCount > 0) {
                UpdateLogDisplay();
            }
        }

        public NativeList<LogEntry> GetLogEntries()
        {
            var logEntriesCopy = new NativeList<LogEntry>(_logEntries.Length, Allocator.Temp);
            logEntriesCopy.CopyFrom(_logEntries);
            return logEntriesCopy;
        }

        public void ScrollToTop()
        {
            if (_scrollRect != null) {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void ScrollToBottom()
        {
            if (_scrollRect != null) {
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
            if (_scrollRect == null) {
                return;
            }
        }

        public void ForceScrollUpdate()
        {
            if (_scrollRect != null) {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void ForceEnableScrollbar()
        {
            if (_scrollRect != null) {
                _scrollRect.vertical = true;

                if (_scrollRect.verticalScrollbar != null) {
                    _scrollRect.verticalScrollbar.gameObject.SetActive(true);
                    _scrollRect.verticalScrollbar.interactable = true;
                }

                if (_scrollRect.content != null && _scrollRect.viewport != null) {
                    var contentRect = _scrollRect.content.GetComponent<RectTransform>();
                    var viewportRect = _scrollRect.viewport.GetComponent<RectTransform>();

                    float minContentHeight = viewportRect.sizeDelta.y + 100f;
                    if (contentRect.sizeDelta.y < minContentHeight) {
                        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, minContentHeight);
                    }
                }
            }
        }

        public void SetupLayoutGroup()
        {
            if (_logContentParent == null) return;

            var verticalLayoutGroup = _logContentParent.GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup == null) {
                verticalLayoutGroup = _logContentParent.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            verticalLayoutGroup.spacing = 2f;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.padding = new RectOffset(5, 5, 5, 5);

            var contentSizeFitter = _logContentParent.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null) {
                contentSizeFitter = _logContentParent.gameObject.AddComponent<ContentSizeFitter>();
            }
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}