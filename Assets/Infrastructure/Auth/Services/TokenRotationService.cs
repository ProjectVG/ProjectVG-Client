using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Network.Http;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.Services
{
    /// <summary>
    /// 토큰 로테이션 및 재사용 감지 서비스
    /// </summary>
    public class TokenRotationService
    {
        private const string REFRESH_ENDPOINT = "/auth/refresh";
        private const string REVOKE_ENDPOINT = "/auth/revoke";
        private const string TOKEN_STATUS_ENDPOINT = "/auth/token/status";
        
        private readonly HttpApiClient _httpClient;
        
        // 재사용 감지를 위한 로컬 상태
        private readonly Dictionary<string, DateTime> _usedTokens = new Dictionary<string, DateTime>();
        private readonly object _lockObject = new object();
        
        public event Action<SecurityEvent> OnSecurityEventDetected;
        
        public TokenRotationService()
        {
            _httpClient = HttpApiClient.Instance;
        }
        
        #region Public Methods
        
        /// <summary>
        /// Refresh 토큰으로 새 토큰 셋 요청
        /// </summary>
        public async UniTask<TokenSet> RefreshTokenAsync(RefreshToken refreshToken)
        {
            if (refreshToken == null)
            {
                throw new ArgumentNullException(nameof(refreshToken));
            }
            
            try
            {
                // 로컬 재사용 체크
                CheckLocalTokenReuse(refreshToken.Token);
                
                // 갱신 요청 생성
                var request = CreateRefreshRequest(refreshToken);
                
                // 서버 요청
                var response = await _httpClient.PostAsync<RefreshTokenResponse>(REFRESH_ENDPOINT, request);
                
                if (!response.Success)
                {
                    HandleRefreshError(response.Error, refreshToken);
                    return null;
                }
                
                // 토큰 사용 기록
                RecordTokenUsage(refreshToken.Token);
                
                // 응답을 TokenSet으로 변환
                var tokenSet = ConvertToTokenSet(response.Data);
                
                Debug.Log($"[TokenRotationService] 토큰 갱신 성공 - 로테이션 카운트: {response.Data.RotationInfo.CurrentRotationCount}");
                
                return tokenSet;
            }
            catch (ApiException ex) when (ex.StatusCode == 403)
            {
                HandleSecurityException(ex, refreshToken);
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenRotationService] 토큰 갱신 실패: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 토큰 무효화
        /// </summary>
        public async UniTask<bool> RevokeTokenAsync(RefreshToken refreshToken, bool revokeAllTokens = false, string reason = "user_logout")
        {
            if (refreshToken == null)
            {
                return true; // 이미 없으면 성공으로 간주
            }
            
            try
            {
                var request = new RevokeTokenRequest
                {
                    RefreshToken = refreshToken.Token,
                    RevokeAllTokens = revokeAllTokens,
                    Reason = reason
                };
                
                var response = await _httpClient.PostAsync<RevokeTokenResponse>(REVOKE_ENDPOINT, request);
                
                if (response?.Success == true)
                {
                    Debug.Log($"[TokenRotationService] 토큰 무효화 성공 - 무효화된 토큰 수: {response.Data.TokensRevoked}");
                    
                    // 로컬 사용 기록에서도 제거
                    RemoveTokenUsageRecord(refreshToken.Token);
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenRotationService] 토큰 무효화 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 토큰 상태 조회
        /// </summary>
        public async UniTask<TokenStatusData> GetTokenStatusAsync(string tokenId, AccessToken accessToken)
        {
            if (string.IsNullOrEmpty(tokenId) || accessToken == null)
            {
                return null;
            }
            
            try
            {
                var headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {accessToken.Token}" }
                };
                
                var response = await _httpClient.GetAsync<TokenStatusResponse>(
                    $"{TOKEN_STATUS_ENDPOINT}?token_id={tokenId}", 
                    headers
                );
                
                if (response?.Success == true)
                {
                    Debug.Log($"[TokenRotationService] 토큰 상태 조회 성공: {response.Data.Status}");
                    return response.Data;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenRotationService] 토큰 상태 조회 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 로컬 토큰 사용 기록 정리
        /// </summary>
        public void CleanupTokenUsageRecords()
        {
            lock (_lockObject)
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-24); // 24시간 이전 기록 삭제
                var tokensToRemove = new List<string>();
                
                foreach (var kvp in _usedTokens)
                {
                    if (kvp.Value < cutoffTime)
                    {
                        tokensToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var token in tokensToRemove)
                {
                    _usedTokens.Remove(token);
                }
                
                if (tokensToRemove.Count > 0)
                {
                    Debug.Log($"[TokenRotationService] {tokensToRemove.Count}개의 오래된 토큰 사용 기록 정리");
                }
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private RefreshTokenRequest CreateRefreshRequest(RefreshToken refreshToken)
        {
            return new RefreshTokenRequest
            {
                RefreshToken = refreshToken.Token,
                DeviceId = refreshToken.DeviceId ?? UnityEngine.SystemInfo.deviceUniqueIdentifier,
                ClientInfo = new ClientInfo(),
                RotationId = ExtractRotationId(refreshToken) // RefreshToken 모델에서 RotationId 추출
            };
        }
        
        private string ExtractRotationId(RefreshToken refreshToken)
        {
            // TODO: RefreshToken 모델에 RotationId 프로퍼티 추가 필요
            // 임시로 토큰 해시의 일부를 사용
            var hash = refreshToken.Token.GetHashCode();
            return $"rot_{Math.Abs(hash):X16}";
        }
        
        private void CheckLocalTokenReuse(string token)
        {
            lock (_lockObject)
            {
                if (_usedTokens.ContainsKey(token))
                {
                    var previousUse = _usedTokens[token];
                    var timeSinceUse = DateTime.UtcNow - previousUse;
                    
                    // Grace Period (30초) 체크
                    if (timeSinceUse.TotalSeconds > 30)
                    {
                        Debug.LogError($"[TokenRotationService] 로컬 토큰 재사용 감지: {token.Substring(0, 10)}...");
                        
                        var securityEvent = new SecurityEvent
                        {
                            EventType = "LOCAL_TOKEN_REUSE_DETECTED",
                            DeviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier,
                            Timestamp = DateTime.UtcNow,
                            Severity = SecurityEventSeverity.High,
                            Details = new
                            {
                                previous_use = previousUse,
                                time_since_use_seconds = timeSinceUse.TotalSeconds,
                                grace_period_exceeded = true
                            }
                        };
                        
                        OnSecurityEventDetected?.Invoke(securityEvent);
                        
                        throw TokenRotationException.TokenReuse(
                            ExtractRotationId(new RefreshToken { Token = token }),
                            previousUse,
                            DateTime.UtcNow
                        );
                    }
                }
            }
        }
        
        private void RecordTokenUsage(string token)
        {
            lock (_lockObject)
            {
                _usedTokens[token] = DateTime.UtcNow;
            }
        }
        
        private void RemoveTokenUsageRecord(string token)
        {
            lock (_lockObject)
            {
                _usedTokens.Remove(token);
            }
        }
        
        private void HandleRefreshError(ApiError error, RefreshToken refreshToken)
        {
            if (error == null) return;
            
            Debug.LogError($"[TokenRotationService] 갱신 오류: {error.Code} - {error.Message}");
            
            var securityEvent = new SecurityEvent
            {
                EventType = $"REFRESH_ERROR_{error.Code}",
                DeviceId = refreshToken.DeviceId,
                Timestamp = DateTime.UtcNow,
                Severity = error.IsTokenReuse ? SecurityEventSeverity.Critical : SecurityEventSeverity.Medium,
                Details = error.Details
            };
            
            OnSecurityEventDetected?.Invoke(securityEvent);
            
            // 적절한 예외 타입으로 변환
            switch (error.Code)
            {
                case "TOKEN_REUSE_DETECTED":
                    throw TokenRotationException.TokenReuse("unknown", DateTime.MinValue, DateTime.UtcNow);
                case "DEVICE_MISMATCH":
                    throw TokenRotationException.DeviceMismatch("expected", "received");
                case "ROTATION_LIMIT_EXCEEDED":
                    throw TokenRotationException.RotationLimitExceeded(100, 100, DateTime.UtcNow.AddDays(1));
                default:
                    throw new TokenRotationException(error.Code, error.Message, error.Details, error.RequiresReauth);
            }
        }
        
        private void HandleSecurityException(ApiException ex, RefreshToken refreshToken)
        {
            var securityEvent = new SecurityEvent
            {
                EventType = "API_SECURITY_EXCEPTION",
                DeviceId = refreshToken.DeviceId,
                Timestamp = DateTime.UtcNow,
                Severity = SecurityEventSeverity.High,
                Details = new
                {
                    status_code = ex.StatusCode,
                    error_message = ex.Message,
                    response_body = ex.ResponseBody
                }
            };
            
            OnSecurityEventDetected?.Invoke(securityEvent);
        }
        
        private TokenSet ConvertToTokenSet(RefreshTokenData data)
        {
            if (data?.AccessToken == null || data?.RefreshToken == null)
            {
                throw new InvalidOperationException("응답에서 토큰 데이터를 찾을 수 없습니다.");
            }
            
            var accessToken = data.AccessToken.ToAccessToken();
            var refreshToken = data.RefreshToken.ToRefreshToken();
            
            return new TokenSet(accessToken, refreshToken);
        }
        
        #endregion
        
        #region Static Helper Methods
        
        /// <summary>
        /// 디바이스 고유 식별자 생성
        /// </summary>
        public static string GenerateDeviceId()
        {
            // Unity의 기본 디바이스 식별자 사용
            var deviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
            
            // 추가 정보로 보강 (선택적)
            var additionalInfo = $"{UnityEngine.SystemInfo.deviceModel}_{UnityEngine.SystemInfo.processorType}";
            var combined = $"{deviceId}_{additionalInfo.GetHashCode():X8}";
            
            return combined;
        }
        
        /// <summary>
        /// 보안 이벤트 로그 형식화
        /// </summary>
        public static string FormatSecurityEvent(SecurityEvent securityEvent)
        {
            return JsonConvert.SerializeObject(securityEvent, Formatting.Indented);
        }
        
        #endregion
    }
}
