using ProjectVG.Domain.Character.Live2D.Model;
using UnityEngine;
using UnityEngine.TextCore.Text;
using ProjectVG.Domain.Character.Component;
using ProjectVG.Core.Audio;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 제어를 단일 진입점으로 제공하는 파사드 구현체.
	/// </summary>
	public class CharacterManager : MonoBehaviour, ICharacterManager
	{
		[SerializeField] private Transform _modelTransform;
		[SerializeField] private Live2DModelRegistry _modelRegistry;

		private ICharacterModelManager _modelManager;
		private ICharacterActionService _actionService;
		private ICharacterActionResolver _actionResolver;

        #region Unity Lifecycle

		void Start()
		{
			Initialize();
        }

        #endregion


        		/// <summary>
		/// 파사드를 초기화한다.
		/// </summary>
		public void Initialize()
		{
			_modelManager = GetComponent<CharacterModelManager>();
			if (_modelManager == null)
			{
				_modelManager = gameObject.AddComponent<CharacterModelManager>();
				Debug.Log($"[CharacterManager] CharacterModelManager가 자동으로 추가되었습니다: {gameObject.name}");
			}
            
            // AudioManager에서 Voice AudioSource 가져오기
            var voiceAudioSource = GetVoiceAudioSource();
            
            _modelManager.Initialize(_modelTransform, _modelRegistry, voiceAudioSource);
            
            _modelManager.LoadModel("zero", true); // 임시 활성화

            if (_modelTransform != null) {
                var scaler = _modelTransform.GetComponent<Live2DModelScaler>();
                if (scaler == null) {
                    scaler = _modelTransform.gameObject.AddComponent<Live2DModelScaler>();
                    Debug.Log($"[CharacterManager] Live2DModelScaler가 자동으로 추가되었습니다: {_modelTransform.name}");
                }
            }
        }


		/// <summary>
		/// 캐릭터를 등록한다.
		/// </summary>
		public void RegisterCharacter(string characterId, bool preload = true)
		{
			if (_modelManager != null) {
				_modelManager.LoadModel(characterId, preload);
			}
		}

		/// <summary>
		/// 캐릭터 등록을 해제한다.
		/// </summary>
		public void UnregisterCharacter(string characterId)
		{
			if (_modelManager != null) {
				_modelManager.UnloadModel(characterId);
			}
		}

		/// <summary>
		/// 활성 캐릭터를 설정한다.
		/// </summary>
		public void SetActiveCharacter(string characterId)
		{
			if (_modelManager != null) {
				_modelManager.ActivateModel(characterId);
			}
		}

		/// <summary>
		/// 활성 캐릭터를 해제한다.
		/// </summary>
		public void ClearActiveCharacter()
		{
			if (_modelManager != null) {
				_modelManager.DeactivateModel();
			}
		}

		/// <summary>
		/// 액션을 적용한다.
		/// </summary>
		public CharacterActionHandle ApplyAction(CharacterActionRequest request)
		{
			// TODO: 액션 서비스 구현 후 연결
			Debug.LogWarning($"[CharacterManager] ApplyAction 아직 구현되지 않음: {request.ActionKey}");
			return new CharacterActionHandle("not_implemented", CharacterActionStatus.Error);
		}

		/// <summary>
		/// 액션을 취소한다.
		/// </summary>
		public void CancelAction(string actionId)
		{
			// TODO: 액션 서비스 구현 후 연결
			Debug.LogWarning($"[CharacterManager] CancelAction 아직 구현되지 않음: {actionId}");
		}

		/// <summary>
		/// 모든 액션을 취소한다.
		/// </summary>
		public void CancelAllActions()
		{
			// TODO: 액션 서비스 구현 후 연결
			Debug.LogWarning("[CharacterManager] CancelAllActions 아직 구현되지 않음");
		}

		/// <summary>
		/// 음성 재생 시작을 알린다.
		/// </summary>
		public void OnVoiceStarted()
		{
			// TODO: 음성 관련 처리 구현
		}

		/// <summary>
		/// 음성 재생 종료를 알린다.
		/// </summary>
		public void OnVoiceFinished()
		{
			// TODO: 음성 관련 처리 구현
		}

		/// <summary>
		/// 활성 캐릭터 ID를 반환한다.
		/// </summary>
		public string GetActiveCharacterId()
		{
			return _modelManager?.GetActiveModelId();
		}

		/// <summary>
		/// 캐릭터 등록 여부를 반환한다.
		/// </summary>
		public bool IsRegistered(string characterId)
		{
			return _modelManager?.IsLoaded(characterId) ?? false;
		}

        /// <summary>
        /// 파사드를 종료한다.
        /// </summary>
        public void Shutdown()
        {
            if (_modelManager != null) {
                _modelManager.UnloadAll();
            }
        }
        

        
        /// <summary>
        /// AudioManager에서 Voice AudioSource를 가져온다.
        /// </summary>
        private AudioSource GetVoiceAudioSource()
        {
            var audioManager = AudioManager.Instance;
            if (audioManager != null && audioManager.IsInitialized)
            {
                var voiceController = audioManager.GetVoiceController();
                if (voiceController != null)
                {
                    var audioSource = voiceController.GetAudioSource();
                    if (audioSource != null)
                    {
                        Debug.Log("[CharacterManager] Voice AudioSource 가져오기 완료");
                        return audioSource;
                    }
                }
            }
            
            Debug.LogWarning("[CharacterManager] AudioManager가 초기화되지 않았거나 Voice AudioSource를 찾을 수 없습니다.");
            return null;
        }
    }
}


