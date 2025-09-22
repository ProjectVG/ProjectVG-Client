#nullable enable
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Domain.Chat.Model;
 

namespace ProjectVG.Domain.Chat.View
{
    /// <summary>
    /// 개별 채팅 버블 UI 컴포넌트
    /// 토스트 애니메이션, 타이핑 효과, 페이드아웃 등을 담당합니다.
    /// </summary>
    public class ChatBubbleUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private RectTransform? _rectTransform;
        [SerializeField] private TextMeshProUGUI? _textComponent;
        [SerializeField] private Image? _backgroundImage;
        
        private CanvasGroup? _canvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private float _slideOutDuration = 0.03f;
        [SerializeField] private float _typingSpeed = 0.025f;
        [SerializeField] private bool _enableAutoDestroy = true;
        [SerializeField] private float _defaultDisplayTime = 3f;
        
        [Header("Toast Animation Settings")]
        [SerializeField] private float _toastBounceDuration = 0.3f;
        [SerializeField] private float _toastBounceHeight = 20f;
        [SerializeField] private float _toastBounceScale = 1.1f;
        [SerializeField] private float _queueSlideDuration = 0.2f;

        [SerializeField] private bool _enableBounceEffect = true;
        [SerializeField] private bool _enableScaleEffect = true;
        [SerializeField] private EasingType _bounceEasing = EasingType.Bounce;
        [SerializeField] private EasingType _queueEasing = EasingType.Quart;
        
        public enum EasingType
        {
            Bounce,
            Quart,
            Back,
            Elastic
        }
        
        [Header("Bubble Sprite Settings")]
        [SerializeField] private Sprite? _userBubbleSprite;
        [SerializeField] private Sprite? _characterBubbleSprite;
        
        [Header("Layout Settings")]
        [SerializeField] private ContentSizeFitter? _contentSizeFitter;
        [SerializeField] private LayoutElement? _layoutElement;
        
        private Actor _actor;
        private string _fullText = string.Empty;
        private float _displayTime;
        
        private bool _isAnimating = false;
        private bool _isTyping = false;
        private float _typingProgress = 0f;
        private Coroutine? _typingCoroutine;
        private Coroutine? _animationCoroutine;
        
        private ChatBubblePanel? _manager;
        
        // 애니메이션 관련 변수들
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private bool _isToastAnimationComplete = false;
        
        public event Action<ChatBubbleUI>? OnBubbleCreated;
        public event Action<ChatBubbleUI>? OnBubbleTypingComplete;
        public event Action<ChatBubbleUI>? OnBubbleDestroyed;
        public event Action<ChatBubbleUI>? OnToastAnimationComplete;
        
