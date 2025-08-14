namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 감정 → Expression 맵핑과 블렌딩을 담당한다.
	/// </summary>
	public interface IEmotionController
	{
		void Initialize();
		void SetEmotion(string emotion, float intensity, int durationMs);
		void ClearEmotion();
	}
}


