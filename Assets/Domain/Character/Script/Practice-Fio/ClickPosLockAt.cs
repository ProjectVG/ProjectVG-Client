using UnityEngine;
using Live2D.Cubism.Core;

// 요구사항
// - 클릭한 위치를 기준으로 캐릭터의 시선이 자연스럽게 따라가야 합니다.
// - SDK에서 제공하는 `LookAt` 또는 `Parameter` API를 사용하여 구현합니다.
// - 단순한 위치 이동이 아닌 얼굴 방향 회전으로 표현되어야 하며, 최대 회전 각도 제한이 적용되어야 합니다.

public class ClickPosLockAt : MonoBehaviour
{
    [SerializeField] private LockAt_ConfigData modelConfig;
    [SerializeField] private MonoBehaviour inputProviderBehaviour;

    private Camera _mainCamera = null;
    private ILookAtInputProvider _inputProvider = null;
    private ILookAtStrategy _lookAtStrategy = null;
    private ILookAtHandler _lookAtHandler = null;

    private void Start()
    {
        _inputProvider = inputProviderBehaviour as ILookAtInputProvider;

        var model = GetComponent<CubismModel>();
        _lookAtHandler = new LookAtHandler(model);

        _lookAtStrategy = new DefaultLookAtStrategy();
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_inputProvider == null || !_inputProvider.HasInput)
        {
            _lookAtHandler.UpdateLookAt(LookAtResult.Zero, modelConfig);
            return;
        }

        Vector3 screenPos = _inputProvider.TargetPosition;
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(_mainCamera.transform.position.z))
        );

        LookAtResult result = _lookAtStrategy.Calculate(worldPos, transform, modelConfig);

        _lookAtHandler.UpdateLookAt(result, modelConfig);
    }
}

public struct LookAtResult
{
    public float HeadX;
    public float HeadY;
    public float EyeX;
    public float EyeY;

    public static LookAtResult Zero => new LookAtResult(0, 0, 0, 0);

    public LookAtResult(float headX, float headY, float eyeX, float eyeY)
    {
        HeadX = headX;
        HeadY = headY;
        EyeX = eyeX;
        EyeY = eyeY;
    }
}