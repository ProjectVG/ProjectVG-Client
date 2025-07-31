using System.Collections.Generic;
using Live2D.Cubism.Framework.Raycasting;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IInputProvider
{
    bool TryGetPosition(out Vector3 position);
}

public interface IInputUpProvider
{
    bool TryGetPosition(out Vector3 position); // 터치 종료 시점만 반환
}

public class DefaultInputProvider : IInputProvider
{
    public bool TryGetPosition(out Vector3 position)
    {
        position = Vector3.zero;

        if (IsPointerOverIgnoredUI())
        {
            return false;
        }

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

        return false;
    }

    /// <summary>
    /// "IgnoreLookAt" 태그가 달린 UI 클릭 여부 체크
    /// </summary>
    private bool IsPointerOverIgnoredUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
        #if UNITY_IOS || UNITY_ANDROID
            position = Input.touchCount > 0 ? Input.GetTouch(0).position : Vector2.zero
        #else
            position = Input.mousePosition
        #endif
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.CompareTag("IgnoreLookAt"))
                return true;
        }
        return false;
    }
}

public class DefaultInputUpProvider : IInputUpProvider
{
    public bool TryGetPosition(out Vector3 position)
    {
    #if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            position = Input.GetTouch(0).position;
            return true;
        }
    #else
        if (Input.GetMouseButtonUp(0))
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
    private Camera _camera = null;

    private IInputProvider _inputProvider = null;
    private IInputUpProvider _inputUpProvider = null;

    private CubismRaycaster _raycaster = null;

    public void Initialize(Camera cam)
    {
        if (_camera == null)
            _camera = cam;
        if(_inputProvider == null)
            _inputProvider = new DefaultInputProvider();
        if (_inputUpProvider == null)
            _inputUpProvider = new DefaultInputUpProvider();
    }

    #region LockAt
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
    /// 스크린 좌표를 Live2D 모델이 사용할 방향 벡터로 변환
    /// </summary>
    private Vector3 ConvertScreenToLookDirection(Vector3 screenPos)
    {
        Vector3 viewportPos = _camera.ScreenToViewportPoint(screenPos);
        viewportPos = (viewportPos * 2) - Vector3.one;  // [-1,1]로 정규화
        return viewportPos;
    }

    #endregion

    #region Raycaster

    public void SetRaycaster(CubismRaycaster cubismRaycaster)
    {
        _raycaster = cubismRaycaster;
    }
    public bool TryGetTapUpPosition(out CubismRaycastHit[] hitResults)
    {
        hitResults = null;

        // 손 뗀 시점이 아니면 false
        if (!_inputUpProvider.TryGetPosition(out var screenPosition))
        {
            return false;
        }

        var results = new CubismRaycastHit[4];
        var ray = _camera.ScreenPointToRay(screenPosition);
        var hitCount = _raycaster.Raycast(ray, results);

        if (hitCount > 0)
        {
            hitResults = results;
            return true;
        }

        return false;
    }
    #endregion
}
