using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Domain.Chat.View;

namespace ProjectVG.Domain.Chat.Service
{
    /// <summary>
    /// ChatBubbleUI들을 생성하고 관리하는 매니저 (기본 버전)
    /// </summary>
    public class ChatBubbleManager : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private GridLayoutGroup _gridLayoutGroup;
        [SerializeField] private ContentSizeFitter _contentSizeFitter;
        
        [Header("Bubble Settings")]
        [SerializeField] private GameObject _chatBubblePrefab;
        [SerializeField] private Transform _bubbleContainer;
        
        private List<ChatBubbleUI> _activeBubbles = new List<ChatBubbleUI>();
        
        public int ActiveBubbleCount => _activeBubbles.Count;
        
        public event Action<ChatBubbleUI> OnBubbleCreated;
        public event Action<ChatBubbleUI> OnBubbleDestroyed;
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
                Debug.LogError("BubbleContainer가 설정되지 않았습니다! 인스펙터에서 설정해주세요.");
                return;
            }
            
            if (_chatBubblePrefab == null)
            {
                Debug.LogError("ChatBubblePrefab이 설정되지 않았습니다!");
                return;
            }
            
            Debug.Log("ChatBubbleManager 초기화 완료");
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
                GameObject bubbleObject = Instantiate(_chatBubblePrefab, _bubbleContainer);
                ChatBubbleUI bubbleUI = bubbleObject.GetComponent<ChatBubbleUI>();
                
                if (bubbleUI == null)
                {
                    Debug.LogError("ChatBubbleUI 컴포넌트를 찾을 수 없습니다!");
                    Destroy(bubbleObject);
                    return;
                }
                
                SetupCanvasGroup(bubbleObject);
                
                bubbleUI.Initialize(actor, text, displayTime, this);
                
                bubbleUI.OnBubbleDestroyed += OnBubbleDestroyed;
                
                _activeBubbles.Add(bubbleUI);
                
                OnBubbleCreated?.Invoke(bubbleUI);
                
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
        public void RemoveBubble(ChatBubbleUI bubble)
        {
            if (bubble == null) return;
            
            try
            {
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