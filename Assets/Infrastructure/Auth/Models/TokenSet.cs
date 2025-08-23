using System;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.Models
{
    [Serializable]
    public class TokenSet
    {
        [JsonProperty("access_token")]
        public AccessToken AccessToken { get; set; }
        
        [JsonProperty("refresh_token")]
        public RefreshToken RefreshToken { get; set; }
        
        [JsonProperty("issued_at")]
        public DateTime IssuedAt { get; set; }
        
        [JsonIgnore]
        public bool IsValid => AccessToken != null && RefreshToken != null && 
                              !AccessToken.IsExpired && !RefreshToken.IsExpired;
        
        [JsonIgnore]
        public bool HasValidAccessToken => AccessToken != null && !AccessToken.IsExpired;
        
        [JsonIgnore]
        public bool HasValidRefreshToken => RefreshToken != null && !RefreshToken.IsExpired;
        
        public TokenSet()
        {
            IssuedAt = DateTime.UtcNow;
        }
        
        public TokenSet(AccessToken accessToken, RefreshToken refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            IssuedAt = DateTime.UtcNow;
        }
        
        public TokenSet UpdateAccessToken(AccessToken newAccessToken)
        {
            return new TokenSet(newAccessToken, RefreshToken);
        }
        
        public TokenSet UpdateRefreshToken(RefreshToken newRefreshToken)
        {
            return new TokenSet(AccessToken, newRefreshToken);
        }
        
        public TokenSet UpdateBothTokens(AccessToken newAccessToken, RefreshToken newRefreshToken)
        {
            return new TokenSet(newAccessToken, newRefreshToken);
        }
    }
}
