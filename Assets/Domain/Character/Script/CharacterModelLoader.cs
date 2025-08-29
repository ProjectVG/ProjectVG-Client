 using Cysharp.Threading.Tasks;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.MouthMovement;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.MotionFade;
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
		/// Animator Controller를 제거한다 (Live2D와 충돌 방지)
		/// </summary>
		private void ClearAnimatorController(GameObject modelInstance)
		{
			var animator = modelInstance.GetComponent<Animator>();
			if (animator != null && animator.runtimeAnimatorController != null)
			{
				Debug.LogWarning($"[CharacterModelLoader] Animator Controller가 설정되어 있습니다. Live2D 호환성을 위해 제거합니다: {modelInstance.name}");
				animator.runtimeAnimatorController = null;
				Debug.Log($"[CharacterModelLoader] Animator Controller 제거 완료: {modelInstance.name}");
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
			// Animator Controller 제거 (Live2D와 충돌)
			ClearAnimatorController(modelInstance);
			
			SetupLipSync(modelInstance, config);
			SetupAutoEyeBlink(modelInstance, config);
			SetupMotionController(modelInstance, config);
			SetupExpressionController(modelInstance, config);
			SetupFadeController(modelInstance, config);
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

			var mouthController = modelInstance.GetComponent<CubismMouthController>();
			if (mouthController == null) {
				mouthController = modelInstance.AddComponent<CubismMouthController>();
				Debug.Log($"[CharacterModelLoader] CubismMouthController 컴포넌트를 추가했습니다: {modelInstance.name}");
			}
            mouthController.BlendMode = CubismParameterBlendMode.Additive;

            var mouthInputController = modelInstance.GetComponent<CubismAudioMouthInput>();
			if (mouthInputController == null) {
                mouthInputController = modelInstance.AddComponent<CubismAudioMouthInput>();
				Debug.Log($"[CharacterModelLoader] CubismAudioMouthInput 컴포넌트를 추가했습니다: {modelInstance.name}");
			}

			if (_voiceAudioSource == null) {
				Debug.LogWarning($"[CharacterModelLoader] Voice AudioSource가 null입니다. 립싱크가 동작하지 않을 수 있습니다: {modelInstance.name}");
			} else {
				Debug.Log($"[CharacterModelLoader] Voice AudioSource 설정 완료: {modelInstance.name}, AudioSource: {_voiceAudioSource.name}");
                mouthInputController.AudioInput = _voiceAudioSource;
			}
            mouthInputController.Gain = config.Gain;
            mouthInputController.Smoothing = config.Smoothing;
			
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
		/// Motion 컨트롤러를 설정한다
		/// </summary>
		private void SetupMotionController(GameObject modelInstance, Live2DModelConfig config)
		{
			var motionController = modelInstance.GetComponent<CubismMotionController>();
			if (motionController == null) {
				motionController = modelInstance.AddComponent<CubismMotionController>();
			}

			Debug.Log($"[CharacterModelLoader] CubismMotionController 설정 완료: {modelInstance.name}");
		}

		/// <summary>
		/// Expression 컨트롤러를 설정한다
		/// </summary>
		private void SetupExpressionController(GameObject modelInstance, Live2DModelConfig config)
		{
			var expressionController = modelInstance.GetComponent<CubismExpressionController>();
			if (expressionController == null) {
				expressionController = modelInstance.AddComponent<CubismExpressionController>();
			}

			Debug.Log($"[CharacterModelLoader] CubismExpressionController 설정 완료: {modelInstance.name}");
		}

		/// <summary>
		/// Fade 컨트롤러를 설정한다
		/// </summary>
		private void SetupFadeController(GameObject modelInstance, Live2DModelConfig config)
		{
			var fadeController = modelInstance.GetComponent<CubismFadeController>();
			if (fadeController == null) {
				fadeController = modelInstance.AddComponent<CubismFadeController>();
				Debug.Log($"[CharacterModelLoader] CubismFadeController 컴포넌트를 추가했습니다: {modelInstance.name}");
			}

			if (config.FadeMotionList != null) {
				fadeController.CubismFadeMotionList = config.FadeMotionList;
				Debug.Log($"[CharacterModelLoader] CubismFadeMotionList 설정 완료: {modelInstance.name}");
			}
			else {
				Debug.LogWarning($"[CharacterModelLoader] CubismFadeMotionList가 설정되지 않았습니다. Live2D 모션 페이드가 제대로 작동하지 않을 수 있습니다: {modelInstance.name}");
				Debug.LogWarning($"해결방법: Live2DModelConfig에서 'Cubism Fade Motion List' 필드를 설정하거나, Live2D 모델에 포함된 .fadeMotionList 에셋을 할당하세요.");
			}
			
			// Fade Controller 리프레시 (컴포넌트 초기화)
			fadeController.Refresh();
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

			var motionController = modelInstance.GetComponent<CubismMotionController>();
			if (motionController == null) {
				Debug.LogWarning($"[CharacterModelLoader] CubismMotionController를 찾을 수 없습니다: {modelInstance.name}");
				return;
			}

			// 현재 모델의 Config 찾기
			string modelId = modelInstance.name;
			if (_modelRegistry != null && _modelRegistry.TryGetConfig(modelId, out var config))
			{
				actionService.Initialize(motionController, config.MotionClips, config.EnableAutoIdle, config.AutoIdleInterval);
				Debug.Log($"[CharacterModelLoader] CharacterActionController 초기화 완료: {modelInstance.name}, Motion Clips: {config.MotionClips?.Count ?? 0}개, Auto Idle: {config.EnableAutoIdle}");
			}
			else
			{
				Debug.LogWarning($"[CharacterModelLoader] 모델 '{modelId}'의 Config를 찾을 수 없어 빈 Motion Clips로 초기화합니다.");
				actionService.Initialize(motionController, new System.Collections.Generic.List<Live2DModelConfig.MotionClipMapping>());
			}
		}

		#endregion
	}
}
