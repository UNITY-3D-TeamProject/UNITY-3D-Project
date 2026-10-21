using System;
using UnityEngine;
using Pet;

namespace Mediator.SubMediators
{
    /// <summary>
    /// PetSpawner 가 생성한 펫의 PetFollower(이동)와 PetAnimator(애니메이션)를 연결하고,
    /// 다른 Mediator 에서 온 캐릭터 상태(점프, 피격, 특수 Idle, 사망)와 스캔 요청을 펫에 전달하는 접착 컴포넌트.
    /// 스캔 요청을 받으면 펫을 스캔 위치(_scanPivot)로 보내고(PetFollower.MoveAndHold), 도착 위치를 스캔 위치로 알린다.
    /// SetPetEnabled 로 펫 오브젝트를 켜고 끄며, 꺼져 있는 동안에는 캐릭터 상태를 펫에 전달하지 않는다.
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
        // 펫 생성 전에 정한 값도 생성 직후 적용한다
        private bool _isPetEnabled = true;
        // 펫이 스캔 위치로 이동 중일 때 도착 위치를 받을 콜백. 이동 중 펫을 끄면 바로 호출한다
        private Action<Vector3> _pendingScanReady;
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
        /// 펫을 켜거나 끈다. 켜면 펫이 따라갈 위치로 바로 옮긴다. 펫 생성 전이면 생성 직후 적용한다.
        /// </summary>
        /// <param name="isEnabled">켜려면 true</param>
        public void SetPetEnabled(bool isEnabled)
        {
            if (_isPetEnabled == isEnabled) return;

            _isPetEnabled = isEnabled;
            ApplyPetEnabled();
        }

        /// <summary>
        /// 펫이 켜져 있는지 반환한다.
        /// </summary>
        /// <returns>펫이 켜져 있으면 true. 펫 생성 전이면 생성 직후 적용할 값</returns>
        public bool IsPetEnabled()
        {
            return _isPetEnabled;
        }

        /// <summary>
        /// 점프 시작을 펫에 전달한다.
        /// </summary>
        public void NotifyJumpStarted()
        {
            if (!_isPetEnabled || !_petAnimator) return;

            _petAnimator.PlayJump();
        }

        /// <summary>
        /// 착지를 펫에 전달한다.
        /// </summary>
        public void NotifyJumpEnded()
        {
            if (!_isPetEnabled || !_petAnimator) return;

            _petAnimator.PlayLand();
        }

        /// <summary>
        /// 피격을 펫에 전달한다.
        /// </summary>
        /// <param name="direction">캐릭터 기준 피격 방향. 0: 앞, 1: 뒤, 2: 왼쪽, 3: 오른쪽</param>
        public void NotifyHit(int direction)
        {
            if (!_isPetEnabled || !_petAnimator) return;

            _petAnimator.PlayHit(direction);
        }

        /// <summary>
        /// 특수 Idle 시작을 펫에 전달한다.
        /// </summary>
        /// <param name="specialIdleIndex">캐릭터가 재생한 특수 Idle 인덱스</param>
        public void NotifySpecialIdle(int specialIdleIndex)
        {
            if (!_isPetEnabled || !_petAnimator) return;

            _petAnimator.PlaySpecialIdle(specialIdleIndex);
        }

        /// <summary>
        /// 사망을 펫에 전달한다. 추종(진행 중인 스캔 이동 포함)을 멈추고 사망 애니메이션을 재생한다. 펫이 꺼져 있으면 애니메이션은 재생하지 않는다.
        /// </summary>
        public void NotifyDeath()
        {
            if (_pet) _pet.StopFollow();
            if (_isPetEnabled && _petAnimator) _petAnimator.PlayDeath();
        }

        /// <summary>
        /// 펫을 스캔 위치로 보내고, 도착하면 그 위치를 onReady 로 알린다.
        /// 펫이나 스캔 위치가 없거나 펫이 꺼져 있으면 기다리지 않고 pivot(없으면 이 오브젝트) 위치를 바로 알린다.
        /// 이동 중 펫을 끄면 그때 펫 위치를 바로 알린다. 펫이 꺼져 있을 때 스캔 실행 자체는 SkillMediator 에서 막는다.
        /// </summary>
        /// <param name="onReady">스캔 위치(월드 좌표)를 받을 콜백</param>
        public void RequestScan(Action<Vector3> onReady)
        {
            if (!_pet || !_scanPivot || !_isPetEnabled)
            {
                Transform pivot = _petSpawner ? _petSpawner.Pivot : null;
                onReady?.Invoke(pivot ? pivot.position : transform.position);
                return;
            }

            _pendingScanReady = onReady;
            _pet.MoveAndHold(_scanPivot, CompleteScanReady);
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
            // 생성 전에 꺼 둔 상태를 적용한다
            if (_pet && !_isPetEnabled) _pet.gameObject.SetActive(false);
        }

        /// <summary>
        /// 현재 켜짐 상태를 펫 오브젝트에 적용한다. 켜면 따라갈 위치로 바로 옮긴다.
        /// </summary>
        private void ApplyPetEnabled()
        {
            if (!_pet) return;

            // 꺼지면 이동 코루틴이 멈춰 도착 알림이 오지 않으므로, 기다리던 스캔은 지금 펫 위치에서 시작한다
            if (!_isPetEnabled) CompleteScanReady(_pet.transform.position);

            _pet.gameObject.SetActive(_isPetEnabled);
            if (_isPetEnabled && _petSpawner) _pet.SetTarget(_petSpawner.Pivot);
        }

        /// <summary>
        /// 기다리던 스캔 콜백에 스캔 위치를 알리고 비운다. 기다리는 콜백이 없으면 아무것도 하지 않는다.
        /// </summary>
        /// <param name="position">스캔 위치(월드 좌표)</param>
        private void CompleteScanReady(Vector3 position)
        {
            Action<Vector3> onReady = _pendingScanReady;
            _pendingScanReady = null;
            onReady?.Invoke(position);
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
