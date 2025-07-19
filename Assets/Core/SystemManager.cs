using Live2D.Cubism.Framework.LookAt;
using UnityEngine;
using UnityEngine.Serialization;

public class SystemManager : Singleton<SystemManager>
{
    [SerializeField] private CubismLookTarget cubismLookTarget = null;
    [SerializeField] private Camera mCamera = null;
    [SerializeField] private ModelConfig initModelConfig = null;

    private GameObject _currentModel = null;

    private void Start()
    {
        ScreenTapManager.Instance.Initialize(mCamera);
        ModelInit(initModelConfig);
    }

    private void ModelInit(ModelConfig newModelConfig)
    {
        if (_currentModel != null)
        {
            Destroy(_currentModel);
        }

        _currentModel = Instantiate(newModelConfig.ModelPrefab);
        var modelController = _currentModel.GetComponent<CubismLookController>();
        modelController.Target = cubismLookTarget.gameObject;

        modelController.Damping = newModelConfig.LockAtDamping;

        cubismLookTarget.Initialize(initModelConfig);
    }

}
