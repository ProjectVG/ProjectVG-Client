using System;
using Newtonsoft.Json;

namespace ProjectVG.Infrastructure.Network.DTOs.Auth
{
    /// <summary>
    /// Guest 로그인 요청 DTO
    /// </summary>
    [Serializable]
    public class GuestLoginRequest
    {
        /// <summary>
        /// 게스트 ID (디바이스 고유 ID 기반)
        /// </summary>
        [JsonProperty("guestId")]
        public string GuestId { get; set; }

        public GuestLoginRequest()
        {
        }

        public GuestLoginRequest(string guestId)
        {
            GuestId = guestId ?? throw new ArgumentNullException(nameof(guestId));
        }

        /// <summary>
        /// 유효성 검사
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(GuestId);
        }

        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public string GetDebugInfo()
        {
            var maskedGuestId = string.IsNullOrEmpty(GuestId) ? "null" : 
                               GuestId.Length > 8 ? $"{GuestId.Substring(0, 4)}****{GuestId.Substring(GuestId.Length - 4)}" : "****";
            
            return $"GuestLoginRequest: GuestId={maskedGuestId}, Valid={IsValid()}";
        }
    }
}