using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
    public class Live2DModelApplier : MonoBehaviour, ILive2DModelApplier
    {
        [SerializeField] private bool _autoApplyOnEnable = true;

        public void Apply(GameObject activeModel, ModelConfig modelConfig)
        {
        }
    }
}


