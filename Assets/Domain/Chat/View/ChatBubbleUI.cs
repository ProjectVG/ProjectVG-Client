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
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private TextMeshProUGUI _textComponent;
        [SerializeField] private Image _backgroundImage;
        
        // CanvasGroup은 동적으로 생성되므로 SerializeField 제거
        private CanvasGroup _canvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private float _slideInDuration = 0.25f;
        [SerializeField] private float _slideOutDuration = 0.03f;
        [SerializeField] private float _typingSpeed = 0.025f;
        [SerializeField] private float _maintainDuration = 1.5f;
        
        [Header("Style Settings")]
        [SerializeField] private Color _userBubbleColor = Color.blue;
        [SerializeField] private Color _characterBubbleColor = Color.gray;
        
        [Header("Layout Settings")]
        [SerializeField] private ContentSizeFitter _contentSizeFitter;
        [SerializeField] private LayoutElement _layoutElement;
        
        private Actor _actor;
        private string _fullText;
        private float _displayTime;
        
        private bool _isInitialized = false;
        private bool _isAnimating = false;
        private bool _isTyping = false;
        private float _typingProgress = 0f;
        private Coroutine _typingCoroutine;
        private Coroutine _lifetimeCoroutine;
        
        private ChatBubbleManager _manager;
        
        // 이벤트
        public event Action<ChatBubbleUI> OnBubbleCreated;
        public event Action<ChatBubbleUI> OnBubbleTypingComplete;
        public event Action<ChatBubbleUI> OnBubbleDestroyed;
        
        // 프로퍼티
        public Actor Actor => _actor;
        public string Text => _fullText;
        public float DisplayTime => _displayTime;
        public bool IsAnimating => _isAnimating;
        public bool IsTyping => _isTyping;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        /// <summary>
        /// 컴포넌트 초기화 - Unity 베스트 프랙티스 적용
        /// </summary>
        private void InitializeComponents()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
                
            _canvasGroup = GetOrCreateCanvasGroup();
                
            if (_textComponent == null)
                _textComponent = GetComponentInChildren<TextMeshProUGUI>();
                
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();
            
            // Layout 컴포넌트 자동 설정
            SetupLayoutComponents();
                
            ValidateComponents();
        }
        
        /// <summary>
        /// Layout 컴포넌트 자동 설정
        /// </summary>
        private void SetupLayoutComponents()
        {
            // ContentSizeFitter 설정
            if (_contentSizeFitter == null)
            {
                _contentSizeFitter = GetComponent<ContentSizeFitter>();
                if (_contentSizeFitter == null)
                {
                    _contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
                    Debug.Log($"ChatBubbleUI에 ContentSizeFitter가 자동으로 추가되었습니다: {gameObject.name}");
                }
            }
            
            // LayoutElement 설정
            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
                if (_layoutElement == null)
                {
                    _layoutElement = gameObject.AddComponent<LayoutElement>();
                    Debug.Log($"ChatBubbleUI에 LayoutElement가 자동으로 추가되었습니다: {gameObject.name}");
                }
            }
            
            // 크기는 ContentSizeFitter와 GridLayoutGroup에 완전히 맡김
            // 직접적인 크기 설정 제거
        }
        
        /// <summary>
        /// CanvasGroup 가져오기 또는 생성
        /// </summary>
        private CanvasGroup GetOrCreateCanvasGroup()
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                Debug.Log($"ChatBubbleUI에 CanvasGroup 컴포넌트가 자동으로 추가되었습니다: {gameObject.name}");
            }
            return canvasGroup;
        }
        
        /// <summary>
        /// 필수 컴포넌트 검증
        /// </summary>
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
        
        /// <summary>
        /// 버블 초기화
        /// </summary>
        /// <param name="actor">메시지 발신자</param>
        /// <param name="text">메시지 내용</param>
        /// <param name="displayTime">표시 시간</param>
        /// <param name="manager">ChatBubbleManager 참조 (선택사항)</param>
        public void Initialize(Actor actor, string text, float displayTime, ChatBubbleManager manager = null)
        {
            _actor = actor;
            _fullText = text;
            _displayTime = displayTime;
            _manager = manager;
            
            ApplyStyle();
            StartAnimation();
            
            _isInitialized = true;
            OnBubbleCreated?.Invoke(this);
            
            Debug.Log($"ChatBubbleUI 초기화: {actor} - {text}");
        }
        
        /// <summary>
        /// 스타일 적용
        /// </summary>
        private void ApplyStyle()
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = _actor == Actor.User ? _userBubbleColor : _characterBubbleColor;
            }
            
            // 크기와 위치는 GridLayoutGroup과 ContentSizeFitter에 완전히 맡김
            Debug.Log($"ChatBubbleUI 스타일 적용 완료: {_actor}");
        }
        
        /// <summary>
        /// 애니메이션 초기화
        /// </summary>
        private void InitializeAnimation()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            
            Debug.Log($"ChatBubbleUI 애니메이션 초기화 완료: {_actor}");
        }
        
        /// <summary>
        /// 애니메이션 시작
        /// </summary>
        private void StartAnimation()
        {
            if (_isAnimating) return;
            
            _isAnimating = true;
            Debug.Log($"ChatBubbleUI 애니메이션 시작: {_actor} - {_fullText}");
            
            // 애니메이션 초기화
            InitializeAnimation();
            
            StartCoroutine(SlideInAnimation());
        }
        
        /// <summary>
        /// 슬라이드 인 애니메이션 (토스트 스타일)
        /// </summary>
        private IEnumerator SlideInAnimation()
        {
            // GridLayoutGroup이 위치를 관리하므로 알파 애니메이션만 수행
            float slideInTime = _slideInDuration;
            float elapsed = 0f;
            
            while (elapsed < slideInTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / slideInTime;
                
                // 알파 페이드 인만 수행
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = progress;
                }
                
                yield return null;
            }
            
            // 최종 상태 설정
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
            
            _isAnimating = false;
            Debug.Log($"ChatBubbleUI 페이드인 애니메이션 완료: {_actor}");
            
            // 텍스트 출력 시작
            StartTextAnimation();
        }
        
        /// <summary>
        /// 텍스트 애니메이션 시작
        /// </summary>
        private void StartTextAnimation()
        {
            if (_textComponent == null) return;
            
            _textComponent.text = "";
            _isTyping = true;
            _typingProgress = 0f;
            
            Debug.Log($"ChatBubbleUI 텍스트 애니메이션 시작: {_actor} - {_fullText}");
            
            if (_actor == Actor.User)
            {
                // User는 즉시 출력
                _textComponent.text = _fullText;
                Debug.Log($"ChatBubbleUI User 텍스트 즉시 출력 완료");
                CompleteTyping();
            }
            else
            {
                // Character는 타이핑 효과
                Debug.Log($"ChatBubbleUI Character 타이핑 시작");
                _typingCoroutine = StartCoroutine(TypeText());
            }
        }
        
        /// <summary>
        /// 텍스트 타이핑 애니메이션
        /// </summary>
        private IEnumerator TypeText()
        {
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
        
        /// <summary>
        /// 타이핑 완료
        /// </summary>
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
            
            // 텍스트 타이핑 완료 후에만 수명 관리 시작
            StartLifetimeManagement();
        }
        
        /// <summary>
        /// 수명 관리 시작
        /// </summary>
        private void StartLifetimeManagement()
        {
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
            }
            
            Debug.Log($"ChatBubbleUI 수명 관리 시작: {_actor}");
            _lifetimeCoroutine = StartCoroutine(LifetimeCoroutine());
        }
        
        /// <summary>
        /// 수명 관리 코루틴
        /// </summary>
        private IEnumerator LifetimeCoroutine()
        {
            // 텍스트 타이핑이 완료될 때까지 대기
            while (_isTyping)
            {
                Debug.Log($"ChatBubbleUI 타이핑 완료 대기 중: {_actor}");
                yield return null;
            }
            
            // Manager의 설정값 사용, 없으면 기본값 사용
            float maintainTime = _maintainDuration;
            
            Debug.Log($"ChatBubbleUI 유지 시간 시작: {_actor} - {maintainTime}초");
            
            // 유지 시간 대기
            yield return new WaitForSeconds(maintainTime);
            
            Debug.Log($"ChatBubbleUI 유지 시간 완료: {_actor}");
            
            // 페이드아웃 시작
            StartFadeOut();
        }
        
        /// <summary>
        /// 페이드아웃 시작
        /// </summary>
        public void StartFadeOut()
        {
            if (_isAnimating) return;
            
            Debug.Log($"ChatBubbleUI 페이드아웃 시작: {_actor}");
            StartCoroutine(FadeOutAnimation());
        }
        
        /// <summary>
        /// 페이드아웃 애니메이션 (알파만 조작)
        /// </summary>
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
                
                // 알파 페이드아웃만 수행
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                }
                
                yield return null;
            }
            
            // 완전히 투명하게
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            
            Debug.Log($"ChatBubbleUI 페이드아웃 완료: {_actor}");
            OnBubbleDestroyed?.Invoke(this);
            
            // 게임오브젝트 제거
            Destroy(gameObject);
        }
        
        /// <summary>
        /// 강제 완료 (디버깅용)
        /// </summary>
        public void ForceComplete()
        {
            if (_isTyping)
            {
                CompleteTyping();
            }
        }
        
        /// <summary>
        /// 강제 제거 (디버깅용)
        /// </summary>
        public void ForceDestroy()
        {
            StartFadeOut();
        }
        
        private void OnDestroy()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }
            
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
            }
        }
    }
} 