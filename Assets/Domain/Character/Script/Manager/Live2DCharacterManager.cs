using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 상위 조정자. 모델 관리자/파라미터 컨트롤러를 묶어 감정/행동 반응을 일관되게 적용한다.
	/// </summary>
	public class Live2DModelManagerFacade : MonoBehaviour, ILive2DModelManagerFacade
	{
		public void Initialize()
		{
		}

		public void ApplyReaction(EmotionData emotionData, ActionData actionData)
		{
		}

		public void OnVoiceStarted()
		{
		}

		public void OnVoiceFinished()
		{
		}
	}
}


