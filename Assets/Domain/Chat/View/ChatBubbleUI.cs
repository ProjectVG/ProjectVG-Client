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
        [SerializeField] private float _slideInDuration = 0.5f;
        [SerializeField] private float _slideOutDuration = 0.3f;
        [SerializeField] private float _typingSpeed = 0.05f;
        [SerializeField] private float _maintainDuration = 3f;
        
        [Header("Style Settings")]
        [SerializeField] private Color _userBubbleColor = Color.blue;
        [SerializeField] private Color _characterBubbleColor = Color.gray;
        [SerializeField] private Vector2 _userBubbleOffset = new Vector2(50f, 0f);
        [SerializeField] private Vector2 _characterBubbleOffset = new Vector2(-50f, 0f);
        
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
            // 1. RectTransform (필수)
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
                
            // 2. CanvasGroup (동적 생성/검색)
            _canvasGroup = GetOrCreateCanvasGroup();
                
            // 3. Text Component
            if (_textComponent == null)
                _textComponent = GetComponentInChildren<TextMeshProUGUI>();
                
            // 4. Background Image
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();
                
            // 5. 컴포넌트 검증
            ValidateComponents();
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
            
            // 정렬 설정
            if (_rectTransform != null)
            {
                Vector2 offset = _actor == Actor.User ? _userBubbleOffset : _characterBubbleOffset;
                _rectTransform.anchoredPosition = offset;
            }
        }
        
        /// <summary>
        /// 애니메이션 시작
        /// </summary>
        private void StartAnimation()
        {
            if (_isAnimating) return;
            
            _isAnimating = true;
            StartCoroutine(SlideInAnimation());
        }
        
        /// <summary>
        /// 슬라이드 인 애니메이션
        /// </summary>
        private IEnumerator SlideInAnimation()
        {
            // 초기 상태 설정
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            
            Vector2 startPos = _rectTransform.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, 100f); // 아래에서 위로
            
            float elapsed = 0f;
            
            while (elapsed < _slideInDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / _slideInDuration;
                
                // 알파 페이드 인
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = progress;
                }
                
                // 위치 이동
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
                
                yield return null;
            }
            
            // 최종 상태 설정
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
            _rectTransform.anchoredPosition = endPos;
            
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
            
            if (_actor == Actor.User)
            {
                // User는 즉시 출력
                _textComponent.text = _fullText;
                CompleteTyping();
            }
            else
            {
                // Character는 타이핑 효과
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
            
            while (currentCharacter < totalCharacters)
            {
                _textComponent.text = _fullText.Substring(0, currentCharacter + 1);
                currentCharacter++;
                _typingProgress = (float)currentCharacter / totalCharacters;
                
                yield return new WaitForSeconds(_typingSpeed);
            }
            
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
            
            OnBubbleTypingComplete?.Invoke(this);
            
            // 수명 관리 시작
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
            
            _lifetimeCoroutine = StartCoroutine(LifetimeCoroutine());
        }
        
        /// <summary>
        /// 수명 관리 코루틴
        /// </summary>
        private IEnumerator LifetimeCoroutine()
        {
            // Manager의 설정값 사용, 없으면 기본값 사용
            float maintainTime = _manager != null ? _manager.BubbleLifetime : _maintainDuration;
            
            // 유지 시간 대기
            yield return new WaitForSeconds(maintainTime);
            
            // 페이드아웃 시작
            StartFadeOut();
        }
        
        /// <summary>
        /// 페이드아웃 시작
        /// </summary>
        public void StartFadeOut()
        {
            if (_isAnimating) return;
            
            StartCoroutine(FadeOutAnimation());
        }
        
        /// <summary>
        /// 페이드아웃 애니메이션
        /// </summary>
        private IEnumerator FadeOutAnimation()
        {
            _isAnimating = true;
            
            // Manager의 설정값 사용, 없으면 기본값 사용
            float fadeOutTime = _manager != null ? _manager.FadeOutDuration : _slideOutDuration;
            
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
            
            // 완전히 투명하게
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            
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