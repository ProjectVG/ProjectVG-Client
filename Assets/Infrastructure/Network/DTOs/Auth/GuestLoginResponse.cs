using System;
using Newtonsoft.Json;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Network.DTOs.Auth
{
    /// <summary>
    /// Guest 로그인 응답 DTO
    /// </summary>
    [Serializable]
    public class GuestLoginResponse
    {
        /// <summary>
        /// 성공 여부
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 토큰 정보
        /// </summary>
        [JsonProperty("tokens")]
        public GuestTokenInfo Tokens { get; set; }

        /// <summary>
        /// 사용자 정보
        /// </summary>
        [JsonProperty("user")]
        public GuestUserInfo User { get; set; }

        /// <summary>
        /// 오류 메시지
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// TokenSet으로 변환
        /// </summary>
        public TokenSet ToTokenSet()
        {
            if (Tokens == null)
            {
                return null;
            }

            var accessToken = new AccessToken(
                Tokens.AccessToken,
                Tokens.ExpiresIn,
                "Bearer",
                "api"
            );

            RefreshToken refreshToken = null;
            if (!string.IsNullOrEmpty(Tokens.RefreshToken))
            {
                refreshToken = new RefreshToken(
                    Tokens.RefreshToken,
                    Tokens.RefreshExpiresIn,
                    User?.UserId ?? "guest"
                );
            }

            return new TokenSet(accessToken, refreshToken);
        }

        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"GuestLoginResponse: Success={Success}";
            
            if (!string.IsNullOrEmpty(Message))
            {
                info += $", Message={Message}";
            }
            
            if (Tokens != null)
            {
                info += $", HasTokens=true";
                info += $", AccessTokenLength={Tokens.AccessToken?.Length ?? 0}";
                info += $", HasRefreshToken={!string.IsNullOrEmpty(Tokens.RefreshToken)}";
            }
            
            if (User != null)
            {
                info += $", UserId={User.UserId}";
            }
            
            return info;
        }
    }

    /// <summary>
    /// Guest 토큰 정보
    /// </summary>
    [Serializable]
    public class GuestTokenInfo
    {
        /// <summary>
        /// 액세스 토큰
        /// </summary>
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

        /// <summary>
        /// 리프레시 토큰
        /// </summary>
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        /// <summary>
        /// 액세스 토큰 만료 시간 (초)
        /// </summary>
        [JsonProperty("expiresIn")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// 리프레시 토큰 만료 시간 (초)
        /// </summary>
        [JsonProperty("refreshExpiresIn")]
        public int RefreshExpiresIn { get; set; }

        /// <summary>
        /// 토큰 타입
        /// </summary>
        [JsonProperty("tokenType")]
        public string TokenType { get; set; } = "Bearer";
    }

    /// <summary>
    /// Guest 사용자 정보
    /// </summary>
    [Serializable]
    public class GuestUserInfo
    {
        /// <summary>
        /// 사용자 ID
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// 게스트 ID
        /// </summary>
        [JsonProperty("guestId")]
        public string GuestId { get; set; }

        /// <summary>
        /// 생성 시간
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 마지막 로그인 시간
        /// </summary>
        [JsonProperty("lastLoginAt")]
        public DateTime LastLoginAt { get; set; }

        /// <summary>
        /// 사용자 타입
        /// </summary>
        [JsonProperty("userType")]
        public string UserType { get; set; } = "guest";
    }
}