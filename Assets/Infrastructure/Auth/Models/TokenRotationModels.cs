using System;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.Models
{
    /// <summary>
    /// 토큰 갱신 요청 모델
    /// </summary>
    [Serializable]
    public class RefreshTokenRequest
    {
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }
        
        [JsonProperty("client_info")]
        public ClientInfo ClientInfo { get; set; }
        
        [JsonProperty("rotation_id")]
        public string RotationId { get; set; }
        
        public RefreshTokenRequest()
        {
            ClientInfo = new ClientInfo();
        }
    }
    
    /// <summary>
    /// 클라이언트 정보 모델
    /// </summary>
    [Serializable]
    public class ClientInfo
    {
        [JsonProperty("platform")]
        public string Platform { get; set; } = "Unity";
        
        [JsonProperty("version")]
        public string Version { get; set; }
        
        [JsonProperty("os")]
        public string OS { get; set; }
        
        [JsonProperty("device_fingerprint")]
        public string DeviceFingerprint { get; set; }
        
        public ClientInfo()
        {
            Version = UnityEngine.Application.version;
            OS = UnityEngine.SystemInfo.operatingSystem;
            DeviceFingerprint = GenerateDeviceFingerprint();
        }
        
        private string GenerateDeviceFingerprint()
        {
            var deviceInfo = $"{UnityEngine.SystemInfo.deviceModel}_{UnityEngine.SystemInfo.processorType}_{UnityEngine.SystemInfo.graphicsDeviceName}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(deviceInfo)).Substring(0, 16);
        }
    }
    
    /// <summary>
    /// 토큰 갱신 응답 모델
    /// </summary>
    [Serializable]
    public class RefreshTokenResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("data")]
        public RefreshTokenData Data { get; set; }
        
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonProperty("error")]
        public ApiError Error { get; set; }
    }
    
    /// <summary>
    /// 토큰 갱신 응답 데이터
    /// </summary>
    [Serializable]
    public class RefreshTokenData
    {
        [JsonProperty("access_token")]
        public AccessTokenResponse AccessToken { get; set; }
        
        [JsonProperty("refresh_token")]
        public RefreshTokenDetailResponse RefreshToken { get; set; }
        
        [JsonProperty("rotation_info")]
        public RotationInfo RotationInfo { get; set; }
    }
    
    /// <summary>
    /// Access 토큰 응답 모델
    /// </summary>
    [Serializable]
    public class AccessTokenResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }
        
        [JsonProperty("token_type")]
        public string TokenType { get; set; } = "Bearer";
        
        [JsonProperty("scope")]
        public string Scope { get; set; }
        
        public AccessToken ToAccessToken()
        {
            return new AccessToken(Token, ExpiresIn, TokenType, Scope);
        }
    }
    
    /// <summary>
    /// Refresh 토큰 상세 응답 모델
    /// </summary>
    [Serializable]
    public class RefreshTokenDetailResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }
        
        [JsonProperty("rotation_id")]
        public string RotationId { get; set; }
        
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }
        
        public RefreshToken ToRefreshToken()
        {
            var token = new RefreshToken(Token, ExpiresIn, DeviceId);
            // RotationId는 RefreshToken 모델에 추가 필요
            return token;
        }
    }
    
    /// <summary>
    /// 토큰 로테이션 정보
    /// </summary>
    [Serializable]
    public class RotationInfo
    {
        [JsonProperty("previous_token_invalidated")]
        public bool PreviousTokenInvalidated { get; set; }
        
        [JsonProperty("grace_period_seconds")]
        public int GracePeriodSeconds { get; set; }
        
        [JsonProperty("max_rotations_per_day")]
        public int MaxRotationsPerDay { get; set; }
        
        [JsonProperty("current_rotation_count")]
        public int CurrentRotationCount { get; set; }
    }
    
    /// <summary>
    /// API 에러 모델
    /// </summary>
    [Serializable]
    public class ApiError
    {
        [JsonProperty("code")]
        public string Code { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("details")]
        public object Details { get; set; }
        
        public bool IsTokenReuse => Code == "TOKEN_REUSE_DETECTED";
        public bool IsTokenExpired => Code == "TOKEN_EXPIRED";
        public bool IsDeviceMismatch => Code == "DEVICE_MISMATCH";
        public bool IsRotationLimitExceeded => Code == "ROTATION_LIMIT_EXCEEDED";
        public bool RequiresReauth => GetDetailBool("requires_reauth");
        
        private bool GetDetailBool(string key)
        {
            if (Details is Newtonsoft.Json.Linq.JObject jobj && jobj.ContainsKey(key))
            {
                return jobj[key].ToObject<bool>();
            }
            return false;
        }
    }
    
    /// <summary>
    /// 토큰 무효화 요청
    /// </summary>
    [Serializable]
    public class RevokeTokenRequest
    {
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        
        [JsonProperty("revoke_all_tokens")]
        public bool RevokeAllTokens { get; set; }
        
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// 토큰 무효화 응답
    /// </summary>
    [Serializable]
    public class RevokeTokenResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("data")]
        public RevokeTokenData Data { get; set; }
    }
    
    /// <summary>
    /// 토큰 무효화 응답 데이터
    /// </summary>
    [Serializable]
    public class RevokeTokenData
    {
        [JsonProperty("tokens_revoked")]
        public int TokensRevoked { get; set; }
        
        [JsonProperty("revocation_time")]
        public DateTime RevocationTime { get; set; }
    }
    
    /// <summary>
    /// 토큰 상태 조회 응답
    /// </summary>
    [Serializable]
    public class TokenStatusResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("data")]
        public TokenStatusData Data { get; set; }
    }
    
    /// <summary>
    /// 토큰 상태 데이터
    /// </summary>
    [Serializable]
    public class TokenStatusData
    {
        [JsonProperty("token_id")]
        public string TokenId { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("issued_at")]
        public DateTime IssuedAt { get; set; }
        
        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }
        
        [JsonProperty("last_used")]
        public DateTime LastUsed { get; set; }
        
        [JsonProperty("rotation_count")]
        public int RotationCount { get; set; }
        
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }
        
        [JsonProperty("is_in_grace_period")]
        public bool IsInGracePeriod { get; set; }
        
        [JsonIgnore]
        public bool IsActive => Status == "active";
        
        [JsonIgnore]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
    
    /// <summary>
    /// 보안 이벤트 로그 모델
    /// </summary>
    [Serializable]
    public class SecurityEvent
    {
        [JsonProperty("event_type")]
        public string EventType { get; set; }
        
        [JsonProperty("user_id")]
        public string UserId { get; set; }
        
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }
        
        [JsonProperty("ip_address")]
        public string IpAddress { get; set; }
        
        [JsonProperty("user_agent")]
        public string UserAgent { get; set; }
        
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonProperty("details")]
        public object Details { get; set; }
        
        [JsonProperty("severity")]
        public SecurityEventSeverity Severity { get; set; }
    }
    
    /// <summary>
    /// 보안 이벤트 심각도
    /// </summary>
    public enum SecurityEventSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    /// <summary>
    /// 토큰 로테이션 예외
    /// </summary>
    public class TokenRotationException : Exception
    {
        public string ErrorCode { get; }
        public object ErrorDetails { get; }
        public bool RequiresReauth { get; }
        
        public TokenRotationException(string errorCode, string message, object details = null, bool requiresReauth = false) 
            : base(message)
        {
            ErrorCode = errorCode;
            ErrorDetails = details;
            RequiresReauth = requiresReauth;
        }
        
        public static TokenRotationException TokenReuse(string tokenId, DateTime originalUse, DateTime reuseDetection)
        {
            var details = new
            {
                reused_token_id = tokenId,
                original_use_time = originalUse,
                reuse_detection_time = reuseDetection,
                all_tokens_invalidated = true
            };
            
            return new TokenRotationException(
                "TOKEN_REUSE_DETECTED",
                "Refresh token reuse detected - all tokens invalidated",
                details,
                requiresReauth: true
            );
        }
        
        public static TokenRotationException DeviceMismatch(string expectedDevice, string receivedDevice)
        {
            var details = new
            {
                expected_device_id = expectedDevice,
                received_device_id = receivedDevice
            };
            
            return new TokenRotationException(
                "DEVICE_MISMATCH",
                "Token issued for different device",
                details,
                requiresReauth: true
            );
        }
        
        public static TokenRotationException RotationLimitExceeded(int maxRotations, int currentCount, DateTime resetTime)
        {
            var details = new
            {
                max_rotations_per_day = maxRotations,
                current_count = currentCount,
                reset_time = resetTime
            };
            
            return new TokenRotationException(
                "ROTATION_LIMIT_EXCEEDED",
                "Too many token rotations in 24 hours",
                details,
                requiresReauth: false
            );
        }
    }
}
