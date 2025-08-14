namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// 감정 → Expression 맵핑과 블렌딩을 담당하는 인터페이스
    /// </summary>
    public interface IEmotionController
    {
        void Initialize();
        void SetEmotion(string emotion, float intensity, int durationMs);
        void ClearEmotion();
    }
}


