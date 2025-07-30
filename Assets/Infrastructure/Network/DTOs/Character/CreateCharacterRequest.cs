using System;

namespace ProjectVG.Infrastructure.Network.DTOs.Character
{
    /// <summary>
    /// 캐릭터 생성 요청 DTO
    /// </summary>
    [Serializable]
    public class CreateCharacterRequest
    {
        public string name;
        public string description;
        public string role;
        public bool isActive = true;
    }
} 