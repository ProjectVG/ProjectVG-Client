using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 모델 전반의 상태를 단일 진입점에서 관리한다.
	/// </summary>
	public interface ILive2DModelManagerFacade
	{
		void Initialize();
		void ApplyReaction(EmotionData emotionData, ActionData actionData);
		void OnVoiceStarted();
		void OnVoiceFinished();
	}
}


