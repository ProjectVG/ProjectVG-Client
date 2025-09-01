#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectVG.Domain.Character.Service;

namespace ProjectVG.Domain.Chat.Model
{
    public class CharacterActionData
    {
        /// <summary>
        /// 주 액션 타입 (첫 번째 액션 또는 기본 액션)
        /// </summary>
        public CharacterActionType ActionType { get; set; } = CharacterActionType.Talk;
        
        /// <summary>
        /// 모든 액션들 (새 API에서 지원하는 복수 액션)
        /// </summary>
        public List<CharacterActionType> Actions { get; set; } = new List<CharacterActionType>();
        
        /// <summary>
        /// 감정 상태 (새 API에서 지원)
        /// </summary>
        public string? Emotion { get; set; }
        
        /// <summary>
        /// 액션 타입으로 초기화
        /// </summary>
        /// <param name="actionType">액션 타입</param>
        public CharacterActionData(CharacterActionType actionType = CharacterActionType.Talk)
        {
            ActionType = actionType;
            Actions = new List<CharacterActionType> { actionType };
        }
        
        /// <summary>
        /// 단일 행동 문자열로 초기화 (레거시 지원)
        /// </summary>
        /// <param name="action">행동 문자열</param>
        public CharacterActionData(string? action = null)
        {
            ActionType = ParseActionString(action);
            Actions = new List<CharacterActionType> { ActionType };
        }
        
        /// <summary>
        /// 복수 액션과 감정으로 초기화 (새 API)
        /// </summary>
        /// <param name="actions">액션 문자열 배열</param>
        /// <param name="emotion">감정 상태</param>
        public CharacterActionData(string[]? actions, string? emotion = null)
        {
            Emotion = emotion;
            Actions = ParseActionsArray(actions);
            ActionType = Actions.FirstOrDefault();
        }
        
        /// <summary>
        /// 행동이 설정되어 있는지 확인
        /// </summary>
        /// <returns>행동이 설정되어 있으면 true</returns>
        public bool HasAction() => Actions.Count > 0;
        
        /// <summary>
        /// 복수의 액션이 있는지 확인
        /// </summary>
        /// <returns>액션이 2개 이상이면 true</returns>
        public bool HasMultipleActions() => Actions.Count > 1;
        
        /// <summary>
        /// 감정이 설정되어 있는지 확인
        /// </summary>
        /// <returns>감정이 설정되어 있으면 true</returns>
        public bool HasEmotion() => !string.IsNullOrEmpty(Emotion);
        
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
                "clapping" => CharacterActionType.Talk, // 추가 액션들은 Talk으로 매핑 (필요시 enum 확장)
                "jumping" => CharacterActionType.Talk,
                "waving" => CharacterActionType.Talk,
                _ => CharacterActionType.Talk
            };
        }
        
        /// <summary>
        /// 액션 배열을 파싱하여 액션 타입 리스트로 변환
        /// </summary>
        /// <param name="actions">액션 문자열 배열</param>
        /// <returns>파싱된 액션 타입 리스트</returns>
        private List<CharacterActionType> ParseActionsArray(string[]? actions)
        {
            if (actions == null || actions.Length == 0)
            {
                return new List<CharacterActionType> { CharacterActionType.Talk };
            }
            
            return actions
                .Where(action => !string.IsNullOrEmpty(action))
                .Select(ParseActionString)
                .ToList();
        }
        
        /// <summary>
        /// 모든 액션을 순차적으로 실행하기 위한 열거자
        /// </summary>
        /// <returns>액션 시퀀스</returns>
        public IEnumerable<CharacterActionType> GetActionSequence()
        {
            return Actions;
        }
    }
}