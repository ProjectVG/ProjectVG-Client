using System;

namespace ProjectVG.Infrastructure.Network.DTOs.Character
{
    /// <summary>
    /// 캐릭터 수정 요청 DTO
    /// </summary>
    [Serializable]
    public class UpdateCharacterRequest
    {
        public string name;
        public string description;
        public string role;
        public bool isActive;
    }
} 