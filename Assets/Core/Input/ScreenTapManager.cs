using UnityEngine;

public interface IInputProvider
{
    bool TryGetPosition(out Vector3 position);
}

public class DefaultInputProvider : IInputProvider
{
    public bool TryGetPosition(out Vector3 position)
    {
#if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0)
        {
            position = Input.GetTouch(0).position;
            return true;
        }
#else
        if (Input.GetMouseButton(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif
        position = Vector3.zero;
        return false;
    }
}

public class ScreenTapManager : Singleton<ScreenTapManager>
{
    private Camera _camera;
    private IInputProvider _inputProvider;

    public void Initialize(Camera cam, IInputProvider inputProvider = null)
    {
        _camera = cam;
        _inputProvider = inputProvider ?? new DefaultInputProvider();
    }

    public bool TryGetLookDirection(out Vector3 lookDir)
    {
        if (_inputProvider.TryGetPosition(out var screenPos))
        {
            lookDir = ConvertScreenToLookDirection(screenPos);
            return true;
        }
        lookDir = Vector3.zero;
        return false;
    }

    /// <summary>
    /// 입력된 화면 좌표를 가져옴
    /// </summary>
    public Vector3 GetScreenInputPosition()
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
        Vector3 viewportPos = _camera.ScreenToViewportPoint(screenPos);
        viewportPos = (viewportPos * 2) - Vector3.one;  // [-1,1]로 정규화
        return viewportPos;
    }
}
