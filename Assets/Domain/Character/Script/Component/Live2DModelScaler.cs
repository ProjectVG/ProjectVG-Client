using UnityEngine;
using ProjectVG.Domain.Character.Config;

namespace ProjectVG.Domain.Character.Component
{
	/// <summary>
	/// Live2D 모델 크기를 해상도에 맞게 자동 조정하는 컴포넌트
	/// </summary>
	public class Live2DModelScaler : MonoBehaviour
	{
		[Header("설정")]
		[SerializeField] private ResolutionModelScaleConfig scaleConfig;
		[SerializeField] private bool applyOnStart = true;
		[SerializeField] private bool applyOnResolutionChange = true;
		
		[Header("디버그")]
		[SerializeField] private bool showDebugInfo = true;
		
		private Vector2 lastResolution;
		private float currentScale;
		private Vector3 originalScale;
		
		#region Unity Lifecycle
		
		private void Start()
		{
			if (applyOnStart)
			{
				ApplyScale();
			}
			
			// ResolutionManager에 자동 등록
			RegisterToResolutionManager();
		}
		
		private void OnDestroy()
		{
			// ResolutionManager에서 제거
			UnregisterFromResolutionManager();
		}
		
		private void Update()
		{
			if (applyOnResolutionChange && HasResolutionChanged())
			{
				ApplyScale();
			}
		}
		
		#endregion
		
		#region Public Methods
		
		/// <summary>
		/// 현재 해상도에 맞는 스케일을 적용한다.
		/// </summary>
		public void ApplyScale()
		{
			if (scaleConfig == null)
			{
				Debug.LogWarning("[Live2DModelScaler] 스케일 설정이 없습니다.");
				return;
			}
			
			// 현재 해상도에 맞는 스케일 계산
			var scale = scaleConfig.CalculateScale();
			ApplyScaleWithPreCalculatedScale(scale);
		}
		
		/// <summary>
		/// 미리 계산된 스케일을 적용한다.
		/// </summary>
		public void ApplyScaleWithPreCalculatedScale(float scale)
		{
			if (scaleConfig == null)
			{
				Debug.LogWarning("[Live2DModelScaler] 스케일 설정이 없습니다.");
				return;
			}
			
			// 원본 스케일 저장 (최초 1회만)
			if (originalScale == Vector3.zero)
			{
				originalScale = transform.localScale;
			}
			
			// 스케일 적용
			currentScale = scale;
			transform.localScale = originalScale * currentScale;
			
			// 해상도 정보 업데이트
			lastResolution = new Vector2(Screen.width, Screen.height);
			
			if (showDebugInfo)
			{
				var info = scaleConfig.GetCurrentResolutionInfo();
				var originalSize = originalScale;
				var newSize = transform.localScale;
				var scaleMultiplier = currentScale;
				
				Debug.Log($"[Live2DModelScaler] 스케일 적용 완료: {gameObject.name}\n" +
						 $"=== 해상도 정보 ===\n" +
						 $"현재 해상도: {info.Resolution.x} x {info.Resolution.y}\n" +
						 $"종횡비: {info.AspectRatio:F3}\n" +
						 $"플랫폼: {(info.IsMobile ? "모바일" : "PC")}\n" +
						 $"=== 스케일 정보 ===\n" +
						 $"적용된 스케일: {scaleMultiplier:F3}배\n" +
						 $"원본 크기: {originalSize}\n" +
						 $"변경된 크기: {newSize}\n" +
						 $"크기 변화: {scaleMultiplier:F1}배");
			}
		}
		
		/// <summary>
		/// 원본 크기로 되돌린다.
		/// </summary>
		public void ResetToOriginalScale()
		{
			if (originalScale != Vector3.zero)
			{
				transform.localScale = originalScale;
				currentScale = 1.0f;
				
				if (showDebugInfo)
				{
					Debug.Log($"[Live2DModelScaler] 원본 크기로 복원: {gameObject.name}");
				}
			}
		}
		
		/// <summary>
		/// 수동으로 스케일을 설정한다.
		/// </summary>
		public void SetManualScale(float scale)
		{
			if (originalScale == Vector3.zero)
			{
				originalScale = transform.localScale;
			}
			
			currentScale = scale;
			transform.localScale = originalScale * scale;
			
			if (showDebugInfo)
			{
				Debug.Log($"[Live2DModelScaler] 수동 스케일 설정: {gameObject.name} -> {scale:F2}");
			}
		}
		
		/// <summary>
		/// 현재 스케일을 반환한다.
		/// </summary>
		public float GetCurrentScale()
		{
			return currentScale;
		}
		
		/// <summary>
		/// 원본 스케일을 반환한다.
		/// </summary>
		public Vector3 GetOriginalScale()
		{
			return originalScale;
		}
		
		#endregion
		
		#region Private Methods
		
		/// <summary>
		/// 해상도가 변경되었는지 확인한다.
		/// </summary>
		private bool HasResolutionChanged()
		{
			var currentResolution = new Vector2(Screen.width, Screen.height);
			return currentResolution != lastResolution;
		}
		
		/// <summary>
		/// ResolutionManager에 등록한다.
		/// </summary>
		private void RegisterToResolutionManager()
		{
			try
			{
				var manager = ProjectVG.Domain.Character.Manager.ResolutionManager.Instance;
				if (manager != null)
				{
					manager.RegisterModelScaler(this);
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning($"[Live2DModelScaler] ResolutionManager 등록 실패: {ex.Message}");
			}
		}
		
		/// <summary>
		/// ResolutionManager에서 제거한다.
		/// </summary>
		private void UnregisterFromResolutionManager()
		{
			try
			{
				var manager = ProjectVG.Domain.Character.Manager.ResolutionManager.Instance;
				if (manager != null)
				{
					manager.UnregisterModelScaler(this);
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning($"[Live2DModelScaler] ResolutionManager 제거 실패: {ex.Message}");
			}
		}
		
		#endregion
		
		#region Editor Methods
		
		#if UNITY_EDITOR
		private void OnValidate()
		{
			if (scaleConfig == null)
			{
				// 기본 설정 자동 생성
				scaleConfig = Resources.Load<ResolutionModelScaleConfig>("ResolutionModelScaleConfig");
				if (scaleConfig == null)
				{
					Debug.LogWarning("[Live2DModelScaler] Resources 폴더에 ResolutionModelScaleConfig가 없습니다.");
				}
			}
		}
		#endif
		
		#endregion
	}
}
