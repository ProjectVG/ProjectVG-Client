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
    /// 실제 초기화는 SystemManager가 담당하고, 이 클래스는 UI와 사용자 피드백에 집중
    /// </summary>
    public class LoadingManager : Singleton<LoadingManager>
    {
        [Header("UI Reference")]
        [SerializeField] private LoadingUI _loadingUI;
        
        private TaskInfo _currentTask;
        private bool _appStarted;
		[SerializeField] private float _autoProgressMax = 0.9f;
		[SerializeField] private float _autoProgressSpeed = 0.25f;
		private Coroutine _autoProgressCoroutine;
        
        public event Action<string> OnInitializationFailed;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            SetupEventListeners();
        }
        
        private void OnDestroy()
        {
            RemoveEventListeners();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 로딩 UI 시작 및 자동 진행 시작
        /// </summary>
        public void BeginLoadingUI()
        {
            UpdateTask("INITIALIZATION", "시스템 초기화 중...", 0.05f);
            StartAutoProgress();
        }

        public void StartInitialization()
        {
            if (SystemManager.Instance != null)
            {
				UpdateTask("INITIALIZATION", "시스템 초기화 중...", 0.05f);
				StartAutoProgress();
				SystemManager.Instance.Initialize();
            }
            else
            {
                Debug.LogError("[LoadingManager] SystemManager가 없습니다.");
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
            
            if (!_appStarted && _currentTask.progress >= 1f)
            {
                StartApp();
            }
        }
        
        public async void StartApp()
        {
            if (_appStarted)
                return;
            _appStarted = true;
            if (_loadingUI != null)
            {
                await _loadingUI.FadeOut();
            }
            if (SystemManager.Instance != null)
            {
                await SystemManager.Instance.TransitionToMainSceneAsync();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void SetupEventListeners()
        {
            if (SystemManager.Instance != null)
            {
                SystemManager.Instance.OnAppInitialized += OnAppInitialized;
                SystemManager.Instance.OnInitializationError += OnInitializationError;
            }
        }
        
        private void RemoveEventListeners()
        {
            if (SystemManager.Instance != null)
            {
                SystemManager.Instance.OnAppInitialized -= OnAppInitialized;
                SystemManager.Instance.OnInitializationError -= OnInitializationError;
            }
        }
        
        private void OnAppInitialized()
        {
            if (!_appStarted)
            {
				StopAutoProgress();
				UpdateTask("INITIALIZATION", "완료", 1f);
                StartApp();
            }
        }
        
        private void OnInitializationError(string error)
        {
            Debug.LogError($"[LoadingManager] 초기화 오류: {error}");
            OnInitializationFailed?.Invoke(error);
        }
        
		/// <summary>
		/// 자동 진행 바 업데이트 시작
		/// </summary>
		private void StartAutoProgress()
		{
			if (_autoProgressCoroutine != null) return;
			_autoProgressCoroutine = StartCoroutine(AutoProgressRoutine());
		}

		/// <summary>
		/// 자동 진행 바 중지
		/// </summary>
		private void StopAutoProgress()
		{
			if (_autoProgressCoroutine == null) return;
			StopCoroutine(_autoProgressCoroutine);
			_autoProgressCoroutine = null;
		}

		private System.Collections.IEnumerator AutoProgressRoutine()
		{
			float p = Mathf.Clamp01(_currentTask.progress);
            while (p < _autoProgressMax && !_appStarted)
			{
				p += Time.deltaTime * _autoProgressSpeed;
				UpdateTask("INITIALIZATION", "시스템 초기화 중...", Mathf.Min(p, _autoProgressMax));
				yield return null;
			}
			_autoProgressCoroutine = null;
		}
        
        #endregion
    }
}
