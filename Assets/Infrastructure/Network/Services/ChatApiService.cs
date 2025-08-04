using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Infrastructure.Network.DTOs.Chat;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Network.Services
{
    /// <summary>
    /// 채팅 API 서비스
    /// </summary>
    public class ChatApiService
    {
        private readonly HttpApiClient _httpClient;
        private const string CHAT_ENDPOINT = "chat";
        private const string DEFAULT_ACTION = "chat";

        public ChatApiService()
        {
            _httpClient = HttpApiClient.Instance;
            ValidateHttpClient();
        }

        /// <summary>
        /// 채팅 요청 전송
        /// </summary>
        /// <param name="request">채팅 요청 데이터</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>채팅 응답</returns>
        public async UniTask<ChatResponse> SendChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);
            ValidateHttpClient();
            
            var serverRequest = CreateServerRequest(request);
            LogRequestDetails(serverRequest);
            
            return await _httpClient.PostAsync<ChatResponse>(CHAT_ENDPOINT, serverRequest, requiresSession: true, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 간편한 채팅 요청
        /// </summary>
        /// <param name="message">메시지</param>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="userId">사용자 ID</param>
        /// <param name="actor">액터 (선택사항)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>채팅 응답</returns>
        public async UniTask<ChatResponse> SendChatAsync(
            string message, 
            string characterId, 
            string userId, 
            string actor = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateSimpleRequest(message, characterId, userId, actor);
            return await SendChatAsync(request, cancellationToken);
        }

        #region Private Methods

        private void ValidateHttpClient()
        {
            if (_httpClient == null)
            {
                throw new InvalidOperationException("HttpApiClient.Instance가 null입니다. HttpApiClient가 생성되지 않았습니다.");
            }
        }

        private void ValidateRequest(ChatRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "채팅 요청이 null입니다.");
            }

            if (string.IsNullOrEmpty(request.message))
            {
                throw new ArgumentException("메시지가 비어있습니다.", nameof(request.message));
            }

            if (string.IsNullOrEmpty(request.characterId))
            {
                throw new ArgumentException("캐릭터 ID가 비어있습니다.", nameof(request.characterId));
            }

            if (string.IsNullOrEmpty(request.userId))
            {
                throw new ArgumentException("사용자 ID가 비어있습니다.", nameof(request.userId));
            }
        }

        private ChatRequest CreateServerRequest(ChatRequest originalRequest)
        {
            return new ChatRequest
            {
                sessionId = originalRequest.sessionId,
                message = originalRequest.message,
                characterId = originalRequest.characterId,
                userId = originalRequest.userId,
                action = originalRequest.action,
                actor = originalRequest.actor,
                instruction = originalRequest.instruction,
                requestedAt = originalRequest.requestedAt
            };
        }

        private ChatRequest CreateSimpleRequest(string message, string characterId, string userId, string actor)
        {
            return new ChatRequest
            {
                sessionId = "", // HttpApiClient에서 자동 주입
                message = message,
                characterId = characterId,
                userId = userId,
                actor = actor,
                action = DEFAULT_ACTION,
                requestedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        private void LogRequestDetails(ChatRequest request)
        {
        }

        #endregion
    }
} 