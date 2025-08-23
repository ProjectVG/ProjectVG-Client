using System;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.Core
{
    public interface IAuthManager
    {
        bool IsAuthenticated { get; }
        bool IsInitialized { get; }
        
        event Action<bool> OnAuthStateChanged;
        event Action<string> OnAuthError;
        
        UniTask InitializeAsync();
        UniTask<bool> LoginAsync();
        UniTask LogoutAsync();
        UniTask<string> GetValidAccessTokenAsync();
        UniTask<bool> RefreshTokenAsync();
        void ClearAuthState();
    }
}
