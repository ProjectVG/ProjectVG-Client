using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Domain.Character.Live2D.Model;
using UnityEngine.UI;
using Live2D.Cubism.Framework.LookAt;
using Live2D.Cubism.Framework.MouthMovement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectVG.Domain.Character.Service
{
    public class Live2DModelManager : MonoBehaviour, ILive2DModelManager
    {
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private Live2DModelRegistry _modelRegistry;
        [SerializeField] private Live2DCharacterConfig _defaultCharacterConfig;
        [SerializeField] private CubismLookTarget _cubismLookTarget;
        [SerializeField] private AudioSource _voiceSource;
        [SerializeField] private Button _expressionChangeBtn;

        private readonly Dictionary<string, GameObject> _characterIdToInstance = new Dictionary<string, GameObject>();
        private string _activeCharacterId;

        /// <summary>
        /// Live2D 모델 관리를 위한 초기 설정을 수행한다.
        /// </summary>
        public void Initialize(Live2DModelRegistry modelRegistry, Live2DCharacterConfig defaultCharacterConfig)
        {
            _modelRegistry = modelRegistry;
            _defaultCharacterConfig = defaultCharacterConfig;
            if (_modelRoot == null)
            {
                _modelRoot = transform;
            }
        }

        /// <summary>
        /// 지정한 캐릭터 모델을 비동기 로드한다.
        /// </summary>
        public UniTask<GameObject> LoadModelAsync(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return UniTask.FromResult<GameObject>(null);
            }
            if (_characterIdToInstance.TryGetValue(characterId, out var existing))
            {
                return UniTask.FromResult(existing);
            }
            Live2DCharacterConfig config = null;
            if (_modelRegistry != null)
            {
                _modelRegistry.TryGetConfig(characterId, out config);
            }
            if (config == null)
            {
                config = _defaultCharacterConfig;
            }
            if (config == null || config.CharacterPrefab == null)
            {
                return UniTask.FromResult<GameObject>(null);
            }
            var instance = Instantiate(config.CharacterPrefab, _modelRoot != null ? _modelRoot : transform);
            instance.name = characterId;
            instance.SetActive(false);
            _characterIdToInstance[characterId] = instance;
            return UniTask.FromResult(instance);
        }

        /// <summary>
        /// 현재 활성 모델을 언로드한다.
        /// </summary>
        public void UnloadActiveModel()
        {
            if (string.IsNullOrEmpty(_activeCharacterId))
            {
                return;
            }
            if (_characterIdToInstance.TryGetValue(_activeCharacterId, out var go) && go != null)
            {
                Destroy(go);
            }
            _characterIdToInstance.Remove(_activeCharacterId);
            _activeCharacterId = null;
        }

        /// <summary>
        /// 지정한 캐릭터 ID의 모델을 활성 모델로 전환한다.
        /// </summary>
        public void SetActiveModel(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return;
            }
            if (!_characterIdToInstance.TryGetValue(characterId, out var target))
            {
                return;
            }
            foreach (var kv in _characterIdToInstance)
            {
                if (kv.Value != null)
                {
                    kv.Value.SetActive(false);
                }
            }
            target.SetActive(true);
            _activeCharacterId = characterId;
        }

        /// <summary>
        /// 현재 활성 모델 GameObject를 반환한다.
        /// </summary>
        public GameObject GetActiveModel()
        {
            if (string.IsNullOrEmpty(_activeCharacterId))
            {
                return null;
            }
            _characterIdToInstance.TryGetValue(_activeCharacterId, out var go);
            return go;
        }

        /// <summary>
        /// 지정한 캐릭터 ID의 모델이 로드되어 있는지 반환한다.
        /// </summary>
        public bool HasModel(string characterId)
        {
            return !string.IsNullOrEmpty(characterId) && _characterIdToInstance.ContainsKey(characterId);
        }

        /// <summary>
        /// 지정한 캐릭터 ID의 모델을 사전 로드한다.
        /// </summary>
        public UniTask PreloadModelAsync(string characterId)
        {
            return LoadModelAsync(characterId).AsUniTask();
        }

        /// <summary>
        /// 활성 모델의 가시성을 설정한다.
        /// </summary>
        public void SetVisibility(bool isVisible)
        {
            var active = GetActiveModel();
            if (active == null)
            {
                return;
            }
            active.SetActive(isVisible);
        }

        /// <summary>
        /// 활성 모델에 캐릭터별 Live2DCharacterConfig를 적용한다.
        /// </summary>
        public void ApplyCharacterConfig(Live2DCharacterConfig characterConfig)
        {
            var active = GetActiveModel();
            if (active == null || characterConfig == null)
            {
                return;
            }

            var lookController = active.GetComponent<CubismLookController>();
            if (lookController != null && _cubismLookTarget != null)
            {
                lookController.Target = _cubismLookTarget.gameObject;
                lookController.Damping = characterConfig.LockAtDamping;
                _cubismLookTarget.Initialize(ToModelConfigProxy(characterConfig));
            }

            var mouthInput = active.GetComponent<CubismAudioMouthInput>();
            if (mouthInput != null && _voiceSource != null)
            {
                mouthInput.AudioInput = _voiceSource;
                mouthInput.Gain = characterConfig.Gain;
                mouthInput.Smoothing = characterConfig.Smoothing;
            }

            var hitHandler = active.GetComponent<CubismHitHandler>();
            if (hitHandler != null)
            {
                hitHandler.Initialize();
                if (_expressionChangeBtn != null)
                {
                    _expressionChangeBtn.onClick.RemoveAllListeners();
                    _expressionChangeBtn.onClick.AddListener(hitHandler.ExpressionChange_Btn);
                }
            }
        }

        /// <summary>
        /// Live2DCharacterConfig를 ModelConfig로 임시 변환한다. (CubismLookTarget.Initialize 호환성)
        /// </summary>
        private ModelConfig ToModelConfigProxy(Live2DCharacterConfig cfg)
        {
            // ModelConfig의 필드가 private이므로 리플렉션을 사용하여 설정
            var proxy = ScriptableObject.CreateInstance<ModelConfig>();
            
            // 리플렉션을 사용하여 private 필드에 값 설정
            var type = typeof(ModelConfig);
            
            var modelNameField = type.GetField("modelName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            modelNameField?.SetValue(proxy, cfg.CharacterName);
            
            var modelDescriptionField = type.GetField("modelDescription", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            modelDescriptionField?.SetValue(proxy, cfg.CharacterDescription);
            
            var thumbnailField = type.GetField("thumbnail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            thumbnailField?.SetValue(proxy, cfg.Thumbnail);
            
            var lookSensitivityField = type.GetField("lookSensitivity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lookSensitivityField?.SetValue(proxy, cfg.LookSensitivity);
            
            var lockAtDampingField = type.GetField("lockAtDamping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lockAtDampingField?.SetValue(proxy, cfg.LockAtDamping);
            
            var isLockAtActiveField = type.GetField("isLockAtActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isLockAtActiveField?.SetValue(proxy, cfg.IsLockAtActive);
            
            var gainField = type.GetField("gain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            gainField?.SetValue(proxy, cfg.Gain);
            
            var smoothingField = type.GetField("smoothing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            smoothingField?.SetValue(proxy, cfg.Smoothing);
            
            var modelPrefabField = type.GetField("modelPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            modelPrefabField?.SetValue(proxy, cfg.CharacterPrefab);
            
            return proxy;
        }
    }
}


