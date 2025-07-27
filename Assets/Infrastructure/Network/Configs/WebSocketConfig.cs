using UnityEngine;
using System;
using System.Text;

namespace ProjectVG.Infrastructure.Network.Configs
{
    /// <summary>
    /// WebSocket 설정을 관리하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "WebSocketConfig", menuName = "ProjectVG/Network/WebSocketConfig")]
    public class WebSocketConfig : ScriptableObject
    {
        [Header("WebSocket Server Configuration")]
        [SerializeField] private string baseUrl = "ws://MTIyLjE1My4xMzAuMjIzOjc5MDA="; // base64 encoded: 122.153.130.223:7900
        [SerializeField] private string wsPath = "ws";
        [SerializeField] private string apiVersion = "v1";

        [Header("Connection Settings")]
        [SerializeField] private float timeout = 30f;
        [SerializeField] private float reconnectDelay = 5f;
        [SerializeField] private int maxReconnectAttempts = 3;
        [SerializeField] private bool autoReconnect = true;
        [SerializeField] private float heartbeatInterval = 30f;
        [SerializeField] private bool enableHeartbeat = true;

        [Header("Message Settings")]
        [SerializeField] private int maxMessageSize = 65536; // 64KB
        [SerializeField] private float messageTimeout = 10f;
        [SerializeField] private bool enableMessageLogging = true;

        [Header("Headers")]
        [SerializeField] private string userAgent = "ProjectVG-Client/1.0";
        [SerializeField] private string contentType = "application/json";

        // Properties
        public string BaseUrl => DecodeBase64Url(baseUrl);
        public string WsPath => wsPath;
        public string ApiVersion => apiVersion;
        public float Timeout => timeout;
        public float ReconnectDelay => reconnectDelay;
        public int MaxReconnectAttempts => maxReconnectAttempts;
        public bool AutoReconnect => autoReconnect;
        public float HeartbeatInterval => heartbeatInterval;
        public bool EnableHeartbeat => enableHeartbeat;
        public int MaxMessageSize => maxMessageSize;
        public float MessageTimeout => messageTimeout;
        public bool EnableMessageLogging => enableMessageLogging;
        public string UserAgent => userAgent;
        public string ContentType => contentType;

        /// <summary>
        /// WebSocket URL 생성
        /// </summary>
        public string GetWebSocketUrl()
        {
            var decodedBaseUrl = DecodeBase64Url(baseUrl);
            return $"{decodedBaseUrl.TrimEnd('/')}/{wsPath.TrimStart('/').TrimEnd('/')}";
        }

        /// <summary>
        /// API 버전이 포함된 WebSocket URL 생성
        /// </summary>
        public string GetWebSocketUrlWithVersion()
        {
            var decodedBaseUrl = DecodeBase64Url(baseUrl);
            return $"{decodedBaseUrl.TrimEnd('/')}/api/{apiVersion.TrimStart('/').TrimEnd('/')}/{wsPath.TrimStart('/').TrimEnd('/')}";
        }

        /// <summary>
        /// 세션별 WebSocket URL 생성
        /// </summary>
        public string GetWebSocketUrlWithSession(string sessionId)
        {
            var baseWsUrl = GetWebSocketUrlWithVersion();
            return $"{baseWsUrl}?sessionId={sessionId}";
        }

        /// <summary>
        /// Base64 인코딩된 URL 디코딩
        /// </summary>
        private string DecodeBase64Url(string encodedUrl)
        {
            try
            {
                if (encodedUrl.StartsWith("ws://") || encodedUrl.StartsWith("wss://"))
                {
                    var protocol = encodedUrl.Substring(0, encodedUrl.IndexOf("://") + 3);
                    var encodedPart = encodedUrl.Substring(encodedUrl.IndexOf("://") + 3);
                    var decodedPart = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPart));
                    return protocol + decodedPart;
                }
                return encodedUrl;
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket URL 디코딩 실패: {ex.Message}");
                return "ws://localhost:7900"; // Fallback
            }
        }

        #region Factory Methods

        /// <summary>
        /// 개발 환경 설정 생성
        /// </summary>
        public static WebSocketConfig CreateDevelopmentConfig()
        {
            var config = CreateInstance<WebSocketConfig>();
            config.baseUrl = "ws://bG9jYWxob3N0Ojc5MDA="; // base64 encoded: localhost:7900
            config.wsPath = "ws";
            config.apiVersion = "v1";
            config.timeout = 10f;
            config.reconnectDelay = 2f;
            config.maxReconnectAttempts = 2;
            config.autoReconnect = true;
            config.heartbeatInterval = 15f;
            config.enableHeartbeat = true;
            config.maxMessageSize = 32768; // 32KB
            config.messageTimeout = 5f;
            config.enableMessageLogging = true;
            return config;
        }

        /// <summary>
        /// 프로덕션 환경 설정 생성
        /// </summary>
        public static WebSocketConfig CreateProductionConfig()
        {
            var config = CreateInstance<WebSocketConfig>();
            config.baseUrl = "ws://MTIyLjE1My4xMzAuMjIzOjc5MDA="; // base64 encoded: 122.153.130.223:7900
            config.wsPath = "ws";
            config.apiVersion = "v1";
            config.timeout = 30f;
            config.reconnectDelay = 5f;
            config.maxReconnectAttempts = 3;
            config.autoReconnect = true;
            config.heartbeatInterval = 30f;
            config.enableHeartbeat = true;
            config.maxMessageSize = 65536; // 64KB
            config.messageTimeout = 10f;
            config.enableMessageLogging = false;
            return config;
        }

        /// <summary>
        /// 테스트 환경 설정 생성
        /// </summary>
        public static WebSocketConfig CreateTestConfig()
        {
            var config = CreateInstance<WebSocketConfig>();
            config.baseUrl = "ws://MTIyLjE1My4xMzAuMjIzOjc5MDA="; // base64 encoded: 122.153.130.223:7900
            config.wsPath = "ws";
            config.apiVersion = "v1";
            config.timeout = 15f;
            config.reconnectDelay = 3f;
            config.maxReconnectAttempts = 2;
            config.autoReconnect = true;
            config.heartbeatInterval = 20f;
            config.enableHeartbeat = true;
            config.maxMessageSize = 32768; // 32KB
            config.messageTimeout = 8f;
            config.enableMessageLogging = true;
            return config;
        }

        #endregion
    }
} 