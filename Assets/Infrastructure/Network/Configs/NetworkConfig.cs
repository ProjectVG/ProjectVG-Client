using UnityEngine;
using ProjectVG.Infrastructure.Config;

namespace ProjectVG.Infrastructure.Network.Configs
{
    /// <summary>
    /// Unity 표준 방식의 네트워크 설정 ScriptableObject
    /// Editor에서 설정 가능하고, 런타임에서는 정적 접근자로 사용
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "ProjectVG/Network/NetworkConfig")]
    public class NetworkConfig : ScriptableObject
    {
        [Header("Environment Settings")]
        [SerializeField] private EnvironmentType environment = EnvironmentType.Development;
        
        [Header("Server Addresses")]
        [SerializeField] private string developmentServer = "localhost:7900";
        [SerializeField] private string testServer = "localhost:7900";
        [SerializeField] private string productionServer = "122.153.130.223:7900";
        
        [Header("HTTP API Settings")]
        [SerializeField] private string apiVersion = "v1";
        [SerializeField] private string apiPath = "api";
        [SerializeField] private float httpTimeout = 30f;
        [SerializeField] private int maxRetryCount = 3;
        [SerializeField] private float retryDelay = 1f;
        
        [Header("File Upload Settings")]
        [SerializeField] private int maxFileSize = 10485760; // 10MB (bytes)
        [SerializeField] private float uploadTimeout = 60f; // 파일 업로드용 더 긴 타임아웃
        [SerializeField] private bool enableFileSizeCheck = true;
        
        [Header("WebSocket Settings")]
        [SerializeField] private string wsPath = "ws";
        [SerializeField] private float wsTimeout = 30f;
        [SerializeField] private float reconnectDelay = 5f;
        [SerializeField] private int maxReconnectAttempts = 3;
        [SerializeField] private bool autoReconnect = true;
        [SerializeField] private float heartbeatInterval = 30f;
        [SerializeField] private bool enableHeartbeat = true;
        [SerializeField] private int maxMessageSize = 1048576;
        [SerializeField] private int receiveBufferSize = 1048576;
        [SerializeField] private float messageTimeout = 10f;
        [SerializeField] private bool enableMessageLogging = true;
        [SerializeField] private string wsMessageType = "json"; // "json" 또는 "binary"
        
        [Header("Common Settings")]
        [SerializeField] private string userAgent = "ProjectVG-Client/1.0";
        [SerializeField] private string contentType = "application/json";
        
        // Environment enum
        public enum EnvironmentType
        {
            Development,
            Test,
            Production
        }
        
        // Singleton instance
        private static NetworkConfig _instance;
        public static NetworkConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<NetworkConfig>("NetworkConfig");
                    if (_instance == null)
                    {
                        Debug.LogError("NetworkConfig를 찾을 수 없습니다. Resources 폴더에 NetworkConfig.asset 파일을 생성하세요.");
                        _instance = CreateDefaultInstance();
                    }
                }
                return _instance;
            }
        }
        
        public static EnvironmentType CurrentEnvironment
        {
            get
            {
                var appEnv = AppEnvironmentConfig.Instance;
                if (appEnv != null)
                {
                    return appEnv.GetCurrentEnvironment();
                }
                return Instance.environment;
            }
        }
        
        private static void ApplyRuntimeGuard(NetworkConfig cfg)
        {
            if (Application.isEditor)
                return;
            
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                cfg.environment = EnvironmentType.Production;
            }
        }
        
        // Properties
        public EnvironmentType Environment => environment;
        public string ApiPath => apiPath;
        public string WsPath => wsPath;
        
        #region Static Accessors (편의 메서드들)
        
        public static string HttpServerAddress
        {
            get
            {
                string server;
                switch (CurrentEnvironment)
                {
                    case EnvironmentType.Development: server = Instance.developmentServer; break;
                    case EnvironmentType.Test:        server = Instance.testServer;        break;
                    case EnvironmentType.Production:  server = Instance.productionServer;  break;
                    default:                          server = Instance.developmentServer; break;
                }
                return $"http://{server}";
            }
        }
        
        public static string WebSocketServerAddress
        {
            get
            {
                string server;
                switch (CurrentEnvironment)
                {
                    case EnvironmentType.Development: server = Instance.developmentServer; break;
                    case EnvironmentType.Test:        server = Instance.testServer;        break;
                    case EnvironmentType.Production:  server = Instance.productionServer;  break;
                    default:                          server = Instance.developmentServer; break;
                }
                return $"ws://{server}";
            }
        }
        
        public static string GetWebSocketServerAddressFor(EnvironmentType env)
        {
            string server;
            switch (env)
            {
                case EnvironmentType.Development: server = Instance.developmentServer; break;
                case EnvironmentType.Test:        server = Instance.testServer;        break;
                case EnvironmentType.Production:  server = Instance.productionServer;  break;
                default:                          server = Instance.developmentServer; break;
            }
            return $"ws://{server}";
        }
        
        public static string GetWebSocketUrl()
        {
            var baseUrl = WebSocketServerAddress;
            return $"{baseUrl.TrimEnd('/')}/{Instance.wsPath.TrimStart('/').TrimEnd('/')}";
        }
        
        public static string GetWebSocketUrlFor(EnvironmentType env)
        {
            var baseUrl = GetWebSocketServerAddressFor(env);
            return $"{baseUrl.TrimEnd('/')}/{Instance.wsPath.TrimStart('/').TrimEnd('/')}";
        }
        
        public static string GetWebSocketUrlWithVersion()
        {
            var baseUrl = WebSocketServerAddress;
            return $"{baseUrl.TrimEnd('/')}/api/{Instance.apiVersion.TrimStart('/').TrimEnd('/')}/{Instance.wsPath.TrimStart('/').TrimEnd('/')}";
        }
        
        public static string GetWebSocketUrlWithSession(string sessionId)
        {
            var baseWsUrl = GetWebSocketUrlWithVersion();
            return $"{baseWsUrl}?sessionId={sessionId}";
        }
        
        // HTTP 공통 설정 정적 접근자 복원
        public static string ApiVersion => Instance.apiVersion;
        public static float HttpTimeout => Instance.httpTimeout;
        public static int MaxRetryCount => Instance.maxRetryCount;
        public static float RetryDelay => Instance.retryDelay;
        public static string UserAgent => Instance.userAgent;
        public static string ContentType => Instance.contentType;
        
        // File Upload Settings
        public static int MaxFileSize => Instance.maxFileSize;
        public static float UploadTimeout => Instance.uploadTimeout;
        public static bool EnableFileSizeCheck => Instance.enableFileSizeCheck;
        
        // WebSocket 설정 정적 접근자 복원
        public static float WebSocketTimeout => Instance.wsTimeout;
        public static float ReconnectDelay => Instance.reconnectDelay;
        public static int MaxReconnectAttempts => Instance.maxReconnectAttempts;
        public static bool AutoReconnect => Instance.autoReconnect;
        public static float HeartbeatInterval => Instance.heartbeatInterval;
        public static bool EnableHeartbeat => Instance.enableHeartbeat;
        public static int MaxMessageSize => Instance.maxMessageSize;
        public static int ReceiveBufferSize => Instance.receiveBufferSize;
        public static float MessageTimeout => Instance.messageTimeout;
        public static bool EnableMessageLogging => Instance.enableMessageLogging;
        public static string WebSocketMessageType => Instance.wsMessageType;
        public static bool IsJsonMessageType => Instance.wsMessageType?.ToLower() == "json";
        public static bool IsBinaryMessageType => Instance.wsMessageType?.ToLower() == "binary";
        
        // HTTP URL 유틸 복원 - /api/v1 자동 추가 제거
        public static string GetFullApiUrl(string endpoint)
        {
            var baseUrl = HttpServerAddress;
            return $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }
        public static string GetUserApiUrl(string path = "") => GetFullApiUrl($"users/{path.TrimStart('/')}");
        public static string GetCharacterApiUrl(string path = "") => GetFullApiUrl($"characters/{path.TrimStart('/')}");
        public static string GetConversationApiUrl(string path = "") => GetFullApiUrl($"conversations/{path.TrimStart('/')}");
        public static string GetAuthApiUrl(string path = "") => GetFullApiUrl($"auth/{path.TrimStart('/')}");
        
        #endregion
        
        #region Private Methods
        
        private static NetworkConfig CreateDefaultInstance()
        {
            var instance = CreateInstance<NetworkConfig>();
            
            instance.environment = EnvironmentType.Development;
            instance.developmentServer = "localhost:7900";
            instance.testServer = "localhost:7900";
            instance.productionServer = "122.153.130.223:7900";
            instance.apiVersion = "v1";
            instance.apiPath = "api";
            instance.httpTimeout = 30f;
            instance.maxRetryCount = 3;
            instance.retryDelay = 1f;
            instance.wsPath = "ws";
            instance.wsTimeout = 30f;
            instance.reconnectDelay = 5f;
            instance.maxReconnectAttempts = 3;
            instance.autoReconnect = true;
            instance.heartbeatInterval = 30f;
            instance.enableHeartbeat = true;
            instance.maxMessageSize = 1048576;
            instance.receiveBufferSize = 1048576;
            instance.messageTimeout = 10f;
            instance.enableMessageLogging = true;
            instance.userAgent = "ProjectVG-Client/1.0";
            instance.contentType = "application/json";
            instance.maxFileSize = 10485760;
            instance.uploadTimeout = 60f;
            instance.enableFileSizeCheck = true;
            
            Debug.LogWarning("기본 NetworkConfig를 생성했습니다. Resources 폴더에 NetworkConfig.asset 파일을 생성하는 것을 권장합니다.");
            
            return instance;
        }
        
        #endregion
    }
} 