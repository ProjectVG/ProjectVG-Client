using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// Live2D 모델 전반의 상태를 단일 진입점에서 관리하는 인터페이스
    /// </summary>
    public interface ILive2DModelManagerFacade
    {
        void Initialize();
        void ApplyReaction(EmotionData emotionData, ActionData actionData);
        void OnVoiceStarted();
        void OnVoiceFinished();
    }
}
