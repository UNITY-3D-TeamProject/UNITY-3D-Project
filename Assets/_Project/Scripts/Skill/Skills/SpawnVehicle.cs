using System;
using System.Collections;
using System.Collections.Generic;
using Skill.Buff;
using Skill.Core;
using Skill.Costs;
using UnityEngine;

namespace Skill.Skills
{
    /// <summary>
    /// 탈것에 탑승하고, 다시 발동하면 하차하는 토글 스킬. 탈것 오브젝트는 탑승 연출이 끝날 때 AttachVehicle 로 생성한다. 탑승 중에는 버프 배율을 적용하고 같은 GameObject 의 DrainCost 로 자원을 계속 소모한다.
    /// 자원이 바닥나거나 스킬이 꺼지거나 Stop 되면 하차한다. 코스트(쿨타임)는 하차할 때 지불한다.
    /// 탑승/하차할 때마다 발동을 알리며, 받는 쪽은 IsRiding 과 BuffRatios 로 상태를 읽는다.
    /// 탑승 중에는 체공당 1회 요시 점프(일정 속도 상승)를 쓸 수 있고, 그동안 자원을 추가로 소모한다. 시작·종료는 OnHoverChanged 로 알린다.
    /// </summary>
    public class SpawnVehicle : SkillBase
    {
        #region Serialized Fields
        [Header("Vehicle")]
        [Tooltip("탑승 시 플레이어 루트 아래에 생성할 탈것 프리팹. 비어 있으면 생성하지 않는다.")]
        [SerializeField] private GameObject _vehiclePrefab;
        [Tooltip("플레이어 루트 기준 탈것 생성 위치")]
        [SerializeField] private Vector3 _vehicleOffset = Vector3.zero;
        [Tooltip("지상에서 탑승할 때 뛰어오르는 높이. 지상 점프 대비 높이 비율 (탑승 버프 적용 후 기준)")]
        [SerializeField, Min(0.0f)] private float _mountJumpPowerRatio = 0.15f;
        [Tooltip("탑승 중 자동 전진 방향이 카메라 방향을 따라 도는 속도(도/초)")]
        [SerializeField, Min(0.0f)] private float _forwardTurnSpeed = 720.0f;

        [Header("Hover")]
        [Tooltip("요시 점프 최대 유지 시간(초)")]
        [SerializeField, Min(0.0f)] private float _hoverDuration = 1.5f;
        [Tooltip("요시 점프 중 상승 속도(m/s)")]
        [SerializeField, Min(0.0f)] private float _hoverRiseSpeed = 1.5f;
        [Tooltip("요시 점프 중 DrainCost 초당 소모량에 더할 추가 소모량")]
        [SerializeField, Min(0.0f)] private float _hoverExtraDrainPerSecond = 10.0f;

        [Header("Buff")]
        [Tooltip("탑승 중 적용할 대상별 배율")]
        [SerializeField] private SBuffRatio[] _buffRatios =
        {
            new SBuffRatio(EBuffTarget.MoveSpeed, 1.5f),
            new SBuffRatio(EBuffTarget.JumpPower, 1.25f),
            new SBuffRatio(EBuffTarget.Gravity, 0.7f),
        };
        #endregion

        #region Private Fields
        private DrainCost _drainCost;
        // 하차 시 보낼 배율. _buffRatios 와 같은 대상에 배율 1 을 넣는다
        private SBuffRatio[] _resetRatios;
        private GameObject _vehicle;
        private Coroutine _hoverRoutine;
        // 이번 체공 중 요시 점프를 이미 썼는지. 착지 시 ResetHover 로 초기화한다
        private bool _hasHovered;
        #endregion

        #region Properties
        /// <summary>탑승 중인지 여부.</summary>
        public bool IsRiding { get; private set; }

        /// <summary>현재 적용해야 할 배율. 탑승 중이면 버프 배율, 아니면 같은 대상에 배율 1.</summary>
        public IReadOnlyList<SBuffRatio> BuffRatios => IsRiding ? _buffRatios : _resetRatios;

        /// <summary>생성된 탈것. 없으면 null.</summary>
        public GameObject Vehicle => _vehicle;

        /// <summary>지상에서 탑승할 때 뛰어오르는 높이. 지상 점프 대비 높이 비율.</summary>
        public float MountJumpPowerRatio => _mountJumpPowerRatio;

