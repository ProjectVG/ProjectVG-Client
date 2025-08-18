using System;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 액션 요청을 나타내는 DTO
	/// </summary>
	[Serializable]
	public struct CharacterActionRequest
	{
		/// <summary>
		/// 대상 캐릭터 ID
		/// </summary>
		public string CharacterId;

		/// <summary>
		/// 액션 키 (예: "happy", "wave", "nod")
		/// </summary>
		public string ActionKey;

		/// <summary>
		/// 액션 강도 (0.0 ~ 1.0)
		/// </summary>
		public float Intensity;

		/// <summary>
		/// 액션 지속 시간 (밀리초, null이면 기본값 사용)
		/// </summary>
		public int? DurationMs;

		/// <summary>
		/// 액션 우선순위
		/// </summary>
		public CharacterActionPriority Priority;

		/// <summary>
		/// 액션 취소 정책
		/// </summary>
		public CharacterCancelPolicy CancelPolicy;

		public CharacterActionRequest(string characterId, string actionKey, float intensity = 1.0f, int? durationMs = null, CharacterActionPriority priority = CharacterActionPriority.Normal, CharacterCancelPolicy cancelPolicy = CharacterCancelPolicy.Replace)
		{
			CharacterId = characterId;
			ActionKey = actionKey;
			Intensity = intensity;
			DurationMs = durationMs;
			Priority = priority;
			CancelPolicy = cancelPolicy;
		}
	}

	/// <summary>
	/// 캐릭터 액션 핸들 (액션 실행 추적용)
	/// </summary>
	[Serializable]
	public struct CharacterActionHandle
	{
		/// <summary>
		/// 액션 고유 ID
		/// </summary>
		public string ActionId;

		/// <summary>
		/// 액션 상태
		/// </summary>
		public CharacterActionStatus Status;

		public CharacterActionHandle(string actionId, CharacterActionStatus status = CharacterActionStatus.Pending)
		{
			ActionId = actionId;
			Status = status;
		}
	}

	/// <summary>
	/// 캐릭터 액션 상태 정보
	/// </summary>
	[Serializable]
	public struct CharacterActionState
	{
		/// <summary>
		/// 현재 실행 중인 액션 ID
		/// </summary>
		public string CurrentActionId;

		/// <summary>
		/// 현재 실행 중인 액션 키
		/// </summary>
		public string CurrentActionKey;

		/// <summary>
		/// 액션이 재생 중인지 여부
		/// </summary>
		public bool IsPlaying;

		/// <summary>
		/// 음성 게이트가 활성화되어 있는지 여부
		/// </summary>
		public bool IsVoiceGated;

		public CharacterActionState(string currentActionId = null, string currentActionKey = null, bool isPlaying = false, bool isVoiceGated = false)
		{
			CurrentActionId = currentActionId;
			CurrentActionKey = currentActionKey;
			IsPlaying = isPlaying;
			IsVoiceGated = isVoiceGated;
		}
	}

	/// <summary>
	/// 해석된 캐릭터 액션 정보
	/// </summary>
	[Serializable]
	public struct CharacterResolvedAction
	{
		/// <summary>
		/// 표현식 키
		/// </summary>
		public string ExpressionKey;

		/// <summary>
		/// 모션 키
		/// </summary>
		public string MotionKey;

		/// <summary>
		/// 지속 시간 (밀리초)
		/// </summary>
		public int? DurationMs;

		/// <summary>
		/// 블렌드 인 시간 (초)
		/// </summary>
		public float BlendInSec;

		/// <summary>
		/// 블렌드 아웃 시간 (초)
		/// </summary>
		public float BlendOutSec;

		public CharacterResolvedAction(string expressionKey = null, string motionKey = null, int? durationMs = null, float blendInSec = 0.3f, float blendOutSec = 0.3f)
		{
			ExpressionKey = expressionKey;
			MotionKey = motionKey;
			DurationMs = durationMs;
			BlendInSec = blendInSec;
			BlendOutSec = blendOutSec;
		}
	}

	/// <summary>
	/// 캐릭터 액션 우선순위
	/// </summary>
	public enum CharacterActionPriority
	{
		/// <summary>
		/// 낮은 우선순위
		/// </summary>
		Low = 0,

		/// <summary>
		/// 일반 우선순위
		/// </summary>
		Normal = 1,

		/// <summary>
		/// 높은 우선순위
		/// </summary>
		High = 2,

		/// <summary>
		/// 최고 우선순위 (음성 등)
		/// </summary>
		Critical = 3
	}

	/// <summary>
	/// 캐릭터 액션 취소 정책
	/// </summary>
	public enum CharacterCancelPolicy
	{
		/// <summary>
		/// 기존 액션을 대체
		/// </summary>
		Replace = 0,

		/// <summary>
		/// 기존 액션이 완료될 때까지 대기
		/// </summary>
		Wait = 1,

		/// <summary>
		/// 기존 액션을 중단하고 즉시 실행
		/// </summary>
		Interrupt = 2,

		/// <summary>
		/// 기존 액션이 우선순위가 낮을 때만 대체
		/// </summary>
		ReplaceIfLowerPriority = 3
	}

	/// <summary>
	/// 캐릭터 액션 상태
	/// </summary>
	public enum CharacterActionStatus
	{
		/// <summary>
		/// 대기 중
		/// </summary>
		Pending = 0,

		/// <summary>
		/// 실행 중
		/// </summary>
		Playing = 1,

		/// <summary>
		/// 완료됨
		/// </summary>
		Completed = 2,

		/// <summary>
		/// 취소됨
		/// </summary>
		Cancelled = 3,

		/// <summary>
		/// 오류 발생
		/// </summary>
		Error = 4
	}
}
