using System;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Auth
{
    public static class JwtTokenParser
    {
        public static DateTime GetExpirationTime(string token)
        {
            if (string.IsNullOrEmpty(token))
                return DateTime.MinValue;

            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return DateTime.MinValue;

                var payload = parts[1];
                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));
                var jwtPayload = JsonConvert.DeserializeObject<JwtPayload>(payloadJson);
                
                if (jwtPayload == null || jwtPayload.exp <= 0)
                    return DateTime.MinValue;

                return DateTimeOffset.FromUnixTimeSeconds(jwtPayload.exp).UtcDateTime;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JwtTokenParser] JWT 파싱 실패: {ex.Message}");
                return DateTime.MinValue;
            }
        }
        
        private static byte[] Base64UrlDecode(string input)
        {
            input = input.Replace('-', '+').Replace('_', '/');
            input = PadBase64String(input);
            return Convert.FromBase64String(input);
        }
        
        private static string PadBase64String(string base64)
        {
            var padding = base64.Length % 4;
            return padding > 0 ? base64.PadRight(base64.Length + (4 - padding), '=') : base64;
        }
        
        private class JwtPayload
        {
            public long exp { get; set; }
        }
    }
}