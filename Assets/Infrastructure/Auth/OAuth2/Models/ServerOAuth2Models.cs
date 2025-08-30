using System;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth.OAuth2.Models
{
    /// <summary>
    /// 서버 OAuth2 인증 요청 모델
    /// </summary>
    [Serializable]
    public class ServerOAuth2AuthorizeRequest
    {
        
        [JsonProperty("state")]
        public string State { get; set; }
        
        [JsonProperty("code_challenge")]
        public string CodeChallenge { get; set; }
        
        [JsonProperty("code_challenge_method")]
        public string CodeChallengeMethod { get; set; } = "S256";
        
        [JsonProperty("code_verifier")]
        public string CodeVerifier { get; set; }
        
        [JsonProperty("client_redirect_uri")]
        public string ClientRedirectUri { get; set; }
    }
    
    /// <summary>
    /// 서버 OAuth2 인증 응답 모델
    /// </summary>
    [Serializable]
    public class ServerOAuth2AuthorizeResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("auth_url")]
        public string AuthUrl { get; set; }
        
        [JsonProperty("state")]
        public string State { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
    }
    
    /// <summary>
    /// 서버 OAuth2 토큰 응답 모델
    /// </summary>
    [Serializable]
    public class ServerOAuth2TokenResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        // HTTP 헤더에서 추출할 토큰 정보
        [JsonIgnore]
        public string AccessToken { get; set; }
        
        [JsonIgnore]
        public string RefreshToken { get; set; }
        
        [JsonIgnore]
        public int ExpiresIn { get; set; }
        
        [JsonIgnore]
        public string UserId { get; set; }
    }
    
    /// <summary>
    /// OAuth2 콜백 URL 파싱 결과
    /// </summary>
    [Serializable]
    public class OAuth2CallbackResult
    {
        /// <summary>
        /// 성공 여부
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// State 파라미터
        /// </summary>
        public string State { get; set; }
        
        /// <summary>
        /// 에러 메시지 (실패 시)
        /// </summary>
        public string Error { get; set; }
        
        /// <summary>
        /// 원본 URL
        /// </summary>
        public string OriginalUrl { get; set; }
        
        /// <summary>
        /// 파싱된 쿼리 파라미터
        /// </summary>
        public System.Collections.Generic.Dictionary<string, string> QueryParameters { get; set; }
        
        public OAuth2CallbackResult()
        {
            QueryParameters = new System.Collections.Generic.Dictionary<string, string>();
        }
        
        /// <summary>
        /// 성공 결과 생성
        /// </summary>
        public static OAuth2CallbackResult SuccessResult(string state, string originalUrl = null)
        {
            return new OAuth2CallbackResult
            {
                Success = true,
                State = state,
                OriginalUrl = originalUrl
            };
        }
        
        /// <summary>
        /// 실패 결과 생성
        /// </summary>
        public static OAuth2CallbackResult ErrorResult(string error, string originalUrl = null)
        {
            return new OAuth2CallbackResult
            {
                Success = false,
                Error = error,
                OriginalUrl = originalUrl
            };
        }
    }
    
    /// <summary>
    /// OAuth2 브라우저 열기 결과
    /// </summary>
    [Serializable]
    public class OAuth2BrowserResult
    {
        /// <summary>
        /// 브라우저 열기 성공 여부
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 열린 URL
        /// </summary>
        public string OpenedUrl { get; set; }
        
        /// <summary>
        /// 에러 메시지
        /// </summary>
        public string Error { get; set; }
        
        /// <summary>
        /// 플랫폼 정보
        /// </summary>
        public string Platform { get; set; }
        
        /// <summary>
        /// 브라우저 타입
        /// </summary>
        public string BrowserType { get; set; }
        
        public static OAuth2BrowserResult SuccessResult(string url, string platform, string browserType)
        {
            return new OAuth2BrowserResult
            {
                Success = true,
                OpenedUrl = url,
                Platform = platform,
                BrowserType = browserType
            };
        }
        
        public static OAuth2BrowserResult ErrorResult(string error, string platform)
        {
            return new OAuth2BrowserResult
            {
                Success = false,
                Error = error,
                Platform = platform
            };
        }
    }
}
