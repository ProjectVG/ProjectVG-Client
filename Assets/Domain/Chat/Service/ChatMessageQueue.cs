#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Domain.Chat.Model;

namespace ProjectVG.Domain.Chat.Service
{
    /// <summary>
    /// 채팅 메시지 큐를 관리하는 클래스
    /// </summary>
    public class ChatMessageQueue
    {
        private readonly Queue<ChatMessage> _messageQueue = new Queue<ChatMessage>();
        private readonly object _queueLock = new object();
        private readonly int _maxQueueSize;
        private bool _isProcessing = false;

        public int Count => _messageQueue.Count;
        public bool IsProcessing => _isProcessing;
        public bool IsEmpty => _messageQueue.Count == 0;

        public event Action<ChatMessage>? OnMessageProcessed;
        public event Action<string>? OnError;

        public ChatMessageQueue(int maxQueueSize = 100)
        {
            _maxQueueSize = maxQueueSize;
        }

        /// <summary>
        /// 메시지를 큐에 추가한다
        /// </summary>
        /// <param name="chatMessage">추가할 메시지</param>
        /// <returns>추가 성공 여부</returns>
        public bool Enqueue(ChatMessage chatMessage)
        {
            if (chatMessage == null)
                return false;

            lock (_queueLock) {
                if (_messageQueue.Count >= _maxQueueSize) {
                    Debug.LogWarning($"[ChatMessageQueue] 메시지 큐가 가득 찼습니다. (최대: {_maxQueueSize})");
                    return false;
                }

                _messageQueue.Enqueue(chatMessage);
            }

            return true;
        }

        /// <summary>
        /// 큐에서 메시지를 제거하고 반환한다
        /// </summary>
        /// <returns>제거된 메시지, 큐가 비어있으면 null</returns>
        public ChatMessage? Dequeue()
        {
            lock (_queueLock) {
                return _messageQueue.Count > 0 ? _messageQueue.Dequeue() : null;
            }
        }

        /// <summary>
        /// 큐의 모든 메시지를 제거한다
        /// </summary>
        public void Clear()
        {
            lock (_queueLock) {
                _messageQueue.Clear();
            }
        }

        /// <summary>
        /// 메시지 큐를 순차적으로 처리한다
        /// </summary>
        /// <param name="processAction">메시지 처리 액션</param>
        /// <returns></returns>
        public async UniTaskVoid ProcessQueueAsync(Func<ChatMessage, UniTask> processAction)
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            try {
                while (true) {
                    ChatMessage? message = Dequeue();
                    
                    if (message == null) {
                        break;
                    }

                    try {
                        await processAction(message);
                        OnMessageProcessed?.Invoke(message);
                    }
                    catch (Exception ex) {
                        Debug.LogError($"[ChatMessageQueue] 메시지 처리 중 오류: {ex.Message}");
                        OnError?.Invoke($"메시지 처리 실패: {ex.Message}");
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError($"[ChatMessageQueue] 큐 처리 중 오류: {ex.Message}");
                OnError?.Invoke($"큐 처리 실패: {ex.Message}");
            }
            finally {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 큐의 상태 정보를 반환한다
        /// </summary>
        /// <returns>큐 상태 정보</returns>
        public QueueStatus GetStatus()
        {
            lock (_queueLock) {
                return new QueueStatus {
                    Count = _messageQueue.Count,
                    MaxSize = _maxQueueSize,
                    IsProcessing = _isProcessing,
                    IsFull = _messageQueue.Count >= _maxQueueSize
                };
            }
        }
    }

    /// <summary>
    /// 큐 상태 정보
    /// </summary>
    public struct QueueStatus
    {
        public int Count;
        public int MaxSize;
        public bool IsProcessing;
        public bool IsFull;
    }
}
