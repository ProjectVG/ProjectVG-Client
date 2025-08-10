using UnityEngine;
using System;
using System.Collections.Generic;
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
        public string taskName;        // 작업 이름 (예: "NETWORK_CONNECTION")
        public string taskDescription; // 작업 설명 (예: "네트워크 연결 중...")
        public float progress;         // 진행률 (0.0 ~ 1.0)
        
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
        
        public event Action<string> OnInitializationFailed;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            SetupEventListeners();
        }
        
        private void OnDestroy()
        {
            RemoveEventListeners();
            Debug.Log("[LoadingManager] LoadingManager 해제");
        }
        
        #endregion
        
        #region Public Methods
        
        public void StartInitialization()
        {
            Debug.Log("[LoadingManager] 로딩 시작");
            
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
        /// 작업 업데이트를 받는 메서드 (이벤트 기반)
        /// </summary>
        /// <param name="taskName">작업 이름 (예: "NETWORK_CONNECTION")</param>
        /// <param name="description">작업 설명 (예: "네트워크 연결 중...")</param>
        /// <param name="progress">진행률 (0.0 ~ 1.0)</param>
        public void UpdateTask(string taskName, string description, float progress)
        {
            _currentTask = new TaskInfo(taskName, description, progress);
            
            // UI 업데이트
            if (_loadingUI != null)
            {
                Debug.Log($"[LoadingManager] 작업 업데이트: {taskName} - {description} ({Mathf.RoundToInt(progress * 100)}%)");
                _loadingUI.UpdateTask(_currentTask);
            }
        }
        

        
        public async void StartGame()
        {
            Debug.Log("[LoadingManager] 게임 시작");
            
            if (_loadingUI != null)
            {
                await _loadingUI.FadeOut();
            }
            
            if (GameManager.Instance != null)
            {
                Debug.Log("[LoadingManager] GameManager를 통해 MainScene으로 전환");
                await GameManager.Instance.TransitionToMainSceneAsync();
            }
            else
            {
                Debug.LogError("[LoadingManager] GameManager가 없습니다.");
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
                
                // InitializationManager의 진행률 이벤트 구독
                var initializationManager = GameManager.Instance.GetComponent<InitializationManager>();
                if (initializationManager != null)
                {
                    initializationManager.OnProgressUpdated += OnProgressUpdated;
                }
                
                Debug.Log("[LoadingManager] GameManager 이벤트 구독 완료");
            }
        }
        
        private void RemoveEventListeners()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= OnGameInitialized;
                GameManager.Instance.OnInitializationError -= OnInitializationError;
                
                // InitializationManager의 진행률 이벤트 구독 해제
                var initializationManager = GameManager.Instance.GetComponent<InitializationManager>();
                if (initializationManager != null)
                {
                    initializationManager.OnProgressUpdated -= OnProgressUpdated;
                }
            }
        }
        
        private void OnGameInitialized()
        {
            Debug.Log("[LoadingManager] 게임 초기화 완료 - 게임 시작");
            StartGame();
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
