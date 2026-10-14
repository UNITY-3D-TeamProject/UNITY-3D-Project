using System;
using UnityEngine;
using Pet;

namespace Mediator.SubMediators
{
    /// <summary>
    /// PetSpawner 가 생성한 펫의 PetFollower(이동)와 PetAnimator(애니메이션)를 연결하고,
    /// 다른 Mediator 에서 온 캐릭터 상태(점프, 피격, 특수 Idle, 사망)와 스캔 요청을 펫에 전달하는 접착 컴포넌트.
    /// 스캔 요청을 받으면 펫을 스캔 위치(_scanPivot)로 보내고(PetFollower.MoveAndHold), 도착 위치를 스캔 위치로 알린다.
    /// </summary>
    public class PetMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private PetSpawner _petSpawner;
        [Tooltip("스캔할 때 펫이 이동할 위치. pivot 과 같은 Transform 아래, 캐릭터 정면에 둔다")]
        [SerializeField] private Transform _scanPivot;
        #endregion

        #region Private Fields
        private PetFollower _pet;
        private PetAnimator _petAnimator;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _petSpawner);
            if (!_petSpawner) return;

            _petSpawner.OnPetSpawned += BindPet;
            // 이미 생성된 뒤라면 바로 연결한다
            if (_petSpawner.Pet) BindPet(_petSpawner.Pet);
        }

        private void OnDestroy()
        {
            if (_petSpawner) _petSpawner.OnPetSpawned -= BindPet;
            if (_pet) _pet.OnMoveSpeedUpdated -= SetPetMoveSpeed;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 점프 시작을 펫에 전달한다.
        /// </summary>
        public void NotifyJumpStarted()
        {
            if (!_petAnimator) return;

            _petAnimator.PlayJump();
        }

        /// <summary>
        /// 착지를 펫에 전달한다.
        /// </summary>
        public void NotifyJumpEnded()
        {
            if (!_petAnimator) return;

            _petAnimator.PlayLand();
        }

        /// <summary>
        /// 피격을 펫에 전달한다.
        /// </summary>
        /// <param name="direction">캐릭터 기준 피격 방향. 0: 앞, 1: 뒤, 2: 왼쪽, 3: 오른쪽</param>
        public void NotifyHit(int direction)
        {
            if (!_petAnimator) return;

            _petAnimator.PlayHit(direction);
        }

        /// <summary>
        /// 특수 Idle 시작을 펫에 전달한다.
        /// </summary>
        /// <param name="specialIdleIndex">캐릭터가 재생한 특수 Idle 인덱스</param>
        public void NotifySpecialIdle(int specialIdleIndex)
        {
            if (!_petAnimator) return;

            _petAnimator.PlaySpecialIdle(specialIdleIndex);
        }

        /// <summary>
        /// 사망을 펫에 전달한다. 추종(진행 중인 스캔 이동 포함)을 멈추고 사망 애니메이션을 재생한다.
        /// </summary>
        public void NotifyDeath()
        {
            if (_pet) _pet.StopFollow();
            if (_petAnimator) _petAnimator.PlayDeath();
        }

        /// <summary>
        /// 펫을 스캔 위치로 보내고, 도착하면 그 위치를 onReady 로 알린다.
        /// 펫이나 스캔 위치가 없으면 기다리지 않고 pivot(없으면 이 오브젝트) 위치를 바로 알린다.
        /// </summary>
        /// <param name="onReady">스캔 위치(월드 좌표)를 받을 콜백</param>
        public void RequestScan(Action<Vector3> onReady)
        {
            if (!_pet || !_scanPivot)
            {
                Transform pivot = _petSpawner ? _petSpawner.Pivot : null;
                onReady?.Invoke(pivot ? pivot.position : transform.position);
                return;
            }

            _pet.MoveAndHold(_scanPivot, onReady);
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

        #region Private Methods
        /// <summary>
        /// 생성된 펫의 이동과 애니메이션을 연결한다.
        /// </summary>
        /// <param name="pet">생성된 펫</param>
        private void BindPet(PetFollower pet)
        {
            if (_pet) _pet.OnMoveSpeedUpdated -= SetPetMoveSpeed;

            _pet = pet;
            _petAnimator = _pet ? _pet.GetComponent<PetAnimator>() : null;
            if (_pet) _pet.OnMoveSpeedUpdated += SetPetMoveSpeed;
        }

        /// <summary>
        /// 펫의 실제 이동 속도를 애니메이션에 전달한다.
        /// </summary>
        /// <param name="moveSpeed">수평 이동 속도(m/s)</param>
        private void SetPetMoveSpeed(float moveSpeed)
        {
            if (!_petAnimator) return;

            _petAnimator.SetMoveSpeed(moveSpeed);
        }
        #endregion
    }
}
