using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using ProjectVG.Core.Managers;

namespace ProjectVG.Core.Loading
{
    public class LoadingManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private LoadingUI _loadingUI;
        
        [Header("Settings")]
        [SerializeField] private bool _autoStartInitialization = true;
        [SerializeField] private string _nextSceneName = "MainSence";
        
        private bool _isInitializationComplete = false;
        
        public bool IsInitializationComplete => _isInitializationComplete;
        
        public event Action OnInitializationCompleted;
        public event Action<string> OnInitializationFailed;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (_autoStartInitialization)
            {
                StartInitialization();
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromGameManager();
        }
        
        #endregion
        
        #region Public Methods
        
        public async void StartInitialization()
        {
            Debug.Log("[LoadingManager] 초기화 시작");
            
            SubscribeToGameManager();
            
            try
            {
                await GameManager.Instance.InitializeGameAsync();
            }
            catch (Exception ex)
            {
                string error = $"초기화 중 오류 발생: {ex.Message}";
                Debug.LogError($"[LoadingManager] {error}");
                OnInitializationFailed?.Invoke(error);
            }
        }
        
        public async void StartGame()
        {
            if (!_isInitializationComplete)
            {
                Debug.LogWarning("[LoadingManager] 초기화가 완료되지 않았습니다.");
                return;
            }
            
            Debug.Log("[LoadingManager] 게임 시작");
            
            if (_loadingUI != null)
            {
                await _loadingUI.FadeOut();
            }
            
            await SceneTransitionManager.Instance.TransitionToScene(_nextSceneName);
        }
        
        #endregion
        
        #region Private Methods
        
        private void SubscribeToGameManager()
        {
            var gameManager = GameManager.Instance;
            
            gameManager.OnPhaseChanged += OnPhaseChanged;
            gameManager.OnProgressChanged += OnProgressChanged;
            gameManager.OnGameInitialized += OnGameInitialized;
            gameManager.OnInitializationError += OnGameManagerError;
        }
        
        private void UnsubscribeFromGameManager()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.OnPhaseChanged -= OnPhaseChanged;
                gameManager.OnProgressChanged -= OnProgressChanged;
                gameManager.OnGameInitialized -= OnGameInitialized;
                gameManager.OnInitializationError -= OnGameManagerError;
            }
        }
        
        private void OnPhaseChanged(InitializationPhase phase)
        {
            Debug.Log($"[LoadingManager] 초기화 단계 변경: {phase}");
            
            if (_loadingUI != null)
            {
                _loadingUI.UpdatePhase(phase);
            }
        }
        
        private void OnProgressChanged(float progress)
        {
            if (_loadingUI != null)
            {
                _loadingUI.UpdateProgress(progress);
            }
        }
        
        private void OnGameInitialized()
        {
            Debug.Log("[LoadingManager] 초기화 완료");
            
            _isInitializationComplete = true;
            
            if (_loadingUI != null)
            {
                _loadingUI.ShowStartButton();
            }
            
            OnInitializationCompleted?.Invoke();
        }
        
        private void OnGameManagerError(string error)
        {
            Debug.LogError($"[LoadingManager] GameManager 오류: {error}");
            
            if (_loadingUI != null)
            {
                _loadingUI.ShowError(error);
            }
            
            OnInitializationFailed?.Invoke(error);
        }
        
        #endregion
    }
}
