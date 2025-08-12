using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using ProjectVG.Core.Managers;
using ProjectVG.Core.Utils;

namespace ProjectVG.Core.Loading
{
    /// <summary>
    /// 작업 정보를 담는 구조체
    /// </summary>
    [System.Serializable]
    public struct TaskInfo
    {
        public string taskName;
        public string taskDescription;
        public float progress;
        
        public TaskInfo(string name, string description, float progressValue)
        {
            taskName = name;
            taskDescription = description;
            progress = Mathf.Clamp01(progressValue);
        }
    }
    
    /// <summary>
    /// 로딩 과정을 전담 관리하는 싱글톤 매니저
    /// 실제 초기화는 GameManager가 담당하고, 이 클래스는 UI와 사용자 피드백에 집중
    /// </summary>
    public class LoadingManager : Singleton<LoadingManager>
    {
        [Header("UI Reference")]
        [SerializeField] private LoadingUI _loadingUI;
        
        private TaskInfo _currentTask;
        private bool _gameStarted;
        
        public event Action<string> OnInitializationFailed;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            SetupEventListeners();
        }
        
        private void OnDestroy()
        {
            RemoveEventListeners();
        }
        
        #endregion
        
        #region Public Methods
        
        public void StartInitialization()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.InitializeGame();
            }
            else
            {
                Debug.LogError("[LoadingManager] GameManager가 없습니다.");
            }
        }
        
        /// <summary>
        /// 외부(InitializationManager)의 진행 이벤트를 큐에 적재
        /// </summary>
        public void UpdateTask(string taskName, string description, float progress)
        {
            _currentTask = new TaskInfo(taskName, description, progress);
            if (_loadingUI != null)
            {
                _loadingUI.UpdateTask(_currentTask);
            }
            
            if (!_gameStarted && _currentTask.progress >= 1f)
            {
                StartGame();
            }
        }
        
        public async void StartGame()
        {
            if (_gameStarted)
                return;
            _gameStarted = true;
            if (_loadingUI != null)
            {
                await _loadingUI.FadeOut();
            }
            if (GameManager.Instance != null)
            {
                await GameManager.Instance.TransitionToMainSceneAsync();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void SetupEventListeners()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized += OnGameInitialized;
                GameManager.Instance.OnInitializationError += OnInitializationError;
                var initializationManager = GameManager.Instance.GetComponent<InitializationManager>();
                if (initializationManager != null)
                {
                    initializationManager.OnProgressUpdated += OnProgressUpdated;
                }
            }
        }
        
        private void RemoveEventListeners()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= OnGameInitialized;
                GameManager.Instance.OnInitializationError -= OnInitializationError;
                var initializationManager = GameManager.Instance.GetComponent<InitializationManager>();
                if (initializationManager != null)
                {
                    initializationManager.OnProgressUpdated -= OnProgressUpdated;
                }
            }
        }
        
        private void OnGameInitialized()
        {
            if (!_gameStarted)
            {
                StartGame();
            }
        }
        
        private void OnInitializationError(string error)
        {
            Debug.LogError($"[LoadingManager] 초기화 오류: {error}");
            OnInitializationFailed?.Invoke(error);
        }
        
        private void OnProgressUpdated(string taskName, string description, float progress)
        {
            UpdateTask(taskName, description, progress);
        }
        
        #endregion
    }
}
