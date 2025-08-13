using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectVG.Domain.Character.Live2D.Model;
using Live2D.Cubism.Framework.LookAt;
using Live2D.Cubism.Framework.MouthMovement;

namespace ProjectVG.Domain.Character.Service
{
    public class Live2DModelManager : Singleton<Live2DModelManager>
    {
        [Header("설정")]
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private Live2DModelRegistry _modelRegistry;

        private readonly Dictionary<string, GameObject> _characterIdToInstance = new Dictionary<string, GameObject>();
        private string _activeCharacterId;

        private void Awake()
        {
            base.Awake();
        }

        #region Public Methods

        /// <summary>
        /// 캐릭터 모델을 로드하고 설정을 적용한다.
        /// </summary>
        public GameObject LoadCharacter(string characterId, bool activateImmediately = false)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogWarning("[Live2DModelManager] 캐릭터 ID가 null입니다.");
                return null;
            }

            // 이미 로드된 모델이 있는지 확인
            if (_characterIdToInstance.TryGetValue(characterId, out var existing))
            {
                Debug.Log($"[Live2DModelManager] 캐릭터 '{characterId}'가 이미 로드되어 있습니다.");
                if (activateImmediately)
                {
                    ActivateCharacter(characterId);
                }
                return existing;
            }

            // 설정 가져오기
            var config = GetCharacterConfig(characterId);
            if (config == null)
            {
                Debug.LogError($"[Live2DModelManager] 캐릭터 '{characterId}'의 설정을 찾을 수 없습니다.");
                return null;
            }

            // 모델 인스턴스 생성
            var instance = CreateModelInstance(config, characterId);
            if (instance == null)
            {
                return null;
            }

            // 설정 적용
            ApplyCharacterConfig(instance, config);

            // 딕셔너리에 저장
            _characterIdToInstance[characterId] = instance;
            
            // 즉시 활성화 옵션
            if (activateImmediately)
            {
                ActivateCharacter(characterId);
            }
            
