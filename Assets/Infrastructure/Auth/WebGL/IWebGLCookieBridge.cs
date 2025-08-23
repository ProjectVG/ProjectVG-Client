using System;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Auth.WebGL
{
    public interface IWebGLCookieBridge
    {
        bool IsAvailable { get; }
        bool IsBFFModeEnabled { get; }
        
        event Action<string> OnCookieError;
        
        UniTask<bool> SetRefreshTokenCookieAsync(RefreshToken token);
        UniTask<RefreshToken> GetRefreshTokenFromCookieAsync();
        UniTask<bool> ClearRefreshTokenCookieAsync();
        
        UniTask<string> GetCSRFTokenAsync();
        UniTask<bool> ValidateCSRFTokenAsync(string token);
        
        UniTask<bool> SetAuthenticationCookieAsync(string sessionId, TimeSpan? maxAge = null);
        UniTask<bool> ClearAuthenticationCookieAsync();
        
        UniTask<bool> TestCookieAccessibilityAsync();
        void EnableBFFMode(bool enabled);
    }
}
