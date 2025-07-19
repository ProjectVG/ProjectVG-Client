using Live2D.Cubism.Framework.LookAt;
using UnityEngine;

public class CubismLookTarget : MonoBehaviour, ICubismLookTarget
{
    private ModelConfig _modelConfig;

    public void Initialize(ModelConfig modelConfig)
    {
        _modelConfig = modelConfig;
    }

    public Vector3 GetPosition()
    {
        if (!ScreenTapManager.Instance.TryGetLookDirection(out var lookDir))
            return Vector3.zero;

        return lookDir * _modelConfig.LookSensitivity;
    }

    public bool IsActive()
    {
        return _modelConfig.IsLockAtActive;
    }
}
