using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Live2D.Cubism.Framework.MotionFade;

namespace ProjectVG.Domain.Character.Live2D.Model
{
    [CreateAssetMenu(fileName = "Live2DModelConfig", menuName = "ProjectVG/Live2D/ModelConfig", order = 100)]
    public class Live2DModelConfig : ScriptableObject
    {
        public enum MotionEndBehavior
        {
            [Tooltip("애니메이션 종료 후 멈춤")]
            Stop,
            [Tooltip("같은 그룹의 다른 애니메이션 반복")]
            Loop,
            [Tooltip("자동으로 Idle 상태로 복귀")]
            ReturnToIdle
        }

        public enum ActionControllerType
        {
            [Tooltip("Live2D CubismMotionController 기반 제어")]
            Live2D,
            [Tooltip("Unity Animator 기반 제어")]
            Animator
        }

        [Serializable]
        public class MotionClipMapping
        {
            [Tooltip("클립 ID (읽기 전용)")]
            [SerializeField, ReadOnly] private string id;

            [Tooltip("AnimationClip 파일")]
            public AnimationClip animationClip;

            [Tooltip("모션 그룹 이름 (파일명에서 자동 추출됨)")]
            [SerializeField] private string motionGroup;

            [Tooltip("애니메이션 종료 후 행동")]
            public MotionEndBehavior endBehavior = MotionEndBehavior.ReturnToIdle;

            #region Fild Auto
           
            public string Id {
                get {
                    if (animationClip != null && string.IsNullOrEmpty(id)) {
                        id = animationClip.name;
                        UpdateMotionGroupFromFileName();
                    }
                    return id;
                }
            }

            public string MotionGroup {
                get {
                    if (animationClip != null && string.IsNullOrEmpty(motionGroup)) {
                        UpdateMotionGroupFromFileName();
                    }
                    return motionGroup;
                }
                set => motionGroup = value;
            }

            /// <summary>
            /// 파일명에서 모션 그룹을 자동 추출한다.
            /// 예: "idle-001" -> "idle", "talk-002" -> "talk"
            /// </summary>
            private void UpdateMotionGroupFromFileName()
            {
                if (animationClip == null) return;

                string fileName = animationClip.name;

                if (fileName.Contains("-")) {
                    motionGroup = fileName.Split('-')[0].ToLower();
                }
                else if (fileName.Contains("_")) {
                    motionGroup = fileName.Split('_')[0].ToLower();
                }
                else {
                    motionGroup = fileName.ToLower();
                }
            }

            /// <summary>
            /// AnimationClip이 변경될 때 호출되는 메서드 (Inspector에서)
            /// </summary>
            public void OnValidate()
            {
                if (animationClip != null) {
                    // 파일명, 모션 그룹 자동 업데이트
                    id = animationClip.name;
                    UpdateMotionGroupFromFileName();
                }
            }

            #endregion
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

        [Space(5)]
        [Header("[ Action Controller 설정 ]")]
        [Space(2)]
        [Tooltip("액션 컨트롤러 타입")]
        [SerializeField] private ActionControllerType actionControllerType = ActionControllerType.Live2D;

        [Tooltip("Unity Animator Controller (Animator 타입 사용 시)")]
        [SerializeField] private RuntimeAnimatorController animatorController;

        [Header("[ Motion 클립 설정 ]")]
        [Space(2)]
        [Tooltip("AnimationClip 목록 (Live2D 타입 사용 시)")]
        [SerializeField] private List<MotionClipMapping> motionClips = new List<MotionClipMapping>();

        [Tooltip("CubismFadeMotionList (Live2D 타입 사용 시, 선택사항)")]
        [SerializeField] private CubismFadeMotionList fadeMotionList;

        [Header("[ Auto Idle 설정 ]")]
        [Space(2)]
        [Tooltip("자동 Idle 모션 재생 여부")]
        [SerializeField] private bool enableAutoIdle = true;

        [Tooltip("Auto Idle 모션 변경 간격 (초)")]
        [Range(2f, 30f)]
        [SerializeField] private float autoIdleInterval = 5f;

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

        // Action Controller 설정
        public ActionControllerType ActionControllerMode => actionControllerType;
        public RuntimeAnimatorController AnimatorController => animatorController;

        // Motion 클립
        public List<MotionClipMapping> MotionClips => motionClips;
        public CubismFadeMotionList FadeMotionList => fadeMotionList;

        // Auto Idle 설정
        public bool EnableAutoIdle => enableAutoIdle;
        public float AutoIdleInterval => autoIdleInterval;

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

        /// <summary>
        /// Inspector에서 값이 변경될 때 자동으로 호출
        /// </summary>
        private void OnValidate()
        {
            if (motionClips != null) {
                foreach (var clip in motionClips) {
                    if (clip != null) {
                        clip.OnValidate();
                    }
                }
            }
        }
    }
}


