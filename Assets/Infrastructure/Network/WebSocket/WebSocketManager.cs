using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Configs;

namespace ProjectVG.Infrastructure.Network.WebSocket
{
    /// <summary>
    /// WebSocket 연결 및 메시지 관리자
    /// 강제된 JSON 형식 {type: "xxx", data: {...}}을 사용합니다.
    /// </summary>
    public class WebSocketManager : MonoBehaviour
    {
        private INativeWebSocket _nativeWebSocket;
        private CancellationTokenSource _cancellationTokenSource;
        
        // 메시지 버퍼링을 위한 필드
        private readonly StringBuilder _messageBuffer = new StringBuilder();
        private readonly object _bufferLock = new object();
        
        private bool _isConnected = false;
        private bool _isConnecting = false;
        private int _reconnectAttempts = 0;
        private string _sessionId;

        public static WebSocketManager Instance { get; private set; }
        
        // 이벤트
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<string> OnSessionIdReceived;
        public event Action<string> OnChatMessageReceived;

        public bool IsConnected => _isConnected;
        public bool IsConnecting => _isConnecting;
        public string SessionId => _sessionId;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            DisconnectAsync().Forget();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        private void InitializeManager()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            InitializeNativeWebSocket();
        }

        private void InitializeNativeWebSocket()
        {
            _nativeWebSocket = WebSocketFactory.Create();
            
            _nativeWebSocket.OnConnected += OnNativeConnected;
            _nativeWebSocket.OnDisconnected += OnNativeDisconnected;
            _nativeWebSocket.OnError += OnNativeError;
            _nativeWebSocket.OnMessageReceived += OnNativeMessageReceived;
        }

