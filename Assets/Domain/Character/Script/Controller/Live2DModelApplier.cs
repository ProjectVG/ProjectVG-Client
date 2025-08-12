using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
    public class Live2DModelApplier : MonoBehaviour, ILive2DModelApplier
    {
        [SerializeField] private bool _autoApplyOnEnable = true;

        public void Apply(GameObject activeModel, ProjectVG.Domain.Character.Live2D.Model.Live2DCharacterConfig characterConfig)
        {
        }
    }
}


