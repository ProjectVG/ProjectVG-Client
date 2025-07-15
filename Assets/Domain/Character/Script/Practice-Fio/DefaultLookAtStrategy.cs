using UnityEngine;

public interface ILookAtStrategy
{
    LookAtResult Calculate(Vector3 targetWorldPos, Transform reference, LockAt_ConfigData config);
}
// TODO : [폴리싱] fio - 시선 방향을 머리 Pivot 기준으로 계산되도록 개선
public class DefaultLookAtStrategy : ILookAtStrategy
{
    public LookAtResult Calculate(Vector3 targetWorldPos, Transform reference, LockAt_ConfigData config)
    {
        Vector3 dir = targetWorldPos - reference.position;

        float normalizedX = Mathf.Clamp(dir.x, -1f, 1f);
        float normalizedY = Mathf.Clamp(dir.y, -1f, 1f);

        return new LookAtResult(
            normalizedX * config.HeadMaxAngle,
            normalizedY * config.HeadMaxAngle,
            normalizedX * config.EyeMaxAngle,
            normalizedY * config.EyeMaxAngle
        );
    }
}