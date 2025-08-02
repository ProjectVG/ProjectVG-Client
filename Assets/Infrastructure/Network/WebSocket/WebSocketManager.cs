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
    public class WebSocketManager : MonoBehaviour, IManager
    {
        private INativeWebSocket _nativeWebSocket;
        private CancellationTokenSource _cancellationTokenSource;
        
        private readonly StringBuilder _messageBuffer = new StringBuilder();
        private readonly object _bufferLock = new object();
        
        private bool _isConnected = false;
        private bool _isConnecting = false;
        private int _reconnectAttempts = 0;
        private string _sessionId;
        
        [Inject] private SessionManager _sessionManager;

        public static WebSocketManager Instance { get; private set; }
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<ChatMessage> OnChatMessageReceived;

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
            Shutdown();
        }
        
        public void Shutdown()
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

        public async UniTask<bool> SendMessageAsync(string type, string data)
        {
            throw new NotImplementedException();
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
                        Debug.LogWarning("JSON 형식이 아닌 메시지가 수신됨");
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
                
                Debug.Log($"[WebSocket] {messageType}: {dataToken}");

                switch (messageType)
                {
                    case "session":
                        ProcessSessionMessage(dataToken.ToString());
                        break;
                    case "chat":
                        ProcessChatMessage(dataToken.ToString());
                        break;
                    default:
                        Debug.LogWarning($"알 수 없는 메시지 타입: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 처리 중 오류: {ex.Message}");
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
                    Debug.LogWarning("SessionManager가 없어서 세션 메시지를 처리할 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"세션 메시지 처리 중 오류: {ex.Message}");
            }
        }

        private void ProcessChatMessage(string data)
        {
            try
            {
                var chatResponse = JsonUtility.FromJson<ChatResponse>(data);
                if (chatResponse == null)
                {
                    Debug.LogError("ChatResponse 파싱 실패");
                    return;
                }

                Debug.Log($"ChatResponse 파싱 성공: Type={chatResponse.Type}, SessionId={chatResponse.SessionId}");

                var chatMessage = ChatMessage.FromChatResponse(chatResponse);
                if (chatMessage == null)
                {
                    Debug.LogError("ChatMessage 변환 실패");
                    return;
                }

                Debug.Log($"ChatMessage 변환 성공: SessionId={chatMessage.SessionId}, HasText={chatMessage.HasTextData()}, HasAudio={chatMessage.HasVoiceData()}");
                
                OnChatMessageReceived?.Invoke(chatMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"채팅 메시지 처리 중 오류: {ex.Message}");
                Debug.LogError($"원시 데이터: {data}");
            }
        }
    }



} 