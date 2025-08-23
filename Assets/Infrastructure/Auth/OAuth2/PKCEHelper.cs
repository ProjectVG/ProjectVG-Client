using System;
using System.Security.Cryptography;
using System.Text;

namespace ProjectVG.Infrastructure.Auth.OAuth2
{
    public class PKCEHelper
    {
        private string _codeVerifier;
        
        public string GenerateCodeChallenge()
        {
            _codeVerifier = GenerateCodeVerifier();
            return GenerateCodeChallengeFromVerifier(_codeVerifier);
        }
        
        public string GetCodeVerifier()
        {
            return _codeVerifier;
        }
        
        private string GenerateCodeVerifier()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._~";
            var random = new System.Random();
            var result = new StringBuilder(128);
            
            for (int i = 0; i < 128; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            
            return result.ToString();
        }
        
        private string GenerateCodeChallengeFromVerifier(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Convert.ToBase64String(challengeBytes)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");
            }
        }
        
        public bool ValidateCodeChallenge(string codeVerifier, string codeChallenge)
        {
            var expectedChallenge = GenerateCodeChallengeFromVerifier(codeVerifier);
            return expectedChallenge == codeChallenge;
        }
    }
}
