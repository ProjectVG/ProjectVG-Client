using System;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.Models
{
    [Serializable]
    public class RefreshToken
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        
        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }
        
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }
        
        [JsonProperty("issued_at")]
        public DateTime IssuedAt { get; set; }
        
        [JsonIgnore]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        
        [JsonIgnore]
        public TimeSpan TimeUntilExpiry => ExpiresAt - DateTime.UtcNow;
        
        public RefreshToken() 
        {
            IssuedAt = DateTime.UtcNow;
        }
        
        public RefreshToken(string token, int expiresInSeconds, string deviceId = null)
        {
            Token = token;
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
            DeviceId = deviceId ?? UnityEngine.SystemInfo.deviceUniqueIdentifier;
            IssuedAt = DateTime.UtcNow;
        }
        
        public bool IsValidFor(TimeSpan bufferTime)
        {
            return DateTime.UtcNow.Add(bufferTime) < ExpiresAt;
        }
    }
}
