using Live2D.Cubism.Framework.LookAt;
using Live2D.Cubism.Framework.MouthMovement;
using UnityEngine;
using UnityEngine.UI;

public class SystemManager : Singleton<SystemManager>
{
    [SerializeField] private CubismLookTarget cubismLookTarget = null;
    [SerializeField] private Camera mCamera = null;
    [SerializeField] private ModelConfig initModelConfig = null;



    private GameObject _currentModel = null;

    // TODO : test 추후 삭제
    [SerializeField] private AudioSource voiceSource = null;
    [SerializeField] private Button expressionChangeBtn = null;

    private void Start()
    {
        ScreenTapManager.Instance.Initialize(mCamera);
        AudioManager.Instance.Initialize();

        ModelInit(initModelConfig);
    }

    private void ModelInit(ModelConfig modelConfig)
    {
        if (_currentModel != null)
        {
            Destroy(_currentModel);
        }

        _currentModel = Instantiate(modelConfig.ModelPrefab);

        // LockAt 설정
        SetLockAt(modelConfig);

        // LipSync 설정
        SetLipSync(modelConfig);

        // raycast 설정
        SetRayCast(modelConfig);
    }

    private void SetLockAt(ModelConfig modelConfig)
    {
        var lookController = _currentModel.GetComponent<CubismLookController>();
        lookController.Target = cubismLookTarget.gameObject;
        lookController.Damping = modelConfig.LockAtDamping;

        cubismLookTarget.Initialize(modelConfig);
    }

    private void SetLipSync(ModelConfig modelConfig)
    {
        var mouthController = _currentModel.GetComponent<CubismAudioMouthInput>();
        mouthController.AudioInput = voiceSource;
        mouthController.Gain = modelConfig.Gain;
        mouthController.Smoothing = modelConfig.Smoothing;
    }

    private void SetRayCast(ModelConfig modelConfig)
    {
        var hitHandler = _currentModel.GetComponent<CubismHitHandler>();
        hitHandler.Initialize();
        expressionChangeBtn.onClick.AddListener(hitHandler.ExpressionChange_Btn);
    }
}
