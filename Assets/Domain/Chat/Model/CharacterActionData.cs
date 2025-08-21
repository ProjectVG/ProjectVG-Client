#nullable enable
using UnityEngine;

namespace ProjectVG.Domain.Chat.Model
{
    public class CharacterActionData
    {
        /// <summary>
        /// 캐릭터가 수행할 행동
        /// </summary>
        public string Action { get; set; } = string.Empty;
        
        /// <summary>
        /// 행동 문자열로 초기화
        /// </summary>
        /// <param name="action">행동 문자열</param>
        public CharacterActionData(string? action = null)
        {
            Action = action ?? "talk";
        }
        
        /// <summary>
        /// 행동이 설정되어 있는지 확인
        /// </summary>
        /// <returns>행동이 설정되어 있으면 true</returns>
        public bool HasAction() => !string.IsNullOrEmpty(Action);
    }
}