using System;
using UnityEngine;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    [Serializable]
    public class OAuth2Config
    {
        [Header("OAuth2 Provider Settings")]
        public string ClientId;
        public string AuthorizationEndpoint;
        public string TokenEndpoint;
        public string RedirectUri;
        public string Scope;
        
        [Header("PKCE Settings")]
        public bool UsePKCE = true;
        
        [Header("Timeouts")]
        public int AuthorizationTimeoutSeconds = 300;
        public int TokenRequestTimeoutSeconds = 30;
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ClientId) &&
                   !string.IsNullOrEmpty(AuthorizationEndpoint) &&
                   !string.IsNullOrEmpty(TokenEndpoint) &&
                   !string.IsNullOrEmpty(RedirectUri);
        }
        
        public static OAuth2Config CreateDefault()
        {
            return new OAuth2Config
            {
                ClientId = "your_client_id",
                AuthorizationEndpoint = "https://your-auth-server.com/oauth2/authorize",
                TokenEndpoint = "https://your-auth-server.com/oauth2/token",
                RedirectUri = "com.yourcompany.yourgame://auth/callback",
                Scope = "openid profile offline_access",
                UsePKCE = true,
                AuthorizationTimeoutSeconds = 300,
                TokenRequestTimeoutSeconds = 30
            };
        }
    }
}
