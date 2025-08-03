#nullable enable
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Domain.Chat.Service;

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
        [SerializeField] private float _slideInDuration = 0.25f;
        [SerializeField] private float _slideOutDuration = 0.03f;
        [SerializeField] private float _typingSpeed = 0.025f;
        [SerializeField] private bool _enableAutoDestroy = true;
        [SerializeField] private float _defaultDisplayTime = 3f;
        
        [Header("Toast Animation Settings")]
        [SerializeField] private float _toastBounceDuration = 0.3f;
        [SerializeField] private float _toastBounceHeight = 20f;
        [SerializeField] private float _toastBounceScale = 1.1f;
        [SerializeField] private float _queueSlideDuration = 0.2f;
        [SerializeField] private float _queueSlideDistance = 10f;
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
        
        [Header("Style Settings")]
        [SerializeField] private Color _userBubbleColor = Color.blue;
        [SerializeField] private Color _characterBubbleColor = Color.gray;
        
        [Header("Layout Settings")]
        [SerializeField] private ContentSizeFitter? _contentSizeFitter;
        [SerializeField] private LayoutElement? _layoutElement;
        
        private Actor _actor;
        private string _fullText = string.Empty;
        private float _displayTime;
        
        private bool _isInitialized = false;
        private bool _isAnimating = false;
        private bool _isTyping = false;
        private float _typingProgress = 0f;
        private Coroutine? _typingCoroutine;
        private Coroutine? _animationCoroutine;
        
        private ChatBubbleManager? _manager;
        
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
        
        public void Initialize(Actor actor, string text, float displayTime, ChatBubbleManager? manager = null)
        {
            _actor = actor;
            _fullText = text;
            _displayTime = displayTime < 0f ? _defaultDisplayTime : displayTime;
            _manager = manager;
            
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
                Debug.Log($"ChatBubbleUI User 타입 즉시 타이핑 완료 상태로 설정");
            }
            
            _isInitialized = true;
            OnBubbleCreated?.Invoke(this);
            
            Debug.Log($"ChatBubbleUI 초기화: {actor} - {text}");
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
            
            Debug.Log($"ChatBubbleUI 타이핑 완료: {_actor} - {_fullText}");
            OnBubbleTypingComplete?.Invoke(this);
            
            StartAutoDestroy();
        }
        
        public void StartFadeOut()
        {
            if (_isAnimating) return;
            
            Debug.Log($"ChatBubbleUI 페이드아웃 시작: {_actor}");
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
                    Debug.Log($"ChatBubbleUI에 ContentSizeFitter가 자동으로 추가되었습니다: {gameObject.name}");
                }
            }
            
            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
                if (_layoutElement == null)
                {
                    _layoutElement = gameObject.AddComponent<LayoutElement>();
                    Debug.Log($"ChatBubbleUI에 LayoutElement가 자동으로 추가되었습니다: {gameObject.name}");
                }
            }
        }
        
        private CanvasGroup GetOrCreateCanvasGroup()
        {
            CanvasGroup? canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                Debug.Log($"ChatBubbleUI에 CanvasGroup 컴포넌트가 자동으로 추가되었습니다: {gameObject.name}");
            }
            return canvasGroup;
        }
        
        private void ValidateComponents()
        {
            if (_rectTransform == null)
            {
                Debug.LogError($"ChatBubbleUI에 RectTransform이 없습니다: {gameObject.name}");
            }
            
            if (_textComponent == null)
            {
                Debug.LogWarning($"ChatBubbleUI에 TextMeshProUGUI가 없습니다: {gameObject.name}");
            }
            
            if (_backgroundImage == null)
            {
                Debug.LogWarning($"ChatBubbleUI에 Image 컴포넌트가 없습니다: {gameObject.name}");
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
            if (_backgroundImage != null)
            {
                _backgroundImage.color = _actor == Actor.User ? _userBubbleColor : _characterBubbleColor;
            }
            
            Debug.Log($"ChatBubbleUI 스타일 적용 완료: {_actor}");
        }
        
        private void StartToastAnimation()
        {
            if (_isAnimating || _rectTransform == null) return;
            
            _isAnimating = true;
            Debug.Log($"ChatBubbleUI 토스트 애니메이션 시작: {_actor} - {_fullText}");
            
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
            
            Debug.Log($"ChatBubbleUI 애니메이션 초기화 완료: {_actor}");
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
            
            Debug.Log($"ChatBubbleUI 토스트 애니메이션 완료: {_actor}");
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
            
            Debug.Log($"ChatBubbleUI 큐 슬라이드 애니메이션 완료: {_actor}");
        }
        
        private void StartTextAnimation()
        {
            if (_textComponent == null) return;
            
            _textComponent.text = "";
            _isTyping = true;
            _typingProgress = 0f;
            
            Debug.Log($"ChatBubbleUI 텍스트 애니메이션 시작: {_actor} - {_fullText}");
            
            if (_actor == Actor.User)
            {
                _textComponent.text = _fullText;
                Debug.Log($"ChatBubbleUI User 텍스트 즉시 출력 완료");
                CompleteTyping();
            }
            else
            {
                Debug.Log($"ChatBubbleUI Character 타이핑 시작");
                _typingCoroutine = StartCoroutine(TypeText());
            }
        }
        
        private IEnumerator TypeText()
        {
            if (_textComponent == null) yield break;
            
            int totalCharacters = _fullText.Length;
            int currentCharacter = 0;
            
            Debug.Log($"ChatBubbleUI 타이핑 시작: 총 {totalCharacters}자");
            
            while (currentCharacter < totalCharacters)
            {
                _textComponent.text = _fullText.Substring(0, currentCharacter + 1);
                currentCharacter++;
                _typingProgress = (float)currentCharacter / totalCharacters;
                
                yield return new WaitForSeconds(_typingSpeed);
            }
            
            Debug.Log($"ChatBubbleUI 타이핑 완료: {totalCharacters}자");
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
            
            Debug.Log($"ChatBubbleUI 자동 삭제 시작: {_actor} - {_fullText}");
            StartFadeOut();
        }
        
        private IEnumerator FadeOutAnimation()
        {
            _isAnimating = true;
            
            float fadeOutTime = _slideOutDuration;
            
            Debug.Log($"ChatBubbleUI 페이드아웃 애니메이션 시작: {_actor} - {fadeOutTime}초");
            
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
            
            Debug.Log($"ChatBubbleUI 페이드아웃 완료: {_actor}");
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