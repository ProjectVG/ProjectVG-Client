using System;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    public interface IOAuth2CallbackHandler
    {
        bool IsHandlingCallback { get; }
        string PlatformName { get; }
        
        event Action<string, string> OnAuthorizationCodeReceived;
        event Action<string> OnCallbackError;
        event Action OnCallbackCancelled;
        
        UniTask<bool> StartListeningForCallbackAsync(string expectedState, TimeSpan timeout);
        void StopListening();
        bool ValidateCallback(string code, string state, string expectedState);
        string GetCallbackUrl();
    }
}
