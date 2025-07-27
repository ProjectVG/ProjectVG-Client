using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Infrastructure.Network.DTOs.Chat;

namespace ProjectVG.Infrastructure.Network.Services
{
    /// <summary>
    /// 채팅 API 서비스
    /// </summary>
    public class ChatApiService
    {
        private readonly HttpApiClient _httpClient;

        public ChatApiService()
        {
            _httpClient = HttpApiClient.Instance;
        }

        /// <summary>
        /// 채팅 요청을 큐에 등록
        /// </summary>
        /// <param name="request">채팅 요청 데이터</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>채팅 응답</returns>
        public async UniTask<ChatResponse> SendChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            return await _httpClient.PostAsync<ChatResponse>("chat", request, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 간편한 채팅 요청 (기본값 사용)
        /// </summary>
        /// <param name="message">메시지</param>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="userId">사용자 ID</param>
        /// <param name="sessionId">세션 ID (선택사항)</param>
        /// <param name="actor">액터 (선택사항)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>채팅 응답</returns>
        public async UniTask<ChatResponse> SendChatAsync(
            string message, 
            string characterId, 
            string userId, 
            string sessionId = null, 
            string actor = null,
            CancellationToken cancellationToken = default)
        {
            var request = new ChatRequest
            {
                sessionId = sessionId,
                actor = actor,
                message = message,
                character_id = characterId,
                user_id = userId
            };

            return await SendChatAsync(request, cancellationToken);
        }
    }
} 