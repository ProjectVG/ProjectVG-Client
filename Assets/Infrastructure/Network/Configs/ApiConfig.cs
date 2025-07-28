using UnityEngine;

namespace ProjectVG.Infrastructure.Network.Configs
{
    /// <summary>
    /// API 설정을 관리하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "ProjectVG/Network/ApiConfig")]
    public class ApiConfig : ScriptableObject
    {
        [Header("Server Configuration")]
        [SerializeField] private string baseUrl = "http://122.153.130.223:7900";
        [SerializeField] private string apiVersion = "v1";
        [SerializeField] private float timeout = 30f;
        [SerializeField] private int maxRetryCount = 3;
        [SerializeField] private float retryDelay = 1f;

        [Header("Authentication")]
        [SerializeField] private string clientId = "";
        [SerializeField] private string clientSecret = "";

        [Header("Endpoints")]
        [SerializeField] private string userEndpoint = "users";
        [SerializeField] private string characterEndpoint = "characters";
        [SerializeField] private string conversationEndpoint = "conversations";
        [SerializeField] private string authEndpoint = "auth";

        [Header("Headers")]
        [SerializeField] private string contentType = "application/json";
        [SerializeField] private string userAgent = "ProjectVG-Client/1.0";

        // Constants
        private const string API_PATH = "api";
        private const string DEFAULT_CLIENT_ID = "unity-client-dev";
        private const string DEFAULT_CLIENT_SECRET = "dev-secret-key";
        private const string DEFAULT_USER_AGENT = "ProjectVG-Unity-Client/1.0";

        // Properties
        public string BaseUrl => baseUrl;
        public string ApiVersion => apiVersion;
        public float Timeout => timeout;
        public int MaxRetryCount => maxRetryCount;
        public float RetryDelay => retryDelay;
        public string ClientId => clientId;
        public string ClientSecret => clientSecret;
        public string ContentType => contentType;
        public string UserAgent => userAgent;

        // Endpoint Properties
        public string UserEndpoint => userEndpoint;
        public string CharacterEndpoint => characterEndpoint;
        public string ConversationEndpoint => conversationEndpoint;
        public string AuthEndpoint => authEndpoint;

        #region URL Generation Methods

        /// <summary>
        /// 전체 API URL 생성
        /// </summary>
        public string GetFullUrl(string endpoint)
        {
            return $"{baseUrl.TrimEnd('/')}/{API_PATH}/{apiVersion.TrimStart('/').TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }

        /// <summary>
        /// 사용자 API URL
        /// </summary>
        public string GetUserUrl(string path = "")
        {
            return GetFullUrl($"{userEndpoint}/{path.TrimStart('/')}");
        }

        /// <summary>
        /// 캐릭터 API URL
        /// </summary>
        public string GetCharacterUrl(string path = "")
        {
            return GetFullUrl($"{characterEndpoint}/{path.TrimStart('/')}");
        }

        /// <summary>
        /// 대화 API URL
        /// </summary>
        public string GetConversationUrl(string path = "")
        {
            return GetFullUrl($"{conversationEndpoint}/{path.TrimStart('/')}");
        }

        /// <summary>
        /// 인증 API URL
        /// </summary>
        public string GetAuthUrl(string path = "")
        {
            return GetFullUrl($"{authEndpoint}/{path.TrimStart('/')}");
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// 개발 환경 설정 생성
        /// </summary>
        public static ApiConfig CreateDevelopmentConfig()
        {
            var config = CreateInstance<ApiConfig>();
            ConfigureDevelopmentSettings(config);
            return config;
        }

        /// <summary>
        /// 프로덕션 환경 설정 생성
        /// </summary>
        public static ApiConfig CreateProductionConfig()
        {
            var config = CreateInstance<ApiConfig>();
            ConfigureProductionSettings(config);
            return config;
        }

        /// <summary>
        /// 테스트 환경 설정 생성
        /// </summary>
        public static ApiConfig CreateTestConfig()
        {
            var config = CreateInstance<ApiConfig>();
            ConfigureTestSettings(config);
            return config;
        }

        #endregion

        #region Private Configuration Methods

        private static void ConfigureDevelopmentSettings(ApiConfig config)
        {
            config.baseUrl = "http://localhost:7901";
            config.apiVersion = "v1";
            config.timeout = 10f;
            config.maxRetryCount = 1;
            config.retryDelay = 0.5f;
            config.clientId = DEFAULT_CLIENT_ID;
            config.clientSecret = DEFAULT_CLIENT_SECRET;
            config.contentType = "application/json";
            config.userAgent = DEFAULT_USER_AGENT;
            
            ConfigureDefaultEndpoints(config);
        }

        private static void ConfigureProductionSettings(ApiConfig config)
        {
            config.baseUrl = "http://122.153.130.223:7900";
            config.apiVersion = "v1";
            config.timeout = 30f;
            config.maxRetryCount = 3;
            config.retryDelay = 1f;
            config.contentType = "application/json";
            config.userAgent = "ProjectVG-Client/1.0";
            
            ConfigureDefaultEndpoints(config);
        }

        private static void ConfigureTestSettings(ApiConfig config)
        {
            config.baseUrl = "http://122.153.130.223:7901";
            config.apiVersion = "v1";
            config.timeout = 15f;
            config.maxRetryCount = 2;
            config.retryDelay = 0.5f;
            config.contentType = "application/json";
            config.userAgent = "ProjectVG-Client/1.0";
            
            ConfigureDefaultEndpoints(config);
        }

        private static void ConfigureDefaultEndpoints(ApiConfig config)
        {
            config.userEndpoint = "users";
            config.characterEndpoint = "characters";
            config.conversationEndpoint = "conversations";
            config.authEndpoint = "auth";
        }

        #endregion
    }
} 