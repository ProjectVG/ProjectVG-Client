using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// Live2D 파라미터를 직접 제어하거나 프리셋을 적용하는 컨트롤러 (비활성화됨)
    /// </summary>
    public class Live2DParameterController : MonoBehaviour
    {
        [Header("Status")]
        [SerializeField] private bool _isEnabled = false;
        
        public void Initialize()
        {
            if (!_isEnabled)
            {
                Debug.Log("[Live2DParameterController] 비활성화됨");
                return;
            }
            
            Debug.Log("[Live2DParameterController] 초기화 완료");
        }
        
        /// <summary>
        /// 프리셋을 적용한다. (비활성화됨)
        /// </summary>
        public void ApplyPreset(string presetKey)
        {
            if (!_isEnabled) return;
            Debug.Log($"[Live2DParameterController] 프리셋 적용: {presetKey}");
        }
        
        /// <summary>
        /// 단일 파라미터를 설정한다. (비활성화됨)
        /// </summary>
        public void SetParameter(string parameterId, float value)
        {
            if (!_isEnabled) return;
            Debug.Log($"[Live2DParameterController] 파라미터 설정: {parameterId} = {value}");
        }
        
        /// <summary>
        /// 여러 파라미터를 한번에 설정한다. (비활성화됨)
        /// </summary>
        public void SetParameters(System.Collections.Generic.Dictionary<string, float> parameters)
        {
            if (!_isEnabled) return;
            Debug.Log($"[Live2DParameterController] 파라미터 일괄 설정: {parameters?.Count ?? 0}개");
        }
        
        /// <summary>
        /// 파라미터 값을 블렌딩하여 설정한다. (비활성화됨)
        /// </summary>
        public void BlendParameter(string parameterId, float targetValue, float blendTime = -1f)
        {
            if (!_isEnabled) return;
            Debug.Log($"[Live2DParameterController] 파라미터 블렌딩: {parameterId} -> {targetValue}");
        }
        
        /// <summary>
        /// 파라미터 값을 반환한다. (비활성화됨)
        /// </summary>
        public float GetParameterValue(string parameterId)
        {
            if (!_isEnabled) return 0f;
            Debug.Log($"[Live2DParameterController] 파라미터 값 조회: {parameterId}");
            return 0f;
        }
        
        /// <summary>
        /// 모든 파라미터를 기본값으로 리셋한다. (비활성화됨)
        /// </summary>
        public void ResetAllParameters()
        {
            if (!_isEnabled) return;
            Debug.Log("[Live2DParameterController] 모든 파라미터 리셋");
        }
    }
}


