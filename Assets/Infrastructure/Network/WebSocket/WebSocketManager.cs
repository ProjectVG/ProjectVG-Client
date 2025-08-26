using System;
using System.Text;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Configs;
using ProjectVG.Infrastructure.Network.DTOs.Chat;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Infrastructure.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProjectVG.Infrastructure.Network.WebSocket
{
    public class WebSocketManager : Singleton<WebSocketManager>
    {
        private INativeWebSocket _nativeWebSocket;
        private CancellationTokenSource _cancellationTokenSource;
        
        private readonly StringBuilder _messageBuffer = new StringBuilder();
        private readonly object _bufferLock = new object();
        
        private bool _isConnected = false;
        private bool _isConnecting = false;
        private int _reconnectAttempts = 0;
        private bool _autoReconnect = true;
        private float _reconnectDelay = 5f;
        private int _maxReconnectAttempts = 10;
        private float _maxReconnectDelay = 60f;
        private bool _useExponentialBackoff = true;
        private bool _isShutdown = false;
        
        private TokenManager _tokenManager;
        private TokenRefreshService _tokenRefreshService;

        // 메시지 이벤트
        public event Action<string> OnMessageReceived;
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<ChatMessage> OnChatMessageReceived;

        public bool IsConnected => _isConnected;
        public bool IsConnecting => _isConnecting;
        public bool AutoReconnect => _autoReconnect;
        public int ReconnectAttempts => _reconnectAttempts;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            _tokenManager = TokenManager.Instance;
            _tokenRefreshService = TokenRefreshService.Instance;
        }

        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion
        
        #region Public Methods

        /// <summary>
        /// 웹소켓 매니저 초기화
        /// </summary>
        public void Initialize()
        {
			if (_cancellationTokenSource != null)
			{
				return;
			}
			_cancellationTokenSource = new CancellationTokenSource();
			InitializeNativeWebSocket();
			
			// TokenManager 이벤트 구독 - 토큰 변경 시 연결 상태 관리
			_tokenManager.OnTokensUpdated += OnTokensUpdated;
			_tokenManager.OnTokensCleared += OnTokensCleared;
			
#pragma warning disable CS4014
			StartConnectionMonitoring();
#pragma warning restore CS4014
        }
        
        /// <summary>
        /// 서버와 웹소켓 연결 시도
        /// </summary>
        public async UniTask<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_isConnected || _isConnecting)
            {
                Debug.LogWarning("[WebSocket] 이미 연결 중이거나 연결되어 있습니다.");
                return _isConnected;
            }

            Console.WriteLine($"[WebSocket] ConnectAsync 호출 - 연결 상태: {_isConnected}, 연결 중: {_isConnecting}");
            Console.WriteLine($"[WebSocket] 토큰 상태 - AccessToken: {_tokenManager.GetAccessToken()?.Substring(0, 10) ?? "null"}, RefreshToken: {_tokenManager.GetRefreshToken()?.Substring(0, 10) ?? "null"}");

            // 로그인 상태 확인
            if (!_tokenManager.HasValidTokens)
            {
                Debug.LogWarning("[WebSocket] 유효한 Access Token이 없어 연결할 수 없습니다.");
                
                // Refresh Token으로 Access Token 재요청 시도
                if (_tokenManager.HasRefreshToken && !_tokenManager.IsRefreshTokenExpired())
                {
                    Debug.Log("[WebSocket] Refresh Token으로 Access Token 갱신 시도");
                    var refreshSuccess = await _tokenRefreshService.RefreshAccessTokenAsync();
                    if (!refreshSuccess)
                    {
                        Debug.LogError("[WebSocket] 토큰 갱신 실패 - 연결 불가");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError("[WebSocket] 유효한 Refresh Token도 없습니다. 재로그인이 필요합니다.");
                    return false;
                }
            }

            _isConnecting = true;

            try
            {
                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;
                
                var wsUrl = GetWebSocketUrlWithToken();
                Debug.Log($"[WebSocket] 환경: {NetworkConfig.CurrentEnvironment}");
                Debug.Log($"[WebSocket] 서버 주소(환경기반): {NetworkConfig.WebSocketServerAddress}");
                Debug.Log($"[WebSocket] 연결 시도 URL: {wsUrl.Substring(0, Math.Min(wsUrl.Length, 100))}...");

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
                var error = $"WebSocket 연결 중 예외 발생: {ex.Message}\n환경: {NetworkConfig.CurrentEnvironment}\n서버 주소: {NetworkConfig.WebSocketServerAddress}";
                Debug.LogError($"[WebSocket] {error}");
                OnError?.Invoke(error);
                return false;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        /// <summary>
        /// 웹소켓 연결 해제
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

            OnDisconnected?.Invoke();
        }
        
        /// <summary>
        /// 웹소켓 메시지 전송
        /// </summary>
        public UniTask<bool> SendMessageAsync(string type, string data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 연결 상태 로깅
        /// </summary>
        public void LogConnectionStatus()
        {
            Debug.Log($"[WebSocket] 연결 상태: {(_isConnected ? "연결됨" : "연결안됨")}, 연결 중: {(_isConnecting ? "예" : "아니오")}, 재연결 시도: {_reconnectAttempts}/{_maxReconnectAttempts}");
        }

        /// <summary>
        /// 매니저 종료 및 리소스 정리
        /// </summary>
        public void Shutdown()
        {
            if (_isShutdown)
            {
                return;
            }
            _isShutdown = true;

            // 이벤트 구독 해제
            if (_tokenManager != null)
            {
                _tokenManager.OnTokensUpdated -= OnTokensUpdated;
                _tokenManager.OnTokensCleared -= OnTokensCleared;
            }

            _autoReconnect = false;
            DisconnectAsync().Forget();
            
            var cts = _cancellationTokenSource;
            _cancellationTokenSource = null;
            if (cts != null)
            {
                try { cts.Cancel(); } catch { }
                try { cts.Dispose(); } catch { }
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeNativeWebSocket()
        {
            _nativeWebSocket = WebSocketFactory.CreateWebSocket();
            
            _nativeWebSocket.OnConnected += OnNativeConnected;
            _nativeWebSocket.OnDisconnected += OnNativeDisconnected;
            _nativeWebSocket.OnError += OnNativeError;
            _nativeWebSocket.OnMessageReceived += OnNativeMessageReceived;
        }

        private string GetWebSocketUrlWithToken()
        {
            string baseUrl = NetworkConfig.GetWebSocketUrl();
            string accessToken = _tokenManager.GetAccessToken();
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                return $"{baseUrl}?token={accessToken}";
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
                await ConnectAsync();
            }
        }
        
        private async UniTaskVoid StartConnectionMonitoring()
        {
            var token = _cancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(30), cancellationToken: token);
                
                if (!_isConnected && !_isConnecting && _autoReconnect && _reconnectAttempts < _maxReconnectAttempts && _tokenManager.HasValidTokens)
                {
                    await ConnectAsync();
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
            
            Debug.LogWarning("[WebSocket] 연결이 끊어졌습니다. 재연결을 시도합니다.");
            
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
                    case "chat":
                        ProcessChatMessage(dataToken.ToString(Formatting.None));
                        break;
                    default:
                        Debug.Log($"[WebSocket] 메시지 타입: {messageType}");
                        OnMessageReceived?.Invoke(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket] 메시지 처리 중 오류: {ex.Message}");
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
        
        /// <summary>
        /// 토큰 업데이트 이벤트 핸들러 - 로그인 완료 시 자동 연결
        /// </summary>
        private void OnTokensUpdated(ProjectVG.Infrastructure.Auth.Models.TokenSet tokenSet)
        {
            Debug.Log("[WebSocket] 토큰이 업데이트되었습니다. 연결을 시도합니다.");
            
            // 로그인 완료 시 WebSocket 자동 연결
            if (!_isConnected && !_isConnecting)
            {
                ConnectAsync().Forget();
            }
        }
        
        /// <summary>
        /// 토큰 클리어 이벤트 핸들러 - 로그아웃 시 연결 해제
        /// </summary>
        private void OnTokensCleared()
        {
            Debug.Log("[WebSocket] 토큰이 클리어되었습니다. 연결을 해제합니다.");
            
            // 로그아웃 시 WebSocket 연결 해제
            _autoReconnect = false;
            DisconnectAsync().Forget();
        }
        
        #endregion
    }
} 