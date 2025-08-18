using Cysharp.Threading.Tasks;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 액션 실행을 관리하는 서비스 인터페이스
	/// </summary>
	public interface ICharacterActionService
	{
		/// <summary>
		/// 서비스를 초기화한다.
		/// </summary>
		void Initialize();

		/// <summary>
		/// 액션을 큐에 추가한다.
		/// </summary>
		/// <param name="request">액션 요청</param>
		/// <returns>액션 핸들</returns>
		CharacterActionHandle Enqueue(CharacterActionRequest request);

		/// <summary>
		/// 특정 액션을 중지한다.
		/// </summary>
		/// <param name="actionId">중지할 액션 ID</param>
		void Stop(string actionId);

		/// <summary>
		/// 모든 액션을 중지한다.
		/// </summary>
		void StopAll();

		/// <summary>
		/// 음성 게이트를 설정한다.
		/// </summary>
		/// <param name="isVoicePlaying">음성이 재생 중인지 여부</param>
		void SetVoiceGate(bool isVoicePlaying);

		/// <summary>
		/// 현재 액션 상태를 반환한다.
		/// </summary>
		/// <returns>액션 상태</returns>
		CharacterActionState GetState();
	}
}
