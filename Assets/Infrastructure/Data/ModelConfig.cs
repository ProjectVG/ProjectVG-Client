using UnityEngine;

[CreateAssetMenu(fileName = "Model Config Data", menuName = "Scriptable Object/Model Config Data", order = int.MaxValue)]
public class ModelConfig : ScriptableObject
{
    [Header("Model 정보")]
    [Tooltip("모델 이름입니다. 앱 내에서 모델을 식별할 때 사용됩니다.")]
    [SerializeField] private string modelName;
    [Tooltip("모델에 대한 설명 또는 특징입니다. 캐릭터를 잘 설명할 수 있는 내용을 담아 주세요.")]
    [SerializeField] private string modelDescription;
    [Tooltip("모델을 대표하는 썸네일 이미지입니다. UI 목록 등에서 표시됩니다.")]
    [SerializeField] private Texture2D thumbnail;

    [Header("설정")]
    [Tooltip("시선 추적 민감도를 조절합니다. 값이 클수록 회전 값이 커집니다.")]
    [Range(0f, 30f)]
    [SerializeField] private float lookSensitivity = 1.0f;
    [Tooltip("시선 추적 반응 속도를 조절합니다. 값이 작을 수록 빠릅니다.")]
    [Range(0f, 5f)]
    [SerializeField] private float lockAtDamping = 0.0f;
    [Tooltip("시선 추적 활성화 여부를 설정합니다.")]
    [SerializeField] private bool isLockAtActive = true;

    [Header("모델 프리팹")]
    [SerializeField] private GameObject modelPrefab;

    public string ModelName => modelName;
    public string ModelDescription => modelDescription;
    public Texture2D Thumbnail => thumbnail;
    public float LookSensitivity => lookSensitivity;
    public float LockAtDamping => lockAtDamping;
    public GameObject ModelPrefab => modelPrefab;
    public bool IsLockAtActive => isLockAtActive;
}
