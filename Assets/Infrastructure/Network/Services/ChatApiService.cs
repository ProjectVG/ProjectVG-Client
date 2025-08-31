using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Infrastructure.Network.DTOs.Chat;
using ProjectVG.Infrastructure.Network.Configs;
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
            ValidateHttpClient();
            
            
            return await _httpClient.PostAsync<ChatResponse>($"api/v1/{CHAT_ENDPOINT}", request, requiresAuth: true, cancellationToken: cancellationToken);
        }

        #region Private Methods

        private void ValidateHttpClient()
        {
            if (_httpClient == null)
            {
                throw new InvalidOperationException("HttpApiClient.Instance가 null입니다. HttpApiClient가 생성되지 않았습니다.");
            }
        }

        #endregion
    }
} 