        public Actor Actor => _actor;
        public string Text => _fullText;
        public float DisplayTime => _displayTime;
        public bool IsAnimating => _isAnimating;
        public bool IsTyping => _isTyping;
        public bool IsToastAnimationComplete => _isToastAnimationComplete;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            Initialize();
        }
        
        private void OnDestroy()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }
            
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public void Initialize(Actor actor, string text, float displayTime, ChatBubblePanel? manager = null)
        {
            _actor = actor;
            _fullText = text;
            _displayTime = displayTime < 0f ? _defaultDisplayTime : displayTime;
            _manager = manager;

            Debug.Log($"[ChatBubbleUI] 기본 Initialize 호출 - Actor: {_actor}");

            ApplyStyle();

            Canvas.ForceUpdateCanvases();
            _originalPosition = _rectTransform?.localPosition ?? Vector3.zero;
            _originalScale = _rectTransform?.localScale ?? Vector3.one;

            StartToastAnimation();

            if (_actor == Actor.User)
            {
                _isTyping = false;
                _typingProgress = 1f;
                if (_textComponent != null)
                {
                    _textComponent.text = _fullText;
                }
            }

            OnBubbleCreated?.Invoke(this);
        }
        
        /// <summary>
        /// 런타임에서 커스텀 스프라이트로 버블을 초기화합니다.
        /// </summary>
        public void Initialize(Actor actor, string text, float displayTime, Sprite? userSprite, Sprite? characterSprite, ChatBubblePanel? manager = null)
        {
            Debug.Log($"[ChatBubbleUI] 스프라이트와 함께 초기화 - UserSprite: {userSprite?.name ?? "null"}, CharacterSprite: {characterSprite?.name ?? "null"}");

            // 런타임 스프라이트 설정
            if (userSprite != null)
                _userBubbleSprite = userSprite;
            if (characterSprite != null)
                _characterBubbleSprite = characterSprite;


            // 기본 초기화 호출
            Initialize(actor, text, displayTime, manager);
        }
        
        public void StartQueueSlideAnimation()
        {
            if (!_isToastAnimationComplete) return;
            
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            
            _animationCoroutine = StartCoroutine(QueueSlideAnimation());
        }
        
        public void CompleteTyping()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }
            
            _isTyping = false;
            _typingProgress = 1f;
            
            if (_textComponent != null)
            {
                _textComponent.text = _fullText;
            }
            
            OnBubbleTypingComplete?.Invoke(this);
            
            StartAutoDestroy();
        }
        
        public void StartFadeOut()
        {
            if (_isAnimating) return;
            
            StartCoroutine(FadeOutAnimation());
        }
        
        public void ForceComplete()
        {
            if (_isTyping)
            {
                CompleteTyping();
            }
        }
        
        public void ForceDestroy()
        {
            StartFadeOut();
        }
        
        /// <summary>
        /// 버블 스프라이트를 런타임에서 설정합니다.
        /// </summary>
        public void SetBubbleSprites(Sprite? userSprite, Sprite? characterSprite)
        {
            Debug.Log($"[ChatBubbleUI] SetBubbleSprites 호출 - UserSprite: {userSprite?.name ?? "null"}, CharacterSprite: {characterSprite?.name ?? "null"}");

            _userBubbleSprite = userSprite;
            _characterBubbleSprite = characterSprite;

            Debug.Log($"[ChatBubbleUI] 스프라이트 설정 완료 - 현재 Actor: {_actor}");

            // 스타일 다시 적용
            ApplyStyle();
        }
        
        
        #endregion
        
        #region Private Methods
        
        private void Initialize()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
                
            _canvasGroup = GetOrCreateCanvasGroup();
                
            if (_textComponent == null)
                _textComponent = GetComponentInChildren<TextMeshProUGUI>();
                
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();
            
            SetupLayoutComponents();
                
            ValidateComponents();
        }
        
        private void SetupLayoutComponents()
        {
            if (_contentSizeFitter == null)
            {
                _contentSizeFitter = GetComponent<ContentSizeFitter>();
                if (_contentSizeFitter == null)
                {
                    _contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
                    Debug.Log($"[ChatBubbleUI] ContentSizeFitter가 자동으로 추가되었습니다: {gameObject.name}");
                }
            }
            
            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
                if (_layoutElement == null)
                {
                    _layoutElement = gameObject.AddComponent<LayoutElement>();
                    Debug.Log($"[ChatBubbleUI] LayoutElement가 자동으로 추가되었습니다: {gameObject.name}");
                }
            }
        }
        
        private CanvasGroup GetOrCreateCanvasGroup()
        {
            CanvasGroup? canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                Debug.Log($"[ChatBubbleUI] CanvasGroup 컴포넌트가 자동으로 추가되었습니다: {gameObject.name}");
            }
            return canvasGroup;
        }
        
        private void ValidateComponents()
        {
            if (_rectTransform == null)
            {
                Debug.LogError($"[ChatBubbleUI] RectTransform이 없습니다: {gameObject.name}");
            }
            
            if (_textComponent == null)
            {
                Debug.LogWarning($"[ChatBubbleUI] TextMeshProUGUI가 없습니다: {gameObject.name}");
            }
            
            if (_backgroundImage == null)
            {
                Debug.LogWarning($"[ChatBubbleUI] Image 컴포넌트가 없습니다: {gameObject.name}");
            }
        }
        
        private float GetEasing(float t, EasingType easingType)
        {
            switch (easingType)
            {
                case EasingType.Bounce:
                    return EaseOutBounce(t);
                case EasingType.Quart:
                    return EaseOutQuart(t);
                case EasingType.Back:
                    return EaseOutBack(t);
                case EasingType.Elastic:
                    return EaseOutElastic(t);
                default:
                    return EaseOutQuart(t);
            }
        }
        
        private void ApplyStyle()
        {
            Debug.Log($"[ChatBubbleUI] ApplyStyle 호출 - Actor: {_actor}");

            if (_backgroundImage != null)
            {
                // 항상 스프라이트 기반 스타일만 사용
                Sprite? targetSprite = _actor == Actor.User ? _userBubbleSprite : _characterBubbleSprite;
                Debug.Log($"[ChatBubbleUI] 대상 스프라이트: {targetSprite?.name ?? "null"} (Actor: {_actor})");

                if (targetSprite != null)
                {
                    _backgroundImage.sprite = targetSprite;
                    // 스프라이트를 사용할 때는 색상을 흰색으로 설정하여 원본 색상이 나오도록 함
                    _backgroundImage.color = Color.white;
                    Debug.Log($"[ChatBubbleUI] 스프라이트 적용 완료: {targetSprite.name}");
                }
                else
                {
                    Debug.LogError($"[ChatBubbleUI] {_actor} 스프라이트가 설정되지 않았습니다! Inspector에서 스프라이트를 할당해주세요.");
                }
            }
            else
            {
                Debug.LogError($"[ChatBubbleUI] Background Image가 null입니다!");
            }
        }
        
        private void StartToastAnimation()
        {
            if (_isAnimating || _rectTransform == null) return;
            
            _isAnimating = true;
            
            InitializeAnimation();
            
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            
            _animationCoroutine = StartCoroutine(ToastBounceAnimation());
        }
        
        private void InitializeAnimation()
        {
            if (_canvasGroup == null) return;
            
            _canvasGroup.alpha = 0f;
            
            if (_rectTransform == null) return;
            
            Vector3 startPosition = _originalPosition;
            startPosition.y -= _toastBounceHeight;
            _rectTransform.localPosition = startPosition;
            
            _rectTransform.localScale = Vector3.zero;
        }
        
        private IEnumerator ToastBounceAnimation()
        {
            if (_rectTransform == null) yield break;
            
            float elapsed = 0f;
            float duration = _toastBounceDuration;
            
            Vector3 startPosition = _rectTransform.localPosition;
            Vector3 targetPosition = _originalPosition;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = _originalScale;
            
            while (elapsed < duration * 0.6f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration * 0.6f);
                float easeProgress = GetEasing(progress, _bounceEasing);
                
                _rectTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, easeProgress);
                _rectTransform.localScale = Vector3.Lerp(startScale, targetScale, easeProgress);
                
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = easeProgress;
                }
                
                yield return null;
            }
            
            Vector3 bouncePosition = targetPosition;
            Vector3 bounceScale = targetScale;
            
            if (_enableBounceEffect)
            {
                bouncePosition += Vector3.up * (_toastBounceHeight * 0.3f);
            }
            
            if (_enableScaleEffect)
            {
                bounceScale = targetScale * _toastBounceScale;
            }
            
            elapsed = 0f;
            while (elapsed < duration * 0.4f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration * 0.4f);
                float easeProgress = GetEasing(progress, _bounceEasing);
                
                _rectTransform.localPosition = Vector3.Lerp(targetPosition, bouncePosition, easeProgress);
                _rectTransform.localScale = Vector3.Lerp(targetScale, bounceScale, easeProgress);
                
                yield return null;
            }
            
            elapsed = 0f;
            Vector3 finalStartPos = _rectTransform.localPosition;
            Vector3 finalStartScale = _rectTransform.localScale;
            
            while (elapsed < duration * 0.3f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration * 0.3f);
                float easeProgress = GetEasing(progress, _bounceEasing);
                
                _rectTransform.localPosition = Vector3.Lerp(finalStartPos, _originalPosition, easeProgress);
                _rectTransform.localScale = Vector3.Lerp(finalStartScale, _originalScale, easeProgress);
                
                yield return null;
            }
            
            _rectTransform.localPosition = _originalPosition;
            _rectTransform.localScale = _originalScale;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
            
            _isAnimating = false;
            _isToastAnimationComplete = true;
            
            OnToastAnimationComplete?.Invoke(this);
            
            StartTextAnimation();
        }
        
        private IEnumerator QueueSlideAnimation()
        {
            if (_rectTransform == null) yield break;
            
            Vector3 startPosition = _rectTransform.localPosition;
            
            Canvas.ForceUpdateCanvases();
            Vector3 targetPosition = _rectTransform.localPosition;
            
            _rectTransform.localPosition = startPosition;
            
            float elapsed = 0f;
            float duration = _queueSlideDuration;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float easeProgress = GetEasing(progress, _queueEasing);
                
                _rectTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, easeProgress);
                
                yield return null;
            }
            
            _rectTransform.localPosition = targetPosition;
            _originalPosition = targetPosition;
        }
        
        private void StartTextAnimation()
        {
            if (_textComponent == null) return;
            
            _textComponent.text = "";
            _isTyping = true;
            _typingProgress = 0f;
            
            if (_actor == Actor.User)
            {
                _textComponent.text = _fullText;
                CompleteTyping();
            }
            else
            {
                _typingCoroutine = StartCoroutine(TypeText());
            }
        }
        
        private IEnumerator TypeText()
        {
            if (_textComponent == null) yield break;
            
            int totalCharacters = _fullText.Length;
            int currentCharacter = 0;
            
            while (currentCharacter < totalCharacters)
            {
                _textComponent.text = _fullText.Substring(0, currentCharacter + 1);
                currentCharacter++;
                _typingProgress = (float)currentCharacter / totalCharacters;
                
                yield return new WaitForSeconds(_typingSpeed);
            }
            
            CompleteTyping();
        }
        
        private void StartAutoDestroy()
        {
            if (_enableAutoDestroy && _displayTime > 0f)
            {
                StartCoroutine(AutoDestroyCoroutine());
            }
        }
        
        private IEnumerator AutoDestroyCoroutine()
        {
            yield return new WaitForSeconds(_displayTime);
            
            StartFadeOut();
        }
        
        private IEnumerator FadeOutAnimation()
        {
            _isAnimating = true;
            
            float fadeOutTime = _slideOutDuration;
            
            float elapsed = 0f;
            float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
            
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeOutTime;
                
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                }
                
                yield return null;
            }
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            
            OnBubbleDestroyed?.Invoke(this);
            
            Destroy(gameObject);
        }
        
        private float EaseOutBounce(float t)
        {
            if (t < 1f / 2.75f)
            {
                return 7.5625f * t * t;
            }
            else if (t < 2f / 2.75f)
            {
                return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
            }
            else if (t < 2.5f / 2.75f)
            {
                return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
            }
            else
            {
                return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
            }
        }
        
        private float EaseOutQuart(float t)
        {
            return 1f - Mathf.Pow(1f - t, 4f);
        }
        
        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        
        private float EaseOutElastic(float t)
        {
            float c4 = (2f * Mathf.PI) / 3f;
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
        
        #endregion
    }
} 