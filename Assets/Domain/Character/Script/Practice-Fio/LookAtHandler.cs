using Live2D.Cubism.Core;
using UnityEngine;

public interface ILookAtHandler
{
    void UpdateLookAt(LookAtResult result, LockAt_ConfigData config);
}

public class LookAtHandler : ILookAtHandler
{
    // 캐싱
    private readonly CubismParameter _paramHeadX;
    private readonly CubismParameter _paramHeadY;
    private readonly CubismParameter _paramEyeX;
    private readonly CubismParameter _paramEyeY;

    public LookAtHandler(CubismModel model)
    {
        _paramHeadX = model.Parameters.FindById("ParamAngleX");
        _paramHeadY = model.Parameters.FindById("ParamAngleY");
        _paramEyeX = model.Parameters.FindById("ParamEyeBallX");
        _paramEyeY = model.Parameters.FindById("ParamEyeBallY");
    }

    public void UpdateLookAt(LookAtResult r, LockAt_ConfigData config)
    {
        if (_paramHeadX != null)
            _paramHeadX.Value = Mathf.Lerp(_paramHeadX.Value, r.HeadX, Time.deltaTime * config.SmoothHeadSpeed);
        if (_paramHeadY != null)
            _paramHeadY.Value = Mathf.Lerp(_paramHeadY.Value, r.HeadY, Time.deltaTime * config.SmoothHeadSpeed);

        if (_paramEyeX != null)
            _paramEyeX.Value = Mathf.Lerp(_paramEyeX.Value, r.EyeX, Time.deltaTime * config.SmoothEyeSpeed);
        if (_paramEyeY != null)
            _paramEyeY.Value = Mathf.Lerp(_paramEyeY.Value, r.EyeY, Time.deltaTime * config.SmoothEyeSpeed);
    }
}