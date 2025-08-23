using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Infrastructure.Auth.Core
{
    public class RefreshScheduler : IRefreshScheduler
    {
        private CancellationTokenSource _cancellationTokenSource;
        private TimeSpan _preRefreshTime = TimeSpan.FromMinutes(1);
        private int _maxRetries = 3;
        private TimeSpan _backoffDelay = TimeSpan.FromSeconds(5);
        
        public bool IsScheduled => _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;
        
        public event Action OnRefreshRequired;
        public event Action<Exception> OnRefreshFailed;
        
        #region Public Methods
        
        public void ScheduleRefresh(AccessToken token)
        {
            if (token == null || token.IsExpired)
            {
                Debug.LogWarning("[RefreshScheduler] 유효하지 않은 토큰으로 스케줄링 시도");
                return;
            }
            
            CancelRefresh();
            
            var refreshTime = CalculateRefreshTime(token);
            if (refreshTime <= TimeSpan.Zero)
            {
                Debug.Log("[RefreshScheduler] 즉시 갱신 필요");
                TriggerRefresh();
                return;
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            _ = ScheduleRefreshInternal(refreshTime, _cancellationTokenSource.Token);
            
            Debug.Log($"[RefreshScheduler] 토큰 갱신 스케줄링: {refreshTime.TotalMinutes:F1}분 후");
        }
        
        public void CancelRefresh()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                
                Debug.Log("[RefreshScheduler] 토큰 갱신 스케줄 취소");
            }
        }
        
        public async UniTask<bool> ForceRefreshAsync()
        {
            try
            {
                return await ExecuteRefreshWithRetryAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RefreshScheduler] 강제 갱신 실패: {ex.Message}");
                OnRefreshFailed?.Invoke(ex);
                return false;
            }
        }
        
        public void UpdateRefreshPolicy(TimeSpan preRefreshTime, int maxRetries, TimeSpan backoffDelay)
        {
            _preRefreshTime = preRefreshTime;
            _maxRetries = Math.Max(1, maxRetries);
            _backoffDelay = backoffDelay;
            
            Debug.Log($"[RefreshScheduler] 갱신 정책 업데이트 - 사전 갱신: {preRefreshTime.TotalMinutes:F1}분, 최대 재시도: {maxRetries}, 백오프: {backoffDelay.TotalSeconds}초");
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask ScheduleRefreshInternal(TimeSpan delay, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(delay, cancellationToken: cancellationToken);
                
                if (!cancellationToken.IsCancellationRequested)
                {
                    await ExecuteRefreshWithRetryAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[RefreshScheduler] 갱신 스케줄이 취소되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RefreshScheduler] 스케줄 실행 실패: {ex.Message}");
                OnRefreshFailed?.Invoke(ex);
            }
        }
        
        private async UniTask<bool> ExecuteRefreshWithRetryAsync(CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    Debug.Log($"[RefreshScheduler] 토큰 갱신 시도 {attempt}/{_maxRetries}");
                    
                    TriggerRefresh();
                    
                    // 갱신이 성공적으로 트리거되었다고 가정
                    // 실제 갱신 결과는 AuthManager에서 처리
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RefreshScheduler] 갱신 시도 {attempt} 실패: {ex.Message}");
                    
                    if (attempt == _maxRetries)
                    {
                        Debug.LogError($"[RefreshScheduler] {_maxRetries}번 시도 후 갱신 실패");
                        OnRefreshFailed?.Invoke(ex);
                        return false;
                    }
                    
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var backoffDelay = CalculateBackoffDelay(attempt);
                        await UniTask.Delay(backoffDelay, cancellationToken: cancellationToken);
                    }
                }
            }
            
            return false;
        }
        
        private TimeSpan CalculateRefreshTime(AccessToken token)
        {
            var timeUntilExpiry = token.TimeUntilExpiry;
            var refreshTime = timeUntilExpiry - _preRefreshTime;
            
            return refreshTime > TimeSpan.Zero ? refreshTime : TimeSpan.Zero;
        }
        
        private TimeSpan CalculateBackoffDelay(int attempt)
        {
            var exponentialBackoff = TimeSpan.FromMilliseconds(_backoffDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
            var maxDelay = TimeSpan.FromMinutes(5);
            
            return exponentialBackoff > maxDelay ? maxDelay : exponentialBackoff;
        }
        
        private void TriggerRefresh()
        {
            try
            {
                OnRefreshRequired?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RefreshScheduler] 갱신 이벤트 트리거 실패: {ex.Message}");
                OnRefreshFailed?.Invoke(ex);
            }
        }
        
        #endregion
    }
}
