using System;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Auth.Core
{
    public interface ITokenStorage
    {
        UniTask StoreRefreshTokenAsync(RefreshToken token);
        UniTask<RefreshToken> LoadRefreshTokenAsync();
        UniTask ClearRefreshTokenAsync();
        
        void StoreAccessTokenInMemory(AccessToken token);
        AccessToken GetAccessTokenFromMemory();
        void ClearAccessTokenFromMemory();
        
        bool HasValidAccessToken { get; }
        bool HasStoredRefreshToken { get; }
    }
}
