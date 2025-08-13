using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 전반의 상태를 단일 진입점에서 관리한다.
	/// </summary>
	public interface ILive2DCharacterManager
	{
		void Initialize();
		void ApplyReaction(EmotionData emotionData, ActionData actionData);
		void OnVoiceStarted();
		void OnVoiceFinished();
	}
}


