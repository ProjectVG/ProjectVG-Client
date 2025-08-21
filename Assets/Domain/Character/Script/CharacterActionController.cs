#nullable enable
using UnityEngine;
using ProjectVG.Domain.Chat.Model;

namespace ProjectVG.Domain.Character.Service
{

	public enum CharacterActionType
	{
		Idle,
		Listen,
		Talk
	}

	public class CharacterActionController : MonoBehaviour
	{
		private Animator? _animator;
		private bool _isPlaying = false;
		private CharacterActionType _currentAction = CharacterActionType.Idle;

		/// <summary>
		/// 서비스를 초기화한다.
		/// </summary>
		/// <param name="animator">캐릭터의 Animator</param>
		public void Initialize(Animator animator)
		{
			_animator = animator;
			_isPlaying = false;
			_currentAction = CharacterActionType.Idle;
		}

		/// <summary>
		/// 액션을 실행한다.
		/// </summary>
		/// <param name="actionType">액션 타입</param>
		public void PlayAction(CharacterActionType actionType)
		{
			if (_animator == null)
			{
				Debug.LogWarning("[CharacterActionController] Animator가 초기화되지 않았습니다.");
				return;
			}

			try
			{
				_currentAction = actionType;

				Debug.Log(actionType);
				
				switch (actionType)
				{
					case CharacterActionType.Idle:
						_animator.SetTrigger("Idle");
						_animator.SetBool("Talk", false);
						_isPlaying = false;
						break;
						
					case CharacterActionType.Listen:
						_animator.SetTrigger("Listen");
						_animator.SetBool("Talk", false);
						_isPlaying = true;
						break;
						
					case CharacterActionType.Talk:
						_animator.SetBool("Talk", true);
						_isPlaying = true;
						break;
				}
				
				Debug.Log($"[CharacterActionController] 액션 재생: {actionType}");
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
			_animator.SetBool("Talk", false);
			_isPlaying = false;
			_currentAction = CharacterActionType.Idle;
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
		
		/// <summary>
		/// 현재 액션 타입을 반환한다.
		/// </summary>
		/// <returns>현재 액션 타입</returns>
		public CharacterActionType GetCurrentAction()
		{
			return _currentAction;
		}
	}
}
