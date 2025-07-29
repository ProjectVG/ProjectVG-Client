using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Configs;
using ProjectVG.Infrastructure.Network.DTOs.WebSocket;
using ProjectVG.Infrastructure.Network.WebSocket.Processors;

namespace ProjectVG.Infrastructure.Network.WebSocket
{
    /// <summary>
    /// WebSocket 연결 및 메시지 관리자 (Bridge Pattern 적용)
    /// 메시지 처리기를 통해 JSON과 바이너리 메시지를 분리하여 처리합니다.
    /// </summary>
    public class WebSocketManager : MonoBehaviour
    {
        [Header("WebSocket Configuration")]
        // NetworkConfig를 사용하여 설정을 관리합니다.

        private INativeWebSocket _nativeWebSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private List<IWebSocketHandler> _handlers = new List<IWebSocketHandler>();
        
        // Bridge Pattern: 메시지 처리기
        private IMessageProcessor _messageProcessor;
        
        // 메시지 버퍼링을 위한 필드 추가
        private readonly StringBuilder _messageBuffer = new StringBuilder();
        private readonly object _bufferLock = new object();
        private bool _isProcessingMessage = false;
        
        private bool _isConnected = false;
        private bool _isConnecting = false;
        private int _reconnectAttempts = 0;
        private string _sessionId;



        public static WebSocketManager Instance { get; private set; }
        
        // 이벤트
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<WebSocketMessage> OnMessageReceived;
        public event Action<byte[]> OnAudioDataReceived;
        public event Action<IntegratedMessage> OnIntegratedMessageReceived;


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
            
            // NetworkConfig 기반으로 메시지 처리기 설정
            InitializeMessageProcessor();
            
            // Native WebSocket 초기화
            InitializeNativeWebSocket();
        }
        
        /// <summary>
        /// NetworkConfig 기반으로 메시지 처리기 초기화
        /// </summary>
        private void InitializeMessageProcessor()
        {
            var messageType = NetworkConfig.WebSocketMessageType;
            _messageProcessor = MessageProcessorFactory.CreateProcessor(messageType);
            Debug.Log($"NetworkConfig 기반 메시지 처리기 초기화: {messageType}");
            
            // 현재 설정 로그 출력
            Debug.Log($"=== WebSocket 메시지 처리 설정 ===");
            Debug.Log($"NetworkConfig 메시지 타입: {messageType}");
            Debug.Log($"JSON 형식 지원: {NetworkConfig.IsJsonMessageType}");
            Debug.Log($"바이너리 형식 지원: {NetworkConfig.IsBinaryMessageType}");
            Debug.Log($"=====================================");
        }

        private void InitializeNativeWebSocket()
        {
            // 플랫폼별 WebSocket 구현체 생성
            _nativeWebSocket = WebSocketFactory.Create();
            
            // 이벤트 연결
            _nativeWebSocket.OnConnected += OnNativeConnected;
            _nativeWebSocket.OnDisconnected += OnNativeDisconnected;
            _nativeWebSocket.OnError += OnNativeError;
            _nativeWebSocket.OnMessageReceived += OnNativeMessageReceived;
            _nativeWebSocket.OnBinaryDataReceived += OnNativeBinaryDataReceived;
        }





        /// <summary>
        /// 핸들러 등록
        /// </summary>
        public void RegisterHandler(IWebSocketHandler handler)
        {
            if (!_handlers.Contains(handler))
            {
                _handlers.Add(handler);
                Debug.Log($"WebSocket 핸들러 등록: {handler.GetType().Name}");
            }
        }

