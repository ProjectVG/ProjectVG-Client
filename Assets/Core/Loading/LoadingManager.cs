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
        
        public void StartInitialization()
        {
            SubscribeToGameManager();
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
            if (gameManager == null)
            {
                Debug.LogError("[LoadingManager] GameManager Instance를 찾을 수 없습니다.");
                return;
            }
            
            // 이미 초기화가 완료된 경우 즉시 처리
            if (gameManager.IsInitialized)
            {
                Debug.Log("[LoadingManager] GameManager가 이미 초기화되었습니다.");
                OnGameInitialized();
                return;
            }
            
            gameManager.OnPhaseChanged += OnPhaseChanged;
            gameManager.OnProgressChanged += OnProgressChanged;
            gameManager.OnGameInitialized += OnGameInitialized;
            gameManager.OnInitializationError += OnGameManagerError;
            
            Debug.Log("[LoadingManager] GameManager 이벤트 구독 완료");
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
                
                Debug.Log("[LoadingManager] GameManager 이벤트 구독 해제 완료");
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
