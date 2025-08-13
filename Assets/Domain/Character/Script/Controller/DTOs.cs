namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 감정/행동 데이터 전달을 위한 단순 DTO.
	/// </summary>
	public struct EmotionData { public string Emotion; public float Intensity; public int DurationMs; }
	public struct ActionData { public string Action; public object Args; }
}


