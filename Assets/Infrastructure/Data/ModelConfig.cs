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

    [Header("시선 설정")]
    [Tooltip("시선 추적 민감도를 조절합니다. 값이 클수록 회전 값이 커집니다.")]
    [Range(0f, 30f)]
    [SerializeField] private float lookSensitivity = 1.0f;
    [Tooltip("시선 추적 반응 속도를 조절합니다. 값이 작을 수록 빠릅니다.")]
    [Range(0f, 5f)]
    [SerializeField] private float lockAtDamping = 0.0f;
    [Tooltip("시선 추적 활성화 여부를 설정합니다.")]
    [SerializeField] private bool isLockAtActive = true;

    [Header("립싱크 설정")]
    [Tooltip("샘플링된 음량을 몇배로 취급할지 설정합니다. 1은 1배입니다.")]
    [Range(1f, 10f)]
    [SerializeField] private float gain = 1f;
    [Tooltip("입의 움직임을 얼마나 부드럽게 할지 설정합니다. 값을 늘릴수록 매끄러워지지만 부하도 증가합니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float smoothing = 1f;

    [Header("모델 프리팹")]
    [SerializeField] private GameObject modelPrefab;

    // 모델 정보
    public string ModelName => modelName;
    public string ModelDescription => modelDescription;
    public Texture2D Thumbnail => thumbnail;

    // 시선 처리
    public float LookSensitivity => lookSensitivity;
    public float LockAtDamping => lockAtDamping;
    public bool IsLockAtActive => isLockAtActive;

    // 립싱크
    public float Gain => gain;
    public float Smoothing => smoothing;

    // 프리팹
    public GameObject ModelPrefab => modelPrefab;

}
