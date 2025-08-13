namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 행동 → 모션/파라미터 트리거를 담당한다.
	/// </summary>
	public interface IActionController
	{
		void Initialize();
		void TriggerAction(string action, object args = null);
	}
}


