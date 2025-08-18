using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using ProjectVG.Domain.Character.Live2D.Model;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 모델의 수명주기를 관리하는 매니저
	/// </summary>
	public class CharacterModelManager : MonoBehaviour, ICharacterModelManager
	{
		#region Private Fields

		private Transform _modelRoot;
		private Live2DModelRegistry _modelRegistry;
		private readonly Dictionary<string, GameObject> _characterIdToInstance = new Dictionary<string, GameObject>();
		private string _activeCharacterId;

		#endregion

		#region Unity Lifecycle

		#endregion

		#region Public Methods

		/// <summary>
		/// 매니저를 초기화한다
		/// </summary>
		public void Initialize(Transform modelRoot, Live2DModelRegistry modelRegistry)
		{
			_modelRoot = modelRoot;
			_modelRegistry = modelRegistry;
		}

		/// <summary>
		/// 캐릭터 모델을 로드한다
		/// </summary>
		public void LoadModel(string characterId, bool activateImmediately = false)
		{
			if (!ValidateLoadRequest(characterId, activateImmediately)) {
				return;
			}

			var config = GetCharacterConfig(characterId);
			if (config == null) {
				Debug.LogError($"[CharacterModelManager] 캐릭터 '{characterId}'의 설정을 찾을 수 없습니다.");
				return;
			}

			var instance = CreateModelInstance(config, characterId);
			if (instance == null) {
				return;
			}

			RegisterModelInstance(characterId, instance, activateImmediately);
		}

		/// <summary>
		/// 캐릭터 모델을 비동기로 로드한다
		/// </summary>
		public async UniTask LoadModelAsync(string characterId, bool activateImmediately = false)
		{
			if (!ValidateLoadRequest(characterId, activateImmediately)) {
				return;
			}

			var config = GetCharacterConfig(characterId);
			if (config == null) {
				Debug.LogError($"[CharacterModelManager] 캐릭터 '{characterId}'의 설정을 찾을 수 없습니다.");
				return;
			}

			var instance = await CreateModelInstanceAsync(config, characterId);
			if (instance == null) {
				return;
			}

			RegisterModelInstance(characterId, instance, activateImmediately);
		}

		/// <summary>
		/// 캐릭터 모델을 언로드한다
		/// </summary>
		public void UnloadModel(string characterId)
		{
			if (!_characterIdToInstance.TryGetValue(characterId, out var instance)) {
				return;
			}

			if (instance != null) {
				Destroy(instance);
			}

			_characterIdToInstance.Remove(characterId);

			if (_activeCharacterId == characterId) {
				_activeCharacterId = null;
			}
		}

		/// <summary>
		/// 모든 캐릭터 모델을 언로드한다
		/// </summary>
		public void UnloadAll()
		{
			foreach (var kvp in _characterIdToInstance) {
				if (kvp.Value != null) {
					Destroy(kvp.Value);
				}
			}

			_characterIdToInstance.Clear();
			_activeCharacterId = null;
		}

		/// <summary>
		/// 지정된 캐릭터를 활성화한다
		/// </summary>
		public void ActivateModel(string characterId)
		{
			if (!_characterIdToInstance.TryGetValue(characterId, out var target)) {
				Debug.LogWarning($"[CharacterModelManager] 캐릭터 '{characterId}'가 로드되지 않았습니다.");
				return;
			}

			DeactivateAllModels();
			
			target.SetActive(true);
			_activeCharacterId = characterId;
		}

		/// <summary>
		/// 활성 모델을 비활성화한다
		/// </summary>
		public void DeactivateModel()
		{
			if (string.IsNullOrEmpty(_activeCharacterId)) {
				return;
			}

			if (_characterIdToInstance.TryGetValue(_activeCharacterId, out var current) && current != null) {
				current.SetActive(false);
			}

			_activeCharacterId = null;
		}

		/// <summary>
		/// 활성 캐릭터 ID를 반환한다
		/// </summary>
		public string GetActiveModelId()
		{
			return _activeCharacterId;
		}

		/// <summary>
		/// 캐릭터 로드 여부를 반환한다
		/// </summary>
		public bool IsLoaded(string characterId)
		{
			return !string.IsNullOrEmpty(characterId) && _characterIdToInstance.ContainsKey(characterId);
		}

		#endregion

		#region Private Methods



		/// <summary>
		/// 로드 요청의 유효성을 검증한다
		/// </summary>
		private bool ValidateLoadRequest(string characterId, bool activateImmediately)
		{
			if (string.IsNullOrEmpty(characterId)) {
				Debug.LogWarning("[CharacterModelManager] 캐릭터 ID가 null이거나 비어있습니다.");
				return false;
			}

			if (_characterIdToInstance.TryGetValue(characterId, out var existing)) {
				if (activateImmediately) {
					ActivateModel(characterId);
				}
				return false;
			}

			return true;
		}

		/// <summary>
		/// 모델 인스턴스를 등록하고 필요시 활성화한다
		/// </summary>
		private void RegisterModelInstance(string characterId, GameObject instance, bool activateImmediately)
		{
			_characterIdToInstance[characterId] = instance;

			if (activateImmediately) {
				ActivateModel(characterId);
			}
		}

		/// <summary>
		/// 캐릭터 설정을 가져온다
		/// </summary>
		private Live2DModelConfig GetCharacterConfig(string characterId)
		{
			if (_modelRegistry != null && _modelRegistry.TryGetConfig(characterId, out var config)) {
				return config;
			}

			return null;
		}

		/// <summary>
		/// 모델 인스턴스를 생성한다
		/// </summary>
		private GameObject CreateModelInstance(Live2DModelConfig config, string characterId)
		{
			if (config == null || config.CharacterPrefab == null) {
				Debug.LogError($"[CharacterModelManager] 캐릭터 '{characterId}'의 프리팹이 null입니다.");
				return null;
			}

			var parent = _modelRoot != null ? _modelRoot : transform;
			
			var instance = Instantiate(config.CharacterPrefab, parent);
			instance.name = characterId;
			instance.SetActive(false);
			return instance;
		}

		/// <summary>
		/// 모델 인스턴스를 비동기로 생성한다
		/// </summary>
		private async UniTask<GameObject> CreateModelInstanceAsync(Live2DModelConfig config, string characterId)
		{
			if (config == null || config.CharacterPrefab == null) {
				Debug.LogError($"[CharacterModelManager] 캐릭터 '{characterId}'의 프리팹이 null입니다.");
				return null;
			}

			var parent = _modelRoot != null ? _modelRoot : transform;
			
			var instance = Instantiate(config.CharacterPrefab, parent);
			instance.name = characterId;
			instance.SetActive(false);
			
			await UniTask.Yield();
			return instance;
		}

		/// <summary>
		/// 모든 모델을 비활성화한다
		/// </summary>
		private void DeactivateAllModels()
		{
			foreach (var kvp in _characterIdToInstance) {
				if (kvp.Value != null) {
					kvp.Value.SetActive(false);
				}
			}
		}

		#endregion
	}
}
