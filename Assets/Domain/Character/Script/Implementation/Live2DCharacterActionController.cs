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
	/// <summary>
	/// 모션 전환 정보를 담는 클래스
	/// </summary>
	public class MotionTransition
	{
		public Live2DModelConfig.MotionClipMapping? previousMotion;
		public Live2DModelConfig.MotionClipMapping newMotion;
		public float startTime;
		public float duration;
		public System.Action? endCallback;
		public bool isCompleted;

		public MotionTransition(Live2DModelConfig.MotionClipMapping? prev, Live2DModelConfig.MotionClipMapping next, float dur, System.Action? callback = null)
		{
			previousMotion = prev;
			newMotion = next;
			startTime = Time.time;
			duration = dur;
			endCallback = callback;
			isCompleted = false;
		}

		/// <summary>
		/// 전환 진행률을 반환한다. (0.0 ~ 1.0)
		/// </summary>
		public float GetProgress()
		{
			if (isCompleted) return 1.0f;
			
			float elapsed = Time.time - startTime;
			return Mathf.Clamp01(elapsed / duration);
		}

		/// <summary>
		/// 부드러운 전환 곡선을 적용한 가중치를 반환한다.
		/// </summary>
		public float GetSmoothWeight()
		{
			float progress = GetProgress();
			return Mathf.SmoothStep(0.0f, 1.0f, progress);
		}

		/// <summary>
		/// 전환 곡선 타입에 따른 가중치를 반환한다.
		/// </summary>
		/// <param name="curveType">전환 곡선 타입</param>
		public float GetWeightByCurveType(TransitionCurveType curveType)
		{
			float progress = GetProgress();
			
			return curveType switch
			{
				TransitionCurveType.Linear => progress,
				TransitionCurveType.EaseIn => progress * progress,
				TransitionCurveType.EaseOut => 1.0f - (1.0f - progress) * (1.0f - progress),
				TransitionCurveType.EaseInOut => Mathf.SmoothStep(0.0f, 1.0f, progress),
				_ => progress
			};
		}

		/// <summary>
		/// 새 모션의 가중치를 반환한다. (0.0 -> 1.0)
		/// </summary>
		public float GetNewMotionWeight(TransitionCurveType curveType)
		{
			return GetWeightByCurveType(curveType);
		}

		/// <summary>
		/// 이전 모션의 가중치를 반환한다. (1.0 -> 0.0)
		/// </summary>
		public float GetPreviousMotionWeight(TransitionCurveType curveType)
		{
			return 1.0f - GetWeightByCurveType(curveType);
		}
	}

	/// <summary>
	/// 전환 곡선 타입
	/// </summary>
	public enum TransitionCurveType
	{
		Linear,
		EaseIn,
		EaseOut,
		EaseInOut
	}

	public class Live2DCharacterActionController : MonoBehaviour, ICharacterActionController
	{
		private CubismMotionController? _motionController;
		private CubismFadeController? _fadeController;
		private List<Live2DModelConfig.MotionClipMapping>? _motionClips;
		
		// 현재 상태
		private CharacterActionType _currentAction = CharacterActionType.Idle;
		private Live2DModelConfig.MotionClipMapping? _currentMotionClip;
		private bool _isPlayingAction = false;
		
		// Talk 상태 관리
		private bool _isTalkModeActive = false;
		private Coroutine? _talkLoopCoroutine;
		
		
		// Motion Control System
		private Coroutine? _currentMotionCoroutine;
		private System.Action? _currentMotionEndCallback;
		
		// Motion Transition System
		private MotionTransition? _currentTransition;
		private Coroutine? _transitionCoroutine;
		
		// Transition Settings
		[SerializeField] private float _transitionDuration = 1.0f; // 부드러운 전환을 위한 1초 설정
		[SerializeField] private bool _enableSmoothTransition = true;
		[SerializeField] private TransitionCurveType _transitionCurveType = TransitionCurveType.EaseInOut;
		
		// Motion End Behavior Control
		public System.Action? OnMotionStop { get; set; }
		public System.Action? OnMotionLoop { get; set; }
		public System.Action? OnMotionReturnToIdle { get; set; }
		public System.Action? OnTalkLoop { get; set; }

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
				Debug.LogWarning("[Live2DCharacterActionController] CubismFadeController를 찾을 수 없습니다. Live2D 자체 페이딩에 의존합니다.");
			}
			else
			{
				Debug.Log("[Live2DCharacterActionController] CubismFadeController 감지됨 - 부드러운 전환 지원");
			}
			
			// 기본 Action 콜백 설정
			SetupDefaultCallbacks();
			
			// 모션 클립 구성 및 전환 설정 디버깅
			Debug.Log($"===============[Live2DCharacterActionController] 초기화 완료 - Motion Clips: {_motionClips?.Count ?? 0}개");
			Debug.Log($"[Live2DCharacterActionController] 부드러운 전환: {_enableSmoothTransition}, 전환 시간: {_transitionDuration}초, 곡선: {_transitionCurveType}");
			
			if (_motionClips != null)
			{
				foreach (var clip in _motionClips)
				{
					Debug.Log($"[Live2DCharacterActionController] Motion Clip: {clip.Id} | Group: '{clip.MotionGroup}' | AnimationClip: {(clip.animationClip != null ? "O" : "X")}");
				}
				
				// idle 그룹 확인
				var idleClips = _motionClips.Where(c => c.MotionGroup.Equals("idle", System.StringComparison.OrdinalIgnoreCase)).ToList();
				Debug.Log($"[Live2DCharacterActionController] idle 그룹 모션 클립 수: {idleClips.Count}개 - Idle 모션 간 부드러운 전환 적용됨");
			}
		}

		/// <summary>
		/// 인터페이스 구현을 위한 매개변수 없는 초기화 메서드
		/// </summary>
		public void Initialize()
		{
			// 이미 초기화된 경우 추가 작업 없음
			if (_motionController != null && _motionClips != null)
			{
				return;
			}
			
			Debug.LogWarning("[Live2DCharacterActionController] Initialize() called without parameters. Make sure to call Initialize(motionController, motionClips) first.");
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
			
			// 추가로 초기화 완료 확인을 위한 짧은 대기
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
				Debug.Log("[Live2DCharacterActionController] 모션 정지");
				_isPlayingAction = false;
			};
			
			OnMotionLoop = () => {
				Debug.Log("[Live2DCharacterActionController] 모션 루프 - 같은 그룹의 다른 모션 재생");
				if (_currentMotionClip != null)
				{
					PlayRandomMotionFromGroup(_currentMotionClip.MotionGroup, endCallback: OnMotionLoop);
				}
			};
			
			OnMotionReturnToIdle = () => {
				Debug.Log("[Live2DCharacterActionController] 모션 종료 - Idle로 복귀");
				PlayAction(CharacterActionType.Idle);
			};
			
			OnTalkLoop = () => {
				Debug.Log("[Live2DCharacterActionController] Talk 모션 루프 - 다음 Talk 모션 재생");
				if (_isTalkModeActive)
				{
					PlayRandomMotionFromGroup("talk", endCallback: OnTalkLoop);
				}
				else
				{
					Debug.Log("[Live2DCharacterActionController] Talk 모드 비활성화 - Idle로 복귀");
					PlayAction(CharacterActionType.Idle);
				}
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
				Debug.LogWarning("[Live2DCharacterActionController] MotionController가 초기화되지 않았습니다.");
				return;
			}

			if (_motionClips == null || _motionClips.Count == 0) {
				Debug.LogWarning("[Live2DCharacterActionController] MotionClips이 설정되지 않았습니다.");
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
				
				Debug.Log($"[Live2DCharacterActionController] 액션 재생: {actionType}, 그룹: {motionGroup}");
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[Live2DCharacterActionController] 액션 재생 실패: {ex.Message}");
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
			Debug.Log("[Live2DCharacterActionController] 액션 중지");
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

			Debug.Log("[Live2DCharacterActionController] 강제 중지 후 Idle로 복귀");
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
		
		#region Talk Mode Control
		
		/// <summary>
		/// Talk 모드를 시작한다. Talk 모션들을 연속적으로 루프한다.
		/// </summary>
		public void StartTalkMode()
		{
			if (_isTalkModeActive)
			{
				Debug.Log("[Live2DCharacterActionController] Talk 모드 이미 활성화됨");
				return;
			}
			
			_isTalkModeActive = true;
			_currentAction = CharacterActionType.Talk;
			
			// Talk 모션 시작 (OnTalkLoop 콜백으로 연속 루프)
			PlayRandomMotionFromGroup("talk", endCallback: OnTalkLoop);
			
			Debug.Log("[Live2DCharacterActionController] Talk 모드 시작 - 연속 루프 모드");
		}
		
		/// <summary>
		/// Talk 모드를 종료한다. 현재 재생 중인 Talk 모션이 끝난 후 Idle로 돌아간다.
		/// </summary>
		public void StopTalkMode()
		{
			if (!_isTalkModeActive)
			{
				Debug.Log("[Live2DCharacterActionController] Talk 모드 이미 비활성화됨");
				return;
			}
			
			_isTalkModeActive = false;
			
			Debug.Log("[Live2DCharacterActionController] Talk 모드 종료 요청 - 현재 모션 종료 후 Idle로 복귀");
			
			// 현재 Talk 모션이 끝나면 OnTalkLoop에서 _isTalkModeActive 확인 후 Idle로 전환
			// 즉시 중단하려면 ForceStopAndReturnToIdle() 사용
		}
		
		/// <summary>
		/// Talk 모드가 활성화되어 있는지 확인한다.
		/// </summary>
		/// <returns>Talk 모드 활성화 여부</returns>
		public bool IsTalkModeActive()
		{
			return _isTalkModeActive;
		}
		
		/// <summary>
		/// Talk 모드를 즉시 중단하고 Idle로 돌아간다.
		/// </summary>
		public void ForceStopTalkMode()
		{
			_isTalkModeActive = false;
			ForceStopAndReturnToIdle();
			Debug.Log("[Live2DCharacterActionController] Talk 모드 즐시 중단 및 Idle 복귀");
		}
		
		#endregion
		
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
		/// 지정된 그룹에서 랜덤한 모션을 재생한다. (부드러운 전환 적용)
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
				Debug.LogWarning($"[Live2DCharacterActionController] '{motionGroup}' 그룹의 모션을 찾을 수 없습니다.");
				return;
			}
			
			// 랜덤 선택
			var selectedMotion = groupMotions[Random.Range(0, groupMotions.Count)];
			
			// 부드러운 전환 시스템 사용
			if (_enableSmoothTransition && _currentMotionClip != null)
			{
				StartMotionTransition(selectedMotion, endCallback);
			}
			else
			{
				// 부드러운 전환이 비활성화되었거나 현재 모션이 없는 경우 즉시 전환
				PerformImmediateTransition(selectedMotion, endCallback);
			}
			
			Debug.Log($"[Live2DCharacterActionController] 모션 재생: {selectedMotion.Id} (그룹: {motionGroup})");
		}
		
		/// <summary>
		/// 짧은 딜레이 후 모션을 재생한다. (Priority 충돌 방지)
		/// </summary>
		private IEnumerator DelayedMotionPlay(AnimationClip clip, int priority, System.Action? endCallback)
		{
			yield return new WaitForSeconds(_transitionDuration * 0.1f); // Priority 충돌 방지용 짧은 대기
			
			if (_motionController != null)
			{
				// 모션 재생 (CubismFadeController가 자동 페이드 처리)
				_motionController.PlayAnimation(clip, priority: 2, isLoop: false);
				_isPlayingAction = true; 
				
				// 모션 종료 처리 설정
				_currentMotionEndCallback = endCallback;
				StopAllMotionCoroutines();
				_currentMotionCoroutine = StartCoroutine(WaitForMotionEnd(clip.length, endCallback));
				
				Debug.Log($"[Live2DCharacterActionController] 모션 재생: {_currentMotionClip?.Id} (그룹: {_currentMotionClip?.MotionGroup}), 길이: {clip.length}s");
			}
		}
		
		/// <summary>
		/// Idle 모션을 다양하게 순차 재생한다.
		/// </summary>
		private void PlayRandomIdleMotion()
		{
			System.Action recursiveIdleCallback = () => {
				if (_currentAction == CharacterActionType.Idle)
				{
					StartCoroutine(DelayedIdleMotionStart());
				}
			};
			
			// Idle 모션 전용 재생 (PlayRandomMotionFromGroup 대신 직접 처리)
			PlayIdleMotionDirect(recursiveIdleCallback);
		}
		
		/// <summary>
		/// Idle 모션을 직접 재생한다. (부드러운 전환 적용)
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
				Debug.LogWarning("[Live2DCharacterActionController] idle 모션을 찾을 수 없습니다.");
				return;
			}
			
			// 현재와 같은 모션을 선택하지 않도록 필터링
			var availableIdles = idleMotions;
			if (_currentMotionClip != null && idleMotions.Count > 1)
			{
				availableIdles = idleMotions.Where(idle => idle.Id != _currentMotionClip.Id).ToList();
				if (availableIdles.Count == 0) availableIdles = idleMotions; // 필터링 후 비어있으면 원본 사용
			}
			
			// 랜덤 선택
			var selectedIdle = availableIdles[Random.Range(0, availableIdles.Count)];
			
			// Idle 모션 간에도 부드러운 전환 적용
			if (_enableSmoothTransition && _currentMotionClip != null)
			{
				// 현재 모션이 있다면 부드러운 전환 적용
				Debug.Log($"[Live2DCharacterActionController] Idle 모션 부드러운 전환: {_currentMotionClip.Id} -> {selectedIdle.Id}");
				StartMotionTransition(selectedIdle, endCallback);
			}
			else
			{
				// 첫 번째 Idle 재생이거나 부드러운 전환이 비활성화된 경우 즉시 전환
				Debug.Log($"[Live2DCharacterActionController] Idle 모션 즉시 전환: {selectedIdle.Id}");
				PerformImmediateIdleTransition(selectedIdle, endCallback);
			}
			
			// Idle 모션 전용 설정
			_isPlayingAction = false; // Idle은 Action이 아님
		}

		/// <summary>
		/// Idle 모션을 즉시 전환한다 (기존 방식 유지)
		/// </summary>
		private void PerformImmediateIdleTransition(Live2DModelConfig.MotionClipMapping selectedIdle, System.Action? endCallback)
		{
			if (_motionController == null || selectedIdle.animationClip == null) return;

			// 전환이 진행 중이라면 중단
			if (_transitionCoroutine != null)
			{
				StopCoroutine(_transitionCoroutine);
				_transitionCoroutine = null;
				_currentTransition = null;
			}

			_motionController.StopAllAnimation();
			_currentMotionClip = selectedIdle;
			
			// 짧은 대기 후 Idle 모션 재생 (PriorityIdle 사용)
			StartCoroutine(DelayedMotionPlay(selectedIdle.animationClip, CubismMotionPriority.PriorityIdle, endCallback));
			
			Debug.Log($"[Live2DCharacterActionController] Idle 모션 즉시 전환 실행: {selectedIdle.Id}");
		}
		
		/// <summary>
		/// 딜레이 후 다음 Idle 모션을 재생한다.
		/// </summary>
		private IEnumerator DelayedIdleMotionStart()
		{
			// Idle 모션 간 전환을 위한 짧은 대기 시간
			yield return new WaitForSeconds(_transitionDuration * 0.1f);
			
			// 여전히 Idle 상태이고 전환이 진행 중이 아닐 때만 다음 모션 재생
			if (_currentAction == CharacterActionType.Idle && !IsTransitioning())
			{
				Debug.Log("[Live2DCharacterActionController] 다음 Idle 모션 재생 시작");
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

		#region Motion Transition Settings

		/// <summary>
		/// 부드러운 전환 기능을 활성화/비활성화한다.
		/// </summary>
		/// <param name="enabled">활성화 여부</param>
		public void SetSmoothTransitionEnabled(bool enabled)
		{
			_enableSmoothTransition = enabled;
			Debug.Log($"[Live2DCharacterActionController] 부드러운 전환 설정: {enabled}");
		}

		/// <summary>
		/// 모션 전환 지속 시간을 설정한다.
		/// </summary>
		/// <param name="duration">전환 시간 (초)</param>
		public void SetTransitionDuration(float duration)
		{
			_transitionDuration = Mathf.Max(0.2f, duration); // 최소 시간을 더 길게
			Debug.Log($"[Live2DCharacterActionController] 전환 지속 시간 설정: {_transitionDuration}초 (단일레이어 최적화)");
		}

		/// <summary>
		/// 전환 곡선 타입을 설정한다.
		/// </summary>
		/// <param name="curveType">곡선 타입</param>
		public void SetTransitionCurveType(TransitionCurveType curveType)
		{
			_transitionCurveType = curveType;
			Debug.Log($"[Live2DCharacterActionController] 전환 곡선 타입 설정: {curveType}");
		}

		/// <summary>
		/// 현재 전환 설정 정보를 반환한다.
		/// </summary>
		public (bool enabled, float duration, TransitionCurveType curveType) GetTransitionSettings()
		{
			return (_enableSmoothTransition, _transitionDuration, _transitionCurveType);
		}

		/// <summary>
		/// 전환이 현재 진행 중인지 확인한다.
		/// </summary>
		public bool IsTransitionInProgress()
		{
			return IsTransitioning();
		}

		/// <summary>
		/// 현재 전환의 진행률을 반환한다. (0.0 ~ 1.0, 전환 중이 아니면 -1)
		/// </summary>
		public float GetTransitionProgress()
		{
			return _currentTransition?.GetProgress() ?? -1.0f;
		}

		/// <summary>
		/// 빠른 전환 설정 적용 (Idle 모션용)
		/// </summary>
		public void SetFastTransition()
		{
			SetTransitionDuration(_transitionDuration * 0.3f);
			SetTransitionCurveType(TransitionCurveType.EaseOut);
			Debug.Log("[Live2DCharacterActionController] 빠른 전환 설정 적용");
		}

		/// <summary>
		/// 부드러운 전환 설정 적용 (액션 모션용)
		/// </summary>
		public void SetSmoothTransition()
		{
			SetTransitionDuration(_transitionDuration * 0.6f);
			SetTransitionCurveType(TransitionCurveType.EaseInOut);
			Debug.Log("[Live2DCharacterActionController] 부드러운 전환 설정 적용");
		}

		/// <summary>
		/// 느린 전환 설정 적용 (특수 효과용)
		/// </summary>
		public void SetSlowTransition()
		{
			SetTransitionDuration(_transitionDuration * 1.5f);
			SetTransitionCurveType(TransitionCurveType.EaseInOut);
			Debug.Log("[Live2DCharacterActionController] 느린 전환 설정 적용");
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
				Debug.LogWarning($"[Live2DCharacterActionController] 모션 상태 체크 실패: {ex.Message}");
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
				Debug.Log("[Live2DCharacterActionController] 모든 모션 강제 중지");
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
			_transitionCoroutine = null;
		}

		/// <summary>
		/// 현재 전환이 진행 중인지 확인한다.
		/// </summary>
		private bool IsTransitioning()
		{
			return _currentTransition != null && !_currentTransition.isCompleted;
		}

		/// <summary>
		/// 새로운 모션 전환을 시작한다.
		/// </summary>
		/// <param name="newMotionClip">새로운 모션 클립</param>
		/// <param name="endCallback">전환 완료 후 실행될 콜백</param>
		private void StartMotionTransition(Live2DModelConfig.MotionClipMapping newMotionClip, System.Action? endCallback = null)
		{
			// 이전 전환 중단
			if (_transitionCoroutine != null)
			{
				StopCoroutine(_transitionCoroutine);
				_transitionCoroutine = null;
			}

			// 부드러운 전환이 비활성화된 경우 즉시 전환
			if (!_enableSmoothTransition)
			{
				PerformImmediateTransition(newMotionClip, endCallback);
				return;
			}

			// 새로운 전환 생성
			_currentTransition = new MotionTransition(_currentMotionClip, newMotionClip, _transitionDuration, endCallback);
			_currentMotionClip = newMotionClip;

			// 전환 코루틴 시작
			_transitionCoroutine = StartCoroutine(TransitionCoroutine());

			Debug.Log($"[Live2DCharacterActionController] 모션 전환 시작: {_currentTransition.previousMotion?.Id} -> {newMotionClip.Id}");
		}

		/// <summary>
		/// 즉시 모션을 전환한다 (기존 방식)
		/// </summary>
		private void PerformImmediateTransition(Live2DModelConfig.MotionClipMapping newMotionClip, System.Action? endCallback = null)
		{
			if (_motionController == null || newMotionClip.animationClip == null) return;

			_motionController.StopAllAnimation();
			_motionController.PlayAnimation(newMotionClip.animationClip, priority: 2, isLoop: false);
			_currentMotionClip = newMotionClip;
			_isPlayingAction = true;

			// 기존 모션 종료 처리 설정
			_currentMotionEndCallback = endCallback;
			StopAllMotionCoroutines();
			_currentMotionCoroutine = StartCoroutine(WaitForMotionEnd(newMotionClip.animationClip.length, endCallback));
		}

		#endregion

		#region Motion Transition Coroutine

		/// <summary>
		/// 부드러운 모션 전환을 처리하는 메인 코루틴 (단일 레이어 최적화)
		/// </summary>
		private IEnumerator TransitionCoroutine()
		{
			if (_currentTransition == null || _motionController == null)
			{
				yield break;
			}

			var transition = _currentTransition;
			var newClip = transition.newMotion.animationClip;
			if (newClip == null)
			{
				yield break;
			}

			// 단일 레이어 환경에서는 Live2D 내장 페이딩 시스템을 활용한 부드러운 전환
			bool useLayerWeights = _motionController.LayerCount > 1;
			
			Debug.Log($"[Live2DCharacterActionController] 전환 시작 - LayerCount: {_motionController.LayerCount}, 단일레이어 최적화: {!useLayerWeights}");

			if (!useLayerWeights)
			{
				// 단일 레이어 환경: 전환 시간을 고려한 딜레이 후 새 모션 재생
				yield return PerformSingleLayerTransition(transition);
			}
			else
			{
				// 다중 레이어 환경: 기존 방식 사용
				yield return PerformMultiLayerTransition(transition);
			}

			// 전환 완료 후 정리
			_transitionCoroutine = null;
			
			// 콜백 실행
			var callback = transition.endCallback;
			_currentTransition = null;
			callback?.Invoke();

			Debug.Log($"[Live2DCharacterActionController] 모션 전환 완료: {transition.newMotion.Id}");
		}

		/// <summary>
		/// 단일 레이어에서의 부드러운 전환 (Live2D 내장 페이딩 활용)
		/// </summary>
		private IEnumerator PerformSingleLayerTransition(MotionTransition transition)
		{
			var newClip = transition.newMotion.animationClip;
			if (newClip == null || _motionController == null) yield break;

			// Idle 모션과 일반 액션 모션에 따른 Priority 설정
			int priority = (transition.newMotion.MotionGroup.Equals("idle", System.StringComparison.OrdinalIgnoreCase)) 
				? CubismMotionPriority.PriorityIdle 
				: 2;

			// CubismFadeController의 FadeInTime을 고려한 최적 전환 타이밍
			// 이전 모션이 일정 부분 재생된 후 새 모션을 시작하여 자연스러운 오버랩 생성
			float fadeOverlap = _transitionDuration * 0.2f; // 전환시간의 20% 오버랩
			float startDelay = Mathf.Max(0.1f, _transitionDuration - fadeOverlap);
			
			Debug.Log($"[Live2DCharacterActionController] 단일레이어 부드러운 전환 - {startDelay:F2}초 후 새 모션 시작 (오버랩: {fadeOverlap:F2}초)");
			
			// 전환 시작까지 대기
			yield return new WaitForSeconds(startDelay);

			// Live2D CubismFadeController가 내장 FadeInTime/FadeOutTime을 사용하여 자동 페이딩 처리
			// Priority와 함께 모션 시작 - Live2D가 자동으로 이전 모션과 블렌딩
			_motionController.PlayAnimation(newClip, layerIndex: 0, priority: priority, isLoop: false);
			_isPlayingAction = !transition.newMotion.MotionGroup.Equals("idle", System.StringComparison.OrdinalIgnoreCase);

			Debug.Log($"[Live2DCharacterActionController] 새 모션 시작: {transition.newMotion.Id} (Priority: {priority})");

			// 페이딩 완료까지 대기
			yield return new WaitForSeconds(fadeOverlap);

			transition.isCompleted = true;
			Debug.Log($"[Live2DCharacterActionController] 단일레이어 전환 완료");
		}

		/// <summary>
		/// 다중 레이어에서의 부드러운 전환 (기존 방식)
		/// </summary>
		private IEnumerator PerformMultiLayerTransition(MotionTransition transition)
		{
			var newClip = transition.newMotion.animationClip;
			if (newClip == null || _motionController == null) yield break;

			// Idle 모션과 일반 액션 모션에 따른 Priority 설정
			int priority = (transition.newMotion.MotionGroup.Equals("idle", System.StringComparison.OrdinalIgnoreCase)) 
				? CubismMotionPriority.PriorityIdle 
				: 2;
			
			// 새 모션을 레이어 1에 즉시 시작
			_motionController.PlayAnimation(newClip, layerIndex: 1, priority: priority, isLoop: false);
			_isPlayingAction = !transition.newMotion.MotionGroup.Equals("idle", System.StringComparison.OrdinalIgnoreCase);

			// 전환 진행
			while (!transition.isCompleted)
			{
				float progress = transition.GetProgress();
				
				if (progress >= 1.0f)
				{
					CompleteTransition();
					break;
				}

				// 가중치 계산
				float newWeight = transition.GetNewMotionWeight(_transitionCurveType);
				float oldWeight = transition.GetPreviousMotionWeight(_transitionCurveType);
				ApplyLayerWeights(oldWeight, newWeight);

				yield return null; // 다음 프레임까지 대기
			}
		}

		/// <summary>
		/// 모션 레이어에 가중치를 적용한다
		/// </summary>
		private void ApplyLayerWeights(float oldWeight, float newWeight)
		{
			// CubismMotionController의 레이어 가중치를 직접 설정할 수 있는지 확인
			// 현재 Live2D SDK에서는 직접적인 레이어 가중치 설정이 제한적이므로
			// CubismFadeController를 통한 자동 페이딩에 의존
			
			Debug.Log($"[Live2DCharacterActionController] 가중치 적용 - Old: {oldWeight:F2}, New: {newWeight:F2}");
		}

		/// <summary>
		/// Fade Controller를 통한 가중치 적용
		/// </summary>
		private void ApplyFadeWeights(float oldWeight, float newWeight)
		{
			// CubismFadeController가 자동으로 모션 간 페이딩을 처리
			// 여기서는 전환 상태만 모니터링
			
			Debug.Log($"[Live2DCharacterActionController] 페이드 가중치 - Old: {oldWeight:F2}, New: {newWeight:F2}");
		}

		/// <summary>
		/// 모션 전환을 완료한다
		/// </summary>
		private void CompleteTransition()
		{
			if (_currentTransition == null) return;

			_currentTransition.isCompleted = true;

			// 이전 모션이 재생 중인 레이어들을 정리
			if (_motionController != null)
			{
				// 레이어 0의 모션 중지 (이전 모션)
				_motionController.StopAnimation(0);
				
				// 새 모션을 레이어 0으로 이동 (선택적)
				// Live2D SDK의 특성상 자동으로 정리되므로 추가 작업 불필요
			}

			// 모션 종료 처리를 위한 대기 코루틴 시작
			var newClip = _currentTransition.newMotion.animationClip;
			if (newClip != null)
			{
				float remainingTime = newClip.length - _currentTransition.duration;
				if (remainingTime > 0)
				{
					_currentMotionCoroutine = StartCoroutine(WaitForMotionEnd(remainingTime, _currentTransition.endCallback));
				}
			}

			Debug.Log("[Live2DCharacterActionController] 모션 전환 완료 처리");
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
			
			// 모든 전환 및 모션 코루틴 정리
			StopAllMotionCoroutines();
			_currentTransition = null;
		}
	}
}