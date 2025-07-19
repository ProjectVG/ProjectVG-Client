using Live2D.Cubism.Framework.LookAt;
using UnityEngine;

public class CubismLookTarget : MonoBehaviour, ICubismLookTarget
{
    private Camera _mCamera = null;
    private ModelConfig _modelConfig;

    public void Initialize(Camera targetCamera, ModelConfig modelConfig)
    {
        _mCamera = targetCamera;
        _modelConfig = modelConfig;
    }

    public Vector3 GetPosition()
    {
        Vector3 screenPos = GetScreenInputPosition();
        if (screenPos == Vector3.zero)
            return Vector3.zero;

        return ConvertScreenToLookDirection(screenPos);
    }

    /// <summary>
    /// 입력된 화면 좌표를 가져옴
    /// </summary>
    private Vector3 GetScreenInputPosition()
    {
    #if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0)
            return Input.GetTouch(0).position;
    #else
        if (Input.GetMouseButton(0))
            return Input.mousePosition;
    #endif
        return Vector3.zero;
    }

    /// <summary>
    /// 스크린 좌표를 Live2D 모델이 사용할 방향 벡터로 변환
    /// </summary>
    private Vector3 ConvertScreenToLookDirection(Vector3 screenPos)
    {
        Vector3 viewportPos = _mCamera.ScreenToViewportPoint(screenPos);
        viewportPos = (viewportPos * 2) - Vector3.one;  // [-1,1]로 정규화
        return viewportPos * _modelConfig.LookSensitivity;
    }

    public bool IsActive()
    {
        return _modelConfig.IsLockAtActive;
    }
}
