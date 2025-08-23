using System;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    public interface IOAuth2Client
    {
        bool IsConfigured { get; }
        
        UniTask<bool> StartAuthorizationAsync();
        UniTask<TokenSet> ExchangeCodeForTokensAsync(string code, string state);
        UniTask<TokenSet> RefreshTokenAsync(RefreshToken refreshToken);
        
        string GenerateAuthorizationUrl(string state, string codeChallenge);
        bool ValidateState(string receivedState, string expectedState);
    }
}
