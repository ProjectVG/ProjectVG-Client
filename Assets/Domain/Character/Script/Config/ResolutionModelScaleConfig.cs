using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectVG.Domain.Character.Config
{
	/// <summary>
	/// 해상도별 Live2D 모델 크기 조정 설정
	/// </summary>
	[CreateAssetMenu(fileName = "ResolutionModelScaleConfig", menuName = "ProjectVG/Character/Resolution Model Scale Config")]
	public class ResolutionModelScaleConfig : ScriptableObject
	{
		[Header("기본 설정")]
		[SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
		[SerializeField] private float referenceScale = 1.0f;
		
		public Vector2 ReferenceResolution => referenceResolution;
		public float ReferenceScale => referenceScale;
		
		[Header("해상도별 스케일 설정")]
		[SerializeField] private List<ResolutionScaleRule> scaleRules = new List<ResolutionScaleRule>();
		
		[Header("자동 조정 설정")]
		[SerializeField] private bool enableAutoScale = true;
		[SerializeField] private float minScale = 0.5f;
		[SerializeField] private float maxScale = 2.0f;
		[SerializeField] private ScaleMode scaleMode = ScaleMode.HeightBased;
		
		/// <summary>
		/// 현재 해상도에 맞는 스케일을 계산한다.
		/// </summary>
		public float CalculateScale()
		{
			if (!enableAutoScale)
			{
				return referenceScale;
			}
			
			var currentResolution = new Vector2(Screen.width, Screen.height);
			var scale = CalculateScaleForResolution(currentResolution);
			var clampedScale = Mathf.Clamp(scale, minScale, maxScale);
			
			return clampedScale;
		}
		
		/// <summary>
		/// 특정 해상도에 대한 스케일을 계산한다.
		/// </summary>
		public float CalculateScaleForResolution(Vector2 resolution)
		{
			// 최적의 규칙을 찾기
			var bestRule = FindBestMatchingRule(resolution);
			
			if (bestRule != null)
			{
				return bestRule.Scale;
			}
			
			// 자동 계산
			switch (scaleMode)
			{
				case ScaleMode.HeightBased:
					return CalculateHeightBasedScale(resolution);
				case ScaleMode.WidthBased:
					return CalculateWidthBasedScale(resolution);
				case ScaleMode.AspectRatioBased:
					return CalculateAspectRatioBasedScale(resolution);
				default:
					return referenceScale;
			}
		}
		
		/// <summary>
		/// 주어진 해상도에 가장 적합한 규칙을 찾는다.
		/// </summary>
		private ResolutionScaleRule FindBestMatchingRule(Vector2 resolution)
		{
			var matchingRules = new List<ResolutionScaleRule>();
			
			// 조건을 만족하는 모든 규칙을 찾기
			foreach (var rule in scaleRules)
			{
				if (rule.Matches(resolution))
				{
					matchingRules.Add(rule);
				}
			}
			
			if (matchingRules.Count == 0)
			{
				return null;
			}
			
			if (matchingRules.Count == 1)
			{
				return matchingRules[0];
			}
			
			// 여러 규칙이 매칭되는 경우 최적의 규칙 선택
			return FindOptimalRule(resolution, matchingRules);
		}
		
		/// <summary>
		/// 여러 매칭 규칙 중에서 최적의 규칙을 선택한다.
		/// </summary>
		private ResolutionScaleRule FindOptimalRule(Vector2 resolution, List<ResolutionScaleRule> matchingRules)
		{
			ResolutionScaleRule bestRule = null;
			float bestScore = float.MaxValue;
			
			foreach (var rule in matchingRules)
			{
				var score = CalculateRuleFitnessScore(resolution, rule);
				
				if (score < bestScore)
				{
					bestScore = score;
					bestRule = rule;
				}
			}
			
			return bestRule;
		}
		
		/// <summary>
		/// 규칙의 적합도를 계산한다. 점수가 낮을수록 더 적합함.
		/// </summary>
		private float CalculateRuleFitnessScore(Vector2 resolution, ResolutionScaleRule rule)
		{
			var ruleCenterWidth = (rule.MinWidth + rule.MaxWidth) / 2f;
			var ruleCenterHeight = (rule.MinHeight + rule.MaxHeight) / 2f;
			
			var widthDistance = Mathf.Abs(resolution.x - ruleCenterWidth);
			var heightDistance = Mathf.Abs(resolution.y - ruleCenterHeight);
			
			var distance = Mathf.Sqrt(widthDistance * widthDistance + heightDistance * heightDistance);
			
			var ruleWidthRange = rule.MaxWidth - rule.MinWidth;
			var ruleHeightRange = rule.MaxHeight - rule.MinHeight;
			var ruleArea = ruleWidthRange * ruleHeightRange;
			
			var specificityBonus = Mathf.Max(0, 1000f - ruleArea) / 1000f;
			
			var finalScore = distance - specificityBonus;
			
			return finalScore;
		}
		
		/// <summary>
		/// 높이 기반 스케일 계산
		/// </summary>
		private float CalculateHeightBasedScale(Vector2 resolution)
		{
			float heightRatio = resolution.y / referenceResolution.y;
			return referenceScale * heightRatio;
		}
		
		/// <summary>
		/// 너비 기반 스케일 계산
		/// </summary>
		private float CalculateWidthBasedScale(Vector2 resolution)
		{
			float widthRatio = resolution.x / referenceResolution.x;
			return referenceScale * widthRatio;
		}
		
		/// <summary>
		/// 종횡비 기반 스케일 계산
		/// </summary>
		private float CalculateAspectRatioBasedScale(Vector2 resolution)
		{
			float currentAspect = resolution.x / resolution.y;
			float referenceAspect = referenceResolution.x / referenceResolution.y;
			float aspectRatio = currentAspect / referenceAspect;
			
			// 종횡비가 크면 너비 기준, 작으면 높이 기준
			if (aspectRatio > 1.0f)
			{
				return CalculateWidthBasedScale(resolution);
			}
			else
			{
				return CalculateHeightBasedScale(resolution);
			}
		}
		
		/// <summary>
		/// 현재 해상도 정보를 반환한다.
		/// </summary>
		public ResolutionInfo GetCurrentResolutionInfo()
		{
			var resolution = new Vector2(Screen.width, Screen.height);
			var scale = CalculateScale();
			
			return new ResolutionInfo
			{
				Resolution = resolution,
				Scale = scale,
				AspectRatio = resolution.x / resolution.y,
				IsMobile = IsMobilePlatform(resolution)
			};
		}
		
		/// <summary>
		/// 해상도를 기반으로 모바일 플랫폼인지 판단한다.
		/// </summary>
		private bool IsMobilePlatform(Vector2 resolution)
		{
			// Unity 에디터에서도 모바일 해상도로 테스트할 수 있도록 해상도 기반 판단
			var aspectRatio = resolution.x / resolution.y;
			
			// 세로 모드 (종횡비 < 1)이고 높이가 1000px 이상이면 모바일로 판단
			if (aspectRatio < 1.0f && resolution.y >= 1000f)
			{
				return true;
			}
			
			// Unity의 기본 플랫폼 감지도 함께 사용
			return Application.isMobilePlatform;
		}
		
		/// <summary>
		/// 스케일 모드
		/// </summary>
		public enum ScaleMode
		{
			HeightBased,      // 높이 기준
			WidthBased,       // 너비 기준
			AspectRatioBased  // 종횡비 기준
		}
		
		/// <summary>
		/// 해상도별 스케일 규칙
		/// </summary>
		[Serializable]
		public class ResolutionScaleRule
		{
			[Header("해상도 조건")]
			[SerializeField] private int minWidth = 0;
			[SerializeField] private int maxWidth = 9999;
			[SerializeField] private int minHeight = 0;
			[SerializeField] private int maxHeight = 9999;
			[SerializeField] private bool isMobileOnly = false;
			
			[Header("스케일 설정")]
			[SerializeField] private float scale = 1.0f;
			[SerializeField] private string description = "";
			
			public float Scale => scale;
			public string Description => description;
			public int MinWidth => minWidth;
			public int MaxWidth => maxWidth;
			public int MinHeight => minHeight;
			public int MaxHeight => maxHeight;
			
			/// <summary>
			/// 해상도가 이 규칙에 맞는지 확인한다.
			/// </summary>
			public bool Matches(Vector2 resolution)
			{
				// 모바일 전용 규칙인 경우 해상도 기반으로도 판단
				if (isMobileOnly)
				{
					var aspectRatio = resolution.x / resolution.y;
					var isMobileByResolution = aspectRatio < 1.0f && resolution.y >= 1000f;
					
					if (!Application.isMobilePlatform && !isMobileByResolution)
					{
						return false;
					}
				}
				
				return resolution.x >= minWidth && resolution.x <= maxWidth &&
					   resolution.y >= minHeight && resolution.y <= maxHeight;
			}
		}
		
		/// <summary>
		/// 해상도 정보
		/// </summary>
		[Serializable]
		public class ResolutionInfo
		{
			public Vector2 Resolution;
			public float Scale;
			public float AspectRatio;
			public bool IsMobile;
		}
	}
}
