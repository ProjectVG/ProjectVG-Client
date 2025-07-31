using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Domain.Chat.View;

namespace ProjectVG.Domain.Chat.Service
{
    /// <summary>
    /// ChatBubbleUI들을 생성하고 관리하는 매니저
    /// </summary>
    public class ChatBubbleManager : MonoBehaviour
    {
        [Header("Bubble Settings")]
        [SerializeField] private GameObject _chatBubblePrefab;
        [SerializeField] private Transform _bubbleContainer;
        [Range(0f, 50f)]
        [SerializeField] private float _bubbleSpacing = 10f;
        [Range(1, 20)]
        [SerializeField] private int _maxBubbles = 10;
        
        [Header("Animation Settings")]
        [Range(1f, 10f)]
        [SerializeField] private float _bubbleLifetime = 5f;
        [Range(0.1f, 3f)]
        [SerializeField] private float _fadeOutDuration = 1f;
        
        [Header("Container Reference")]
        [SerializeField] private string _containerPath = "Canvas/ChatBubbleContainer";
        
        private Queue<ChatBubbleUI> _bubbleQueue = new Queue<ChatBubbleUI>();
        private List<ChatBubbleUI> _activeBubbles = new List<ChatBubbleUI>();
        
        public int ActiveBubbleCount => _activeBubbles.Count;
        public int QueueCount => _bubbleQueue.Count;
        
        // 슬라이더 조절용 프로퍼티들
        public float BubbleSpacing
        {
            get => _bubbleSpacing;
            set
            {
                _bubbleSpacing = value;
                UpdateBubblePositions(); // 즉시 위치 업데이트
            }
        }
        
        public int MaxBubbles
        {
            get => _maxBubbles;
            set
            {
                _maxBubbles = Mathf.Max(1, value); // 최소값 1 보장
                // 현재 버블 개수가 새로운 최대값을 초과하면 오래된 버블들 제거
                while (_activeBubbles.Count > _maxBubbles)
                {
                    RemoveOldestBubble();
                }
            }
        }
        
        public float BubbleLifetime
        {
            get => _bubbleLifetime;
            set => _bubbleLifetime = Mathf.Max(0.1f, value); // 최소값 0.1초 보장
        }
        
        public float FadeOutDuration
        {
            get => _fadeOutDuration;
            set => _fadeOutDuration = Mathf.Max(0.1f, value); // 최소값 0.1초 보장
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
        /// 매니저 초기화 - Canvas 외부에서 Container 참조 설정
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
                Debug.LogError("ChatBubbleContainer를 찾을 수 없습니다! Canvas 내부에 ChatBubbleContainer가 있는지 확인하세요.");
                return;
            }
            
            if (_chatBubblePrefab == null)
            {
                Debug.LogError("ChatBubblePrefab이 설정되지 않았습니다!");
                return;
            }
            
            Debug.Log("ChatBubbleTestUI 초기화 완료");
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
        /// <param name="actor">메시지 발신자</param>
        /// <param name="text">메시지 내용</param>
        /// <param name="displayTime">표시 시간</param>
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
                
                // CanvasGroup 자동 설정 (Unity 베스트 프랙티스)
                SetupCanvasGroup(bubbleObject);
                
                // 버블 초기화 (Manager 참조 전달)
                bubbleUI.Initialize(actor, text, displayTime, this);
                
                // 이벤트 구독
                bubbleUI.OnBubbleDestroyed += OnBubbleDestroyed;
                
                // 큐에 추가
                _bubbleQueue.Enqueue(bubbleUI);
                _activeBubbles.Add(bubbleUI);
                
                OnBubbleCreated?.Invoke(bubbleUI);
                
                // 위치 업데이트
                UpdateBubblePositions();
                
                // 최대 개수 제한
                if (_activeBubbles.Count > _maxBubbles)
                {
                    RemoveOldestBubble();
                }
                
                Debug.Log($"새로운 버블 생성: {actor} - {text}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"버블 생성 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 버블 제거
        /// </summary>
        /// <param name="bubble">제거할 버블</param>
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
                _bubbleQueue = new Queue<ChatBubbleUI>(_activeBubbles);
                
                OnBubbleDestroyed?.Invoke(bubble);
                
                UpdateBubblePositions();
                
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
                _bubbleQueue.Clear();
                
                OnAllBubblesCleared?.Invoke();
                
                Debug.Log("모든 버블 제거 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"모든 버블 제거 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 버블 위치 업데이트
        /// </summary>
        private void UpdateBubblePositions()
        {
            float currentY = 0f;
            
            for (int i = 0; i < _activeBubbles.Count; i++)
            {
                ChatBubbleUI bubble = _activeBubbles[i];
                if (bubble != null)
                {
                    RectTransform rectTransform = bubble.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        Vector3 position = rectTransform.anchoredPosition;
                        position.y = currentY;
                        rectTransform.anchoredPosition = position;
                        
                        currentY += rectTransform.rect.height + _bubbleSpacing;
                    }
                }
            }
        }
        
        /// <summary>
        /// CanvasGroup 자동 설정 (Unity 베스트 프랙티스)
        /// </summary>
        private void SetupCanvasGroup(GameObject bubbleObject)
        {
            // CanvasGroup이 없으면 자동으로 추가
            CanvasGroup canvasGroup = bubbleObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = bubbleObject.AddComponent<CanvasGroup>();
                Debug.Log($"ChatBubble에 CanvasGroup이 자동으로 추가되었습니다: {bubbleObject.name}");
            }
            
            // 초기 상태 설정
            canvasGroup.alpha = 0f; // 페이드 인을 위해 초기값 설정
        }

        
        private void OnDestroy()
        {
            ClearAllBubbles();
        }
    }
} 