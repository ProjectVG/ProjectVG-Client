using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectVG.Domain.Character.Live2D.Model
{
    [CreateAssetMenu(fileName = "Live2DCharacterConfig", menuName = "ProjectVG/Live2D/CharacterConfig", order = 100)]
    public class Live2DCharacterConfig : ScriptableObject
    {
        [Serializable]
        public class EmotionMapping
        {
            public string emotionKey;
            public string expressionName;
            public float defaultIntensity = 0.5f;
            public int defaultDurationMs = 2000;
        }

        [Serializable]
        public class ActionMapping
        {
            public string actionKey;
            public string motionGroup;
            public string motionName;
        }

        public string characterId;
        public GameObject characterPrefab;
        public ModelConfig modelConfig;
        public List<EmotionMapping> emotionMappings = new List<EmotionMapping>();
        public List<ActionMapping> actionMappings = new List<ActionMapping>();
    }
}


