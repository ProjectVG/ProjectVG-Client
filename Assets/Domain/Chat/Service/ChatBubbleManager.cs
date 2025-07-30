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
        [SerializeField] private float _bubbleSpacing = 10f;
        [SerializeField] private int _maxBubbles = 10;
        
        [Header("Animation Settings")]
        [SerializeField] private float _bubbleLifetime = 5f;
        [SerializeField] private float _fadeOutDuration = 1f;
        
        private Queue<ChatBubbleUI> _bubbleQueue = new Queue<ChatBubbleUI>();
        private List<ChatBubbleUI> _activeBubbles = new List<ChatBubbleUI>();
        
        public int ActiveBubbleCount => _activeBubbles.Count;
        public int QueueCount => _bubbleQueue.Count;
        
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
        /// 매니저 초기화
        /// </summary>
        private void InitializeManager()
        {
            if (_bubbleContainer == null)
            {
                _bubbleContainer = transform;
            }
            
            if (_chatBubblePrefab == null)
            {
                Debug.LogError("ChatBubblePrefab이 설정되지 않았습니다!");
            }
            
            Debug.Log("ChatBubbleManager 초기화 완료");
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
                
                // 버블 초기화
                bubbleUI.Initialize(actor, text, displayTime);
                
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
        

        

        
        private void OnDestroy()
        {
            ClearAllBubbles();
        }
    }
} 