using Cysharp.Threading.Tasks;
using ProjectVG.Domain.Character.Live2D.Model;
using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
	/// <summary>
	/// 캐릭터 모델의 수명주기를 관리하는 인터페이스
	/// </summary>
	public interface ICharacterModelManager
	{

		/// <summary>
		/// 매니저를 초기화 한다.
		/// </summary>
		/// <param name="modelRoot">모델 위치</param>
		/// <param name="modelRegistry">모델 등록자</param>
		/// <param name="voiceAudioSource">음성 AudioSource (선택사항)</param>
		public void Initialize(Transform modelRoot, Live2DModelRegistry modelRegistry, AudioSource voiceAudioSource);

        /// <summary>
        /// 캐릭터 모델을 로드한다.
        /// </summary>
        /// <param name="characterId">로드할 캐릭터의 고유 ID</param>
        /// <param name="preload">사전 로드 여부 (true면 로드 후 즉시 활성화)</param>
        void LoadModel(string characterId, bool preload = false);

		/// <summary>
		/// 캐릭터 모델을 비동기로 로드한다.
		/// </summary>
		/// <param name="characterId">로드할 캐릭터의 고유 ID</param>
		/// <param name="preload">사전 로드 여부 (true면 로드 후 즉시 활성화)</param>
		UniTask LoadModelAsync(string characterId, bool preload = false);

		/// <summary>
		/// 캐릭터 모델을 언로드한다.
		/// </summary>
		/// <param name="characterId">언로드할 캐릭터의 고유 ID</param>
		void UnloadModel(string characterId);

		/// <summary>
		/// 모든 캐릭터 모델을 언로드한다.
		/// </summary>
		void UnloadAll();

		/// <summary>
		/// 지정된 캐릭터를 활성화한다.
		/// </summary>
		/// <param name="characterId">활성화할 캐릭터의 고유 ID</param>
		void ActivateModel(string characterId);

		/// <summary>
		/// 활성 모델을 비활성화한다.
		/// </summary>
		void DeactivateModel();

		/// <summary>
		/// 캐릭터 로드 여부를 반환한다.
		/// </summary>
		/// <param name="characterId">확인할 캐릭터의 고유 ID</param>
		/// <returns>캐릭터가 로드되어 있으면 true, 아니면 false</returns>
		bool IsLoaded(string characterId);

		/// <summary>
		/// 활성 캐릭터 ID를 반환한다.
		/// </summary>
		/// <returns>활성 캐릭터의 ID, 활성 캐릭터가 없으면 null</returns>
		string GetActiveModelId();
	}
}
