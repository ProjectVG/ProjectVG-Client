using UnityEngine;
using ProjectVG.Domain.Character.Config;

namespace ProjectVG.Domain.Character.Component
{
	/// <summary>
	/// 해상도에 따라 카메라의 Orthographic Size를 자동 조정하는 컴포넌트
	/// </summary>
	public class CameraResolutionScaler : MonoBehaviour
	{
		[Header("설정")]
		[SerializeField] private ResolutionModelScaleConfig scaleConfig;
		[SerializeField] private bool applyOnStart = true;
		[SerializeField] private bool applyOnResolutionChange = true;
		[SerializeField] private float baseOrthographicSize = 5f;
		[SerializeField] private Vector2 baseResolution = new Vector2(1080, 1920);
		
		[Header("디버그")]
		[SerializeField] private bool showDebugInfo = true;
		
		private Camera targetCamera;
		private Vector2 lastResolution;
		private float originalOrthographicSize;
		
		#region Unity Lifecycle
		
		private void Start()
		{
			// Initialize();
		}
		
		private void Update()
		{
			// if (applyOnResolutionChange && HasResolutionChanged())
			// {
			// 	ApplyCameraScale();
			// }
		}
		
		#endregion
		
		#region Public Methods
		
		/// <summary>
		/// 컴포넌트를 초기화한다.
		/// </summary>
		public void Initialize()
		{
			targetCamera = GetComponent<Camera>();
			if (targetCamera == null)
			{
				Debug.LogError("[CameraResolutionScaler] Camera 컴포넌트를 찾을 수 없습니다.");
				return;
			}
			
			if (scaleConfig == null)
			{
				scaleConfig = Resources.Load<ResolutionModelScaleConfig>("ResolutionModelScaleConfig");
				if (scaleConfig == null)
				{
					Debug.LogWarning("[CameraResolutionScaler] ResolutionModelScaleConfig를 찾을 수 없습니다.");
				}
			}
			
			originalOrthographicSize = targetCamera.orthographicSize;
			lastResolution = new Vector2(Screen.width, Screen.height);
			
			if (applyOnStart)
			{
				ApplyCameraScale();
			}
			
			if (showDebugInfo)
			{
				Debug.Log($"[CameraResolutionScaler] 초기화 완료. 카메라: {targetCamera.name}");
			}
		}
		
		/// <summary>
		/// 카메라 스케일을 적용한다.
		/// </summary>
		public void ApplyCameraScale()
		{
			if (targetCamera == null)
			{
				Debug.LogWarning("[CameraResolutionScaler] 카메라가 없습니다.");
				return;
			}
			
			var currentResolution = new Vector2(Screen.width, Screen.height);
			var scale = CalculateCameraScale(currentResolution);
			
			targetCamera.orthographicSize = baseOrthographicSize * scale;
			lastResolution = currentResolution;
			
			if (showDebugInfo)
			{
				var info = scaleConfig?.GetCurrentResolutionInfo();
				Debug.Log($"[CameraResolutionScaler] 카메라 스케일 적용 완료: {targetCamera.name}\n" +
						 $"=== 해상도 정보 ===\n" +
						 $"현재 해상도: {currentResolution.x} x {currentResolution.y}\n" +
						 $"기준 해상도: {baseResolution.x} x {baseResolution.y}\n" +
						 $"플랫폼: {(info?.IsMobile == true ? "모바일" : "PC")}\n" +
						 $"=== 카메라 설정 ===\n" +
						 $"원본 Orthographic Size: {originalOrthographicSize:F2}\n" +
						 $"계산된 스케일: {scale:F3}배\n" +
						 $"적용된 Orthographic Size: {targetCamera.orthographicSize:F2}");
			}
		}
		
		/// <summary>
		/// 원본 크기로 되돌린다.
		/// </summary>
		public void ResetToOriginalSize()
		{
			if (targetCamera != null)
			{
				targetCamera.orthographicSize = originalOrthographicSize;
				
				if (showDebugInfo)
				{
					Debug.Log($"[CameraResolutionScaler] 원본 크기로 복원: {targetCamera.name}");
				}
			}
		}
		
		/// <summary>
		/// 현재 카메라 스케일을 반환한다.
		/// </summary>
		public float GetCurrentCameraScale()
		{
			if (targetCamera == null) return 1f;
			return targetCamera.orthographicSize / baseOrthographicSize;
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
		/// 카메라 스케일을 계산한다.
		/// </summary>
		private float CalculateCameraScale(Vector2 resolution)
		{
			// ResolutionModelScaleConfig에서 스케일 가져오기
			if (scaleConfig != null)
			{
				var configScale = scaleConfig.CalculateScale();
				
				// 카메라 스케일은 해상도 비율과 설정 스케일을 조합
				var resolutionRatio = CalculateResolutionRatio(resolution);
				var finalScale = resolutionRatio * configScale;
				
				if (showDebugInfo)
				{
					Debug.Log($"[CameraResolutionScaler] 스케일 계산:\n" +
							 $"해상도 비율: {resolutionRatio:F3}\n" +
							 $"설정 스케일: {configScale:F3}\n" +
							 $"최종 스케일: {finalScale:F3}");
				}
				
				return finalScale;
			}
			
			// 설정이 없으면 해상도 비율만 사용
			return CalculateResolutionRatio(resolution);
		}
		
		/// <summary>
		/// 해상도 비율을 계산한다.
		/// </summary>
		private float CalculateResolutionRatio(Vector2 resolution)
		{
			// 세로 기준으로 비율 계산 (모바일 세로 모드 고려)
			var currentAspect = resolution.x / resolution.y;
			var baseAspect = baseResolution.x / baseResolution.y;
			
			// 세로 모드인 경우 높이 기준, 가로 모드인 경우 너비 기준
			if (currentAspect < 1.0f) // 세로 모드
			{
				return resolution.y / baseResolution.y;
			}
			else // 가로 모드
			{
				return resolution.x / baseResolution.x;
			}
		}
		
		#endregion
		
		#region Editor Methods
		
		#if UNITY_EDITOR
		private void OnValidate()
		{
			if (scaleConfig == null)
			{
				scaleConfig = Resources.Load<ResolutionModelScaleConfig>("ResolutionModelScaleConfig");
			}
		}
		#endif
		
		#endregion
	}
}
