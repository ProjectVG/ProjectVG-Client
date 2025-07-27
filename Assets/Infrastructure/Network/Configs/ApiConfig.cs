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

        /// <summary>
        /// 전체 API URL 생성
        /// </summary>
        public string GetFullUrl(string endpoint)
        {
            return $"{baseUrl.TrimEnd('/')}/api/{apiVersion.TrimStart('/').TrimEnd('/')}/{endpoint.TrimStart('/')}";
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

        /// <summary>
        /// 환경별 설정을 위한 팩토리 메서드
        /// </summary>
        public static ApiConfig CreateDevelopmentConfig()
        {
            var config = CreateInstance<ApiConfig>();
            config.baseUrl = "http://localhost:7900";
            config.apiVersion = "v1";
            config.timeout = 10f;
            config.maxRetryCount = 1;
            config.retryDelay = 0.5f;
            return config;
        }

        /// <summary>
        /// 환경별 설정을 위한 팩토리 메서드
        /// </summary>
        public static ApiConfig CreateProductionConfig()
        {
            var config = CreateInstance<ApiConfig>();
            config.baseUrl = "http://122.153.130.223:7900";
            config.apiVersion = "v1";
            config.timeout = 30f;
            config.maxRetryCount = 3;
            config.retryDelay = 1f;
            return config;
        }

        /// <summary>
        /// 환경별 설정을 위한 팩토리 메서드
        /// </summary>
        public static ApiConfig CreateTestConfig()
        {
            var config = CreateInstance<ApiConfig>();
            config.baseUrl = "http://122.153.130.223:7900";
            config.apiVersion = "v1";
            config.timeout = 15f;
            config.maxRetryCount = 2;
            config.retryDelay = 0.5f;
            return config;
        }
    }
} 