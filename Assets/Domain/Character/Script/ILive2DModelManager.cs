using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Domain.Character.Service
{
    public interface ILive2DModelManager
    {
        /** Live2D 모델 관리를 위한 초기 설정을 수행한다. */
        void Initialize(ProjectVG.Domain.Character.Live2D.Model.Live2DModelRegistry modelRegistry, ProjectVG.Domain.Character.Live2D.Model.Live2DCharacterConfig defaultCharacterConfig);

        /** 지정한 캐릭터 모델을 비동기 로드한다. */
        UniTask<GameObject> LoadModelAsync(string characterId);

        /** 현재 활성 모델을 언로드한다. */
        void UnloadActiveModel();

        /** 지정한 캐릭터 ID의 모델을 활성 모델로 전환한다. */
        void SetActiveModel(string characterId);

        /** 현재 활성 모델 GameObject를 반환한다. */
        GameObject GetActiveModel();

        /** 지정한 캐릭터 ID의 모델이 로드되어 있는지 반환한다. */
        bool HasModel(string characterId);

        /** 지정한 캐릭터 ID의 모델을 사전 로드한다. */
        UniTask PreloadModelAsync(string characterId);

        /** 활성 모델의 가시성을 설정한다. */
        void SetVisibility(bool isVisible);

        /** 활성 모델에 캐릭터별 ModelConfig를 적용한다. */
        void ApplyModelConfig(ModelConfig modelConfig);
    }
}


