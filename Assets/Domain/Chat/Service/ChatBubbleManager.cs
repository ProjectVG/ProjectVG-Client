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
        
        [Header("Container Reference")]
        [SerializeField] private string _containerPath = "Canvas/ChatBubbleContainer";
        
        private List<ChatBubbleUI> _activeBubbles = new List<ChatBubbleUI>();
        
        public int ActiveBubbleCount => _activeBubbles.Count;
        
        // 이벤트
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
            
            Debug.Log("ChatBubbleManager 초기화 완료");
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
            
            // GridLayoutGroup 설정
            if (_gridLayoutGroup == null)
            {
                _gridLayoutGroup = _bubbleContainer.GetComponent<GridLayoutGroup>();
                if (_gridLayoutGroup == null)
                {
                    _gridLayoutGroup = _bubbleContainer.gameObject.AddComponent<GridLayoutGroup>();
                    Debug.Log("GridLayoutGroup이 자동으로 추가되었습니다.");
                }
            }
            
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