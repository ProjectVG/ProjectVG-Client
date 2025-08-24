using System;

namespace ProjectVG.Infrastructure.Auth.OAuth2.Models
{
    /// <summary>
    /// PKCE (Proof Key for Code Exchange) 파라미터
    /// </summary>
    [Serializable]
    public class PKCEParameters
    {
        /// <summary>
        /// Code Verifier (43-128자 랜덤 문자열)
        /// </summary>
        public string CodeVerifier { get; set; }
        
        /// <summary>
        /// Code Challenge (SHA256 해시된 Code Verifier)
        /// </summary>
        public string CodeChallenge { get; set; }
        
        /// <summary>
        /// State 파라미터 (CSRF 방지)
        /// </summary>
        public string State { get; set; }
        
        /// <summary>
        /// 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        public PKCEParameters()
        {
            CreatedAt = DateTime.UtcNow;
        }
        
        public PKCEParameters(string codeVerifier, string codeChallenge, string state)
        {
            CodeVerifier = codeVerifier;
            CodeChallenge = codeChallenge;
            State = state;
            CreatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// PKCE 파라미터 유효성 검사
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(CodeVerifier) &&
                   !string.IsNullOrEmpty(CodeChallenge) &&
                   !string.IsNullOrEmpty(State) &&
                   CodeVerifier.Length >= 43 && CodeVerifier.Length <= 128;
        }
        
        /// <summary>
        /// 만료 시간 검사 (기본 10분)
        /// </summary>
        public bool IsExpired(TimeSpan? maxAge = null)
        {
            var age = DateTime.UtcNow - CreatedAt;
            var maxAgeValue = maxAge ?? TimeSpan.FromMinutes(10);
            return age > maxAgeValue;
        }
    }
}
