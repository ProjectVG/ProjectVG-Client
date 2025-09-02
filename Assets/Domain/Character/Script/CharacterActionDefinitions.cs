#nullable enable
using System;

namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// 캐릭터 액션 타입과 관련된 상수들을 정의하는 Enum
    /// </summary>
    public enum CharacterAction
    {
        Idle,
        Talk,
        Listen
    }

    /// <summary>
    /// CharacterAction의 확장 메서드들
    /// </summary>
    public static class CharacterActionExtensions
    {
        /// <summary>
        /// 액션 타입을 Animator 상태 이름으로 변환 (소문자)
        /// </summary>
        public static string ToStateName(this CharacterAction action)
        {
            return action switch
            {
                CharacterAction.Idle => "idle",
                CharacterAction.Talk => "talk",
                CharacterAction.Listen => "listen",
                _ => "idle"
            };
        }

        /// <summary>
        /// 액션 타입을 Animator 트리거 이름으로 변환 (PlayXxx 형태)
        /// </summary>
        public static string ToTriggerName(this CharacterAction action)
        {
            return action switch
            {
                CharacterAction.Idle => "PlayIdle",
                CharacterAction.Talk => "PlayTalk", 
                CharacterAction.Listen => "PlayListen",
                _ => "PlayIdle"
            };
        }

        /// <summary>
        /// 기존 CharacterActionType을 새로운 CharacterAction으로 변환
        /// </summary>
        public static CharacterAction ToCharacterAction(this CharacterActionType actionType)
        {
            return actionType switch
            {
                CharacterActionType.Idle => CharacterAction.Idle,
                CharacterActionType.Talk => CharacterAction.Talk,
                CharacterActionType.Listen => CharacterAction.Listen,
                _ => CharacterAction.Idle
            };
        }

        /// <summary>
        /// CharacterAction을 기존 CharacterActionType으로 변환 (하위 호환성)
        /// </summary>
        public static CharacterActionType ToCharacterActionType(this CharacterAction action)
        {
            return action switch
            {
                CharacterAction.Idle => CharacterActionType.Idle,
                CharacterAction.Talk => CharacterActionType.Talk,
                CharacterAction.Listen => CharacterActionType.Listen,
                _ => CharacterActionType.Idle
            };
        }
    }
}