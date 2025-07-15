using UnityEngine;

public interface ILookAtInputProvider
{
    bool HasInput { get; }
    Vector3 TargetPosition { get; }
}

public class LockAtInputProvider : MonoBehaviour, ILookAtInputProvider
{
    public bool HasInput =>
        (Application.isMobilePlatform && Input.touchCount > 0) || Input.GetMouseButton(0);

    public Vector3 TargetPosition
    {
        get
        {
            if (Application.isMobilePlatform && Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
        }
    }
}