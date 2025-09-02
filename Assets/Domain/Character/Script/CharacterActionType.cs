#nullable enable

namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// 캐릭터 액션 타입
    /// </summary>
    public enum CharacterActionType
    {
        /// <summary>
        /// 대기 상태
        /// </summary>
        Idle,
        
        /// <summary>
        /// 듣기 상태
        /// </summary>
        Listen,
        
        /// <summary>
        /// 말하기 상태
        /// </summary>
        Talk
    }
}