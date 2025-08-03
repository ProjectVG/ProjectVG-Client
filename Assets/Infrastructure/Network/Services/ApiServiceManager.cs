using UnityEngine;

namespace ProjectVG.Infrastructure.Network.Services
{
    /// <summary>
    /// API 서비스 매니저
    /// 모든 API 서비스의 중앙 관리자
    /// </summary>
    public class ApiServiceManager : Singleton<ApiServiceManager>
    {
        private ChatApiService _chatService;
        private CharacterApiService _characterService;

        public ChatApiService Chat => _chatService ??= new ChatApiService();
        public CharacterApiService Character => _characterService ??= new CharacterApiService();

        protected override void Awake()
        {
            base.Awake();
            InitializeServices();
        }

        private void InitializeServices()
        {
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