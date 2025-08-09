using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Cysharp.Threading.Tasks;
using ProjectVG.Core.Managers;

namespace ProjectVG.Core.Loading
{
    public class LoadingUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _progressText;
        [SerializeField] private Button _startButton;
        [SerializeField] private CanvasGroup _loadingPanel;
        [SerializeField] private CanvasGroup _errorPanel;
        [SerializeField] private Text _errorText;
        
        [Header("Animation Settings")]
        [SerializeField] private float _fadeSpeed = 1f;
        [SerializeField] private AnimationCurve _progressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private LoadingManager _loadingManager;
        private Coroutine _progressAnimationCoroutine;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _loadingManager = GetComponent<LoadingManager>();
            
            InitializeUI();
        }
        
        private void Start()
        {
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(OnStartButtonClicked);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public void UpdatePhase(InitializationPhase phase)
        {
            if (_statusText != null)
            {
                _statusText.text = GetPhaseText(phase);
            }
            
            Debug.Log($"[LoadingUI] 상태 업데이트: {GetPhaseText(phase)}");
        }
        
        public void UpdateProgress(float progress)
        {
            if (_progressAnimationCoroutine != null)
            {
                StopCoroutine(_progressAnimationCoroutine);
            }
            
            _progressAnimationCoroutine = StartCoroutine(AnimateProgress(progress));
        }
        
        public void ShowStartButton()
        {
            if (_startButton != null)
            {
                _startButton.gameObject.SetActive(true);
                _startButton.interactable = true;
            }
            
            if (_statusText != null)
            {
                _statusText.text = "준비 완료! 게임을 시작하세요.";
            }
        }
        
        public void ShowError(string errorMessage)
        {
            if (_errorPanel != null)
            {
                _errorPanel.gameObject.SetActive(true);
                _errorPanel.alpha = 1f;
            }
            
            if (_errorText != null)
            {
                _errorText.text = errorMessage;
            }
            
            if (_loadingPanel != null)
            {
                _loadingPanel.alpha = 0f;
            }
        }
        
        public async UniTask FadeOut()
        {
            if (_loadingPanel != null)
            {
                float currentAlpha = _loadingPanel.alpha;
                float elapsedTime = 0f;
                
                while (elapsedTime < _fadeSpeed)
                {
                    elapsedTime += Time.deltaTime;
                    float alpha = Mathf.Lerp(currentAlpha, 0f, elapsedTime / _fadeSpeed);
                    _loadingPanel.alpha = alpha;
                    await UniTask.Yield();
                }
                
                _loadingPanel.alpha = 0f;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeUI()
        {
            if (_progressBar != null)
            {
                _progressBar.value = 0f;
            }
            
            if (_progressText != null)
            {
                _progressText.text = "0%";
            }
            
            if (_startButton != null)
            {
                _startButton.gameObject.SetActive(false);
            }
            
            if (_loadingPanel != null)
            {
                _loadingPanel.alpha = 1f;
            }
            
            if (_errorPanel != null)
            {
                _errorPanel.gameObject.SetActive(false);
            }
            
            if (_statusText != null)
            {
                _statusText.text = "초기화 준비 중...";
            }
        }
        
        private string GetPhaseText(InitializationPhase phase)
        {
            return phase switch
            {
                InitializationPhase.NotStarted => "초기화 준비 중...",
                InitializationPhase.InitializingManagers => "시스템 매니저 초기화 중...",
                InitializationPhase.ConnectingToServer => "서버 연결 중...",
                InitializationPhase.LoadingResources => "리소스 로딩 중...",
                InitializationPhase.Completed => "초기화 완료!",
                _ => "알 수 없는 상태"
            };
        }
        
        private IEnumerator AnimateProgress(float targetProgress)
        {
            float startProgress = _progressBar != null ? _progressBar.value : 0f;
            float elapsedTime = 0f;
            float animationDuration = 0.3f;
            
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / animationDuration;
                float curveValue = _progressCurve.Evaluate(normalizedTime);
                float currentProgress = Mathf.Lerp(startProgress, targetProgress, curveValue);
                
                if (_progressBar != null)
                {
                    _progressBar.value = currentProgress;
                }
                
                if (_progressText != null)
                {
                    _progressText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
                }
                
                yield return null;
            }
            
            if (_progressBar != null)
            {
                _progressBar.value = targetProgress;
            }
            
            if (_progressText != null)
            {
                _progressText.text = $"{Mathf.RoundToInt(targetProgress * 100)}%";
            }
        }
        
        private void OnStartButtonClicked()
        {
            if (_loadingManager != null)
            {
                _loadingManager.StartGame();
            }
        }
        
        #endregion
    }
}
