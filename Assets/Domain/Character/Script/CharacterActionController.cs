#nullable enable
using UnityEngine;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.MotionFade;
using Live2D.Cubism.Core;
using ProjectVG.Domain.Character.Live2D.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		private CubismMotionController? _motionController;
		private CubismFadeController? _fadeController;
		private List<Live2DModelConfig.MotionClipMapping>? _motionClips;
		
		// 현재 상태
		private CharacterActionType _currentAction = CharacterActionType.Idle;
		private Live2DModelConfig.MotionClipMapping? _currentMotionClip;
		private bool _isPlayingAction = false;
		
		// Auto Idle System
		private bool _enableAutoIdle = true;
		private float _autoIdleInterval = 5f;
		private Coroutine? _autoIdleCoroutine;
		
		// Motion Control System
		private Coroutine? _currentMotionCoroutine;
		private System.Action? _currentMotionEndCallback;
		
		// Motion End Behavior Control
		public System.Action? OnMotionStop;
		public System.Action? OnMotionLoop;
		public System.Action? OnMotionReturnToIdle;

		/// <summary>
		/// 서비스를 초기화한다.
		/// </summary>
		/// <param name="motionController">Live2D Motion Controller</param>
		/// <param name="motionClips">Motion Clip Mappings</param>
		/// <param name="enableAutoIdle">Auto Idle 활성화 여부</param>
		/// <param name="autoIdleInterval">Auto Idle 간격</param>
		public void Initialize(CubismMotionController motionController, List<Live2DModelConfig.MotionClipMapping> motionClips, bool enableAutoIdle = true, float autoIdleInterval = 5f)
		{
			_motionController = motionController;
			_fadeController = GetComponent<CubismFadeController>();
			_motionClips = motionClips;
			_enableAutoIdle = enableAutoIdle;
			_autoIdleInterval = autoIdleInterval;
			_currentAction = CharacterActionType.Idle;
			_isPlayingAction = false;
			
			// 모션 종료 이벤트 핸들러 등록
			if (_motionController != null)
			{
				_motionController.AnimationEndHandler += OnLive2DMotionEnd;
			}
			
			if (_fadeController == null)
			{
				Debug.LogWarning("[CharacterActionController] CubismFadeController를 찾을 수 없습니다. 모션 전환이 부자연스러울 수 있습니다.");
			}
			
			// 기본 Action 콜백 설정
			SetupDefaultCallbacks();
			
			// 모션 클립 구성 디버깅
			Debug.Log($"===============[CharacterActionController] 초기화 완료 - Motion Clips: {_motionClips?.Count ?? 0}개, Auto Idle: {_enableAutoIdle}");
			if (_motionClips != null)
			{
				foreach (var clip in _motionClips)
				{
					Debug.Log($"[CharacterActionController] Motion Clip: {clip.Id} | Group: '{clip.MotionGroup}' | AnimationClip: {(clip.animationClip != null ? "O" : "X")}");
				}
				
				// idle 그룹 확인
				var idleClips = _motionClips.Where(c => c.MotionGroup.Equals("idle", System.StringComparison.OrdinalIgnoreCase)).ToList();
				Debug.Log($"[CharacterActionController] idle 그룹 모션 클립 수: {idleClips.Count}개");
			}
		}

		/// <summary>
		/// GameObject가 활성화될 때 Auto Idle을 시작한다.
		/// </summary>
		private void OnEnable()
		{
			if (_enableAutoIdle && _currentAction == CharacterActionType.Idle && _motionController != null)
			{
				// CubismMotionController가 완전히 초기화될 때까지 잠시 대기
				StartCoroutine(DelayedAutoIdleStart());
			}
		}

		/// <summary>
		/// CubismMotionController 초기화 대기 후 Auto Idle 시작
		/// </summary>
		private IEnumerator DelayedAutoIdleStart()
		{
			// 1프레임 대기 (CubismMotionController OnEnable 완료 대기)
			yield return null;
			
			// 추가로 0.1초 대기 (초기화 완료 확인)
			yield return new WaitForSeconds(0.1f);
			
			if (_enableAutoIdle && _currentAction == CharacterActionType.Idle && _motionController != null)
			{
				StartAutoIdle();
			}
		}

		/// <summary>
		/// GameObject가 비활성화될 때 모든 코루틴을 중지한다.
		/// </summary>
		private void OnDisable()
		{
			StopAllMotionCoroutines();
		}

		/// <summary>
		/// 기본 모션 종료 콜백을 설정한다.
		/// </summary>
		private void SetupDefaultCallbacks()
		{
			OnMotionStop = () => {
				Debug.Log("[CharacterActionController] 모션 정지");
				_isPlayingAction = false;
			};
			
			OnMotionLoop = () => {
				Debug.Log("[CharacterActionController] 모션 루프 - 같은 그룹의 다른 모션 재생");
				if (_currentMotionClip != null)
				{
					PlayRandomMotionFromGroup(_currentMotionClip.MotionGroup, endCallback: OnMotionLoop);
				}
			};
			
			OnMotionReturnToIdle = () => {
				Debug.Log("[CharacterActionController] 모션 종료 - Idle로 복귀");
				PlayAction(CharacterActionType.Idle);
			};
		}

		/// <summary>
		/// 액션을 실행한다.
		/// </summary>
		/// <param name="actionType">액션 타입</param>
		public void PlayAction(CharacterActionType actionType)
		{
			if (_motionController == null)
			{
				Debug.LogWarning("[CharacterActionController] MotionController가 초기화되지 않았습니다.");
				return;
			}

			if (_motionClips == null || _motionClips.Count == 0) {
				Debug.LogWarning("[CharacterActionController] MotionClips이 설정되지 않았습니다.");
				return;
			}

            try
			{
				// 기존 Auto Idle 중지 (새 액션이 트리거될 때)
				if (_currentAction == CharacterActionType.Idle && actionType != CharacterActionType.Idle)
				{
					StopAutoIdle();
				}

				_currentAction = actionType;
				string motionGroup = GetMotionGroupName(actionType);
				
				if (actionType == CharacterActionType.Idle)
				{
					// Idle 액션인 경우 다양한 Idle 모션을 순차 재생
					PlayRandomIdleMotion();
				}
				else
				{
					// 일반 액션인 경우 ReturnToIdle로 종료
					PlayRandomMotionFromGroup(motionGroup, endCallback: OnMotionReturnToIdle);
				}
				
				Debug.Log($"[CharacterActionController] 액션 재생: {actionType}, 그룹: {motionGroup}");
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[CharacterActionController] 액션 재생 실패: {ex.Message}");
				_isPlayingAction = false;
			}
		}

		/// <summary>
		/// 현재 재생 중인 액션을 중지한다.
		/// </summary>
		public void StopCurrentAction()
		{
			if (_motionController == null) return;

			// 모든 모션 관련 코루틴 중지
			StopAllMotionCoroutines();

			// Idle 모션 재생
			PlayAction(CharacterActionType.Idle);
			_isPlayingAction = false;
			_currentAction = CharacterActionType.Idle;
			Debug.Log("[CharacterActionController] 액션 중지");
		}

		/// <summary>
		/// 현재 모션을 즉시 중지하고 Idle로 돌아간다.
		/// </summary>
		public void ForceStopAndReturnToIdle()
		{
			if (_motionController == null) return;

			// 모든 모션과 코루틴 중지
			ForceStopMotion();
			StopAllMotionCoroutines();

			// 즉시 Idle로 전환
			_currentAction = CharacterActionType.Idle;
			_isPlayingAction = false;
			_currentMotionClip = null;
			
			if (_enableAutoIdle)
			{
				StartAutoIdle();
			}
			else
			{
				PlayRandomIdleMotion();
			}

			Debug.Log("[CharacterActionController] 강제 중지 후 Idle로 복귀");
		}

		/// <summary>
		/// Auto Idle 기능을 켜거나 끈다.
		/// </summary>
		public void SetAutoIdle(bool enabled)
		{
			_enableAutoIdle = enabled;
			
			if (_enableAutoIdle && _currentAction == CharacterActionType.Idle)
			{
				StartAutoIdle();
			}
			else if (!_enableAutoIdle)
			{
				StopAutoIdle();
			}
		}

		/// <summary>
		/// 액션이 재생 중인지 확인한다.
		/// </summary>
		/// <returns>액션 재생 중이면 true</returns>
		public bool IsPlaying()
		{
			return _isPlayingAction && _motionController != null;
		}
		
		/// <summary>
		/// 현재 액션 타입을 반환한다.
		/// </summary>
		/// <returns>현재 액션 타입</returns>
		public CharacterActionType GetCurrentAction()
		{
			return _currentAction;
		}
		
		/// <summary>
		/// 액션 타입을 모션 그룹 이름으로 변환한다.
		/// </summary>
		private string GetMotionGroupName(CharacterActionType actionType)
		{
			return actionType switch
			{
				CharacterActionType.Idle => "idle",
				CharacterActionType.Listen => "reaction",
				CharacterActionType.Talk => "talk",
				_ => "idle"
			};
		}
		
		#region Motion Control System
		
		/// <summary>
		/// 지정된 그룹에서 랜덤한 모션을 재생한다.
		/// </summary>
		/// <param name="motionGroup">모션 그룹</param>
		/// <param name="endCallback">모션 종료 시 호출될 콜백</param>
		private void PlayRandomMotionFromGroup(string motionGroup, System.Action? endCallback = null)
		{
			if (_motionClips == null || _motionController == null) return;
			
			// 해당 그룹에서 모션 선택
			var groupMotions = _motionClips
				.Where(clip => clip.MotionGroup.Equals(motionGroup, System.StringComparison.OrdinalIgnoreCase))
				.Where(clip => clip.animationClip != null)
				.ToList();
				
			if (groupMotions.Count == 0)
			{
				Debug.LogWarning($"[CharacterActionController] '{motionGroup}' 그룹의 모션을 찾을 수 없습니다.");
				return;
			}
			
			// 랜덤 선택
			var selectedMotion = groupMotions[Random.Range(0, groupMotions.Count)];
			_currentMotionClip = selectedMotion;
			
			// 모션 재생 (CubismFadeController가 자동 페이드 처리)
			_motionController.PlayAnimation(selectedMotion.animationClip, isLoop: false, priority: CubismMotionPriority.PriorityNormal);
			_isPlayingAction = true;
			
			// 모션 종료 처리 설정
			_currentMotionEndCallback = endCallback;
			StopAllMotionCoroutines();
			_currentMotionCoroutine = StartCoroutine(WaitForMotionEnd(selectedMotion.animationClip.length, endCallback));
			
			Debug.Log($"[CharacterActionController] 모션 재생: {selectedMotion.Id} (그룹: {motionGroup}), 길이: {selectedMotion.animationClip.length}s");
		}
		
		/// <summary>
		/// Idle 모션을 다양하게 순차 재생한다.
		/// </summary>
		private void PlayRandomIdleMotion()
		{
			if (_motionClips == null || _motionController == null) return;
			
			// idle 그룹 모션 찾기
			var idleMotions = _motionClips
				.Where(clip => clip.MotionGroup.Equals("idle", System.StringComparison.OrdinalIgnoreCase))
				.Where(clip => clip.animationClip != null)
				.ToList();
				
			if (idleMotions.Count == 0)
			{
				Debug.LogWarning("[CharacterActionController] idle 모션을 찾을 수 없습니다.");
				return;
			}
			
			// 랜덤 선택
			var selectedIdle = idleMotions[Random.Range(0, idleMotions.Count)];
			_currentMotionClip = selectedIdle;
			
			// Idle 모션을 루프 없이 재생 (끝나면 다음 Idle 모션 재생)
			_motionController.PlayAnimation(selectedIdle.animationClip, isLoop: false, priority: CubismMotionPriority.PriorityIdle);
			_isPlayingAction = false; // Idle은 Action이 아니므로 false
			
			// 모션 종료 후 다음 Idle 모션 재생
			_currentMotionEndCallback = () => {
				if (_currentAction == CharacterActionType.Idle)
				{
					PlayRandomIdleMotion(); // 다음 Idle 모션 재생
				}
			};
			
			StopAllMotionCoroutines();
			_currentMotionCoroutine = StartCoroutine(WaitForMotionEnd(selectedIdle.animationClip.length, _currentMotionEndCallback));
			
			Debug.Log($"[CharacterActionController] Idle 모션 시작: {selectedIdle.Id}, 길이: {selectedIdle.animationClip.length}s");
		}
		
		/// <summary>
		/// 모션 종료를 기다리는 코루틴
		/// </summary>
		private IEnumerator WaitForMotionEnd(float duration, System.Action? endCallback)
		{
			yield return new WaitForSeconds(duration);
			
			// 모션이 완료되면 콜백 호출
			endCallback?.Invoke();
			_currentMotionCoroutine = null;
		}
		
		/// <summary>
		/// 모션 종료 동작을 변경한다.
		/// </summary>
		public void SetMotionEndBehavior(System.Action? onStop = null, System.Action? onLoop = null, System.Action? onReturnToIdle = null)
		{
			if (onStop != null) OnMotionStop = onStop;
			if (onLoop != null) OnMotionLoop = onLoop;
			if (onReturnToIdle != null) OnMotionReturnToIdle = onReturnToIdle;
		}
		
		/// <summary>
		/// 현재 모션을 중지한다.
		/// </summary>
		public void StopCurrentMotion()
		{
			OnMotionStop?.Invoke();
		}
		
		/// <summary>
		/// 현재 모션을 루프한다.
		/// </summary>
		public void LoopCurrentMotion()
		{
			OnMotionLoop?.Invoke();
		}
		
		/// <summary>
		/// 현재 모션을 종료하고 Idle로 돌아간다.
		/// </summary>
		public void ReturnToIdle()
		{
			OnMotionReturnToIdle?.Invoke();
		}
		
		#endregion

		/// <summary>
		/// Live2D 모션 상태를 체크한다.
		/// </summary>
		private bool IsMotionPlaying()
		{
			if (_motionController == null) return false;
			
			try
			{
				// Layer 0에서 모션이 재생 중인지 확인
				return _motionController.IsPlayingAnimation(0);
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning($"[CharacterActionController] 모션 상태 체크 실패: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// 현재 재생 중인 모션을 강제로 중지한다.
		/// </summary>
		private void ForceStopMotion()
		{
			if (_motionController != null)
			{
				_motionController.StopAllAnimation();
				Debug.Log("[CharacterActionController] 모든 모션 강제 중지");
			}
		}

		#region Auto Idle & Motion End Handling

		/// <summary>
		/// Auto Idle을 시작한다.
		/// </summary>
		private void StartAutoIdle()
		{
			Debug.Log("[CharacterActionController] Auto Idle 시작 요청");
            if (!_enableAutoIdle || _currentAction != CharacterActionType.Idle)
			{
				Debug.Log($"[CharacterActionController] Auto Idle 시작 조건 불충족 - EnableAutoIdle: {_enableAutoIdle}, CurrentAction: {_currentAction}");
				return;
			}

			// idle 그룹 모션이 있는지 확인
			if (_motionClips != null)
			{
				var idleClips = _motionClips.Where(c => c.MotionGroup.Equals("idle", System.StringComparison.OrdinalIgnoreCase) && c.animationClip != null).ToList();
				if (idleClips.Count == 0)
				{
					Debug.LogWarning("[CharacterActionController] Auto Idle을 시작할 수 없습니다. 'idle' 그룹의 모션 클립이 없습니다. Live2DModelConfig에서 motionGroup을 'idle'로 설정한 AnimationClip을 추가하세요.");
					return;
				}
			}

			StopAutoIdle(); // 기존 Auto Idle 중지
			_autoIdleCoroutine = StartCoroutine(AutoIdleCoroutine());
			Debug.Log("[CharacterActionController] Auto Idle 코루틴 시작됨");
		}

		/// <summary>
		/// Auto Idle을 중지한다.
		/// </summary>
		private void StopAutoIdle()
		{
			if (_autoIdleCoroutine != null)
			{
				StopCoroutine(_autoIdleCoroutine);
				_autoIdleCoroutine = null;
			}
		}

		/// <summary>
		/// Auto Idle 코루틴
		/// </summary>
		private IEnumerator AutoIdleCoroutine()
		{
			while (_enableAutoIdle && _currentAction == CharacterActionType.Idle)
			{
				// 현재 모션이 끝났는지 확인
				if (!IsMotionPlaying())
				{
					PlayRandomIdleMotion();
				}
				
				yield return new WaitForSeconds(_autoIdleInterval);
			}
		}


		/// <summary>
		/// 모든 모션 관련 코루틴을 중지한다.
		/// </summary>
		private void StopAllMotionCoroutines()
		{
			StopAutoIdle();
			
			if (_currentMotionCoroutine != null)
			{
				StopCoroutine(_currentMotionCoroutine);
				_currentMotionCoroutine = null;
			}
			
			_currentMotionEndCallback = null;
		}

		#endregion

		/// <summary>
		/// Live2D 모션이 종료될 때 호출되는 이벤트 핸들러
		/// </summary>
		private void OnLive2DMotionEnd(int instanceId)
		{
			Debug.Log($"[CharacterActionController] Live2D 모션 종료 이벤트 (InstanceId: {instanceId})");
			
			// 코루틴에서 모션 종료를 처리하므로 여기서는 로깅만 수행
			// 만약 코루틴이 없을 경우에 대비하여 콜백 호출
			if (_currentMotionCoroutine == null && _currentMotionEndCallback != null)
			{
				var callback = _currentMotionEndCallback;
				_currentMotionEndCallback = null;
				callback.Invoke();
			}
		}

		private void OnDestroy()
		{
			// 이벤트 핸들러 해제
			if (_motionController != null)
			{
				_motionController.AnimationEndHandler -= OnLive2DMotionEnd;
			}
			
			StopAllMotionCoroutines();
		}
	}
}
