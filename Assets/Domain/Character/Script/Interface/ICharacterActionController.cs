#nullable enable
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Motion;
using ProjectVG.Domain.Character.Live2D.Model;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectVG.Domain.Character.Service
{
    public interface ICharacterActionController
    {
        /// <summary>
        /// 액션 컨트롤러를 초기화한다.
        /// </summary>
        void Initialize();

        /// <summary>
        /// 액션을 실행한다.
        /// </summary>
        /// <param name="actionType">액션 타입</param>
        void PlayAction(CharacterActionType actionType);

        /// <summary>
        /// 현재 재생 중인 액션을 중지한다.
        /// </summary>
        void StopCurrentAction();

        /// <summary>
        /// 현재 모션을 즉시 중지하고 Idle로 돌아간다.
        /// </summary>
        void ForceStopAndReturnToIdle();

        /// <summary>
        /// 액션이 재생 중인지 확인한다.
        /// </summary>
        /// <returns>액션 재생 중이면 true</returns>
        bool IsPlaying();

        /// <summary>
        /// 현재 액션 타입을 반환한다.
        /// </summary>
        /// <returns>현재 액션 타입</returns>
        CharacterActionType GetCurrentAction();

        /// <summary>
        /// 모션 종료 동작을 변경한다.
        /// </summary>
        void SetMotionEndBehavior(System.Action? onStop = null, System.Action? onLoop = null, System.Action? onReturnToIdle = null);

        /// <summary>
        /// 현재 모션을 중지한다.
        /// </summary>
        void StopCurrentMotion();

        /// <summary>
        /// 현재 모션을 루프한다.
        /// </summary>
        void LoopCurrentMotion();

        /// <summary>
        /// 현재 모션을 종료하고 Idle로 돌아간다.
        /// </summary>
        void ReturnToIdle();

        // Events
        System.Action? OnMotionStop { get; set; }
        System.Action? OnMotionLoop { get; set; }
        System.Action? OnMotionReturnToIdle { get; set; }
    }
}