        /// <summary>
        /// WebSocket 연결
        /// </summary>
        public async UniTask<bool> ConnectAsync(string sessionId = null, CancellationToken cancellationToken = default)
        {
            if (_isConnected || _isConnecting)
            {
                Debug.LogWarning("이미 연결 중이거나 연결되어 있습니다.");
                return _isConnected;
            }

            _isConnecting = true;
            _sessionId = sessionId;

            try
            {
                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;
                
                var wsUrl = GetWebSocketUrl(sessionId);
                Debug.Log($"WebSocket 연결 시도: {wsUrl}");

                var success = await _nativeWebSocket.ConnectAsync(wsUrl, combinedCancellationToken);
                
                if (success)
                {
                    _isConnected = true;
                    _isConnecting = false;
                    _reconnectAttempts = 0;
                    Debug.Log("WebSocket 연결 성공");
                    return true;
                }
                else
                {
                    _isConnecting = false;
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("WebSocket 연결이 취소되었습니다.");
                return false;
            }
            catch (Exception ex)
            {
                var error = $"WebSocket 연결 중 예외 발생: {ex.Message}";
                Debug.LogError(error);
                OnError?.Invoke(error);
                return false;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        /// <summary>
        /// WebSocket 연결 해제
        /// </summary>
        public async UniTask DisconnectAsync()
        {
            if (!_isConnected)
            {
                return;
            }

            _isConnected = false;
            _isConnecting = false;

            if (_nativeWebSocket != null)
            {
                await _nativeWebSocket.DisconnectAsync();
            }

            Debug.Log("WebSocket 연결 해제");
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 메시지 전송
        /// </summary>
        public async UniTask<bool> SendMessageAsync(string type, object data, CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                Debug.LogWarning("WebSocket이 연결되지 않았습니다.");
                return false;
            }

            try
            {
                var message = new WebSocketMessage
                {
                    type = type,
                    data = data
                };
                
                var jsonMessage = JsonUtility.ToJson(message);
                return await _nativeWebSocket.SendMessageAsync(jsonMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 전송 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 채팅 메시지 전송
        /// </summary>
        public async UniTask<bool> SendChatMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            var chatData = new ChatData
            {
                message = message,
                sessionId = _sessionId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            return await SendMessageAsync("chat", chatData, cancellationToken);
        }

        /// <summary>
        /// WebSocket URL 생성
        /// </summary>
        private string GetWebSocketUrl(string sessionId = null)
        {
            string baseUrl = NetworkConfig.GetWebSocketUrl();
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                return $"{baseUrl}?sessionId={sessionId}";
            }
            
            return baseUrl;
        }

        /// <summary>
        /// 자동 재연결 시도
        /// </summary>
        private async UniTaskVoid TryReconnectAsync()
        {
            bool autoReconnect = NetworkConfig.AutoReconnect;
            int maxReconnectAttempts = NetworkConfig.MaxReconnectAttempts;
            float reconnectDelay = NetworkConfig.ReconnectDelay;
            
            if (!autoReconnect || _reconnectAttempts >= maxReconnectAttempts)
            {
                return;
            }

            _reconnectAttempts++;
            Debug.Log($"WebSocket 재연결 시도 {_reconnectAttempts}/{maxReconnectAttempts}");
            
            await UniTask.Delay(TimeSpan.FromSeconds(reconnectDelay));
            
            if (!_isConnected)
            {
                ConnectAsync(_sessionId).Forget();
            }
        }

        #region Native WebSocket Event Handlers

        private void OnNativeConnected()
        {
            _isConnected = true;
            _isConnecting = false;
            _reconnectAttempts = 0;
            
            Debug.Log("WebSocket 연결 성공");
            OnConnected?.Invoke();
        }

        private void OnNativeDisconnected()
        {
            _isConnected = false;
            _isConnecting = false;
            
            OnDisconnected?.Invoke();
            TryReconnectAsync().Forget();
        }

        private void OnNativeError(string error)
        {
            _isConnected = false;
            _isConnecting = false;
            
            OnError?.Invoke(error);
        }

        private void OnNativeMessageReceived(string message)
        {
            try
            {
                Debug.Log($"메시지 수신: {message?.Length ?? 0} bytes");
                ProcessBufferedMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 파싱 실패: {ex.Message}");
                Debug.LogError($"원시 메시지: {message}");
            }
        }
        
        /// <summary>
        /// 메시지 버퍼링 및 완전한 JSON 메시지 처리
        /// </summary>
        private void ProcessBufferedMessage(string message)
        {
            lock (_bufferLock)
            {
                _messageBuffer.Append(message);
                string bufferedMessage = _messageBuffer.ToString();
                
                Debug.Log($"버퍼링된 메시지 길이: {bufferedMessage.Length}");
                
                if (IsCompleteJsonMessage(bufferedMessage))
                {
                    Debug.Log("완전한 JSON 메시지 감지됨. 처리 시작.");
                    
                    if (IsValidJsonMessage(bufferedMessage))
                    {
                        ProcessMessage(bufferedMessage);
                    }
                    else
                    {
                        Debug.LogWarning("JSON 형식이 아닌 메시지가 수신됨");
                    }
                    
                    _messageBuffer.Clear();
                    Debug.Log("메시지 처리 완료. 버퍼 초기화됨.");
                }
                else
                {
                    Debug.Log($"불완전한 메시지. 버퍼에 누적 중... (현재 길이: {bufferedMessage.Length})");
                }
            }
        }
        
        /// <summary>
        /// 완전한 JSON 메시지인지 확인
        /// </summary>
        private bool IsCompleteJsonMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;
                
            int openBraces = 0;
            int closeBraces = 0;
            bool inString = false;
            char escapeChar = '\\';
            
            for (int i = 0; i < message.Length; i++)
            {
                char c = message[i];
                
                if (c == '"' && (i == 0 || message[i - 1] != escapeChar))
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '{')
                        openBraces++;
                    else if (c == '}')
                        closeBraces++;
                }
            }
            
            bool isComplete = openBraces > 0 && openBraces == closeBraces;
            Debug.Log($"JSON 완성도 체크: 열린괄호={openBraces}, 닫힌괄호={closeBraces}, 완성={isComplete}");
            
            return isComplete;
        }
        
        /// <summary>
        /// JSON 형식인지 확인
        /// </summary>
        private bool IsValidJsonMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;
                
            message = message.Trim();
            
            if (message.StartsWith("{") && message.EndsWith("}"))
                return true;
                
            if (message.StartsWith("[") && message.EndsWith("]"))
                return true;
                
            return false;
        }

        /// <summary>
        /// 메시지 타입에 따른 처리
        /// </summary>
        private void ProcessMessage(string message)
        {
            try
            {
                var webSocketMessage = JsonUtility.FromJson<WebSocketMessage>(message);
                if (webSocketMessage == null)
                {
                    Debug.LogError("WebSocket 메시지 파싱 실패");
                    return;
                }

                Debug.Log($"메시지 타입: {webSocketMessage.type}");

                switch (webSocketMessage.type)
                {
                    case "session_id":
                        ProcessSessionIdMessage(webSocketMessage.data);
                        break;
                    case "chat":
                        ProcessChatMessage(webSocketMessage.data);
                        break;
                    default:
                        Debug.LogWarning($"알 수 없는 메시지 타입: {webSocketMessage.type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 처리 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 세션 ID 메시지 처리
        /// </summary>
        private void ProcessSessionIdMessage(object data)
        {
            try
            {
                var sessionIdData = JsonUtility.FromJson<SessionIdData>(JsonUtility.ToJson(data));
                if (sessionIdData != null)
                {
                    _sessionId = sessionIdData.session_id;
                    Debug.Log($"세션 ID 수신: {sessionIdData.session_id}");
                    OnSessionIdReceived?.Invoke(sessionIdData.session_id);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"세션 ID 메시지 처리 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 채팅 메시지 처리
        /// </summary>
        private void ProcessChatMessage(object data)
        {
            try
            {
                var chatData = JsonUtility.FromJson<ChatData>(JsonUtility.ToJson(data));
                if (chatData != null)
                {
                    Debug.Log($"채팅 메시지 수신: {chatData.message}");
                    OnChatMessageReceived?.Invoke(chatData.message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"채팅 메시지 처리 중 오류: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// WebSocket 메시지 기본 구조
    /// </summary>
    [Serializable]
    public class WebSocketMessage
    {
        public string type;
        public object data;
    }

    /// <summary>
    /// 세션 ID 데이터
    /// </summary>
    [Serializable]
    public class SessionIdData
    {
        public string session_id;
    }

    /// <summary>
    /// 채팅 데이터
    /// </summary>
    [Serializable]
    public class ChatData
    {
        public string message;
        public string sessionId;
        public long timestamp;
    }
} 