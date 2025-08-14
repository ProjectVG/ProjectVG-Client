using UnityEngine;
using System;
using System.Collections;
using Live2D.Cubism.Framework.Motion;

namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// 행동 → 모션/파라미터 트리거를 구현하는 컨트롤러
    /// </summary>
    public class ActionController : MonoBehaviour, IActionController
    {
        [Header("Components")]
        [SerializeField] private CubismMotionController _motionController;
        
        [Header("Action Mapping")]
        [SerializeField] private ActionMotionMapping[] _actionMappings;
        
        [Header("Settings")]
        [SerializeField] private float _defaultMotionDuration = 2.0f;
        
        private string _currentAction;
        private Coroutine _actionCoroutine;
        private bool _isActionPlaying = false;
        
        #region IActionController Implementation
        
        public void Initialize()
        {
            if (_motionController == null)
            {
                _motionController = GetComponent<CubismMotionController>();
            }
            
            if (_motionController == null)
            {
                Debug.LogError("[ActionController] CubismMotionController를 찾을 수 없습니다.");
                return;
            }
            
            Debug.Log("[ActionController] 초기화 완료");
        }
        
        public void TriggerAction(string action, object args = null)
        {
            if (string.IsNullOrEmpty(action))
            {
                Debug.LogWarning("[ActionController] 액션이 null입니다.");
                return;
            }
            
            // 현재 액션이 재생 중이라면 중단
            if (_isActionPlaying)
            {
                StopCurrentAction();
            }
            
            StartAction(action, args);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 현재 액션을 중단한다.
        /// </summary>
        public void StopCurrentAction()
        {
            if (_actionCoroutine != null)
            {
                StopCoroutine(_actionCoroutine);
                _actionCoroutine = null;
            }
            
            _isActionPlaying = false;
            _currentAction = null;
            
            Debug.Log("[ActionController] 액션 중단");
        }
        
        /// <summary>
        /// 현재 액션을 반환한다.
        /// </summary>
        public string GetCurrentAction()
        {
            return _currentAction;
        }
        
        /// <summary>
        /// 액션이 재생 중인지 확인한다.
        /// </summary>
        public bool IsActionPlaying()
        {
            return _isActionPlaying;
        }
        
        #endregion
        
        #region Private Methods
        
        private void StartAction(string action, object args)
        {
            var motionKey = GetMotionKey(action);
            if (string.IsNullOrEmpty(motionKey))
            {
                Debug.LogWarning($"[ActionController] 액션 '{action}'에 대한 모션을 찾을 수 없습니다.");
                return;
            }
            
            _actionCoroutine = StartCoroutine(ActionCoroutine(action, motionKey, args));
        }
        
        private IEnumerator ActionCoroutine(string action, string motionKey, object args)
        {
            _currentAction = action;
            _isActionPlaying = true;
            
            Debug.Log($"[ActionController] 액션 시작: {action}");
            
            // 모션 재생 (현재는 더미 처리)
            PlayMotion(motionKey, args);
            
            // 모션 지속 시간만큼 대기
            var duration = GetActionDuration(action);
            yield return new WaitForSeconds(duration);
            
            // 액션 완료
            _isActionPlaying = false;
            _currentAction = null;
            
            Debug.Log($"[ActionController] 액션 완료: {action}");
        }
        
        private void PlayMotion(string motionKey, object args)
        {
            if (_motionController == null) return;
            
            // TODO: 실제 모션 재생 로직 구현
            // 현재는 더미 처리로 로그만 출력
            Debug.Log($"[ActionController] 모션 재생: {motionKey}");
            
            // 향후 구현 예정:
            // _motionController.PlayMotion(motionKey);
            // 또는
            // _motionController.PlayMotionGroup(motionKey);
        }
        
        private string GetMotionKey(string action)
        {
            foreach (var mapping in _actionMappings)
            {
                if (mapping.Action.Equals(action, StringComparison.OrdinalIgnoreCase))
                {
                    return mapping.MotionKey;
                }
            }
            
            // 기본값 반환
            return "idle";
        }
        
        private float GetActionDuration(string action)
        {
            foreach (var mapping in _actionMappings)
            {
                if (mapping.Action.Equals(action, StringComparison.OrdinalIgnoreCase))
                {
                    return mapping.Duration > 0 ? mapping.Duration : _defaultMotionDuration;
                }
            }
            
            return _defaultMotionDuration;
        }
        
        #endregion
        
        #region Action Mapping
        
        [System.Serializable]
        public class ActionMotionMapping
        {
            public string Action;
            public string MotionKey;
            public float Duration;
        }
        
        #endregion
    }
}


