namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 액션 요청을 해석하는 인터페이스
	/// </summary>
	public interface ICharacterActionResolver
	{
		/// <summary>
		/// 액션 요청을 해석하여 구체적인 액션 정보로 변환한다.
		/// </summary>
		/// <param name="request">액션 요청</param>
		/// <returns>해석된 액션 정보</returns>
		CharacterResolvedAction Resolve(CharacterActionRequest request);
	}
}
