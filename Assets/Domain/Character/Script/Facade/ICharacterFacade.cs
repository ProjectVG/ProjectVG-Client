namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 제어를 위한 파사드 인터페이스
	/// </summary>
	public interface ICharacterFacade
	{
		/// <summary>
		/// 파사드를 초기화한다.
		/// </summary>
		void Initialize();

		/// <summary>
		/// 파사드를 종료한다.
		/// </summary>
		void Shutdown();

		/// <summary>
		/// 캐릭터를 등록한다.
		/// </summary>
		/// <param name="characterId">캐릭터 ID</param>
		/// <param name="preload">사전 로드 여부</param>
		void RegisterCharacter(string characterId, bool preload = false);

		/// <summary>
		/// 캐릭터 등록을 해제한다.
		/// </summary>
		/// <param name="characterId">캐릭터 ID</param>
		void UnregisterCharacter(string characterId);

		/// <summary>
		/// 활성 캐릭터를 설정한다.
		/// </summary>
		/// <param name="characterId">캐릭터 ID</param>
		void SetActiveCharacter(string characterId);

		/// <summary>
		/// 활성 캐릭터를 해제한다.
		/// </summary>
		void ClearActiveCharacter();

		/// <summary>
		/// 액션을 적용한다.
		/// </summary>
		/// <param name="request">액션 요청</param>
		/// <returns>액션 핸들</returns>
		CharacterActionHandle ApplyAction(CharacterActionRequest request);

		/// <summary>
		/// 액션을 취소한다.
		/// </summary>
		/// <param name="actionId">액션 ID</param>
		void CancelAction(string actionId);

		/// <summary>
		/// 모든 액션을 취소한다.
		/// </summary>
		void CancelAllActions();

		/// <summary>
		/// 음성 재생 시작을 알린다.
		/// </summary>
		void OnVoiceStarted();

		/// <summary>
		/// 음성 재생 종료를 알린다.
		/// </summary>
		void OnVoiceFinished();

		/// <summary>
		/// 활성 캐릭터 ID를 반환한다.
		/// </summary>
		/// <returns>활성 캐릭터 ID</returns>
		string GetActiveCharacterId();

		/// <summary>
		/// 캐릭터 등록 여부를 반환한다.
		/// </summary>
		/// <param name="characterId">캐릭터 ID</param>
		/// <returns>등록 여부</returns>
		bool IsRegistered(string characterId);
	}
}
