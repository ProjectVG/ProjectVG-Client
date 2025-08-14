using UnityEngine;
using ProjectVG.Domain.Character.Live2D.Model;

namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// Live2D 모델에 설정을 적용하는 인터페이스
    /// </summary>
    public interface ILive2DModelApplier
    {
        void Apply(GameObject activeModel, Live2DModelConfig characterConfig);
    }
}