        /// <summary>
        /// 핸들러 해제
        /// </summary>
        public void UnregisterHandler(IWebSocketHandler handler)
        {
            if (_handlers.Remove(handler))
            {
                Debug.Log($"WebSocket 핸들러 해제: {handler.GetType().Name}");
            }
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

                // Native WebSocket을 통한 실제 연결
                var success = await _nativeWebSocket.ConnectAsync(wsUrl, combinedCancellationToken);
                
                if (success)
                {
                    _isConnected = true;
                    _isConnecting = false;
                    _reconnectAttempts = 0;
                    
                    Debug.Log("WebSocket 연결 성공");
                    
                    // 연결 후 서버 설정 로드 시도 (선택적)
                    // LoadServerConfigAsync().Forget();
                    
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
                foreach (var handler in _handlers)
                {
                    handler.OnError(error);
                }
                
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

            // Native WebSocket 연결 해제
            if (_nativeWebSocket != null)
            {
                await _nativeWebSocket.DisconnectAsync();
            }

            Debug.Log("WebSocket 연결 해제");
            
            OnDisconnected?.Invoke();
            foreach (var handler in _handlers)
            {
                handler.OnDisconnected();
            }

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 메시지 전송
        /// </summary>
        public async UniTask<bool> SendMessageAsync(WebSocketMessage message, CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                Debug.LogWarning("WebSocket이 연결되지 않았습니다.");
                return false;
            }

            try
            {
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
        public async UniTask<bool> SendChatMessageAsync(string message, string characterId, string userId, string actor = null, CancellationToken cancellationToken = default)
        {
            var chatMessage = new ChatMessage
            {
                type = "chat",
                sessionId = _sessionId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                message = message,
                characterId = characterId,
                userId = userId,
                actor = actor
            };

            return await SendMessageAsync(chatMessage, cancellationToken);
        }

        /// <summary>
        /// WebSocket URL 생성
        /// </summary>
        private string GetWebSocketUrl(string sessionId = null)
        {
            string baseUrl = NetworkConfig.GetWebSocketUrl();
            
            Debug.Log($"=== WebSocket URL 생성 ===");
            Debug.Log($"기본 URL: {baseUrl}");
            Debug.Log($"세션 ID: {sessionId}");
            
            string finalUrl;
            if (!string.IsNullOrEmpty(sessionId))
            {
                finalUrl = $"{baseUrl}?sessionId={sessionId}";
            }
            else
            {
                finalUrl = baseUrl;
            }
            
            Debug.Log($"최종 URL: {finalUrl}");
            Debug.Log($"================================");
            
            return finalUrl;
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
            
            Debug.Log("WebSocket 연결 성공 - 핸들러 수: " + _handlers.Count);
            
            // 이벤트 발생
            OnConnected?.Invoke();
            foreach (var handler in _handlers)
            {
                Debug.Log($"핸들러에게 연결 이벤트 전달: {handler.GetType().Name}");
                handler.OnConnected();
            }
        }

        private void OnNativeDisconnected()
        {
            _isConnected = false;
            _isConnecting = false;
            
            // 이벤트 발생
            OnDisconnected?.Invoke();
            foreach (var handler in _handlers)
            {
                handler.OnDisconnected();
            }
            
            // 자동 재연결 시도
            TryReconnectAsync().Forget();
        }

        private void OnNativeError(string error)
        {
            _isConnected = false;
            _isConnecting = false;
            
            // 이벤트 발생
            OnError?.Invoke(error);
            foreach (var handler in _handlers)
            {
                handler.OnError(error);
            }
        }

        private void OnNativeMessageReceived(string message)
        {
            try
            {
                Debug.Log($"=== 메시지 수신 디버깅 ===");
                Debug.Log($"원시 메시지 길이: {message?.Length ?? 0}");
                Debug.Log($"원시 메시지 (처음 100자): {message?.Substring(0, Math.Min(100, message?.Length ?? 0))}");
                Debug.Log($"핸들러 수: {_handlers.Count}");
                Debug.Log($"현재 메시지 처리기: {_messageProcessor.MessageType}");
                Debug.Log($"================================");
                
                // 메시지 버퍼링 처리
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
                // 메시지를 버퍼에 추가
                _messageBuffer.Append(message);
                string bufferedMessage = _messageBuffer.ToString();
                
                Debug.Log($"버퍼링된 메시지 길이: {bufferedMessage.Length}");
                Debug.Log($"버퍼링된 메시지 (처음 100자): {bufferedMessage.Substring(0, Math.Min(100, bufferedMessage.Length))}");
                
                // 완전한 JSON 메시지인지 확인
                if (IsCompleteJsonMessage(bufferedMessage))
                {
                    Debug.Log("완전한 JSON 메시지 감지됨. 처리 시작.");
                    
                    // JSON 형식인지 확인
                    if (IsValidJsonMessage(bufferedMessage))
                    {
                        Debug.Log("JSON 형식으로 처리합니다.");
                        // Bridge Pattern: 메시지 처리기에 위임
                        _messageProcessor.ProcessMessage(bufferedMessage, _handlers);
                    }
                    else
                    {
                        Debug.LogWarning("JSON 형식이 아닌 메시지가 문자열로 수신됨. 바이너리 처리기로 전달합니다.");
                        // 바이너리 처리기로 전달
                        var binaryProcessor = MessageProcessorFactory.CreateProcessor("binary");
                        binaryProcessor.ProcessMessage(bufferedMessage, _handlers);
                    }
                    
                    // 버퍼 초기화
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
                
            // 중괄호 개수로 완전한 JSON인지 확인
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
            
            // JSON 객체 시작/끝 확인
            if (message.StartsWith("{") && message.EndsWith("}"))
                return true;
                
            // JSON 배열 시작/끝 확인
            if (message.StartsWith("[") && message.EndsWith("]"))
                return true;
                
            // 세션 ID 메시지 특별 처리
            if (message.Contains("\"type\":\"session_id\""))
                return true;
                
            return false;
        }

        private void OnNativeBinaryDataReceived(byte[] data)
        {
            try
            {
                Debug.Log($"바이너리 데이터 수신: {data.Length} bytes");
                Debug.Log($"현재 메시지 처리기: {_messageProcessor.MessageType}");
                
                // Bridge Pattern: 메시지 처리기에 위임
                _messageProcessor.ProcessBinaryMessage(data, _handlers);
            }
            catch (Exception ex)
            {
                Debug.LogError($"바이너리 데이터 처리 실패: {ex.Message}");
            }
        }

        #endregion
    }


} 