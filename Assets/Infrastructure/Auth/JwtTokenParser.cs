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
                payload = PadBase64String(payload);
                
                var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var jwtPayload = JsonConvert.DeserializeObject<JwtPayload>(payloadJson);
                
                return DateTimeOffset.FromUnixTimeSeconds(jwtPayload.exp).DateTime;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JwtTokenParser] JWT 파싱 실패: {ex.Message}");
                return DateTime.MinValue;
            }
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