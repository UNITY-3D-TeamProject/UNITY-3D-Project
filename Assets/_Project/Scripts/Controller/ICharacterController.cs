using System;
using UnityEngine;

namespace Controller
{
    /// <summary>
    /// 캐릭터 조작 요청(이동/시점/점프)의 공급자.
    /// 플레이어 입력, AI 등 조작 주체가 구현하며, 요청을 받을 콜백을 Set/Clear 로 연결한다.
    /// 각 요청은 단일 콜백만 유지한다 (Set 은 덮어쓰기).
    /// </summary>
    public interface ICharacterController
    {
        /// <summary>이동 요청 콜백을 등록한다.</summary>
        /// <param name="callback">이동 입력 (x: 좌우, y: 전후)</param>
        void SetMoveRequest(Action<Vector2> callback);

        /// <summary>시점 회전 요청 콜백을 등록한다.</summary>
        /// <param name="callback">시점 입력 (x: yaw, y: pitch)</param>
        void SetLookRequest(Action<Vector2> callback);

        /// <summary>점프 요청 콜백을 등록한다.</summary>
        void SetJumpRequest(Action callback);

        /// <summary>이동 요청 콜백을 해제한다.</summary>
        void ClearMoveRequest();

        /// <summary>시점 회전 요청 콜백을 해제한다.</summary>
        void ClearLookRequest();

        /// <summary>점프 요청 콜백을 해제한다.</summary>
        void ClearJumpRequest();
    }
}
