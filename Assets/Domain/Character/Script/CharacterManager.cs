using ProjectVG.Domain.Character.Live2D.Model;
using UnityEngine;
using ProjectVG.Domain.Character.Component;
using ProjectVG.Core.Audio;
using ProjectVG.Domain.Chat.Model;
using System.Threading.Tasks;
using System.Threading;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 제어를 단일 진입점으로 제공하는 파사드 구현체.
	/// </summary>
	public class CharacterManager : MonoBehaviour
	{
		[SerializeField] private Transform _modelTransform;
		[SerializeField] private Live2DModelRegistry _modelRegistry;

		private CharacterModelLoader _modelLoader;
		private GameObject _currentCharacter;
		private ICharacterActionController _actionController;
		private int _loadVersion = 0;

        #region Unity Lifecycle
		void Start()
		{
			Initialize();
        }

        void OnDestroy()
        {
			Shutdown();
        }
        public void Initialize()
        {
            _modelLoader = GetComponent<CharacterModelLoader>();
            if (_modelLoader == null) {
                _modelLoader = gameObject.AddComponent<CharacterModelLoader>();
                Debug.Log($"[CharacterManager] CharacterModelLoader가 자동으로 추가되었습니다: {gameObject.name}");
            }

            if (_modelRegistry == null)
            {
                Debug.LogError("[CharacterManager] Live2DModelRegistry가 할당되지 않았습니다. Inspector에서 설정하세요.");
                return;
            }

            // AudioManager에서 Voice AudioSource 가져오기
            var voiceAudioSource = GetVoiceAudioSource();

            _modelLoader.Initialize(_modelRegistry, voiceAudioSource);

            // 임시로 zero 캐릭터 로드
            LoadCharacter("zero");

        }
        public void Shutdown()
        {
            UnloadCurrentCharacter();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 캐릭터를 로드하고 활성화한다.
        /// </summary>
        public async Task LoadCharacter(string characterId)
		{
			if (_modelLoader == null) return;

			// 요청 버전 증가(새 요청 식별자)
			var requestId = Interlocked.Increment(ref _loadVersion);

			// 기존 캐릭터 제거
			UnloadCurrentCharacter();

			// 새 캐릭터 로드
			var newCharacter = await _modelLoader.LoadAndInitializeModelAsync(characterId, _modelTransform);

			// 더 최신 요청이 진행되었다면 현재 결과는 폐기
			if (requestId != _loadVersion)
			{
				if (newCharacter != null) Destroy(newCharacter);
				return;
			}

			if (newCharacter != null)
			{
				_currentCharacter = newCharacter;
				// ICharacterActionController 인터페이스를 구현하는 컴포넌트를 찾기
				_actionController = _currentCharacter.GetComponent<ICharacterActionController>();
				if (_actionController == null)
				{
					// 구현체들을 직접 확인 (fallback)
					_actionController = _currentCharacter.GetComponent<Live2DCharacterActionController>();
					if (_actionController == null)
					{
						_actionController = _currentCharacter.GetComponent<AnimatorCharacterActionController>();
					}
				}
				
				_currentCharacter.SetActive(true);
				Debug.Log($"[CharacterManager] 캐릭터 로드 완료: {characterId}, ActionController: {_actionController?.GetType().Name ?? "None"}");
			}
		}

		/// <summary>
		/// 현재 캐릭터를 언로드한다.
		/// </summary>
		public void UnloadCurrentCharacter()
		{
			if (_currentCharacter != null)
			{
				Destroy(_currentCharacter);
				_currentCharacter = null;
				_actionController = null;
			}
		}

		/// <summary>
		/// 액션을 실행한다.
		/// </summary>
		/// <param name="actionData">액션 데이터</param>
		public void PlayAction(CharacterActionData actionData)
		{
			if (_actionController != null && actionData.HasAction())
			{
				_actionController.PlayAction(actionData.ActionType);
			}
		}

		/// <summary>
		/// 현재 액션을 중지한다.
		/// </summary>
		public void StopCurrentAction()
		{
			_actionController?.StopCurrentAction();
		}

		/// <summary>
		/// 액션이 재생 중인지 확인한다.
		/// </summary>
		/// <returns>액션 재생 중이면 true</returns>
		public bool IsActionPlaying()
		{
			return _actionController?.IsPlaying() ?? false;
		}

		/// <summary>
		/// 현재 캐릭터가 로드되어 있는지 확인한다.
		/// </summary>
		/// <returns>캐릭터가 로드되어 있으면 true</returns>
		public bool HasCharacter()
		{
			return _currentCharacter != null;
		}

        #endregion

        #region Private Methods

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

        #endregion
    }
}


