using System.Collections.Generic;
using UnityEngine;

namespace ProjectVG.Domain.Character.Live2D.Model
{
    [CreateAssetMenu(fileName = "Live2DModelRegistry", menuName = "ProjectVG/Live2D/ModelRegistry", order = 101)]
    public class Live2DModelRegistry : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string characterId;
            public Live2DCharacterConfig characterConfig;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public bool TryGetConfig(string characterId, out Live2DCharacterConfig config)
        {
            foreach (var e in _entries)
            {
                if (e != null && e.characterId == characterId)
                {
                    config = e.characterConfig;
                    return config != null;
                }
            }
            config = null;
            return false;
        }
    }
}


