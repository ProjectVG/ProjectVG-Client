#nullable enable
using UnityEngine;
using System.Collections;

namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// Unity Animator를 사용한 캐릭터 액션 컨트롤러 구현체
    /// </summary>
    public class AnimatorCharacterActionController : MonoBehaviour, ICharacterActionController
    {
        private Animator? _animator;
        private CharacterActionType _currentAction = CharacterActionType.Idle;
        private bool _isPlayingAction = false;
        
        // Motion End Behavior Control
        public System.Action? OnMotionStop { get; set; }
        public System.Action? OnMotionLoop { get; set; }
        public System.Action? OnMotionReturnToIdle { get; set; }

        #region Unity Lifecycle

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError("[AnimatorCharacterActionController] Animator component not found!");
            }
            
            SetupDefaultCallbacks();
        }

        private void OnEnable()
        {
            // 활성화 시 Idle 상태로 시작
            if (_currentAction == CharacterActionType.Idle)
            {
                StartCoroutine(DelayedIdleStart());
            }
        }

        /// <summary>
        /// Animator 초기화 대기 후 Idle 애니메이션 시작
        /// </summary>
        private IEnumerator DelayedIdleStart()
        {
            // 1프레임 대기 (Animator 초기화 완료 대기)
            yield return null;
            
            if (_currentAction == CharacterActionType.Idle && _animator != null)
            {
                var idleAction = _currentAction.ToCharacterAction();
                _animator.SetTrigger(idleAction.ToTriggerName());
            }
        }

        #endregion

        #region ICharacterActionController Implementation

        /// <summary>
        /// Animator를 주입하여 초기화한다.
        /// </summary>
        /// <param name="animator">Unity Animator 컴포넌트</param>
        public void Initialize(Animator animator)
        {
            _animator = animator;
            _currentAction = CharacterActionType.Idle;
            _isPlayingAction = false;
            
            if (_animator == null)
            {
                Debug.LogError("[AnimatorCharacterActionController] Animator가 null입니다.");
                return;
            }
            
            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogError("[AnimatorCharacterActionController] Animator Controller가 설정되지 않았습니다.");
                return;
            }
            
            Debug.Log($"[AnimatorCharacterActionController] 초기화 완료 - Controller: {_animator.runtimeAnimatorController.name}");
        }

        /// <summary>
        /// 인터페이스 구현을 위한 매개변수 없는 초기화 메서드
        /// </summary>
        public void Initialize()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            
            if (_animator == null)
            {
                Debug.LogError("[AnimatorCharacterActionController] Initialize() called but no Animator found!");
                return;
            }
            
            Initialize(_animator);
        }

        /// <summary>
        /// 액션을 실행한다.
        /// </summary>
        /// <param name="actionType">액션 타입</param>
        public void PlayAction(CharacterActionType actionType)
        {
            if (_animator == null)
            {
                Debug.LogWarning("[AnimatorCharacterActionController] Animator가 초기화되지 않았습니다.");
                return;
            }
            
            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("[AnimatorCharacterActionController] Animator Controller가 설정되지 않았습니다.");
                return;
            }

            try
            {
                _currentAction = actionType;
                
                var characterAction = actionType.ToCharacterAction();
                string triggerName = characterAction.ToTriggerName();
                
                _animator.SetTrigger(triggerName);
                
                if (actionType != CharacterActionType.Idle)
                {
                    _isPlayingAction = true;
                    // 액션 애니메이션이 끝나면 Idle로 돌아가도록 스케줄링
                    StartCoroutine(WaitForAnimationAndReturnToIdle(actionType));
                }
                else
                {
                    _isPlayingAction = false;
                }
                
                Debug.Log($"[AnimatorCharacterActionController] 액션 재생: {actionType} (트리거: {triggerName})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnimatorCharacterActionController] 액션 재생 실패: {ex.Message}");
                _isPlayingAction = false;
            }
        }

        /// <summary>
        /// 현재 재생 중인 액션을 중지한다.
        /// </summary>
        public void StopCurrentAction()
        {
            if (_animator == null) return;

            StopAllCoroutines();
            PlayAction(CharacterActionType.Idle);
            _isPlayingAction = false;
            _currentAction = CharacterActionType.Idle;
            
            Debug.Log("[AnimatorCharacterActionController] 액션 중지");
        }

        /// <summary>
        /// 현재 모션을 즉시 중지하고 Idle로 돌아간다.
        /// </summary>
        public void ForceStopAndReturnToIdle()
        {
            if (_animator == null) return;

            StopAllCoroutines();
            
            // 즉시 Idle로 전환
            _currentAction = CharacterActionType.Idle;
            _isPlayingAction = false;
            
            var idleAction = CharacterAction.Idle;
            _animator.SetTrigger(idleAction.ToTriggerName());
            
            Debug.Log("[AnimatorCharacterActionController] 강제 중지 후 Idle로 복귀");
        }

        /// <summary>
        /// 액션이 재생 중인지 확인한다.
        /// </summary>
        /// <returns>액션 재생 중이면 true</returns>
        public bool IsPlaying()
        {
            return _isPlayingAction && _animator != null;
        }

        /// <summary>
        /// 현재 액션 타입을 반환한다.
        /// </summary>
        /// <returns>현재 액션 타입</returns>
        public CharacterActionType GetCurrentAction()
        {
            return _currentAction;
        }

        /// <summary>
        /// 모션 종료 동작을 변경한다.
        /// </summary>
        public void SetMotionEndBehavior(System.Action? onStop = null, System.Action? onLoop = null, System.Action? onReturnToIdle = null)
        {
            if (onStop != null) OnMotionStop = onStop;
            if (onLoop != null) OnMotionLoop = onLoop;
            if (onReturnToIdle != null) OnMotionReturnToIdle = onReturnToIdle;
        }

        /// <summary>
        /// 현재 모션을 중지한다.
        /// </summary>
        public void StopCurrentMotion()
        {
            OnMotionStop?.Invoke();
        }

        /// <summary>
        /// 현재 모션을 루프한다.
        /// </summary>
        public void LoopCurrentMotion()
        {
            OnMotionLoop?.Invoke();
        }

        /// <summary>
        /// 현재 모션을 종료하고 Idle로 돌아간다.
        /// </summary>
        public void ReturnToIdle()
        {
            OnMotionReturnToIdle?.Invoke();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 기본 모션 종료 콜백을 설정한다.
        /// </summary>
        private void SetupDefaultCallbacks()
        {
            OnMotionStop = () => {
                Debug.Log("[AnimatorCharacterActionController] 모션 정지");
                _isPlayingAction = false;
            };
            
            OnMotionLoop = () => {
                Debug.Log("[AnimatorCharacterActionController] 모션 루프 - 같은 액션 반복");
                PlayAction(_currentAction);
            };
            
            OnMotionReturnToIdle = () => {
                Debug.Log("[AnimatorCharacterActionController] 모션 종료 - Idle로 복귀");
                PlayAction(CharacterActionType.Idle);
            };
        }


        /// <summary>
        /// 애니메이션 완료를 기다린 후 Idle로 돌아간다.
        /// </summary>
        private IEnumerator WaitForAnimationAndReturnToIdle(CharacterActionType actionType)
        {
            if (_animator == null) yield break;
            
            var characterAction = actionType.ToCharacterAction();
            string stateName = characterAction.ToStateName();
            
            // 애니메이션이 시작될 때까지 대기
            yield return new WaitUntil(() => IsInState(stateName));
            
            // 애니메이션이 완료될 때까지 대기
            yield return new WaitUntil(() => !IsInState(stateName) || GetNormalizedTime() >= 0.95f);
            
            // Idle로 돌아가기
            if (_currentAction == actionType) // 중간에 다른 액션이 실행되지 않았다면
            {
                OnMotionReturnToIdle?.Invoke();
            }
        }

        /// <summary>
        /// 현재 Animator가 지정된 상태에 있는지 확인한다.
        /// </summary>
        private bool IsInState(string stateName)
        {
            if (_animator == null) return false;
            
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(stateName);
        }

        /// <summary>
        /// 현재 애니메이션의 정규화된 시간을 반환한다.
        /// </summary>
        private float GetNormalizedTime()
        {
            if (_animator == null) return 1.0f;
            
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.normalizedTime;
        }

        #endregion
    }
}