using UnityEngine;

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
        
        [Header("WebSocket Settings")]
        [SerializeField] private string wsPath = "ws";
        [SerializeField] private float wsTimeout = 30f;
        [SerializeField] private float reconnectDelay = 5f;
        [SerializeField] private int maxReconnectAttempts = 3;
        [SerializeField] private bool autoReconnect = true;
        [SerializeField] private float heartbeatInterval = 30f;
        [SerializeField] private bool enableHeartbeat = true;
        [SerializeField] private int maxMessageSize = 65536; // 64KB
        [SerializeField] private float messageTimeout = 10f;
        [SerializeField] private bool enableMessageLogging = true;
        
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
        
        // Properties
        public EnvironmentType Environment => environment;
        public string ApiPath => apiPath;
        public string WsPath => wsPath;
        

        
        #region Environment Configuration
        

        
        #endregion
        
        #region Static Accessors (편의 메서드들)
        
        /// <summary>
        /// 현재 환경
        /// </summary>
        public static EnvironmentType CurrentEnvironment => Instance.Environment;
        
        /// <summary>
        /// HTTP 서버 주소
        /// </summary>
        public static string HttpServerAddress
        {
            get
            {
                string server;
                switch (Instance.environment)
                {
                    case EnvironmentType.Development:
                        server = Instance.developmentServer;
                        break;
                    case EnvironmentType.Test:
                        server = Instance.testServer;
                        break;
                    case EnvironmentType.Production:
                        server = Instance.productionServer;
                        break;
                    default:
                        server = Instance.developmentServer;
                        break;
                }
                return $"http://{server}";
            }
        }
        
        /// <summary>
        /// WebSocket 서버 주소
        /// </summary>
        public static string WebSocketServerAddress
        {
            get
            {
                string server;
                switch (Instance.environment)
                {
                    case EnvironmentType.Development:
                        server = Instance.developmentServer;
                        break;
                    case EnvironmentType.Test:
                        server = Instance.testServer;
                        break;
                    case EnvironmentType.Production:
                        server = Instance.productionServer;
                        break;
                    default:
                        server = Instance.developmentServer;
                        break;
                }
                return $"ws://{server}";
            }
        }
        
        /// <summary>
        /// API 버전
        /// </summary>
        public static string ApiVersion => Instance.apiVersion;
        
        /// <summary>
        /// HTTP 타임아웃
        /// </summary>
        public static float HttpTimeout => Instance.httpTimeout;
        
        /// <summary>
        /// 최대 재시도 횟수
        /// </summary>
        public static int MaxRetryCount => Instance.maxRetryCount;
        
        /// <summary>
        /// 재시도 지연 시간
        /// </summary>
        public static float RetryDelay => Instance.retryDelay;
        
        /// <summary>
        /// WebSocket 타임아웃
        /// </summary>
        public static float WebSocketTimeout => Instance.wsTimeout;
        
        /// <summary>
        /// 재연결 지연 시간
        /// </summary>
        public static float ReconnectDelay => Instance.reconnectDelay;
        
        /// <summary>
        /// 최대 재연결 시도 횟수
        /// </summary>
        public static int MaxReconnectAttempts => Instance.maxReconnectAttempts;
        
        /// <summary>
        /// 자동 재연결
        /// </summary>
        public static bool AutoReconnect => Instance.autoReconnect;
        
        /// <summary>
        /// 하트비트 간격
        /// </summary>
        public static float HeartbeatInterval => Instance.heartbeatInterval;
        
        /// <summary>
        /// 하트비트 활성화
        /// </summary>
        public static bool EnableHeartbeat => Instance.enableHeartbeat;
        
        /// <summary>
        /// 최대 메시지 크기
        /// </summary>
        public static int MaxMessageSize => Instance.maxMessageSize;
        
        /// <summary>
        /// 메시지 타임아웃
        /// </summary>
        public static float MessageTimeout => Instance.messageTimeout;
        
        /// <summary>
        /// 메시지 로깅 활성화
        /// </summary>
        public static bool EnableMessageLogging => Instance.enableMessageLogging;
        
        /// <summary>
        /// 사용자 에이전트
        /// </summary>
        public static string UserAgent => Instance.userAgent;
        
        /// <summary>
        /// 콘텐츠 타입
        /// </summary>
        public static string ContentType => Instance.contentType;
        
        /// <summary>
        /// 전체 API URL 생성
        /// </summary>
        public static string GetFullApiUrl(string endpoint)
        {
            var baseUrl = HttpServerAddress;
            return $"{baseUrl.TrimEnd('/')}/{Instance.apiPath.TrimStart('/').TrimEnd('/')}/{Instance.apiVersion.TrimStart('/').TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }
        
        /// <summary>
        /// 사용자 API URL
        /// </summary>
        public static string GetUserApiUrl(string path = "")
        {
            return GetFullApiUrl($"users/{path.TrimStart('/')}");
        }
        
        /// <summary>
        /// 캐릭터 API URL
        /// </summary>
        public static string GetCharacterApiUrl(string path = "")
        {
            return GetFullApiUrl($"characters/{path.TrimStart('/')}");
        }
        
        /// <summary>
        /// 대화 API URL
        /// </summary>
        public static string GetConversationApiUrl(string path = "")
        {
            return GetFullApiUrl($"conversations/{path.TrimStart('/')}");
        }
        
        /// <summary>
        /// 인증 API URL
        /// </summary>
        public static string GetAuthApiUrl(string path = "")
        {
            return GetFullApiUrl($"auth/{path.TrimStart('/')}");
        }
        
        /// <summary>
        /// WebSocket URL
        /// </summary>
        public static string GetWebSocketUrl()
        {
            var baseUrl = WebSocketServerAddress;
            return $"{baseUrl.TrimEnd('/')}/{Instance.wsPath.TrimStart('/').TrimEnd('/')}";
        }
        
        /// <summary>
        /// 버전이 포함된 WebSocket URL
        /// </summary>
        public static string GetWebSocketUrlWithVersion()
        {
            var baseUrl = WebSocketServerAddress;
            return $"{baseUrl.TrimEnd('/')}/api/{Instance.apiVersion.TrimStart('/').TrimEnd('/')}/{Instance.wsPath.TrimStart('/').TrimEnd('/')}";
        }
        
        /// <summary>
        /// 세션이 포함된 WebSocket URL
        /// </summary>
        public static string GetWebSocketUrlWithSession(string sessionId)
        {
            var baseWsUrl = GetWebSocketUrlWithVersion();
            return $"{baseWsUrl}?sessionId={sessionId}";
        }
       
        
        /// <summary>
        /// 개발 환경 설정
        /// </summary>
        public static void SetDevelopmentEnvironment()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("런타임 중에는 환경 설정을 변경할 수 없습니다.");
                return;
            }
            
            Instance.environment = EnvironmentType.Development;
        }
        
        /// <summary>
        /// 테스트 환경 설정
        /// </summary>
        public static void SetTestEnvironment()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("런타임 중에는 환경 설정을 변경할 수 없습니다.");
                return;
            }
            
            Instance.environment = EnvironmentType.Test;
        }
        
        /// <summary>
        /// 프로덕션 환경 설정
        /// </summary>
        public static void SetProductionEnvironment()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("런타임 중에는 환경 설정을 변경할 수 없습니다.");
                return;
            }
            
            Instance.environment = EnvironmentType.Production;
        }
        
        /// <summary>
        /// 현재 설정 로그 출력
        /// </summary>
        public static void LogCurrentSettings()
        {
            Debug.Log($"=== NetworkConfig 현재 설정 ===");
            Debug.Log($"환경: {CurrentEnvironment}");
            Debug.Log($"HTTP 서버: {HttpServerAddress}");
            Debug.Log($"WebSocket 서버: {WebSocketServerAddress}");
            Debug.Log($"API 버전: {ApiVersion}");
            Debug.Log($"HTTP 타임아웃: {HttpTimeout}s");
            Debug.Log($"WebSocket 타임아웃: {WebSocketTimeout}s");
            Debug.Log($"자동 재연결: {AutoReconnect}");
            Debug.Log($"하트비트: {EnableHeartbeat} ({HeartbeatInterval}s)");
            Debug.Log($"================================");
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 기본 인스턴스 생성 (Resources 폴더에 파일이 없을 때)
        /// </summary>
        private static NetworkConfig CreateDefaultInstance()
        {
            var instance = CreateInstance<NetworkConfig>();
            
            // 기본 설정
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
            instance.maxMessageSize = 65536;
            instance.messageTimeout = 10f;
            instance.enableMessageLogging = true;
            instance.userAgent = "ProjectVG-Client/1.0";
            instance.contentType = "application/json";
            
            Debug.LogWarning("기본 NetworkConfig를 생성했습니다. Resources 폴더에 NetworkConfig.asset 파일을 생성하는 것을 권장합니다.");
            
            return instance;
        }
        
        #endregion
    }
} 