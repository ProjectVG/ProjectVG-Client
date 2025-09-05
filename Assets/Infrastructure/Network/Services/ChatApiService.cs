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
        private const string CHAT_ENDPOINT = "/api/v1/chat";
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
            return await _httpClient.PostAsync<ChatResponse>(CHAT_ENDPOINT, request, requiresAuth: true, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 간편한 채팅 요청
        /// </summary>
        /// <param name="message">메시지</param>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="userId">사용자 ID</param>
        /// <param name="action">행위</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>채팅 응답</returns>
        public async UniTask<ChatResponse> SendChatAsync(
            string message, 
            string characterId,
            string action = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateSimpleRequest(message, characterId, action);
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
        }

        private ChatRequest CreateSimpleRequest(string message, string characterId, string action)
        {
            return new ChatRequest
            {
                message = message,
                characterId = characterId,
                action = action,
                requestedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        #endregion
    }
} 