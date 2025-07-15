using UnityEngine;

[CreateAssetMenu(fileName = "LockAtConfigData", menuName = "Scriptable Objects/LockAtConfigData")]
public class LockAt_ConfigData : ScriptableObject
{
    [Header("고개 회전")]
    public float HeadMaxAngle = 0f;
    public float SmoothHeadSpeed = 0f;

    [Header("눈동자 움직임")]
    public float EyeMaxAngle = 0f;
    public float SmoothEyeSpeed = 0f;
}
