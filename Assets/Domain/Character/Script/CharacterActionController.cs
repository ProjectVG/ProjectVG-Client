#nullable enable
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.MotionFade;
using ProjectVG.Domain.Character.Live2D.Model;
using ProjectVG.Domain.Chat.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

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
		
		
		// Motion Control System
		private Coroutine? _currentMotionCoroutine;
		private System.Action? _currentMotionEndCallback;
		
		// Motion End Behavior Control
		public System.Action? OnMotionStop;
		public System.Action? OnMotionLoop;
		public System.Action? OnMotionReturnToIdle;

        #region Unity Lifecycle

        /// <summary>
        /// 서비스를 초기화한다.
        /// </summary>
        /// <param name="motionController">Live2D Motion Controller</param>
        /// <param name="motionClips">Motion Clip Mappings</param>
        public void Initialize(CubismMotionController motionController, List<Live2DModelConfig.MotionClipMapping> motionClips)
		{
			_motionController = motionController;
			_fadeController = GetComponent<CubismFadeController>();
			_motionClips = motionClips;
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
			Debug.Log($"===============[CharacterActionController] 초기화 완료 - Motion Clips: {_motionClips?.Count ?? 0}개");
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
		/// GameObject가 활성화될 때 Idle 모션을 시작한다.
		/// </summary>
		private void OnEnable()
		{
			if (_currentAction == CharacterActionType.Idle && _motionController != null)
			{
				// CubismMotionController가 완전히 초기화될 때까지 잠시 대기
				StartCoroutine(DelayedIdleStart());
			}
		}

        /// <summary>
        /// CubismMotionController 초기화 대기 후 Idle 모션 시작
        /// </summary>
        private IEnumerator DelayedIdleStart()
		{
			// 1프레임 대기 (CubismMotionController OnEnable 완료 대기)
			yield return null;
			
			// 추가로 0.1초 대기 (초기화 완료 확인)
			yield return new WaitForSeconds(0.1f);
			
			if (_currentAction == CharacterActionType.Idle && _motionController != null)
			{
				PlayRandomIdleMotion();
			}
		}

		/// <summary>
		/// GameObject가 비활성화될 때 모든 코루틴을 중지한다.
		/// </summary>
		private void OnDisable()
		{
			StopAllMotionCoroutines();
		}

        #endregion


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
				// 새 액션 시작 전에 항상 이전 모션 관련 코루틴 정리
				StopAllMotionCoroutines();

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
			
			PlayRandomIdleMotion();

			Debug.Log("[CharacterActionController] 강제 중지 후 Idle로 복귀");
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
			
			// Priority 충돌 방지를 위해 이전 모션을 강제 중지
			_motionController.StopAllAnimation();
			
			// 짧은 대기 후 모션 재생 (Priority 충돌 방지)
			AnimationClip? clip = selectedMotion.animationClip;
			if (clip != null)
			{
                StartCoroutine(DelayedMotionPlay(clip, CubismMotionPriority.PriorityNormal, endCallback));
			}
		}
		
		/// <summary>
		/// 짧은 딜레이 후 모션을 재생한다. (Priority 충돌 방지)
		/// </summary>
		private IEnumerator DelayedMotionPlay(AnimationClip clip, int priority, System.Action? endCallback)
		{
			yield return new WaitForSeconds(0.1f); // Priority 충돌 방지용 짧은 대기
			
			if (_motionController != null)
			{
				// 모션 재생 (CubismFadeController가 자동 페이드 처리)
				_motionController.PlayAnimation(clip, priority: 2, isLoop: false);
				_isPlayingAction = true; 
				
				// 모션 종료 처리 설정
				_currentMotionEndCallback = endCallback;
				StopAllMotionCoroutines();
				_currentMotionCoroutine = StartCoroutine(WaitForMotionEnd(clip.length, endCallback));
				
				Debug.Log($"[CharacterActionController] 모션 재생: {_currentMotionClip?.Id} (그룹: {_currentMotionClip?.MotionGroup}), 길이: {clip.length}s");
			}
		}
		
		/// <summary>
		/// Idle 모션을 다양하게 순차 재생한다.
		/// </summary>
		private void PlayRandomIdleMotion()
		{
			// Idle 모션은 항상 재귀적으로 다음 Idle 모션 재생 (0.3초 딜레이 후)
			System.Action recursiveIdleCallback = () => {
				if (_currentAction == CharacterActionType.Idle)
				{
					StartCoroutine(DelayedIdleMotionStart()); // 딜레이 후 다음 Idle 모션 재생
				}
			};
			
			// Idle 모션 전용 재생 (PlayRandomMotionFromGroup 대신 직접 처리)
			PlayIdleMotionDirect(recursiveIdleCallback);
		}
		
		/// <summary>
		/// Idle 모션을 직접 재생한다. (Priority 충돌 없이)
		/// </summary>
		private void PlayIdleMotionDirect(System.Action? endCallback)
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
			
			// Priority 충돌 방지를 위해 이전 모션을 강제 중지
			_motionController.StopAllAnimation();
			
			// 짧은 대기 후 Idle 모션 재생 (PriorityIdle 사용)
			AnimationClip? idleClip = selectedIdle.animationClip;
			if (idleClip != null)
			{
				StartCoroutine(DelayedMotionPlay(idleClip, CubismMotionPriority.PriorityIdle, endCallback));
			}
			
			// Idle 모션 전용 설정
			_isPlayingAction = false; // Idle은 Action이 아님
		}
		
		/// <summary>
		/// 0.3초 딜레이 후 Idle 모션을 재생한다.
		/// </summary>
		private IEnumerator DelayedIdleMotionStart()
		{
			yield return new WaitForSeconds(0.0f);
			
			if (_currentAction == CharacterActionType.Idle)
			{
				PlayRandomIdleMotion();
			}
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

		#region Motion Control Helpers

		/// <summary>
		/// 모든 모션 관련 코루틴을 중지한다.
		/// </summary>
		private void StopAllMotionCoroutines()
		{
			// 이 컴포넌트가 시작한 모든 코루틴 중지
			StopAllCoroutines();
			_currentMotionCoroutine = null;
			_currentMotionEndCallback = null;
		}

		#endregion

		/// <summary>
		/// Live2D 모션이 종료될 때 호출되는 이벤트 핸들러
		/// </summary>
		private void OnLive2DMotionEnd(int instanceId)
		{
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