        /// <summary>탑승 중 자동 전진 방향이 카메라 방향을 따라 도는 속도(도/초).</summary>
        public float ForwardTurnSpeed => _forwardTurnSpeed;

        /// <summary>요시 점프 중 상승 속도(m/s).</summary>
        public float HoverRiseSpeed => _hoverRiseSpeed;

        /// <summary>요시 점프 중인지 여부.</summary>
        public bool IsHovering { get; private set; }

        /// <summary>코스트는 하차할 때 지불한다.</summary>
        protected override bool IsPayOnExecute => false;
        #endregion

        #region Events
        /// <summary>요시 점프를 시작하거나 끝냈을 때 발생한다. 시작이면 true.</summary>
        public event Action<bool> OnHoverChanged;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _drainCost = GetComponent<DrainCost>();
            if (_drainCost) _drainCost.OnDepleted += Dismount;

            _resetRatios = new SBuffRatio[_buffRatios.Length];
            for (int i = 0; i < _buffRatios.Length; i++)
            {
                _resetRatios[i] = new SBuffRatio(_buffRatios[i].Target, 1.0f);
            }
        }

        private void OnDisable()
        {
            Dismount();
        }

        private void OnDestroy()
        {
            if (_drainCost) _drainCost.OnDepleted -= Dismount;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 탑승 중이면 하차한다.
        /// </summary>
        public override void Stop()
        {
            Dismount();
        }

        /// <summary>
        /// 탑승 중이고 아직 탈것이 없으면 플레이어 루트 아래에 탈것을 생성한다.
        /// 탑승 시 뛰어오르는 연출이 끝난 뒤(착지) 호출한다.
        /// </summary>
        public void AttachVehicle()
        {
            if (!IsRiding || _vehicle || !_vehiclePrefab) return;

            _vehicle = Instantiate(_vehiclePrefab, transform.root);
            _vehicle.transform.localPosition = _vehicleOffset;
            _vehicle.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// 탑승 중이고 이번 체공에 아직 쓰지 않았으면 요시 점프를 시작한다. 유지 시간이 지나면 스스로 끝난다.
        /// </summary>
        /// <returns>시작했으면 true</returns>
        public bool TryStartHover()
        {
            if (!IsRiding || IsHovering || _hasHovered) return false;

            _hasHovered = true;
            IsHovering = true;
            if (_drainCost) _drainCost.SetExtraDrainPerSecond(_hoverExtraDrainPerSecond);
            _hoverRoutine = StartCoroutine(CoHover());
            OnHoverChanged?.Invoke(true);
            return true;
        }

        /// <summary>
        /// 요시 점프 중이면 끝낸다.
        /// </summary>
        public void StopHover()
        {
            if (!IsHovering) return;

            if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);
            _hoverRoutine = null;
            IsHovering = false;
            if (_drainCost) _drainCost.SetExtraDrainPerSecond(0.0f);
            OnHoverChanged?.Invoke(false);
        }

        /// <summary>
        /// 요시 점프 사용 여부를 초기화한다. 착지 시 호출한다.
        /// </summary>
        public void ResetHover()
        {
            _hasHovered = false;
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (IsRiding)
            {
                Dismount();
                return;
            }

            Mount();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 자원 소모를 시작한 뒤 탑승을 알린다. 탈것은 AttachVehicle 로 따로 생성한다.
        /// </summary>
        private void Mount()
        {
            if (_drainCost) _drainCost.StartDrain();
            IsRiding = true;
            base.Execute();
        }

        /// <summary>
        /// 탑승 중이면 자원 소모를 멈추고 탈것을 제거한 뒤, 코스트를 지불하고 하차를 알린다.
        /// </summary>
        private void Dismount()
        {
            if (!IsRiding) return;

            StopHover();
            if (_drainCost) _drainCost.StopDrain();
            if (_vehicle) Destroy(_vehicle);
            _vehicle = null;
            IsRiding = false;

            PayCosts();
            base.Execute();
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// 유지 시간이 지나면 요시 점프를 끝낸다.
        /// </summary>
        private IEnumerator CoHover()
        {
            yield return new WaitForSeconds(_hoverDuration);

            _hoverRoutine = null;
            StopHover();
        }
        #endregion
    }
}
