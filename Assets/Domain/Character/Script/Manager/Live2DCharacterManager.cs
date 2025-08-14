using UnityEngine;
using System;
using ProjectVG.Core.Audio;

namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// Live2D 캐릭터의 상위 조정자. 모델 관리자/컨트롤러를 묶어 감정/행동 반응을 일관되게 적용
    /// </summary>
    public class Live2DCharacterManager : MonoBehaviour, ILive2DModelManagerFacade
    {
        [Header("Components")]
        [SerializeField] private Live2DModelManager _modelManager;
        [SerializeField] private EmotionController _emotionController;
        [SerializeField] private ActionController _actionController;
        
        [Header("Settings")]
        [SerializeField] private bool _enableEmotionControl = true;
        [SerializeField] private bool _enableActionControl = true;
        
        private CharacterStateModel _stateModel;
        private AudioManager _audioManager;
        
        private static Live2DCharacterManager _instance;
        public static Live2DCharacterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<Live2DCharacterManager>();
                }
                return _instance;
            }
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            _stateModel = new CharacterStateModel();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        #endregion
        
        #region ILive2DModelManagerFacade Implementation
        
        public void Initialize()
        {
            try
            {
                _audioManager = AudioManager.Instance;
                
                if (_emotionController != null)
                {
                    _emotionController.Initialize();
                }
                
                if (_actionController != null)
                {
                    _actionController.Initialize();
                }
                
                Debug.Log("[Live2DCharacterManager] 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Live2DCharacterManager] 초기화 실패: {ex.Message}");
            }
        }
        
        public void ApplyReaction(EmotionData emotionData, ActionData actionData)
        {
            try
            {
                // Action > Voice Gating > Emotion 우선순위 적용
                if (_enableActionControl && !string.IsNullOrEmpty(actionData.Action))
                {
                    _actionController?.TriggerAction(actionData.Action, actionData.Args);
                    _stateModel.currentAction = actionData.Action;
                    _stateModel.actionPlaying = true;
                    
                    Debug.Log($"[Live2DCharacterManager] 액션 적용: {actionData.Action}");
                }
                
                if (_enableEmotionControl && !string.IsNullOrEmpty(emotionData.Emotion))
                {
                    _emotionController?.SetEmotion(emotionData.Emotion, emotionData.Intensity, emotionData.DurationMs);
                    _stateModel.currentEmotion = emotionData.Emotion;
                    _stateModel.emotionIntensity = emotionData.Intensity;
                    _stateModel.emotionExpireAt = DateTime.Now.AddMilliseconds(emotionData.DurationMs);
                    
                    Debug.Log($"[Live2DCharacterManager] 감정 적용: {emotionData.Emotion} (강도: {emotionData.Intensity})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Live2DCharacterManager] 반응 적용 실패: {ex.Message}");
            }
        }
        
        public void OnVoiceStarted()
        {
            _stateModel.isVoicePlaying = true;
            Debug.Log("[Live2DCharacterManager] 음성 재생 시작");
        }
        
        public void OnVoiceFinished()
        {
            _stateModel.isVoicePlaying = false;
            
            // 음성 재생 완료 시 상태 복원
            if (_stateModel.actionPlaying)
            {
                _stateModel.actionPlaying = false;
                _stateModel.currentAction = null;
                
                // 액션 완료 후 이전 감정 복원
                if (!string.IsNullOrEmpty(_stateModel.currentEmotion))
                {
                    _emotionController?.SetEmotion(_stateModel.currentEmotion, _stateModel.emotionIntensity, 2000);
                }
            }
            
            Debug.Log("[Live2DCharacterManager] 음성 재생 완료, 상태 복원");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 현재 캐릭터 상태를 반환한다.
        /// </summary>
        public CharacterStateModel GetCurrentState()
        {
            return _stateModel;
        }
        
        /// <summary>
        /// 감정을 즉시 해제한다.
        /// </summary>
        public void ClearEmotion()
        {
            _emotionController?.ClearEmotion();
            _stateModel.currentEmotion = null;
            _stateModel.emotionIntensity = 0f;
            _stateModel.emotionExpireAt = DateTime.MinValue;
        }
        
        /// <summary>
        /// 액션을 즉시 중단한다.
        /// </summary>
        public void StopAction()
        {
            _stateModel.actionPlaying = false;
            _stateModel.currentAction = null;
        }
        
        #endregion
        
        #region Character State Model
        
        /// <summary>
        /// 캐릭터 상태를 관리하는 모델
        /// </summary>
        [System.Serializable]
        public class CharacterStateModel
        {
            public string currentEmotion;
            public string pendingEmotion;
            public float emotionIntensity;
            public DateTime emotionExpireAt;
            
            public string currentAction;
            public bool actionPlaying;
            public DateTime actionExpireAt;
            
            public bool isVoicePlaying;
        }
        
        #endregion
    }
}


