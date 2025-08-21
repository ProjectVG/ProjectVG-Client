using Cysharp.Threading.Tasks;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.MouthMovement;
using Live2D.Cubism.Core;
using ProjectVG.Core.Audio;
using ProjectVG.Domain.Character.Live2D.Model;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// Live2D 모델 로딩 및 초기화를 담당하는 매니저
	/// </summary>
	public class CharacterModelLoader : MonoBehaviour
	{

		private Live2DModelRegistry _modelRegistry;
		private AudioSource _voiceAudioSource;

		#region Unity Lifecycle

		#endregion

		#region Public Methods

		/// <summary>
		/// 로더를 초기화한다
		/// </summary>
		public void Initialize(Live2DModelRegistry modelRegistry, AudioSource voiceAudioSource)
		{
			_modelRegistry = modelRegistry;
			_voiceAudioSource = voiceAudioSource;
		}

		/// <summary>
		/// 캐릭터 모델을 로드하고 초기화된 인스턴스를 반환한다
		/// </summary>
		/// <param name="characterId">캐릭터 ID</param>
		/// <param name="parent">부모 Transform</param>
		/// <returns>초기화된 모델 인스턴스</returns>
		public async UniTask<GameObject> LoadAndInitializeModelAsync(string characterId, Transform parent = null)
		{
			var config = GetCharacterConfig(characterId);
			if (config == null) {
				Debug.LogError($"[CharacterModelLoader] 캐릭터 '{characterId}'의 설정을 찾을 수 없습니다.");
				return null;
			}

			return await CreateModelInstanceAsync(config, characterId, parent);
		}

		#endregion

		#region Private Methods

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
		private async UniTask<GameObject> CreateModelInstanceAsync(Live2DModelConfig config, string characterId, Transform parent = null)
		{
			if (config == null || config.CharacterPrefab == null) {
				Debug.LogError($"[CharacterModelLoader] 캐릭터 '{characterId}'의 프리팹이 null입니다.");
				return null;
			}

			var targetParent = parent != null ? parent : transform;
			
			var instance = Instantiate(config.CharacterPrefab, targetParent);
			instance.name = characterId;
			instance.SetActive(false);
			
			SetupModelComponents(instance, config);
			await UniTask.Yield();
			return instance;
		}

		/// <summary>
		/// 모델에 필요한 컴포넌트들을 설정한다
		/// </summary>
		private void SetupModelComponents(GameObject modelInstance, Live2DModelConfig config)
		{
			SetupLipSync(modelInstance, config);
			SetupAutoEyeBlink(modelInstance, config);
			SetupActionController(modelInstance);
		}

		/// <summary>
		/// 립싱크 컴포넌트를 설정한다
		/// </summary>
		private void SetupLipSync(GameObject modelInstance, Live2DModelConfig config)
		{
			if (!config.UseLipSync) {
				Debug.Log($"[CharacterModelLoader] 립싱크가 비활성화되어 있습니다: {modelInstance.name}");
				return;
			}

			var mouthController = modelInstance.GetComponent<CubismAudioMouthInput>();
			if (mouthController == null) {
				mouthController = modelInstance.AddComponent<CubismAudioMouthInput>();
				Debug.Log($"[CharacterModelLoader] CubismAudioMouthInput 컴포넌트를 추가했습니다: {modelInstance.name}");
			}

			if (_voiceAudioSource == null) {
				Debug.LogWarning($"[CharacterModelLoader] Voice AudioSource가 null입니다. 립싱크가 동작하지 않을 수 있습니다: {modelInstance.name}");
			} else {
				Debug.Log($"[CharacterModelLoader] Voice AudioSource 설정 완료: {modelInstance.name}, AudioSource: {_voiceAudioSource.name}");
			}

			mouthController.AudioInput = _voiceAudioSource;
			mouthController.Gain = config.Gain;
			mouthController.Smoothing = config.Smoothing;
			
			// Live2D 모델에 Mouth 파라미터가 있는지 확인
			var model = modelInstance.GetComponent<CubismModel>();
			if (model != null) {
				var mouthParameter = model.Parameters.FindById("ParamMouthOpenY");
				if (mouthParameter != null) {
					Debug.Log($"[CharacterModelLoader] Mouth 파라미터 발견: {mouthParameter.Id}, 현재값: {mouthParameter.Value}");
				} else {
					Debug.LogWarning($"[CharacterModelLoader] Mouth 파라미터(ParamMouthOpenY)를 찾을 수 없습니다: {modelInstance.name}");
				}
			}
			
			Debug.Log($"[CharacterModelLoader] 립싱크 설정 완료: {modelInstance.name}, Gain: {config.Gain}, Smoothing: {config.Smoothing}");
		}

		/// <summary>
		/// 자동 눈 깜빡임 컴포넌트를 설정한다
		/// </summary>
		private void SetupAutoEyeBlink(GameObject modelInstance, Live2DModelConfig config)
		{
			if (!config.UseAutoEyeBlink) {
				return;
			}

			var eyeBlinkController = modelInstance.GetComponent<CubismAutoEyeBlinkInput>();
			if (eyeBlinkController == null) {
				eyeBlinkController = modelInstance.AddComponent<CubismAutoEyeBlinkInput>();
			}
			
			eyeBlinkController.Mean = config.EyeBlinkMean;
			eyeBlinkController.MaximumDeviation = config.EyeBlinkMaximumDeviation;
			eyeBlinkController.Timescale = config.EyeBlinkTimescale;
			eyeBlinkController.SetBlinkingSettings(
				config.EyeBlinkClosingSeconds,
				config.EyeBlinkClosedSeconds,
				config.EyeBlinkOpeningSeconds
			);
		}

		/// <summary>
		/// 액션 서비스를 설정한다
		/// </summary>
		private void SetupActionController(GameObject modelInstance)
		{
			var actionService = modelInstance.GetComponent<CharacterActionController>();
			if (actionService == null) {
				actionService = modelInstance.AddComponent<CharacterActionController>();
			}

			var animator = modelInstance.GetComponent<Animator>();
			if (animator == null) {
                Debug.LogWarning($"[CharacterModelLoader] Animator를 찾을 수 없습니다: {modelInstance.name}");
				return;
            }
            actionService.Initialize(animator);
            Debug.Log($"[CharacterModelLoader] CharacterActionController 초기화 완료: {modelInstance.name}");
        }

		#endregion
	}
}
