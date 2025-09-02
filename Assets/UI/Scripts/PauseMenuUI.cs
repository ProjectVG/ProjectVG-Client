using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectVG.Core.Settings;

namespace ProjectVG.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("Panel")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _backgroundPanel;

        [Header("Animation Settings")]
        [SerializeField] private bool _enableFadeAnimation = true;
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.2f;

        private PauseMenuManager _pauseMenuManager;
        private bool _isAnimating = false;

        private void Awake()
        {
            SetupComponents();
            SetupButtons();
        }

        private void Start()
        {
            _pauseMenuManager = PauseMenuManager.Instance;
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.SetPauseMenuUI(gameObject);
            }

            // 초기 상태 설정
            gameObject.SetActive(false);
        }

        private void SetupComponents()
        {
            // CanvasGroup이 없으면 추가
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // 배경 패널이 없으면 현재 오브젝트를 배경으로 사용
            if (_backgroundPanel == null)
            {
                _backgroundPanel = gameObject;
            }
        }

        private void SetupButtons()
        {
            // 버튼 이벤트 연결
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveAllListeners();
                _resumeButton.onClick.AddListener(OnResumeButtonClick);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(OnSettingsButtonClick);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveAllListeners();
                _quitButton.onClick.AddListener(OnQuitButtonClick);
            }
        }

        private void OnEnable()
        {
            if (_enableFadeAnimation && !_isAnimating)
            {
                PlayFadeInAnimation();
            }
        }

        private void OnResumeButtonClick()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.OnResumeButtonClick();
            }
        }

        private void OnSettingsButtonClick()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.OnSettingsButtonClick();
            }
        }

        private void OnQuitButtonClick()
        {
            // 확인 대화상자를 표시할 수 있음
            if (ShouldShowQuitConfirmation())
            {
                ShowQuitConfirmationDialog();
            }
            else
            {
                if (_pauseMenuManager != null)
                {
                    _pauseMenuManager.OnQuitButtonClick();
                }
            }
        }

        private bool ShouldShowQuitConfirmation()
        {
            // 모바일 플랫폼에서는 확인 대화상자 표시
            return Application.platform == RuntimePlatform.Android || 
                   Application.platform == RuntimePlatform.IPhonePlayer;
        }

        private void ShowQuitConfirmationDialog()
        {
            // 간단한 확인 대화상자 (나중에 더 예쁜 UI로 교체 가능)
            if (Application.platform == RuntimePlatform.Android)
            {
                // Android에서는 시스템 대화상자 사용 가능
                Debug.Log("[PauseMenuUI] 게임을 종료하시겠습니까?");
                if (_pauseMenuManager != null)
                {
                    _pauseMenuManager.OnQuitButtonClick();
                }
            }
            else
            {
                if (_pauseMenuManager != null)
                {
                    _pauseMenuManager.OnQuitButtonClick();
                }
            }
        }

        private void PlayFadeInAnimation()
        {
            if (_canvasGroup == null) return;

            _isAnimating = true;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;

            StartCoroutine(FadeCoroutine(0f, 1f, _fadeInDuration, () =>
            {
                _canvasGroup.interactable = true;
                _isAnimating = false;
            }));
        }

        public void PlayFadeOutAnimation(System.Action onComplete = null)
        {
            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            _isAnimating = true;
            _canvasGroup.interactable = false;

            StartCoroutine(FadeCoroutine(1f, 0f, _fadeOutDuration, () =>
            {
                _canvasGroup.blocksRaycasts = false;
                _isAnimating = false;
                onComplete?.Invoke();
            }));
        }

        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / duration;
                progress = Mathf.SmoothStep(0f, 1f, progress); // Smooth easing
                
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                yield return null;
            }
            
            _canvasGroup.alpha = endAlpha;
            onComplete?.Invoke();
        }

        // 외부에서 버튼 참조를 설정할 때 사용
        public void SetButtonReferences(Button resumeButton, Button settingsButton, Button quitButton)
        {
            _resumeButton = resumeButton;
            _settingsButton = settingsButton;
            _quitButton = quitButton;
            SetupButtons();
        }

        // 애니메이션 설정
        public void SetAnimationSettings(bool enableFade, float fadeInDuration, float fadeOutDuration)
        {
            _enableFadeAnimation = enableFade;
            _fadeInDuration = fadeInDuration;
            _fadeOutDuration = fadeOutDuration;
        }

        private void OnDestroy()
        {
            // 버튼 이벤트 정리
            if (_resumeButton != null)
                _resumeButton.onClick.RemoveAllListeners();
            if (_settingsButton != null)
                _settingsButton.onClick.RemoveAllListeners();
            if (_quitButton != null)
                _quitButton.onClick.RemoveAllListeners();
        }
    }
}