using System.Collections.Generic;
using Live2D.Cubism.Framework.Raycasting;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IInputProvider
{
    bool TryGetPosition(out Vector3 position);
    bool IsUIBlockingInput();
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

    public bool IsUIBlockingInput()
    {
        return IsPointerOverIgnoredUI();
    }

    /// <summary>
    /// UI 클릭 여부 체크
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
            // UI 요소인지 확인
            if (result.gameObject.layer == LayerMask.NameToLayer("UI") ||
                result.gameObject.GetComponent<Canvas>() != null ||
                result.gameObject.GetComponent<UnityEngine.UI.Graphic>() != null ||
                result.gameObject.GetComponent<UnityEngine.UI.Selectable>() != null)
            {
                return true;
            }
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

    [Header("디버그 설정")]
    [SerializeField] private bool _enableDebugLog = false;
    [SerializeField] private bool _enableDebugRay = false;
    [SerializeField] private Color _debugRayColor = Color.red;
    [SerializeField] private float _debugRayDuration = 2f;

    private Vector3 _lastTapPosition = Vector3.zero;
    private bool _hasTapped = false;

    public void Initialize(Camera cam)
    {
        _camera = cam;
        if(_inputProvider == null)
            _inputProvider = new DefaultInputProvider();
        if (_inputUpProvider == null)
            _inputUpProvider = new DefaultInputUpProvider();
    }

    private void Update()
    {
        UpdateTapDebug();
    }

    private void UpdateTapDebug()
    {
        if (_inputProvider.TryGetPosition(out var currentPosition))
        {
            if (!_hasTapped)
            {
                _lastTapPosition = currentPosition;
                _hasTapped = true;
                LogTapDebug(currentPosition, "탭 시작");
                DrawDebugRay(currentPosition);
            }
        }
        else
        {
            if (_hasTapped && _enableDebugLog && _inputProvider is DefaultInputProvider defaultProvider)
            {
                if (defaultProvider.IsUIBlockingInput())
                {
                    Debug.Log("[ScreenTapManager] UI가 입력을 차단했습니다.");
                }
            }
            _hasTapped = false;
        }
    }

    private void LogTapDebug(Vector3 screenPosition, string action)
    {
        if (!_enableDebugLog) return;
        
        var worldPosition = _camera != null ? _camera.ScreenToWorldPoint(screenPosition) : Vector3.zero;
        var viewportPosition = _camera != null ? _camera.ScreenToViewportPoint(screenPosition) : Vector3.zero;
        
        Debug.Log($"[ScreenTapManager] {action} - 스크린: {screenPosition}, 월드: {worldPosition}, 뷰포트: {viewportPosition}");
    }

    private void DrawDebugRay(Vector3 screenPosition)
    {
        if (!_enableDebugRay || _camera == null) return;
        
        var ray = _camera.ScreenPointToRay(screenPosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, _debugRayColor, _debugRayDuration);
    }

    public Vector3 GetLastTapPosition()
    {
        return _lastTapPosition;
    }

    public bool HasTapped()
    {
        return _hasTapped;
    }

    public void SetDebugEnabled(bool enableLog, bool enableRay)
    {
        _enableDebugLog = enableLog;
        _enableDebugRay = enableRay;
    }

    private void OnDrawGizmos()
    {
        if (!_enableDebugRay || _camera == null || !_hasTapped) return;

        Gizmos.color = _debugRayColor;
        var worldPosition = _camera.ScreenToWorldPoint(_lastTapPosition);
        Gizmos.DrawWireSphere(worldPosition, 0.1f);
        
        var ray = _camera.ScreenPointToRay(_lastTapPosition);
        Gizmos.DrawRay(ray.origin, ray.direction * 10f);
    }

    /// <summary>
    /// 씬 전환 시 Camera를 업데이트한다.
    /// </summary>
    public void UpdateCamera(Camera newCamera)
    {
        _camera = newCamera;
        Debug.Log($"[ScreenTapManager] Camera 업데이트: {(newCamera != null ? newCamera.name : "null")}");
    }

    /// <summary>
    /// 현재 Main Camera로 Camera를 업데이트한다.
    /// </summary>
    public void UpdateToMainCamera()
    {
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            UpdateCamera(mainCamera);
        }
        else
        {
            Debug.LogWarning("[ScreenTapManager] Main Camera를 찾을 수 없습니다.");
        }
    }

    #region LockAt
    public bool TryGetLookDirection(out Vector3 lookDir)
    {
        if (_inputProvider.TryGetPosition(out var screenPos))
        {
            lookDir = ConvertScreenToLookDirection(screenPos);
            if (_enableDebugLog)
            {
                Debug.Log($"[ScreenTapManager] LookAt 방향: {lookDir}");
            }
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

        // 필수 컴포넌트 null 체크
        if (_camera == null)
        {
            Debug.LogWarning("[ScreenTapManager] Camera가 null입니다. Initialize()를 호출해주세요.");
            return false;
        }

        if (_raycaster == null)
        {
            Debug.LogWarning("[ScreenTapManager] CubismRaycaster가 null입니다. SetRaycaster()를 호출해주세요.");
            return false;
        }

        if (_inputUpProvider == null)
        {
            Debug.LogWarning("[ScreenTapManager] InputUpProvider가 null입니다. Initialize()를 호출해주세요.");
            return false;
        }

        // 손 뗀 시점이 아니면 false
        if (!_inputUpProvider.TryGetPosition(out var screenPosition))
        {
            return false;
        }

        LogTapDebug(screenPosition, "탭 종료");
        DrawDebugRay(screenPosition);

        var results = new CubismRaycastHit[4];
        var ray = _camera.ScreenPointToRay(screenPosition);
        var hitCount = _raycaster.Raycast(ray, results);

        if (hitCount > 0)
        {
            hitResults = results;
            if (_enableDebugLog)
            {
                Debug.Log($"[ScreenTapManager] Raycast 히트: {hitCount}개 오브젝트");
                for (int i = 0; i < hitCount; i++)
                {
                    var drawable = results[i].Drawable;
                    var worldPos = results[i].WorldPosition;
                    var localPos = results[i].LocalPosition;
                    var distance = results[i].Distance;
                    
                    Debug.Log($"[ScreenTapManager] 히트 {i}: Drawable={drawable.name}, 월드위치={worldPos}, 로컬위치={localPos}, 거리={distance:F2}");
                }
            }
            return true;
        }

        if (_enableDebugLog)
        {
            Debug.Log("[ScreenTapManager] Raycast 히트 없음");
        }

        return false;
    }
    #endregion
}
