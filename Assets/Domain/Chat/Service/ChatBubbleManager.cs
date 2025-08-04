#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Domain.Chat.View;

namespace ProjectVG.Domain.Chat.Service
{
    public class ChatBubbleManager : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private ScrollRect? _scrollRect;
        [SerializeField] private GridLayoutGroup? _gridLayoutGroup;
        [SerializeField] private ContentSizeFitter? _contentSizeFitter;
        
        [Header("Bubble Settings")]
        [SerializeField] private GameObject? _chatBubblePrefab;
        [SerializeField] private Transform? _bubbleContainer;
        
        [Header("Queue Animation Settings")]
        [SerializeField] private bool _enableQueueAnimation = true;
        [SerializeField] private float _queueAnimationDelay = 0.1f;
        
        [Header("Performance Settings")]
        [SerializeField] private int _maxBubbles = 20;
        [SerializeField] private bool _autoCleanup = true;
        [SerializeField] private int _cleanupThreshold = 15;
        
        private List<ChatBubbleUI> _activeBubbles = new List<ChatBubbleUI>();
        
        public int ActiveBubbleCount => _activeBubbles.Count;
        
        public event Action<ChatBubbleUI>? OnBubbleCreated;
        public event Action<ChatBubbleUI>? OnBubbleDestroyed;
        public event Action? OnAllBubblesCleared;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            Initialize();
        }
        
        private void OnDestroy()
        {
            ClearAllBubbles();
        }
        
        #endregion
        
        #region Public Methods
        
        public void CreateBubble(Actor actor, string text, float displayTime = -1f)
        {
            if (_chatBubblePrefab == null || _bubbleContainer == null)
            {
                Debug.LogError("[ChatBubbleManager] ChatBubblePrefab 또는 BubbleContainer가 설정되지 않았습니다!");
                return;
            }
            
            try
            {
                GameObject? bubbleObject = Instantiate(_chatBubblePrefab, _bubbleContainer);
                ChatBubbleUI? bubbleUI = bubbleObject?.GetComponent<ChatBubbleUI>();
                
                if (bubbleUI == null)
                {
                    Debug.LogError("[ChatBubbleManager] ChatBubbleUI 컴포넌트를 찾을 수 없습니다!");
                    if (bubbleObject != null)
                        Destroy(bubbleObject);
                    return;
                }
                
                SetupCanvasGroup(bubbleObject);
                
                if (_activeBubbles.Count >= _maxBubbles)
                {
                    Debug.LogWarning($"[ChatBubbleManager] 최대 버블 수({_maxBubbles})에 도달했습니다. 가장 오래된 버블을 제거합니다.");
                    RemoveOldestBubble();
                }
                
                if (_autoCleanup && _activeBubbles.Count >= _cleanupThreshold)
                {
                    CleanupOldBubbles();
                }
                
                if (_enableQueueAnimation && _activeBubbles.Count > 0)
                {
                    StartQueueAnimationForExistingBubbles();
                }
                
                bubbleUI.Initialize(actor, text, displayTime, this);
                
                bubbleUI.OnBubbleDestroyed += OnBubbleDestroyed;
                bubbleUI.OnToastAnimationComplete += OnBubbleToastAnimationComplete;
                
                _activeBubbles.Add(bubbleUI);
                
                OnBubbleCreated?.Invoke(bubbleUI);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatBubbleManager] 버블 생성 실패: {ex.Message}");
            }
        }
        
        public void RemoveBubble(ChatBubbleUI? bubble)
        {
            if (bubble == null) return;
            
            try
            {
                bubble.OnBubbleDestroyed -= OnBubbleDestroyed;
                bubble.OnToastAnimationComplete -= OnBubbleToastAnimationComplete;
                
                _activeBubbles.Remove(bubble);
                
                OnBubbleDestroyed?.Invoke(bubble);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatBubbleManager] 버블 제거 실패: {ex.Message}");
            }
        }
        
        public void ClearAllBubbles()
        {
            try
            {
                foreach (var bubble in _activeBubbles.ToArray())
                {
                    if (bubble != null)
                    {
                        bubble.OnBubbleDestroyed -= OnBubbleDestroyed;
                        bubble.OnToastAnimationComplete -= OnBubbleToastAnimationComplete;
                        Destroy(bubble.gameObject);
                    }
                }
                
                _activeBubbles.Clear();
                
                OnAllBubblesCleared?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatBubbleManager] 모든 버블 제거 실패: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void Initialize()
        {
            if (_bubbleContainer == null)
            {
                Debug.LogError("[ChatBubbleManager] BubbleContainer가 설정되지 않았습니다! 인스펙터에서 설정해주세요.");
                return;
            }
            
            if (_chatBubblePrefab == null)
            {
                Debug.LogError("[ChatBubbleManager] ChatBubblePrefab이 설정되지 않았습니다!");
                return;
            }
            
            Debug.Log("[ChatBubbleManager] 초기화 완료");
        }
        
        private void StartQueueAnimationForExistingBubbles()
        {
            Canvas.ForceUpdateCanvases();
            
            for (int i = 0; i < _activeBubbles.Count; i++)
            {
                var bubble = _activeBubbles[i];
                if (bubble != null && bubble.IsToastAnimationComplete)
                {
                    float delay = i * _queueAnimationDelay;
                    StartCoroutine(QueueAnimationWithDelay(bubble, delay));
                }
            }
        }
        
        private System.Collections.IEnumerator QueueAnimationWithDelay(ChatBubbleUI? bubble, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (bubble != null)
            {
                bubble.StartQueueSlideAnimation();
            }
        }
        
        private void OnBubbleToastAnimationComplete(ChatBubbleUI? bubble)
        {
            if (bubble == null) return;
            
            
            if (_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        private void RemoveOldestBubble()
        {
            if (_activeBubbles.Count > 0)
            {
                var oldestBubble = _activeBubbles[0];
                RemoveBubble(oldestBubble);
                if (oldestBubble != null)
                {
                    Destroy(oldestBubble.gameObject);
                }
            }
        }
        
        private void CleanupOldBubbles()
        {
            int bubblesToRemove = _activeBubbles.Count - _cleanupThreshold;
            for (int i = 0; i < bubblesToRemove && i < _activeBubbles.Count; i++)
            {
                var bubble = _activeBubbles[0];
                RemoveBubble(bubble);
                if (bubble != null)
                {
                    Destroy(bubble.gameObject);
                }
            }
        }
        
        private void SetupCanvasGroup(GameObject? bubbleObject)
        {
            if (bubbleObject == null) return;
            
            CanvasGroup? canvasGroup = bubbleObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = bubbleObject.AddComponent<CanvasGroup>();
                Debug.Log($"[ChatBubbleManager] ChatBubble에 CanvasGroup이 자동으로 추가되었습니다: {bubbleObject.name}");
            }
            
            canvasGroup.alpha = 0f;
        }
        
        #endregion
    }
} 