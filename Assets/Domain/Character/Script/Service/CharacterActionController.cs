#nullable enable
using UnityEngine;
using ProjectVG.Domain.Chat.Model;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 액션 실행을 관리하는 서비스 구현체
	/// </summary>
	public class CharacterActionController : MonoBehaviour
	{
		private Animator? _animator;
		private bool _isPlaying = false;

		/// <summary>
		/// 서비스를 초기화한다.
		/// </summary>
		/// <param name="animator">캐릭터의 Animator</param>
		public void Initialize(Animator animator)
		{
			_animator = animator;
			_isPlaying = false;
		}

		/// <summary>
		/// 액션을 실행한다.
		/// </summary>
		/// <param name="actionData">액션 데이터</param>
		public void PlayAction(CharacterActionData actionData)
		{
			if (_animator == null)
			{
				Debug.LogWarning("[CharacterActionController] Animator가 초기화되지 않았습니다.");
				return;
			}

			if (!actionData.HasAction())
			{
				Debug.LogWarning("[CharacterActionController] 액션 데이터가 비어있습니다.");
				return;
			}

			try
			{
				_isPlaying = true;
				_animator.SetTrigger(actionData.Action);
				Debug.Log($"[CharacterActionController] 액션 재생: {actionData.Action}");
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[CharacterActionController] 액션 재생 실패: {ex.Message}");
				_isPlaying = false;
			}
		}

		/// <summary>
		/// 현재 재생 중인 액션을 중지한다.
		/// </summary>
		public void StopCurrentAction()
		{
			if (_animator == null) return;

			_animator.SetTrigger("Idle");
			_isPlaying = false;
			Debug.Log("[CharacterActionController] 액션 중지");
		}

		/// <summary>
		/// 액션이 재생 중인지 확인한다.
		/// </summary>
		/// <returns>액션 재생 중이면 true</returns>
		public bool IsPlaying()
		{
			return _isPlaying && _animator != null;
		}
	}
}
