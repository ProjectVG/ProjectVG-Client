using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 행동 → 모션/파라미터 트리거를 구현하는 컨트롤러의 스켈레톤.
	/// </summary>
	public class ActionController : MonoBehaviour, IActionController
	{
		public void Initialize()
		{
		}

		public void TriggerAction(string action, object args = null)
		{
		}
	}
}


