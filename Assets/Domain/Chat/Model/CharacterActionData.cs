#nullable enable
using UnityEngine;
using ProjectVG.Domain.Character.Service;

namespace ProjectVG.Domain.Chat.Model
{
    public class CharacterActionData
    {
        /// <summary>
        /// 캐릭터가 수행할 행동 타입
        /// </summary>
        public CharacterActionType ActionType { get; set; } = CharacterActionType.Talk;
        
        /// <summary>
        /// 액션 타입으로 초기화
        /// </summary>
        /// <param name="actionType">액션 타입</param>
        public CharacterActionData(CharacterActionType actionType = CharacterActionType.Talk)
        {
            ActionType = actionType;
        }
        
        /// <summary>
        /// 행동 문자열로 초기화
        /// </summary>
        /// <param name="action">행동 문자열</param>
        public CharacterActionData(string? action = null)
        {
            ActionType = ParseActionString(action);
        }
        
        /// <summary>
        /// 행동이 설정되어 있는지 확인
        /// </summary>
        /// <returns>행동이 설정되어 있으면 true</returns>
        public bool HasAction() => true;
        
        /// <summary>
        /// 문자열을 액션 타입으로 파싱
        /// </summary>
        /// <param name="actionString">액션 문자열</param>
        /// <returns>파싱된 액션 타입</returns>
        private CharacterActionType ParseActionString(string? actionString)
        {
            if (string.IsNullOrEmpty(actionString))
                return CharacterActionType.Talk;
                
            return actionString.ToLower() switch
            {
                "idle" => CharacterActionType.Idle,
                "listen" => CharacterActionType.Listen,
                "talk" => CharacterActionType.Talk,
                "nodding" => CharacterActionType.Nodding,
                "shaking_head" => CharacterActionType.ShakingHead,
                "looking_away" => CharacterActionType.LookingAway,
                "tilting_head" => CharacterActionType.TiltingHead,
                "sighing" => CharacterActionType.Sighing,
                "pouting" => CharacterActionType.Pouting,
                _ => CharacterActionType.Idle
            };
        }
    }
}