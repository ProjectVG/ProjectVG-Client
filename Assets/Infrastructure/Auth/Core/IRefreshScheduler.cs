using System;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Auth.Core
{
    public interface IRefreshScheduler
    {
        bool IsScheduled { get; }
        
        event Action OnRefreshRequired;
        event Action<Exception> OnRefreshFailed;
        
        void ScheduleRefresh(AccessToken token);
        void CancelRefresh();
        UniTask<bool> ForceRefreshAsync();
        
        void UpdateRefreshPolicy(TimeSpan preRefreshTime, int maxRetries, TimeSpan backoffDelay);
    }
}
