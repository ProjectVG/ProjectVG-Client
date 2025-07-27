using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Configs;
using ProjectVG.Infrastructure.Network.DTOs.WebSocket;
using ProjectVG.Infrastructure.Network.WebSocket.Platforms;

namespace ProjectVG.Infrastructure.Network.WebSocket
{
    /// <summary>
    /// WebSocket 연결 및 메시지 관리자
    /// UnityWebRequest를 사용하여 WebSocket 연결을 관리하고, 비동기 결과를 Handler로 전달합니다.
    /// </summary>
    public class WebSocketManager : MonoBehaviour
    {
                            [Header("WebSocket Configuration")]
                    [SerializeField] private WebSocketConfig webSocketConfig;

        private INativeWebSocket _nativeWebSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private List<IWebSocketHandler> _handlers = new List<IWebSocketHandler>();
        
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

        // 프로퍼티
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
            
            if (webSocketConfig == null)
            {
                Debug.LogWarning("WebSocketConfig가 설정되지 않았습니다. 기본 설정을 사용합니다.");
            }
            
            // Native WebSocket 초기화
            InitializeNativeWebSocket();
        }

                            private void InitializeNativeWebSocket()
                    {
                        // 시뮬레이션 WebSocket 구현체 사용
                        _nativeWebSocket = new UnityWebSocket();
                        Debug.Log("WebSocket 시뮬레이션 구현체를 사용합니다.");
                        
                        // 이벤트 연결
                        _nativeWebSocket.OnConnected += OnNativeConnected;
                        _nativeWebSocket.OnDisconnected += OnNativeDisconnected;
                        _nativeWebSocket.OnError += OnNativeError;
                        _nativeWebSocket.OnMessageReceived += OnNativeMessageReceived;
                        _nativeWebSocket.OnBinaryDataReceived += OnNativeBinaryDataReceived;
                    }

        /// <summary>
        /// WebSocketConfig 설정
        /// </summary>
        public void SetWebSocketConfig(WebSocketConfig config)
        {
            webSocketConfig = config;
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
        /// 수신된 메시지 처리 (더미 클라이언트와 동일한 방식)
        /// </summary>
        private void ProcessReceivedMessage(WebSocketMessage baseMessage)
        {
            try
            {
                Debug.Log($"메시지 수신: {baseMessage.type} - {baseMessage.data}");
                
                // 이벤트 발생
                OnMessageReceived?.Invoke(baseMessage);
                foreach (var handler in _handlers)
                {
                    handler.OnMessageReceived(baseMessage);
                }

                // 메시지 타입에 따른 처리
                switch (baseMessage.type?.ToLower())
                {
                    case "session_id":
                        var sessionMessage = JsonUtility.FromJson<SessionIdMessage>(JsonUtility.ToJson(baseMessage));
                        _sessionId = sessionMessage.session_id; // 세션 ID 저장
                        foreach (var handler in _handlers)
                        {
                            handler.OnSessionIdMessageReceived(sessionMessage);
                        }
                        break;
                        
                    case "chat":
                        var chatMessage = JsonUtility.FromJson<ChatMessage>(JsonUtility.ToJson(baseMessage));
                        foreach (var handler in _handlers)
                        {
                            handler.OnChatMessageReceived(chatMessage);
                        }
                        break;
                        
                    case "system":
                        var systemMessage = JsonUtility.FromJson<SystemMessage>(JsonUtility.ToJson(baseMessage));
                        foreach (var handler in _handlers)
                        {
                            handler.OnSystemMessageReceived(systemMessage);
                        }
                        break;
                        
                    case "connection":
                        var connectionMessage = JsonUtility.FromJson<ConnectionMessage>(JsonUtility.ToJson(baseMessage));
                        foreach (var handler in _handlers)
                        {
                            handler.OnConnectionMessageReceived(connectionMessage);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 처리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 오디오 데이터 처리 (더미 클라이언트와 동일한 방식)
        /// </summary>
        private void ProcessAudioData(byte[] audioData)
        {
            try
            {
                Debug.Log($"오디오 데이터 수신: {audioData.Length} bytes");
                
                // 이벤트 발생
                foreach (var handler in _handlers)
                {
                    handler.OnAudioDataReceived(audioData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"오디오 데이터 처리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// WebSocket URL 생성 (더미 클라이언트와 동일한 방식)
        /// </summary>
        private string GetWebSocketUrl(string sessionId = null)
        {
            string baseUrl;
            
            if (webSocketConfig != null)
            {
                baseUrl = webSocketConfig.GetWebSocketUrl();
            }
            else
            {
                // 기본값 (base64 인코딩된 IP:Port 사용)
                baseUrl = "ws://MTIyLjE1My4xMzAuMjIzOjc5MDA=/ws";
            }
            
            // 세션 ID가 있으면 쿼리 파라미터로 추가 (더미 클라이언트와 동일)
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
            var config = webSocketConfig ?? CreateDefaultWebSocketConfig();
            
            if (!config.AutoReconnect || _reconnectAttempts >= config.MaxReconnectAttempts)
            {
                return;
            }

            _reconnectAttempts++;
            Debug.Log($"WebSocket 재연결 시도 {_reconnectAttempts}/{config.MaxReconnectAttempts}");
            
            await UniTask.Delay(TimeSpan.FromSeconds(config.ReconnectDelay));
            
            if (!_isConnected)
            {
                ConnectAsync(_sessionId).Forget();
            }
        }

        /// <summary>
        /// 기본 WebSocket 설정 생성
        /// </summary>
        private WebSocketConfig CreateDefaultWebSocketConfig()
        {
            return WebSocketConfig.CreateDevelopmentConfig();
        }

        #region Native WebSocket Event Handlers

        private void OnNativeConnected()
        {
            _isConnected = true;
            _isConnecting = false;
            _reconnectAttempts = 0;
            
            // 이벤트 발생
            OnConnected?.Invoke();
            foreach (var handler in _handlers)
            {
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
                // JSON 메시지 파싱
                var baseMessage = JsonUtility.FromJson<WebSocketMessage>(message);
                ProcessReceivedMessage(baseMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 파싱 실패: {ex.Message}");
            }
        }

        private void OnNativeBinaryDataReceived(byte[] data)
        {
            ProcessAudioData(data);
        }

        #endregion
    }
} 