using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Core.Loading
{
    public class SceneTransitionManager : Singleton<SceneTransitionManager>
    {
        [Header("Transition Settings")]
        [SerializeField] private float _transitionDuration = 1f;
        [SerializeField] private CanvasGroup _fadePanel;
        
        private bool _isTransitioning = false;
        
        public bool IsTransitioning => _isTransitioning;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            if (_fadePanel == null)
            {
                CreateFadePanel();
            }
            
            _fadePanel.alpha = 0f;
            _fadePanel.blocksRaycasts = false;
        }
        
        #endregion
        
        #region Public Methods
        
        public async UniTask TransitionToScene(string sceneName)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[SceneTransitionManager] 이미 씬 전환 중입니다.");
                return;
            }
            
            _isTransitioning = true;
            
            Debug.Log($"[SceneTransitionManager] '{sceneName}' 씬으로 전환 시작");
            
            try
            {
                await FadeOut();
                
                await SceneManager.LoadSceneAsync(sceneName);
                
                await FadeIn();
                
                Debug.Log($"[SceneTransitionManager] '{sceneName}' 씬 전환 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneTransitionManager] 씬 전환 중 오류: {ex.Message}");
            }
            finally
            {
                _isTransitioning = false;
            }
        }
        
        public async UniTask TransitionToSceneWithProgress(string sceneName, System.IProgress<float> progress = null)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[SceneTransitionManager] 이미 씬 전환 중입니다.");
                return;
            }
            
            _isTransitioning = true;
            
            Debug.Log($"[SceneTransitionManager] '{sceneName}' 씬으로 전환 시작 (진행률 포함)");
            
            try
            {
                progress?.Report(0f);
                
                await FadeOut();
                progress?.Report(0.2f);
                
                var loadOperation = SceneManager.LoadSceneAsync(sceneName);
                loadOperation.allowSceneActivation = false;
                
                while (loadOperation.progress < 0.9f)
                {
                    float loadProgress = Mathf.Lerp(0.2f, 0.8f, loadOperation.progress / 0.9f);
                    progress?.Report(loadProgress);
                    await UniTask.Yield();
                }
                
                progress?.Report(0.8f);
                loadOperation.allowSceneActivation = true;
                
                await UniTask.WaitUntil(() => loadOperation.isDone);
                progress?.Report(0.9f);
                
                await FadeIn();
                progress?.Report(1f);
                
                Debug.Log($"[SceneTransitionManager] '{sceneName}' 씬 전환 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneTransitionManager] 씬 전환 중 오류: {ex.Message}");
            }
            finally
            {
                _isTransitioning = false;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void CreateFadePanel()
        {
            var fadeCanvas = new GameObject("FadeCanvas");
            var canvas = fadeCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            
            var canvasScaler = fadeCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            fadeCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            var fadePanelObj = new GameObject("FadePanel");
            fadePanelObj.transform.SetParent(fadeCanvas.transform, false);
            
            var image = fadePanelObj.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.black;
            
            var rectTransform = fadePanelObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            _fadePanel = fadePanelObj.AddComponent<CanvasGroup>();
            
            DontDestroyOnLoad(fadeCanvas);
        }
        
        private async UniTask FadeOut()
        {
            if (_fadePanel == null) return;
            
            _fadePanel.blocksRaycasts = true;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < _transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                _fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsedTime / _transitionDuration);
                await UniTask.Yield();
            }
            
            _fadePanel.alpha = 1f;
        }
        
        private async UniTask FadeIn()
        {
            if (_fadePanel == null) return;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < _transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                _fadePanel.alpha = Mathf.Lerp(1f, 0f, elapsedTime / _transitionDuration);
                await UniTask.Yield();
            }
            
            _fadePanel.alpha = 0f;
            _fadePanel.blocksRaycasts = false;
        }
        
        #endregion
    }
}
