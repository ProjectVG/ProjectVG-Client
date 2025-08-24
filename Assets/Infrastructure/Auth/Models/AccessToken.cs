using System;

namespace ProjectVG.Infrastructure.Auth.Models
{
    /// <summary>
    /// Access Token 모델
    /// </summary>
    [Serializable]
    public class AccessToken
    {
        /// <summary>
        /// 토큰 문자열
        /// </summary>
        public string Token { get; set; }
        
        /// <summary>
        /// 만료 시간 (초)
        /// </summary>
        public int ExpiresIn { get; set; }
        
        /// <summary>
        /// 만료 시간 (DateTime)
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// 토큰 타입 (예: Bearer)
        /// </summary>
        public string TokenType { get; set; }
        
        /// <summary>
        /// 토큰 스코프
        /// </summary>
        public string Scope { get; set; }
        
        /// <summary>
        /// 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        public AccessToken()
        {
            CreatedAt = DateTime.UtcNow;
        }
        
        public AccessToken(string token, int expiresIn, string tokenType = "Bearer", string scope = "")
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            ExpiresIn = expiresIn;
            TokenType = tokenType ?? "Bearer";
            Scope = scope ?? "";
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = CreatedAt.AddSeconds(expiresIn);
        }
        
        public AccessToken(string token, DateTime expiresAt, string tokenType = "Bearer", string scope = "")
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            ExpiresAt = expiresAt;
            TokenType = tokenType ?? "Bearer";
            Scope = scope ?? "";
            CreatedAt = DateTime.UtcNow;
            ExpiresIn = (int)(expiresAt - CreatedAt).TotalSeconds;
        }
        
        /// <summary>
        /// 토큰 유효성 검사
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Token) && !IsExpired();
        }
        
        /// <summary>
        /// 토큰 만료 여부 확인
        /// </summary>
        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAt;
        }
        
        /// <summary>
        /// 토큰 만료까지 남은 시간
        /// </summary>
        public TimeSpan TimeUntilExpiry()
        {
            var timeLeft = ExpiresAt - DateTime.UtcNow;
            return timeLeft > TimeSpan.Zero ? timeLeft : TimeSpan.Zero;
        }
        
        /// <summary>
        /// 토큰 만료까지 남은 시간 (초)
        /// </summary>
        public int SecondsUntilExpiry()
        {
            return (int)TimeUntilExpiry().TotalSeconds;
        }
        
        /// <summary>
        /// 토큰이 곧 만료될 예정인지 확인
        /// </summary>
        /// <param name="bufferMinutes">버퍼 시간 (분)</param>
        /// <returns>곧 만료될 예정인지 여부</returns>
        public bool IsExpiringSoon(int bufferMinutes = 5)
        {
            var timeLeft = TimeUntilExpiry();
            return timeLeft <= TimeSpan.FromMinutes(bufferMinutes);
        }
        
        /// <summary>
        /// Authorization 헤더 값 생성
        /// </summary>
        /// <returns>Authorization 헤더 값</returns>
        public string GetAuthorizationHeader()
        {
            return $"{TokenType} {Token}";
        }
        
        /// <summary>
        /// 토큰 정보 복사
        /// </summary>
        /// <returns>새로운 AccessToken 인스턴스</returns>
        public AccessToken Clone()
        {
            return new AccessToken(Token, ExpiresAt, TokenType, Scope);
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        /// <returns>디버그 정보</returns>
        public string GetDebugInfo()
        {
            var info = $"AccessToken Debug Info:\n";
            info += $"Token: {Token?.Substring(0, Math.Min(20, Token.Length))}...\n";
            info += $"Expires In: {ExpiresIn}초\n";
            info += $"Expires At: {ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC\n";
            info += $"Created At: {CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
            info += $"Token Type: {TokenType}\n";
            info += $"Scope: {Scope}\n";
            info += $"Is Valid: {IsValid()}\n";
            info += $"Is Expired: {IsExpired()}\n";
            info += $"Time Until Expiry: {TimeUntilExpiry()}\n";
            info += $"Is Expiring Soon: {IsExpiringSoon()}\n";
            
            return info;
        }
    }
}
