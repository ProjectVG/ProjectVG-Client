using System;

namespace ProjectVG.Infrastructure.Auth.Models
{
    /// <summary>
    /// Refresh Token 모델
    /// </summary>
    [Serializable]
    public class RefreshToken
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
        /// 디바이스 ID
        /// </summary>
        public string DeviceId { get; set; }
        
        /// <summary>
        /// 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        public RefreshToken()
        {
            CreatedAt = DateTime.UtcNow;
        }
        
        public RefreshToken(string token, int expiresIn, string deviceId = null)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            ExpiresIn = expiresIn;
            DeviceId = deviceId ?? "";
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = CreatedAt.AddSeconds(expiresIn);
        }
        
        public RefreshToken(string token, DateTime expiresAt, string deviceId = null)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            ExpiresAt = expiresAt;
            DeviceId = deviceId ?? "";
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
        /// <param name="bufferDays">버퍼 시간 (일)</param>
        /// <returns>곧 만료될 예정인지 여부</returns>
        public bool IsExpiringSoon(int bufferDays = 7)
        {
            var timeLeft = TimeUntilExpiry();
            return timeLeft <= TimeSpan.FromDays(bufferDays);
        }
        
        /// <summary>
        /// 토큰 정보 복사
        /// </summary>
        /// <returns>새로운 RefreshToken 인스턴스</returns>
        public RefreshToken Clone()
        {
            return new RefreshToken(Token, ExpiresAt, DeviceId);
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        /// <returns>디버그 정보</returns>
        public string GetDebugInfo()
        {
            var info = $"RefreshToken Debug Info:\n";
            info += $"Token: {Token?.Substring(0, Math.Min(20, Token.Length))}...\n";
            info += $"Expires In: {ExpiresIn}초\n";
            info += $"Expires At: {ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC\n";
            info += $"Created At: {CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
            info += $"Device ID: {DeviceId}\n";
            info += $"Is Valid: {IsValid()}\n";
            info += $"Is Expired: {IsExpired()}\n";
            info += $"Time Until Expiry: {TimeUntilExpiry()}\n";
            info += $"Is Expiring Soon: {IsExpiringSoon()}\n";
            
            return info;
        }
    }
}
