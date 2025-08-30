using System;

namespace ProjectVG.Infrastructure.Auth.Models
{
    [Serializable]
    public class RefreshToken
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string DeviceId { get; set; }
        
        public RefreshToken() { }
        
        public RefreshToken(string token, int expiresIn, string deviceId = null)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            DeviceId = deviceId ?? string.Empty;
        }
        
        public RefreshToken(string token, DateTime expiresAt, string deviceId = null)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            ExpiresAt = expiresAt;
            DeviceId = deviceId ?? string.Empty;
        }
        
        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAt;
        }
    }
}