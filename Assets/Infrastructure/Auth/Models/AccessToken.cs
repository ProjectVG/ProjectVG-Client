using System;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.Models
{
    [Serializable]
    public class AccessToken
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        
        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }
        
        [JsonProperty("token_type")]
        public string TokenType { get; set; } = "Bearer";
        
        [JsonProperty("scope")]
        public string Scope { get; set; }
        
        [JsonIgnore]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        
        [JsonIgnore]
        public bool IsExpiringSoon => DateTime.UtcNow >= ExpiresAt.AddMinutes(-1);
        
        [JsonIgnore]
        public TimeSpan TimeUntilExpiry => ExpiresAt - DateTime.UtcNow;
        
        public AccessToken() { }
        
        public AccessToken(string token, int expiresInSeconds, string tokenType = "Bearer", string scope = null)
        {
            Token = token;
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
            TokenType = tokenType;
            Scope = scope;
        }
        
        public bool IsValidFor(TimeSpan bufferTime)
        {
            return DateTime.UtcNow.Add(bufferTime) < ExpiresAt;
        }
    }
}
