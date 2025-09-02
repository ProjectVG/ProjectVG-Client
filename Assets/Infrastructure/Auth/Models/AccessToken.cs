using System;

namespace ProjectVG.Infrastructure.Auth.Models
{
    [Serializable]
    public class AccessToken
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        
        public AccessToken() { }
        
        public AccessToken(string token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            var exp = JwtTokenParser.GetExpirationTime(token);
            ExpiresAt = exp == DateTime.MinValue ? DateTime.MinValue : exp.ToUniversalTime();
        }
        
        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAt;
        }
        
        public bool IsExpiringSoon(int minutesBeforeExpiry)
        {
            return DateTime.UtcNow.AddMinutes(minutesBeforeExpiry) >= ExpiresAt;
        }
    }
}