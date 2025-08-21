using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectVG.Domain.Character.Live2D.Model
{
    [CreateAssetMenu(fileName = "Live2DModelConfig", menuName = "ProjectVG/Live2D/ModelConfig", order = 100)]
    public class Live2DModelConfig : ScriptableObject
    {
        [Serializable]
        public class EmotionMapping
        {
            [Header("감정 설정")]
            [Tooltip("감정 키입니다. 서버에서 전송되는 감정 값과 일치해야 합니다.")]
            public string emotionKey;
            
            [Tooltip("Live2D Expression 이름입니다. 모델의 표정 파일명과 일치해야 합니다.")]
            public string expressionName;
            
            [Header("기본값")]
            [Tooltip("감정의 기본 강도입니다. (0.0 ~ 1.0)")]
            [Range(0f, 1f)]
            public float defaultIntensity = 0.5f;
            
            [Tooltip("감정의 기본 지속시간입니다. (밀리초)")]
            [Range(500, 10000)]
            public int defaultDurationMs = 2000;
        }

        [Serializable]
        public class ActionMapping
        {
            [Header("행동 설정")]
            [Tooltip("행동 키입니다. 서버에서 전송되는 행동 값과 일치해야 합니다.")]
            public string actionKey;
            
            [Tooltip("Live2D 모션 그룹 이름입니다.")]
            public string motionGroup;
            
            [Tooltip("Live2D 모션 파일 이름입니다.")]
            public string motionName;
        }

        [Header("[ 캐릭터 기본정보 ]")]
        [Space(5)]
        [Tooltip("캐릭터 고유 ID")]
        [SerializeField] private string characterId;

        [Tooltip("캐릭터 이름")]
        [SerializeField] private string characterName;
        
        [Tooltip("Live2D 캐릭터 프리팹 (Cubism 모델 포함)")]
        [SerializeField] private GameObject characterPrefab;

        
        [Tooltip("캐릭터 썸네일 이미지")]
        [SerializeField] private Texture2D thumbnail;
        
        [Tooltip("캐릭터 설명")]
        [SerializeField, Multiline] private string characterDescription;

        [Space(5)]
        [Header("────────────────────────────────────────")]
        [Header("[ 행동/표정 ]")]
        [Space(2)]
        [Tooltip("감정 → Live2D Expression 매핑")]
        [SerializeField] private List<EmotionMapping> emotionMappings = new List<EmotionMapping>();
        
        [Tooltip("행동 → Live2D Motion 매핑")]
        [SerializeField] private List<ActionMapping> actionMappings = new List<ActionMapping>();
        
        [Space(5)]
        [Header("────────────────────────────────────────")]
        [Header("[ 시선 설정 ]")]
        [Space(2)]

        [Tooltip("시선 추적 사용 여부")]
        [SerializeField] private bool isLookAtActive = true;
        
        [Tooltip("시선 민감도 (값이 클수록 회전이 커짐)")]
        [Range(0f, 30f)]
        [SerializeField] private float lookSensitivity = 1.0f;
        
        [Tooltip("시선 반응 속도 (값이 작을수록 빠름)")]
        [Range(0f, 5f)]
        [SerializeField, FormerlySerializedAs("lockAtDamping")]
        private float lookAtDamping = 0.0f;

        [Space(5)]
        [Header("────────────────────────────────────────")]
        [Header("[ 립싱크 설정 ]")]
        [Space(2)]

        [Tooltip("립싱크 사용 여부")]
        [SerializeField] private bool useLipSync = true;

        [Tooltip("음량 배수 (1 = 기본)")]
        [Range(1f, 10f)]
        [SerializeField] private float gain = 1f;
        
        [Tooltip("입 움직임 부드러움 (값이 클수록 부드럽지만 부하 증가)")]
        [Range(0f, 1f)]
        [SerializeField] private float smoothing = 1f;

        [Space(5)]
        [Header("────────────────────────────────────────")]
        [Header("[ 자동 애니메이션 설정 ]")]
        [Space(2)]

        [Tooltip("자동 눈 깜빡임 사용 여부")]
        [SerializeField] private bool useAutoEyeBlink = true;

        [Header("눈 깜빡임 타이밍 설정")]
        [Tooltip("눈 깜빡임 간격의 평균 시간 (초)")]
        [Range(1f, 10f)]
        [SerializeField] private float eyeBlinkMean = 2.5f;
        
        [Tooltip("평균에서의 최대 편차 (초)")]
        [Range(0.5f, 5f)]
        [SerializeField] private float eyeBlinkMaximumDeviation = 2f;
        
        [Tooltip("눈 깜빡임 시간 스케일")]
        [Range(1f, 20f)]
        [SerializeField] private float eyeBlinkTimescale = 10f;

        [Header("눈 깜빡임 동작 세부 설정")]
        [Tooltip("눈을 감는 동작 시간 (초)")]
        [Range(0.1f, 3f)]
        [SerializeField] private float eyeBlinkClosingSeconds = 1.0f;
        
        [Tooltip("눈이 감긴 상태 지속 시간 (초)")]
        [Range(0.1f, 2f)]
        [SerializeField] private float eyeBlinkClosedSeconds = 0.5f;
        
        [Tooltip("눈을 여는 동작 시간 (초)")]
        [Range(0.1f, 3f)]
        [SerializeField] private float eyeBlinkOpeningSeconds = 1.5f;

        // 캐릭터 기본정보
        public string CharacterId => characterId;
        public string CharacterName => characterName;
        public GameObject CharacterPrefab => characterPrefab;
        public Texture2D Thumbnail => thumbnail;
        public string CharacterDescription => characterDescription;

        public string ModelId => characterId;
        public string ModelName => characterName;
        public GameObject ModelPrefab => characterPrefab;
        public string ModelDescription => characterDescription;

        // 행동/표정
        public List<EmotionMapping> EmotionMappings => emotionMappings;
        public List<ActionMapping> ActionMappings => actionMappings;

        // 세부 설정
        public bool IsLookAtActive => isLookAtActive;
        public float LookSensitivity => lookSensitivity;
        [Obsolete("Use LookAtDamping instead.")]
        public float LockAtDamping => lookAtDamping;
        public float LookAtDamping => lookAtDamping;
        public float Gain => gain;
        public float Smoothing => smoothing;
        public bool UseLipSync => useLipSync;
        public bool UseAutoEyeBlink => useAutoEyeBlink;
        
        // 눈 깜빡임 설정
        public float EyeBlinkMean => eyeBlinkMean;
        public float EyeBlinkMaximumDeviation => eyeBlinkMaximumDeviation;
        public float EyeBlinkTimescale => eyeBlinkTimescale;
        public float EyeBlinkClosingSeconds => eyeBlinkClosingSeconds;
        public float EyeBlinkClosedSeconds => eyeBlinkClosedSeconds;
        public float EyeBlinkOpeningSeconds => eyeBlinkOpeningSeconds;
    }
}


