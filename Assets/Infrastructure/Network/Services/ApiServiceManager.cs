using UnityEngine;

namespace ProjectVG.Infrastructure.Network.Services
{
    /// <summary>
    /// API 서비스 매니저
    /// 모든 API 서비스의 중앙 관리자
    /// </summary>
    public class ApiServiceManager : MonoBehaviour
    {
        private static ApiServiceManager _instance;
        public static ApiServiceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ApiServiceManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ApiServiceManager");
                        _instance = go.AddComponent<ApiServiceManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // API 서비스 인스턴스들
        private ChatApiService _chatService;
        private CharacterApiService _characterService;

        // 프로퍼티로 서비스 접근
        public ChatApiService Chat => _chatService ??= new ChatApiService();
        public CharacterApiService Character => _characterService ??= new CharacterApiService();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeServices();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeServices()
        {
            // 서비스 초기화
            _chatService = new ChatApiService();
            _characterService = new CharacterApiService();
            
            Debug.Log("API 서비스 매니저 초기화 완료");
        }

        /// <summary>
        /// 모든 서비스 재초기화
        /// </summary>
        public void ReinitializeServices()
        {
            _chatService = new ChatApiService();
            _characterService = new CharacterApiService();
            
            Debug.Log("API 서비스 재초기화 완료");
        }
    }
} 