using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Core.Loading
{
    /// <summary>
    /// 간단하고 현대적인 로딩 UI
    /// </summary>
    public class LoadingUI : MonoBehaviour
    {
        [Header("Essential UI Components")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private CanvasGroup _loadingPanel;
        
        [Header("Animation Settings")]
        [SerializeField] private float _fadeSpeed = 1f;
        [SerializeField] private AnimationCurve _progressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _progressAnimationSpeed = 0.3f;
        
        private Coroutine _progressAnimationCoroutine;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeUI();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 작업 정보로 UI 업데이트 (메인 메서드)
        /// </summary>
        public void UpdateTask(TaskInfo taskInfo)
        {
            UpdateStatus(taskInfo.taskDescription);
            UpdateProgress(taskInfo.progress);
            
            Debug.Log($"[LoadingUI] {taskInfo.taskName}: {taskInfo.taskDescription} ({Mathf.RoundToInt(taskInfo.progress * 100)}%)");
        }
        
        /// <summary>
        /// 상태 메시지 업데이트
        /// </summary>
        public void UpdateStatus(string statusMessage)
        {
            if (_statusText != null)
            {
                _statusText.text = statusMessage;
            }
        }
        
        /// <summary>
        /// 진행률 업데이트 (애니메이션 포함)
        /// </summary>
        public void UpdateProgress(float progress)
        {
            if (_progressAnimationCoroutine != null)
            {
                StopCoroutine(_progressAnimationCoroutine);
            }
            
            _progressAnimationCoroutine = StartCoroutine(AnimateProgress(progress));
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
        
        /// <summary>
        /// UI 초기 상태 설정
        /// </summary>
        private void InitializeUI()
        {
            // 진행률 바 초기화
            if (_progressBar != null)
            {
                _progressBar.value = 0f;
            }
            
            // 로딩 패널 표시
            if (_loadingPanel != null)
            {
                _loadingPanel.alpha = 1f;
                _loadingPanel.gameObject.SetActive(true);
            }
            
            // 초기 상태 메시지
            UpdateStatus("게임 시작 준비 중...");
        }
        
        /// <summary>
        /// 부드러운 진행률 애니메이션
        /// </summary>
        private IEnumerator AnimateProgress(float targetProgress)
        {
            if (_progressBar == null) yield break;
            
            float startProgress = _progressBar.value;
            float elapsedTime = 0f;
            
            while (elapsedTime < _progressAnimationSpeed)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / _progressAnimationSpeed;
                float curveValue = _progressCurve.Evaluate(normalizedTime);
                float currentProgress = Mathf.Lerp(startProgress, targetProgress, curveValue);
                
                _progressBar.value = currentProgress;
                yield return null;
            }
            
            _progressBar.value = targetProgress;
        }
        
        #endregion
    }
}
