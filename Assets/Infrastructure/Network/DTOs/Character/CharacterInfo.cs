using System;

namespace ProjectVG.Infrastructure.Network.DTOs.Character
{
    /// <summary>
    /// 캐릭터 정보 DTO
    /// </summary>
    [Serializable]
    public class CharacterInfo
    {
        public string id;
        public string name;
        public string description;
        public string role;
        public bool isActive;
    }
} 