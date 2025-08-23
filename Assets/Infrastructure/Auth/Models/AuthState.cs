using System;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.Models
{
    [Serializable]
    public class AuthState
    {
        [JsonProperty("is_authenticated")]
        public bool IsAuthenticated { get; set; }
        
        [JsonProperty("user_id")]
        public string UserId { get; set; }
        
        [JsonProperty("login_time")]
        public DateTime? LoginTime { get; set; }
        
        [JsonProperty("last_activity")]
        public DateTime LastActivity { get; set; }
        
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }
        
        [JsonProperty("session_id")]
        public string SessionId { get; set; }
        
        [JsonIgnore]
        public TimeSpan SessionDuration => LoginTime.HasValue ? 
            DateTime.UtcNow - LoginTime.Value : TimeSpan.Zero;
        
        public AuthState()
        {
            IsAuthenticated = false;
            LastActivity = DateTime.UtcNow;
            DeviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
        }
        
        public void MarkAsAuthenticated(string userId, string sessionId = null)
        {
            IsAuthenticated = true;
            UserId = userId;
            LoginTime = DateTime.UtcNow;
            SessionId = sessionId;
            UpdateActivity();
        }
        
        public void MarkAsUnauthenticated()
        {
            IsAuthenticated = false;
            UserId = null;
            LoginTime = null;
            SessionId = null;
            UpdateActivity();
        }
        
        public void UpdateActivity()
        {
            LastActivity = DateTime.UtcNow;
        }
    }
}
