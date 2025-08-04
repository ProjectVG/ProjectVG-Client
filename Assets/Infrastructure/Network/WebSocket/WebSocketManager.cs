using System;
using System.Text;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Configs;
using ProjectVG.Infrastructure.Network.DTOs.Chat;
using ProjectVG.Infrastructure.Network.Services;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Core.Managers;
using ProjectVG.Core.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProjectVG.Infrastructure.Network.WebSocket
{
    public class WebSocketManager : Singleton<WebSocketManager>, IManager
    {
        private INativeWebSocket _nativeWebSocket;
        private CancellationTokenSource _cancellationTokenSource;
        
        private readonly StringBuilder _messageBuffer = new StringBuilder();
        private readonly object _bufferLock = new object();
        
        private bool _isConnected = false;
        private bool _isConnecting = false;
        private int _reconnectAttempts = 0;
        private string _sessionId;
        private bool _autoReconnect = true;
        private float _reconnectDelay = 5f;
        private int _maxReconnectAttempts = 10;
        private float _maxReconnectDelay = 60f;
        private bool _useExponentialBackoff = true;
        
        [Inject] private SessionManager _sessionManager;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<ChatMessage> OnChatMessageReceived;

        public bool IsConnected => _isConnected;
        public bool IsConnecting => _isConnecting;
        public string SessionId => _sessionId;
        public bool AutoReconnect => _autoReconnect;
        public int ReconnectAttempts => _reconnectAttempts;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion
        
        #region Public Methods
        
        public async UniTask<bool> ConnectAsync(string sessionId = null, CancellationToken cancellationToken = default)
        {
            if (_isConnected || _isConnecting)
            {
                Debug.LogWarning("[WebSocket] 이미 연결 중이거나 연결되어 있습니다.");
                return _isConnected;
            }

            _isConnecting = true;
            _sessionId = sessionId;

            try
            {
                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;
                
                var wsUrl = GetWebSocketUrl(sessionId);
                Debug.Log($"[WebSocket] 연결 시도: {wsUrl}");

                var success = await _nativeWebSocket.ConnectAsync(wsUrl, combinedCancellationToken);
                
                if (success)
                {
                    _isConnected = true;
                    _isConnecting = false;
                    _reconnectAttempts = 0;
                    Debug.Log("[WebSocket] 연결 성공");
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
                Debug.Log("[WebSocket] 연결이 취소되었습니다.");
                return false;
            }
            catch (Exception ex)
            {
                var error = $"WebSocket 연결 중 예외 발생: {ex.Message}";
                Debug.LogError($"[WebSocket] {error}");
                OnError?.Invoke(error);
                return false;
            }
            finally
            {
                _isConnecting = false;
            }
        }

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

            OnDisconnected?.Invoke();
        }
        
        public void SetAutoReconnect(bool enabled)
        {
            _autoReconnect = enabled;
        }
        
        public void SetReconnectSettings(int maxAttempts, float delay, float maxDelay = 60f, bool useExponentialBackoff = true)
        {
            _maxReconnectAttempts = maxAttempts;
            _reconnectDelay = delay;
            _maxReconnectDelay = maxDelay;
            _useExponentialBackoff = useExponentialBackoff;
        }

        public async UniTask<bool> SendMessageAsync(string type, string data)
        {
            throw new NotImplementedException();
        }

        public void LogConnectionStatus()
        {
            Debug.Log($"[WebSocket] 연결 상태: {(_isConnected ? "연결됨" : "연결안됨")}, 연결 중: {(_isConnecting ? "예" : "아니오")}, 재연결 시도: {_reconnectAttempts}/{_maxReconnectAttempts}");
        }

        public void Shutdown()
        {
            _autoReconnect = false;
            DisconnectAsync().Forget();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
        
        #endregion
        
        #region Private Methods
        
        private void Initialize()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            InitializeNativeWebSocket();
            StartConnectionMonitoring();
        }

        private void InitializeNativeWebSocket()
        {
            _nativeWebSocket = WebSocketFactory.Create();
            
            _nativeWebSocket.OnConnected += OnNativeConnected;
            _nativeWebSocket.OnDisconnected += OnNativeDisconnected;
            _nativeWebSocket.OnError += OnNativeError;
            _nativeWebSocket.OnMessageReceived += OnNativeMessageReceived;
        }

        private string GetWebSocketUrl(string sessionId = null)
        {
            string baseUrl = NetworkConfig.GetWebSocketUrl();
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                return $"{baseUrl}?sessionId={sessionId}";
            }
            
            return baseUrl;
        }

        private async UniTaskVoid TryReconnectAsync()
        {
            if (!_autoReconnect || _reconnectAttempts >= _maxReconnectAttempts)
            {
                Debug.LogWarning($"[WebSocket] 재연결 시도 횟수 초과: {_reconnectAttempts}/{_maxReconnectAttempts}");
                return;
            }

            _reconnectAttempts++;
            
            float delay = _reconnectDelay;
            if (_useExponentialBackoff)
            {
                delay = Mathf.Min(_reconnectDelay * Mathf.Pow(2, _reconnectAttempts - 1), _maxReconnectDelay);
            }
            
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            
            if (!_isConnected)
            {
                await ConnectAsync(_sessionId);
            }
        }
        
        private async UniTaskVoid StartConnectionMonitoring()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(30), cancellationToken: _cancellationTokenSource.Token);
                
                if (!_isConnected && !_isConnecting && _autoReconnect && _reconnectAttempts < _maxReconnectAttempts)
                {
                    await ConnectAsync(_sessionId);
                }
            }
        }

        private void OnNativeConnected()
        {
            _isConnected = true;
            _isConnecting = false;
            _reconnectAttempts = 0;
            
            Debug.Log("[WebSocket] 세션이 연결되었습니다.");
            OnConnected?.Invoke();
        }

        private void OnNativeDisconnected()
        {
            _isConnected = false;
            _isConnecting = false;
            
            Debug.LogWarning("[WebSocket] 세션이 끊어졌습니다. 재연결을 시도합니다.");
            OnDisconnected?.Invoke();
            
            if (_autoReconnect)
            {
                TryReconnectAsync().Forget();
            }
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
                int messageLength = message?.Length ?? 0;
                string truncatedMessage = message?.Length > 50 ? message.Substring(0, 50) + "..." : message ?? "";
                
                Debug.Log($"[WebSocket] 메시지 수신: {messageLength} bytes - {truncatedMessage}");
                ProcessBufferedMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket] 메시지 파싱 실패: {ex.Message}");
                Debug.LogError($"[WebSocket] 원시 메시지: {message}");
            }
        }

        private void ProcessBufferedMessage(string message)
        {
            lock (_bufferLock)
            {
                _messageBuffer.Append(message);
                string bufferedMessage = _messageBuffer.ToString();
                
                if (IsCompleteJsonMessage(bufferedMessage))
                {
                    if (IsValidJsonMessage(bufferedMessage))
                    {
                        ProcessMessage(bufferedMessage);
                    }
                    else
                    {
                        Debug.LogWarning("[WebSocket] JSON 형식이 아닌 메시지가 수신됨");
                    }
                    
                    _messageBuffer.Clear();
                }
            }
        }
        
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
            
            return openBraces > 0 && openBraces == closeBraces;
        }
        
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

        private void ProcessMessage(string message)
        {
            try
            {
                var jsonObject = JObject.Parse(message);
                string messageType = jsonObject["type"]?.ToString();
                JToken dataToken = jsonObject["data"];

                switch (messageType)
                {
                    case "session":
                        ProcessSessionMessage(dataToken.ToString(Formatting.None));
                        break;
                    case "chat":
                        ProcessChatMessage(dataToken.ToString(Formatting.None));
                        break;
                    default:
                        Debug.LogWarning($"[WebSocket] 알 수 없는 메시지 타입: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket] 메시지 처리 중 오류: {ex.Message}");
            }
        }

        private void ProcessSessionMessage(string data)
        {
            try
            {
                if (_sessionManager != null)
                {
                    _sessionManager.HandleSessionMessage(data);
                }
                else
                {
                    Debug.LogWarning("[WebSocket] SessionManager가 없어서 세션 메시지를 처리할 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket] 세션 메시지 처리 중 오류: {ex.Message}");
            }
        }

        private void ProcessChatMessage(string data)
        {
            try
            {
                var chatResponse = JsonConvert.DeserializeObject<ChatResponse>(data);
                if (chatResponse == null)
                {
                    Debug.LogError("[WebSocket] ChatResponse 파싱 실패");
                    return;
                }

                var chatMessage = ChatMessage.FromChatResponse(chatResponse);
                if (chatMessage == null)
                {
                    Debug.LogError("[WebSocket] ChatMessage 변환 실패");
                    return;
                }
                
                OnChatMessageReceived?.Invoke(chatMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket] 채팅 메시지 처리 중 오류: {ex.Message}");
                Debug.LogError($"[WebSocket] 원시 데이터: {data}");
            }
        }
        
        #endregion
    }
} 