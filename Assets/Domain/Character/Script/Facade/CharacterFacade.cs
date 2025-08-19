using UnityEngine;
using ProjectVG.Domain.Character.Live2D.Model;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 제어를 단일 진입점으로 제공하는 파사드 구현체.
	/// </summary>
	public class CharacterFacade : MonoBehaviour, ICharacterFacade
	{
		[SerializeField] private Transform _modelTransform;
		[SerializeField] private Live2DModelRegistry _modelRegistry;


		private ICharacterModelManager _modelManager;
		private ICharacterActionService _actionService;
		private ICharacterActionResolver _actionResolver;

		/// <summary>
		/// 파사드를 초기화한다.
		/// </summary>
		public void Initialize()
		{
			_modelManager = GetComponent<CharacterModelManager>();
			_modelManager.Initialize(_modelTransform, _modelRegistry);



		}

		

		/// <summary>
		/// 캐릭터를 등록한다.
		/// </summary>
		public void RegisterCharacter(string characterId, bool preload = false)
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
			Debug.LogWarning($"[CharacterFacade] ApplyAction 아직 구현되지 않음: {request.ActionKey}");
			return new CharacterActionHandle("not_implemented", CharacterActionStatus.Error);
		}

		/// <summary>
		/// 액션을 취소한다.
		/// </summary>
		public void CancelAction(string actionId)
		{
			// TODO: 액션 서비스 구현 후 연결
			Debug.LogWarning($"[CharacterFacade] CancelAction 아직 구현되지 않음: {actionId}");
		}

		/// <summary>
		/// 모든 액션을 취소한다.
		/// </summary>
		public void CancelAllActions()
		{
			// TODO: 액션 서비스 구현 후 연결
			Debug.LogWarning("[CharacterFacade] CancelAllActions 아직 구현되지 않음");
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
    }
}

