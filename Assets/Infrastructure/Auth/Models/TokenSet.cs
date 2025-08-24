using System;

namespace ProjectVG.Infrastructure.Auth.Models
{
    /// <summary>
    /// 토큰 세트 (Access Token + Refresh Token)
    /// </summary>
    [Serializable]
    public class TokenSet
    {
        /// <summary>
        /// Access Token
        /// </summary>
        public AccessToken AccessToken { get; set; }
        
        /// <summary>
        /// Refresh Token
        /// </summary>
        public RefreshToken RefreshToken { get; set; }
        
        /// <summary>
        /// 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        public TokenSet()
        {
            CreatedAt = DateTime.UtcNow;
        }
        
        public TokenSet(AccessToken accessToken, RefreshToken refreshToken = null)
        {
            AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            RefreshToken = refreshToken;
            CreatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// 토큰 세트 유효성 검사
        /// </summary>
        public bool IsValid()
        {
            return AccessToken != null && AccessToken.IsValid();
        }
        
        /// <summary>
        /// Access Token이 만료되었는지 확인
        /// </summary>
        public bool IsAccessTokenExpired()
        {
            return AccessToken?.IsExpired() ?? true;
        }
        
        /// <summary>
        /// Refresh Token이 있는지 확인
        /// </summary>
        public bool HasRefreshToken()
        {
            return RefreshToken != null && RefreshToken.IsValid();
        }
        
        /// <summary>
        /// Refresh Token이 만료되었는지 확인
        /// </summary>
        public bool IsRefreshTokenExpired()
        {
            return RefreshToken?.IsExpired() ?? true;
        }
        
        /// <summary>
        /// 토큰 갱신이 필요한지 확인
        /// </summary>
        /// <param name="bufferMinutes">만료 전 버퍼 시간 (분)</param>
        /// <returns>갱신 필요 여부</returns>
        public bool NeedsRefresh(int bufferMinutes = 5)
        {
            if (AccessToken == null)
                return true;
            
            if (AccessToken.IsExpired())
                return true;
            
            // 만료 5분 전에 갱신
            var refreshTime = AccessToken.ExpiresAt.AddMinutes(-bufferMinutes);
            return DateTime.UtcNow >= refreshTime;
        }
        
        /// <summary>
        /// 토큰 세트를 새로 업데이트
        /// </summary>
        /// <param name="newAccessToken">새 Access Token</param>
        /// <param name="newRefreshToken">새 Refresh Token (선택적)</param>
        public void UpdateTokens(AccessToken newAccessToken, RefreshToken newRefreshToken = null)
        {
            AccessToken = newAccessToken ?? throw new ArgumentNullException(nameof(newAccessToken));
            
            // Refresh Token이 제공된 경우에만 업데이트
            if (newRefreshToken != null)
            {
                RefreshToken = newRefreshToken;
            }
            
            CreatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// 토큰 세트 초기화
        /// </summary>
        public void Clear()
        {
            AccessToken = null;
            RefreshToken = null;
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        /// <returns>디버그 정보</returns>
        public string GetDebugInfo()
        {
            var info = $"TokenSet Debug Info:\n";
            info += $"Created At: {CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
            info += $"Is Valid: {IsValid()}\n";
            info += $"Has Refresh Token: {HasRefreshToken()}\n";
            info += $"Needs Refresh: {NeedsRefresh()}\n";
            
            if (AccessToken != null)
            {
                info += $"Access Token:\n";
                info += $"  Token: {AccessToken.Token?.Substring(0, Math.Min(20, AccessToken.Token.Length))}...\n";
                info += $"  Expires At: {AccessToken.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                info += $"  Is Expired: {AccessToken.IsExpired()}\n";
                info += $"  Token Type: {AccessToken.TokenType}\n";
                info += $"  Scope: {AccessToken.Scope}\n";
            }
            
            if (RefreshToken != null)
            {
                info += $"Refresh Token:\n";
                info += $"  Token: {RefreshToken.Token?.Substring(0, Math.Min(20, RefreshToken.Token.Length))}...\n";
                info += $"  Expires At: {RefreshToken.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC\n";
                info += $"  Is Expired: {RefreshToken.IsExpired()}\n";
                info += $"  Device ID: {RefreshToken.DeviceId}\n";
            }
            
            return info;
        }
    }
}
