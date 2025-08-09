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
        
        public event Action OnInitializationCompleted;
        public event Action<string> OnInitializationFailed;
        
        #region Unity Lifecycle
        
        // Auto-start 제거 - 명시적으로 StartInitialization() 호출 필요
        
        private void OnDestroy()
        {
            // 정리 작업
            Debug.Log("[LoadingManager] LoadingManager 해제");
        }
        
        #endregion
        
        #region Public Methods
        
        public void StartInitialization()
        {
            Debug.Log("[LoadingManager] 로딩 시작");
        }
        
        /// <summary>
        /// 외부에서 작업 업데이트를 받는 메서드 (간소화된 버전)
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
            
            OnInitializationCompleted?.Invoke();
        }
        
        #endregion
        
        #region Private Methods

        #endregion
    }
}
