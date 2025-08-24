using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ProjectVG.Infrastructure.Auth.OAuth2.Models;

namespace ProjectVG.Infrastructure.Auth.OAuth2.Utils
{
    /// <summary>
    /// PKCE (Proof Key for Code Exchange) 생성 유틸리티
    /// 서버 권장사항에 따른 구현
    /// </summary>
    public static class PKCEGenerator
    {
        /// <summary>
        /// PKCE 파라미터 생성 (서버 권장사항 준수)
        /// </summary>
        /// <param name="codeVerifierLength">Code Verifier 길이 (43-128)</param>
        /// <param name="stateLength">State 길이 (16-64)</param>
        /// <returns>PKCE 파라미터</returns>
        public static async Task<PKCEParameters> GeneratePKCEAsync(int codeVerifierLength = 64, int stateLength = 16)
        {
            try
            {
                // 1. Code Verifier 생성 (43-128자 랜덤 문자열)
                var codeVerifier = GenerateRandomString(codeVerifierLength);
                
                // 2. Code Challenge 생성 (SHA256 해시)
                var codeChallenge = await GenerateCodeChallengeAsync(codeVerifier);
                
                // 3. State 생성
                var state = GenerateRandomString(stateLength);
                
                return new PKCEParameters(codeVerifier, codeChallenge, state);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"PKCE 생성 실패: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 랜덤 문자열 생성 (Base64Url 인코딩)
        /// </summary>
        /// <param name="length">생성할 길이</param>
        /// <returns>Base64Url 인코딩된 랜덤 문자열</returns>
        private static string GenerateRandomString(int length)
        {
            if (length < 1)
                throw new ArgumentException("길이는 1 이상이어야 합니다.", nameof(length));
            
            // 랜덤 바이트 생성
            var randomBytes = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }
            
            // Base64 인코딩 후 URL 안전하게 변환
            var base64String = Convert.ToBase64String(randomBytes);
            return base64String
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "")
                .Substring(0, Math.Min(length, base64String.Length));
        }
        
        /// <summary>
        /// Code Challenge 생성 (SHA256 해시)
        /// </summary>
        /// <param name="codeVerifier">Code Verifier</param>
        /// <returns>Base64Url 인코딩된 Code Challenge</returns>
        private static async Task<string> GenerateCodeChallengeAsync(string codeVerifier)
        {
            if (string.IsNullOrEmpty(codeVerifier))
                throw new ArgumentException("Code Verifier가 비어있습니다.", nameof(codeVerifier));
            
            try
            {
                // SHA256 해시 계산
                using (var sha256 = SHA256.Create())
                {
                    var codeVerifierBytes = Encoding.UTF8.GetBytes(codeVerifier);
                    var hashBytes = await Task.Run(() => sha256.ComputeHash(codeVerifierBytes));
                    
                    // Base64 인코딩 후 URL 안전하게 변환
                    var base64String = Convert.ToBase64String(hashBytes);
                    return base64String
                        .Replace("+", "-")
                        .Replace("/", "_")
                        .Replace("=", "");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Code Challenge 생성 실패: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// PKCE 파라미터 유효성 검사
        /// </summary>
        /// <param name="pkce">검사할 PKCE 파라미터</param>
        /// <returns>유효성 여부</returns>
        public static bool ValidatePKCE(PKCEParameters pkce)
        {
            if (pkce == null)
                return false;
            
            // 기본 유효성 검사
            if (!pkce.IsValid())
                return false;
            
            // 만료 검사
            if (pkce.IsExpired())
                return false;
            
            // Code Verifier 길이 검사 (43-128자)
            if (pkce.CodeVerifier.Length < 43 || pkce.CodeVerifier.Length > 128)
            {
                Debug.LogError($"[PKCEGenerator] Code Verifier 길이 검사 실패: {pkce.CodeVerifier.Length} (43-128 사이여야 함)");
                return false;
            }
            
            // Base64Url 형식 검사
            if (!IsBase64UrlSafe(pkce.CodeVerifier) || !IsBase64UrlSafe(pkce.CodeChallenge) || !IsBase64UrlSafe(pkce.State))
            {
                Debug.LogError("[PKCEGenerator] Base64Url 형식 검사 실패");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Base64Url 안전 문자열 검사
        /// </summary>
        /// <param name="input">검사할 문자열</param>
        /// <returns>Base64Url 안전 여부</returns>
        private static bool IsBase64UrlSafe(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
            
            // Base64Url 안전 문자만 포함하는지 검사
            foreach (char c in input)
            {
                if (!((c >= 'A' && c <= 'Z') || 
                      (c >= 'a' && c <= 'z') || 
                      (c >= '0' && c <= '9') || 
                      c == '-' || c == '_'))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// PKCE 파라미터 디버그 정보 출력
        /// </summary>
        /// <param name="pkce">PKCE 파라미터</param>
        /// <returns>디버그 정보 문자열</returns>
        public static string GetDebugInfo(PKCEParameters pkce)
        {
            if (pkce == null)
                return "PKCE 파라미터가 null입니다.";
            
            var info = $"PKCE Debug Info:\n";
            info += $"Code Verifier: {pkce.CodeVerifier}\n";
            info += $"Code Verifier Length: {pkce.CodeVerifier?.Length ?? 0}\n";
            info += $"Code Challenge: {pkce.CodeChallenge}\n";
            info += $"Code Challenge Length: {pkce.CodeChallenge?.Length ?? 0}\n";
            info += $"State: {pkce.State}\n";
            info += $"State Length: {pkce.State?.Length ?? 0}\n";
            info += $"Created At: {pkce.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
            info += $"Is Valid: {pkce.IsValid()}\n";
            info += $"Is Expired: {pkce.IsExpired()}\n";
            
            return info;
        }
    }
}
