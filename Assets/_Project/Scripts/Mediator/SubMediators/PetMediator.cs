using System;
using UnityEngine;
using Pet;

namespace Mediator.SubMediators
{
    /// <summary>
    /// 다른 Mediator 에서 온 캐릭터 상태(점프, 피격, 특수 Idle, 사망)와 스캔 요청을 PetSpawner 로 전달하는 접착 컴포넌트.
    /// </summary>
    public class PetMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private PetSpawner _petSpawner;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _petSpawner);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 점프 시작을 펫에 전달한다.
        /// </summary>
        public void NotifyJumpStarted()
        {
            if (!_petSpawner) return;

            _petSpawner.PlayJump();
        }

        /// <summary>
        /// 착지를 펫에 전달한다.
        /// </summary>
        public void NotifyJumpEnded()
        {
            if (!_petSpawner) return;

            _petSpawner.PlayLand();
        }

        /// <summary>
        /// 피격을 펫에 전달한다.
        /// </summary>
        /// <param name="direction">캐릭터 기준 피격 방향. 0: 앞, 1: 뒤, 2: 왼쪽, 3: 오른쪽</param>
        public void NotifyHit(int direction)
        {
            if (!_petSpawner) return;

            _petSpawner.PlayHit(direction);
        }

        /// <summary>
        /// 특수 Idle 시작을 펫에 전달한다.
        /// </summary>
        /// <param name="specialIdleIndex">캐릭터가 재생한 특수 Idle 인덱스</param>
        public void NotifySpecialIdle(int specialIdleIndex)
        {
            if (!_petSpawner) return;

            _petSpawner.PlaySpecialIdle(specialIdleIndex);
        }

        /// <summary>
        /// 사망을 펫에 전달한다.
        /// </summary>
        public void NotifyDeath()
        {
            if (!_petSpawner) return;

            _petSpawner.PlayDeath();
        }

        /// <summary>
        /// 펫을 스캔 위치로 보내고, 스캔할 위치가 정해지면 onReady 로 알린다.
        /// </summary>
        /// <param name="onReady">스캔 위치(월드 좌표)를 받을 콜백</param>
        public void RequestScan(Action<Vector3> onReady)
        {
            if (!_petSpawner)
            {
                onReady?.Invoke(transform.position);
                return;
            }

            _petSpawner.PlayScan(onReady);
        }
        #endregion

        #region Protected Methods
        protected override void InitAttributeCallback()
        {
        }

        protected override void InitValue()
        {
        }
        #endregion
    }
}
