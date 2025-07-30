using System;
using UnityEngine;

namespace ProjectVG.Infrastructure.Network.DTOs.Character
{
    /// <summary>
    /// 캐릭터 정보 DTO
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        [SerializeField] public string id;
        [SerializeField] public string name;
        [SerializeField] public string description;
        [SerializeField] public string role;
        [SerializeField] public bool isActive;
    }
} 