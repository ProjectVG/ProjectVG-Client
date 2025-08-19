using UnityEngine;
using ProjectVG.Domain.Character.Manager;
using ProjectVG.Domain.Character.Config;

namespace ProjectVG.Domain.Character.Test
{
	/// <summary>
	/// 해상도 디버깅을 위한 테스트 스크립트
	/// </summary>
	public class ResolutionDebugger : MonoBehaviour
	{
		[Header("테스트 설정")]
		[SerializeField] private bool testOnStart = true;
		[SerializeField] private Vector2 testResolution = new Vector2(1440, 2960);
		
		[Header("디버그 정보")]
		[SerializeField] private bool showCurrentResolution = true;
		[SerializeField] private bool showScaleCalculation = true;
		
		private void Start()
		{
			if (testOnStart)
			{
				TestResolution();
			}
		}
		
		[ContextMenu("해상도 테스트")]
		public void TestResolution()
		{
			Debug.Log("=== 해상도 디버깅 시작 ===");
			
			// 현재 해상도 정보
			if (showCurrentResolution)
			{
				Debug.Log($"=== 현재 시스템 정보 ===\n" +
						 $"해상도: {Screen.width} x {Screen.height}\n" +
						 $"종횡비: {(float)Screen.width / Screen.height:F3}\n" +
						 $"플랫폼: {(Application.isMobilePlatform ? "모바일" : "PC")}\n" +
						 $"DPI: {Screen.dpi:F1}");
			}
			
			// ResolutionManager 테스트
			var manager = ResolutionManager.Instance;
			if (manager != null)
			{
				var info = manager.GetCurrentResolutionInfo();
				Debug.Log($"=== ResolutionManager 상태 ===\n" +
						 $"해상도: {info.Resolution.x} x {info.Resolution.y}\n" +
						 $"적용 스케일: {info.Scale:F3}배\n" +
						 $"등록된 스케일러: {manager.GetRegisteredScalerCount()}개");
			}
			else
			{
				Debug.LogWarning("ResolutionManager를 찾을 수 없습니다.");
			}
			
			// 설정 파일 직접 테스트
			var config = Resources.Load<ResolutionModelScaleConfig>("ResolutionModelScaleConfig");
			if (config != null)
			{
				Debug.Log($"=== 설정 파일 정보 ===\n" +
						 $"파일명: {config.name}\n" +
						 $"기준 해상도: {config.ReferenceResolution.x} x {config.ReferenceResolution.y}\n" +
						 $"기준 스케일: {config.ReferenceScale:F3}배");
				
				// 현재 해상도로 스케일 계산
				var currentScale = config.CalculateScale();
				Debug.Log($"=== 현재 해상도 스케일 ===\n" +
						 $"해상도: {Screen.width} x {Screen.height}\n" +
						 $"적용 스케일: {currentScale:F3}배");
				
				// 테스트 해상도로 스케일 계산
				var testScale = config.CalculateScaleForResolution(testResolution);
				Debug.Log($"=== 테스트 해상도 스케일 ===\n" +
						 $"해상도: {testResolution.x} x {testResolution.y}\n" +
						 $"적용 스케일: {testScale:F3}배");
				
				if (showScaleCalculation)
				{
					TestScaleCalculation(config);
				}
			}
			else
			{
				Debug.LogError("ResolutionModelScaleConfig를 찾을 수 없습니다.");
			}
			
			Debug.Log("=== 해상도 디버깅 완료 ===");
		}
		
		private void TestScaleCalculation(ResolutionModelScaleConfig config)
		{
			Debug.Log("--- 스케일 계산 상세 분석 ---");
			
			var currentResolution = new Vector2(Screen.width, Screen.height);
			var testResolution = new Vector2(1440, 2960);
			
			// 각 해상도별 스케일 계산
			var resolutions = new Vector2[]
			{
				currentResolution,
				testResolution,
				new Vector2(1920, 1080), // PC FHD
				new Vector2(1080, 1920), // 모바일 세로
				new Vector2(390, 844),   // iPhone 12
				new Vector2(375, 667)    // iPhone SE
			};
			
			foreach (var resolution in resolutions)
			{
				var scale = config.CalculateScaleForResolution(resolution);
				var aspectRatio = resolution.x / resolution.y;
				var platform = Application.isMobilePlatform ? "모바일" : "PC";
				Debug.Log($"=== {resolution.x} x {resolution.y} ===\n" +
						 $"종횡비: {aspectRatio:F3}\n" +
						 $"플랫폼: {platform}\n" +
						 $"적용 스케일: {scale:F3}배");
			}
		}
		
		[ContextMenu("강제 스케일 적용")]
		public void ForceApplyScale()
		{
			var manager = ResolutionManager.Instance;
			if (manager != null)
			{
				manager.ApplyScaleToAllModels();
				Debug.Log("모든 모델에 스케일 강제 적용 완료");
			}
		}
		
		[ContextMenu("스케일러 찾기")]
		public void FindScalers()
		{
			var scalers = FindObjectsOfType<ProjectVG.Domain.Character.Component.Live2DModelScaler>();
			Debug.Log($"=== 발견된 Live2DModelScaler: {scalers.Length}개 ===");
			
			if (scalers.Length == 0)
			{
				Debug.LogWarning("Live2DModelScaler가 발견되지 않았습니다.");
				return;
			}
			
			foreach (var scaler in scalers)
			{
				var originalScale = scaler.GetOriginalScale();
				var currentScale = scaler.GetCurrentScale();
				var transform = scaler.transform;
				
				Debug.Log($"=== {scaler.name} ===\n" +
						 $"위치: {transform.position}\n" +
						 $"원본 크기: {originalScale}\n" +
						 $"현재 크기: {transform.localScale}\n" +
						 $"적용 스케일: {currentScale:F3}배\n" +
						 $"부모: {(transform.parent != null ? transform.parent.name : "없음")}");
			}
		}
	}
}