            Debug.Log($"[Live2DModelManager] 캐릭터 '{characterId}' 로드 완료");
            return instance;
        }

        /// <summary>
        /// 캐릭터 모델을 비동기로 로드하고 설정을 적용한다.
        /// </summary>
        public async UniTask<GameObject> LoadCharacterAsync(string characterId, bool activateImmediately = false)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogWarning("[Live2DModelManager] 캐릭터 ID가 null입니다.");
                return null;
            }

            // 이미 로드된 모델이 있는지 확인
            if (_characterIdToInstance.TryGetValue(characterId, out var existing))
            {
                Debug.Log($"[Live2DModelManager] 캐릭터 '{characterId}'가 이미 로드되어 있습니다.");
                if (activateImmediately)
                {
                    ActivateCharacter(characterId);
                }
                return existing;
            }

            // 설정 가져오기
            var config = GetCharacterConfig(characterId);
            if (config == null)
            {
                Debug.LogError($"[Live2DModelManager] 캐릭터 '{characterId}'의 설정을 찾을 수 없습니다.");
                return null;
            }

            // 모델 인스턴스 생성 (비동기)
            var instance = await CreateModelInstanceAsync(config, characterId);
            if (instance == null)
            {
                return null;
            }

            // 설정 적용
            ApplyCharacterConfig(instance, config);

            // 딕셔너리에 저장
            _characterIdToInstance[characterId] = instance;
            
            // 즉시 활성화 옵션
            if (activateImmediately)
            {
                ActivateCharacter(characterId);
            }
            
            Debug.Log($"[Live2DModelManager] 캐릭터 '{characterId}' 비동기 로드 완료");
            return instance;
        }

        /// <summary>
        /// 캐릭터를 활성화한다.
        /// </summary>
        public void ActivateCharacter(string characterId)
        {
            if (!_characterIdToInstance.TryGetValue(characterId, out var target))
            {
                Debug.LogWarning($"[Live2DModelManager] 캐릭터 '{characterId}'가 로드되지 않았습니다.");
                return;
            }

            // 모든 모델 비활성화
            DeactivateAllModels();

            // 대상 모델 활성화
            target.SetActive(true);
            _activeCharacterId = characterId;
            
            Debug.Log($"[Live2DModelManager] 캐릭터 '{characterId}' 활성화");
        }

        /// <summary>
        /// 현재 활성 캐릭터를 반환한다.
        /// </summary>
        public GameObject GetActiveCharacter()
        {
            if (string.IsNullOrEmpty(_activeCharacterId))
            {
                return null;
            }
            
            _characterIdToInstance.TryGetValue(_activeCharacterId, out var character);
            return character;
        }

        /// <summary>
        /// 캐릭터가 로드되어 있는지 확인한다.
        /// </summary>
        public bool HasCharacter(string characterId)
        {
            return !string.IsNullOrEmpty(characterId) && _characterIdToInstance.ContainsKey(characterId);
        }

        /// <summary>
        /// 캐릭터를 언로드한다.
        /// </summary>
        public void UnloadCharacter(string characterId)
        {
            if (!_characterIdToInstance.TryGetValue(characterId, out var instance))
            {
                return;
            }

            if (instance != null)
            {
                Destroy(instance);
            }

            _characterIdToInstance.Remove(characterId);

            // 현재 활성 캐릭터였다면 활성 ID 초기화
            if (_activeCharacterId == characterId)
            {
                _activeCharacterId = null;
            }

            Debug.Log($"[Live2DModelManager] 캐릭터 '{characterId}' 언로드 완료");
        }

        /// <summary>
        /// 모든 캐릭터를 언로드한다.
        /// </summary>
        public void UnloadAllCharacters()
        {
            foreach (var kvp in _characterIdToInstance)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }

            _characterIdToInstance.Clear();
            _activeCharacterId = null;
            
            Debug.Log("[Live2DModelManager] 모든 캐릭터 언로드 완료");
        }

        /// <summary>
        /// 활성 캐릭터의 가시성을 설정한다.
        /// </summary>
        public void SetCharacterVisibility(bool isVisible)
        {
            var active = GetActiveCharacter();
            if (active == null)
            {
                Debug.LogWarning("[Live2DModelManager] 활성 캐릭터가 없습니다.");
                return;
            }

            active.SetActive(isVisible);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 캐릭터 설정을 가져온다.
        /// </summary>
        private Live2DModelConfig GetCharacterConfig(string characterId)
        {
            // 레지스트리에서 먼저 찾기
            if (_modelRegistry != null && _modelRegistry.TryGetConfig(characterId, out var config))
            {
                return config;
            }

            // 설정을 찾을 수 없음
            Debug.LogError($"[Live2DModelManager] 캐릭터 '{characterId}'의 설정을 찾을 수 없습니다. 레지스트리를 확인해주세요.");
            return null;
        }

        /// <summary>
        /// 모델 인스턴스를 생성한다.
        /// </summary>
        private GameObject CreateModelInstance(Live2DModelConfig config, string characterId)
        {
            if (config.CharacterPrefab == null)
            {
                Debug.LogError($"[Live2DModelManager] 캐릭터 '{characterId}'의 프리팹이 null입니다.");
                return null;
            }

            var parent = _modelRoot != null ? _modelRoot : transform;
            var instance = Instantiate(config.CharacterPrefab, parent);
            instance.name = characterId;
            instance.SetActive(false);

            return instance;
        }

        /// <summary>
        /// 모델 인스턴스를 비동기로 생성한다.
        /// </summary>
        private async UniTask<GameObject> CreateModelInstanceAsync(Live2DModelConfig config, string characterId)
        {
            if (config.CharacterPrefab == null)
            {
                Debug.LogError($"[Live2DModelManager] 캐릭터 '{characterId}'의 프리팹이 null입니다.");
                return null;
            }

            var parent = _modelRoot != null ? _modelRoot : transform;
            var instance = Instantiate(config.CharacterPrefab, parent);
            instance.name = characterId;
            instance.SetActive(false);

            // 비동기 작업을 위한 지연 (필요시)
            await UniTask.Yield();

            return instance;
        }

        /// <summary>
        /// 캐릭터 설정을 적용한다.
        /// </summary>
        private void ApplyCharacterConfig(GameObject character, Live2DModelConfig config)
        {
            if (character == null || config == null)
            {
                return;
            }

            ApplyLookAtSettings(character, config);
            ApplyLipSyncSettings(character, config);
            InitializeHitHandler(character);
        }

        /// <summary>
        /// 시선 추적 설정을 적용한다.
        /// </summary>
        private void ApplyLookAtSettings(GameObject character, Live2DModelConfig config)
        {
            var lookController = character.GetComponent<CubismLookController>();
            if (lookController == null)
            {
                Debug.LogWarning($"[Live2DModelManager] CubismLookController를 찾을 수 없습니다: {character.name}");
                return;
            }

            lookController.Target = null; // TODO: 시선 타겟 설정 필요
            lookController.Damping = config.LockAtDamping;
        }

        /// <summary>
        /// 립싱크 설정을 적용한다.
        /// </summary>
        private void ApplyLipSyncSettings(GameObject character, Live2DModelConfig config)
        {
            var mouthInput = character.GetComponent<CubismAudioMouthInput>();
            if (mouthInput == null)
            {
                Debug.LogWarning($"[Live2DModelManager] CubismAudioMouthInput을 찾을 수 없습니다: {character.name}");
                return;
            }

            mouthInput.AudioInput = null; // TODO: 오디오 소스 설정 필요
            mouthInput.Gain = config.Gain;
            mouthInput.Smoothing = config.Smoothing;
        }

        /// <summary>
        /// 터치 핸들러를 초기화한다.
        /// </summary>
        private void InitializeHitHandler(GameObject character)
        {
            var hitHandler = character.GetComponent<CubismHitHandler>();
            if (hitHandler == null)
            {
                Debug.LogWarning($"[Live2DModelManager] CubismHitHandler를 찾을 수 없습니다: {character.name}");
                return;
            }

            hitHandler.Initialize();
        }

        /// <summary>
        /// 모든 모델을 비활성화한다.
        /// </summary>
        private void DeactivateAllModels()
        {
            foreach (var kvp in _characterIdToInstance)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(false);
                }
            }
        }

        #endregion
    }
}


