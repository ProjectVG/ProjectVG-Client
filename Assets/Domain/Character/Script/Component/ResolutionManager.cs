using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectVG.Domain.Character.Component;
using ProjectVG.Domain.Character.Config;

namespace ProjectVG.Domain.Character.Manager
{
	/// <summary>
	/// 해상도 변경을 감지하고 Live2D 모델 크기를 자동 조정하는 매니저
	/// </summary>
	public class ResolutionManager : MonoBehaviour
	{
		[Header("설정")]
		[SerializeField] private ResolutionModelScaleConfig scaleConfig;
		[SerializeField] private bool autoDetectScalers = true;
		[SerializeField] private bool applyOnResolutionChange = true;
		
		[Header("디버그")]
		[SerializeField] private bool showDebugInfo = true;
		
		private Vector2 lastResolution;
		private List<Live2DModelScaler> modelScalers = new List<Live2DModelScaler>();
		
		#region Unity Lifecycle
		
		private void Start()
		{
			Initialize();
		}
		
		private void Update()
		{
			if (applyOnResolutionChange && HasResolutionChanged())
			{
				ApplyScaleToAllModels();
			}
		}
		
		#endregion
		
		#region Public Methods
		
		/// <summary>
		/// 매니저를 초기화한다.
		/// </summary>
		public void Initialize()
		{
			if (scaleConfig == null)
			{
				scaleConfig = Resources.Load<ResolutionModelScaleConfig>("ResolutionModelScaleConfig");
				if (scaleConfig == null)
				{
					Debug.LogError("[ResolutionManager] ResolutionModelScaleConfig를 찾을 수 없습니다.");
					return;
				}
			}
			
			lastResolution = new Vector2(Screen.width, Screen.height);
			
			if (autoDetectScalers)
			{
				FindAllModelScalers();
			}
			
			// 초기 스케일 적용
			ApplyScaleToAllModels();
			
			if (showDebugInfo)
			{
				Debug.Log($"[ResolutionManager] 초기화 완료. 발견된 스케일러: {modelScalers.Count}개");
			}
		}
		
		/// <summary>
		/// 모든 모델 스케일러를 찾는다.
		/// </summary>
		public void FindAllModelScalers()
		{
			modelScalers.Clear();
			var scalers = FindObjectsOfType<Live2DModelScaler>();
			modelScalers.AddRange(scalers);
			
			if (showDebugInfo)
			{
				Debug.Log($"[ResolutionManager] {modelScalers.Count}개의 모델 스케일러를 발견했습니다.");
			}
		}
		
		/// <summary>
		/// 모든 모델에 스케일을 적용한다.
		/// </summary>
		public void ApplyScaleToAllModels()
		{
			if (scaleConfig == null)
			{
				Debug.LogWarning("[ResolutionManager] 스케일 설정이 없습니다.");
				return;
			}
			
			// 자동 감지가 활성화되어 있으면 스케일러 다시 찾기
			if (autoDetectScalers)
			{
				FindAllModelScalers();
			}
			
			var currentResolution = new Vector2(Screen.width, Screen.height);
			
			// 스케일 계산은 한 번만 수행
			var scale = scaleConfig.CalculateScale();
			
			foreach (var scaler in modelScalers)
			{
				if (scaler != null)
				{
					// 스케일러에 미리 계산된 스케일을 전달
					scaler.ApplyScaleWithPreCalculatedScale(scale);
				}
			}
			
			lastResolution = currentResolution;
			
			if (showDebugInfo)
			{
				var info = scaleConfig.GetCurrentResolutionInfo();
				Debug.Log($"[ResolutionManager] 모든 모델에 스케일 적용 완료\n" +
						 $"=== 해상도 설정 ===\n" +
						 $"현재 해상도: {info.Resolution.x} x {info.Resolution.y}\n" +
						 $"종횡비: {info.AspectRatio:F3}\n" +
						 $"플랫폼: {(info.IsMobile ? "모바일" : "PC")}\n" +
						 $"=== 스케일 설정 ===\n" +
						 $"적용된 스케일: {scale:F3}배\n" +
						 $"적용된 모델 수: {modelScalers.Count}개\n" +
						 $"=== 모델 목록 ===\n" +
						 string.Join("\n", modelScalers.Select(s => $"- {s.name}: {s.GetCurrentScale():F3}배")));
			}
		}
		
		/// <summary>
		/// 특정 모델 스케일러를 등록한다.
		/// </summary>
		public void RegisterModelScaler(Live2DModelScaler scaler)
		{
			if (scaler != null && !modelScalers.Contains(scaler))
			{
				modelScalers.Add(scaler);
				
				if (showDebugInfo)
				{
					Debug.Log($"[ResolutionManager] 모델 스케일러 등록: {scaler.name}");
				}
			}
		}
		
		/// <summary>
		/// 특정 모델 스케일러를 제거한다.
		/// </summary>
		public void UnregisterModelScaler(Live2DModelScaler scaler)
		{
			if (modelScalers.Remove(scaler) && showDebugInfo)
			{
				Debug.Log($"[ResolutionManager] 모델 스케일러 제거: {scaler.name}");
			}
		}
		
		/// <summary>
		/// 현재 해상도 정보를 반환한다.
		/// </summary>
		public ResolutionModelScaleConfig.ResolutionInfo GetCurrentResolutionInfo()
		{
			return scaleConfig?.GetCurrentResolutionInfo();
		}
		
		/// <summary>
		/// 등록된 모델 스케일러 수를 반환한다.
		/// </summary>
		public int GetRegisteredScalerCount()
		{
			return modelScalers.Count;
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
		
		#endregion
		
		#region Singleton Pattern
		
		private static ResolutionManager _instance;
		public static ResolutionManager Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<ResolutionManager>();
					if (_instance == null)
					{
						var go = new GameObject("ResolutionManager");
						_instance = go.AddComponent<ResolutionManager>();
						DontDestroyOnLoad(go);
					}
				}
				return _instance;
			}
		}
		
		private void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else if (_instance != this)
			{
				Destroy(gameObject);
			}
		}
		
		#endregion
	}
}
