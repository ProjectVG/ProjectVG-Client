using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Domain.Character.Service
{
    public class Live2DModelManager : MonoBehaviour, ILive2DModelManager
    {
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private ProjectVG.Domain.Character.Live2D.Model.Live2DModelRegistry _modelRegistry;
        [SerializeField] private ProjectVG.Domain.Character.Live2D.Model.Live2DCharacterConfig _defaultCharacterConfig;

        private readonly Dictionary<string, GameObject> _characterIdToInstance = new Dictionary<string, GameObject>();
        private string _activeCharacterId;

        /** Live2D 모델 관리를 위한 초기 설정을 수행한다. */
        public void Initialize(ProjectVG.Domain.Character.Live2D.Model.Live2DModelRegistry modelRegistry, ProjectVG.Domain.Character.Live2D.Model.Live2DCharacterConfig defaultCharacterConfig)
        {
        }

        /** 지정한 캐릭터 모델을 비동기 로드한다. */
        public UniTask<GameObject> LoadModelAsync(string characterId)
        {
            return default;
        }

        /** 현재 활성 모델을 언로드한다. */
        public void UnloadActiveModel()
        {
        }

        /** 지정한 캐릭터 ID의 모델을 활성 모델로 전환한다. */
        public void SetActiveModel(string characterId)
        {
        }

        /** 현재 활성 모델 GameObject를 반환한다. */
        public GameObject GetActiveModel()
        {
            return null;
        }

        /** 지정한 캐릭터 ID의 모델이 로드되어 있는지 반환한다. */
        public bool HasModel(string characterId)
        {
            return false;
        }

        /** 지정한 캐릭터 ID의 모델을 사전 로드한다. */
        public UniTask PreloadModelAsync(string characterId)
        {
            return default;
        }

        /** 활성 모델의 가시성을 설정한다. */
        public void SetVisibility(bool isVisible)
        {
        }

        /** 활성 모델에 캐릭터별 ModelConfig를 적용한다. */
        public void ApplyModelConfig(ModelConfig modelConfig)
        {
        }
    }
}


