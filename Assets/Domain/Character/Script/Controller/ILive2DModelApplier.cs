using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
    public interface ILive2DModelApplier
    {
        /** 활성 모델과 구성에 대해 LookAt, LipSync, 썸네일 등 시각 설정을 적용한다. */
        void Apply(GameObject activeModel, ProjectVG.Domain.Character.Live2D.Model.Live2DModelConfig characterConfig);
    }
}


