using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Domain.Chat.View;

namespace ProjectVG.Domain.Chat.Service
{
    /// <summary>
    /// ChatBubbleUI들을 생성하고 관리하는 매니저 (Layout 시스템 활용)
    /// </summary>
    public class ChatBubbleManager : MonoBehaviour
    {
        [Header("UI Layout Components")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private VerticalLayoutGroup _verticalLayoutGroup;
        [SerializeField] private ContentSizeFitter _contentSizeFitter;
        
        [Header("Bubble Settings")]
        [SerializeField] private GameObject _chatBubblePrefab;
        [SerializeField] private Transform _bubbleContainer;
        [Range(10f, 100f)]
        [SerializeField] private float _bubbleSpacing = 30f;
        [Range(1, 20)]
        [SerializeField] private int _maxBubbles = 10;
        
        [Header("Animation Settings")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _remainingTimeAfterTyping = 2.5f;
        [Range(0.01f, 0.2f)]
        [SerializeField] private float _fadeOutDuration = 0.03f;
        [Range(0.1f, 1f)]
        [SerializeField] private float _fadeInDuration = 0.25f;
        
        [Header("Container Reference")]
        [SerializeField] private string _containerPath = "Canvas/ChatBubbleContainer";
        
        private List<ChatBubbleUI> _activeBubbles = new List<ChatBubbleUI>();
        
        public int ActiveBubbleCount => _activeBubbles.Count;
        
        // 프로퍼티들
        public float BubbleSpacing
        {
            get => _verticalLayoutGroup != null ? _verticalLayoutGroup.spacing : _bubbleSpacing;
            set
            {
                _bubbleSpacing = value;
                if (_verticalLayoutGroup != null)
                {
                    _verticalLayoutGroup.spacing = value;
                }
            }
        }
        
        public int MaxBubbles
        {
            get => _maxBubbles;
            set
            {
                _maxBubbles = Mathf.Max(1, value);
                while (_activeBubbles.Count > _maxBubbles)
                {
                    RemoveOldestBubble();
                }
            }
        }
        
        public float RemainingTimeAfterTyping
        {
            get => _remainingTimeAfterTyping;
            set => _remainingTimeAfterTyping = Mathf.Max(0.1f, value);
        }
        
        public float FadeInDuration
        {
            get => _fadeInDuration;
            set => _fadeInDuration = Mathf.Max(0.05f, value);
        }
        
        public float FadeOutDuration
        {
            get => _fadeOutDuration;
            set => _fadeOutDuration = Mathf.Max(0.005f, value);
        }
        
        // 이벤트
        public event Action<ChatBubbleUI> OnBubbleCreated;
        public event Action<ChatBubbleUI> OnBubbleDestroyed;
        public event Action<ChatBubbleUI> OnBubbleTypingComplete;
        public event Action OnAllBubblesCleared;
        
        private void Awake()
        {
            InitializeManager();
        }
        
        /// <summary>
        /// 매니저 초기화 - Layout 시스템 설정
        /// </summary>
        private void InitializeManager()
        {
            // Container 참조 설정
            if (_bubbleContainer == null)
            {
                _bubbleContainer = FindBubbleContainer();
            }
            
            if (_bubbleContainer == null)
            {
                Debug.LogError("ChatBubbleContainer를 찾을 수 없습니다!");
                return;
            }
            
            // Layout 컴포넌트 자동 설정
            SetupLayoutComponents();
            
            if (_chatBubblePrefab == null)
            {
                Debug.LogError("ChatBubblePrefab이 설정되지 않았습니다!");
                return;
            }
            
            Debug.Log("ChatBubbleManager Layout 시스템 초기화 완료");
        }
        
        /// <summary>
        /// Layout 컴포넌트 자동 설정
        /// </summary>
        private void SetupLayoutComponents()
        {
            // ScrollRect 설정
            if (_scrollRect == null)
            {
                _scrollRect = _bubbleContainer.GetComponentInParent<ScrollRect>();
                if (_scrollRect == null)
                {
                    Debug.LogWarning("ScrollRect를 찾을 수 없습니다. 자동으로 추가합니다.");
                    _scrollRect = _bubbleContainer.gameObject.AddComponent<ScrollRect>();
                }
            }
            
            // ScrollRect 필수 컴포넌트 확인
            if (_scrollRect != null)
            {
                if (_scrollRect.content == null)
                {
                    _scrollRect.content = _bubbleContainer.GetComponent<RectTransform>();
                    Debug.Log("ScrollRect content가 자동으로 설정되었습니다.");
                }
                
                if (_scrollRect.viewport == null)
                {
                    // Viewport가 없으면 ScrollRect 자체를 viewport로 사용
                    _scrollRect.viewport = _scrollRect.GetComponent<RectTransform>();
                    Debug.Log("ScrollRect viewport가 자동으로 설정되었습니다.");
                }
            }
            
            // VerticalLayoutGroup 설정
            if (_verticalLayoutGroup == null)
            {
                _verticalLayoutGroup = _bubbleContainer.GetComponent<VerticalLayoutGroup>();
                if (_verticalLayoutGroup == null)
                {
                    _verticalLayoutGroup = _bubbleContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                    Debug.Log("VerticalLayoutGroup이 자동으로 추가되었습니다.");
                }
            }
            
            // VerticalLayoutGroup 설정
            _verticalLayoutGroup.spacing = _bubbleSpacing;
            _verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            _verticalLayoutGroup.childControlWidth = true;
            _verticalLayoutGroup.childControlHeight = true;
            _verticalLayoutGroup.childForceExpandWidth = false;
            _verticalLayoutGroup.childForceExpandHeight = false;
            
            // ContentSizeFitter 설정
            if (_contentSizeFitter == null)
            {
                _contentSizeFitter = _bubbleContainer.GetComponent<ContentSizeFitter>();
                if (_contentSizeFitter == null)
                {
                    _contentSizeFitter = _bubbleContainer.gameObject.AddComponent<ContentSizeFitter>();
                    Debug.Log("ContentSizeFitter가 자동으로 추가되었습니다.");
                }
            }
            
            _contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        
        /// <summary>
        /// Bubble Container 찾기
        /// </summary>
        private Transform FindBubbleContainer()
        {
            // 1. Inspector에서 설정된 경로로 찾기
            if (!string.IsNullOrEmpty(_containerPath))
            {
                Transform container = transform.root.Find(_containerPath);
                if (container != null)
                {
                    return container;
                }
            }
            
            // 2. Canvas 내부에서 "ChatBubbleContainer" 이름으로 찾기
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform container = canvas.transform.Find("ChatBubbleContainer");
                if (container != null)
                {
                    return container;
                }
            }
            
            // 3. 전체 씬에서 찾기
            Transform foundContainer = GameObject.Find("ChatBubbleContainer")?.transform;
            if (foundContainer != null)
            {
                return foundContainer;
            }
            
            return null;
        }
        
        /// <summary>
        /// 새로운 채팅 버블 생성
        /// </summary>
        public void CreateBubble(Actor actor, string text, float displayTime = 3f)
        {
            if (_chatBubblePrefab == null)
            {
                Debug.LogError("ChatBubblePrefab이 설정되지 않았습니다!");
                return;
            }
            
            if (_bubbleContainer == null)
            {
                Debug.LogError("BubbleContainer가 설정되지 않았습니다!");
                return;
            }
            
            try
            {
                // 프리팹 인스턴스 생성
                GameObject bubbleObject = Instantiate(_chatBubblePrefab, _bubbleContainer);
                ChatBubbleUI bubbleUI = bubbleObject.GetComponent<ChatBubbleUI>();
                
                if (bubbleUI == null)
                {
                    Debug.LogError("ChatBubbleUI 컴포넌트를 찾을 수 없습니다!");
                    Destroy(bubbleObject);
                    return;
                }
                
                // CanvasGroup 자동 설정
                SetupCanvasGroup(bubbleObject);
                
                // 버블 초기화
                bubbleUI.Initialize(actor, text, displayTime, this);
                
                // 이벤트 구독
                bubbleUI.OnBubbleDestroyed += OnBubbleDestroyed;
                
                // 리스트에 추가
                _activeBubbles.Add(bubbleUI);
                
                OnBubbleCreated?.Invoke(bubbleUI);
                
                // 최대 개수 제한
                if (_activeBubbles.Count > _maxBubbles)
                {
                    RemoveOldestBubble();
                }
                
                // 스크롤을 맨 아래로 이동
                StartCoroutine(ScrollToBottom());
                
                Debug.Log($"새로운 버블 생성: {actor} - {text}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"버블 생성 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 스크롤을 맨 아래로 이동
        /// </summary>
        private System.Collections.IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            
            if (_scrollRect != null && _scrollRect.content != null)
            {
                try
                {
                    _scrollRect.verticalNormalizedPosition = 0f;
                    Debug.Log("스크롤을 맨 아래로 이동했습니다.");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"스크롤 이동 실패: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("ScrollRect 또는 content가 설정되지 않았습니다.");
            }
        }
        
        /// <summary>
        /// 버블 제거
        /// </summary>
        public void RemoveBubble(ChatBubbleUI bubble)
        {
            if (bubble == null) return;
            
            try
            {
                // 이벤트 구독 해제
                if (bubble != null)
                {
                    bubble.OnBubbleDestroyed -= OnBubbleDestroyed;
                }
                
                _activeBubbles.Remove(bubble);
                
                OnBubbleDestroyed?.Invoke(bubble);
                
                Debug.Log("버블 제거 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"버블 제거 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 가장 오래된 버블 제거
        /// </summary>
        private void RemoveOldestBubble()
        {
            if (_activeBubbles.Count > 0)
            {
                ChatBubbleUI oldestBubble = _activeBubbles[0];
                RemoveBubble(oldestBubble);
            }
        }
        
        /// <summary>
        /// 모든 버블 제거
        /// </summary>
        public void ClearAllBubbles()
        {
            try
            {
                foreach (var bubble in _activeBubbles.ToArray())
                {
                    if (bubble != null)
                    {
                        Destroy(bubble.gameObject);
                    }
                }
                
                _activeBubbles.Clear();
                
                OnAllBubblesCleared?.Invoke();
                
                Debug.Log("모든 버블 제거 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"모든 버블 제거 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// CanvasGroup 자동 설정
        /// </summary>
        private void SetupCanvasGroup(GameObject bubbleObject)
        {
            CanvasGroup canvasGroup = bubbleObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = bubbleObject.AddComponent<CanvasGroup>();
                Debug.Log($"ChatBubble에 CanvasGroup이 자동으로 추가되었습니다: {bubbleObject.name}");
            }
            
            canvasGroup.alpha = 0f;
        }
        
        private void OnDestroy()
        {
            ClearAllBubbles();
        }
    }
} 