using System.Collections.Generic;
using Skill.Buff;
using Skill.Core;
using Skill.Costs;
using UnityEngine;

namespace Skill.Skills
{
    /// <summary>
    /// 탈것을 소환해 탑승하고, 다시 발동하면 하차하는 토글 스킬. 탑승 중에는 버프 배율을 적용하고 같은 GameObject 의 DrainCost 로 자원을 계속 소모한다.
    /// 자원이 바닥나거나 스킬이 꺼지거나 Stop 되면 하차한다. 코스트(쿨타임)는 하차할 때 지불한다.
    /// 탑승/하차할 때마다 발동을 알리며, 받는 쪽은 IsRiding 과 BuffRatios 로 상태를 읽는다.
    /// </summary>
    public class SpawnVehicle : SkillBase
    {
        #region Serialized Fields
        [Header("Vehicle")]
        [Tooltip("탑승 시 플레이어 루트 아래에 생성할 탈것 프리팹. 비어 있으면 생성하지 않는다.")]
        [SerializeField] private GameObject _vehiclePrefab;
        [Tooltip("플레이어 루트 기준 탈것 생성 위치")]
        [SerializeField] private Vector3 _vehicleOffset = Vector3.zero;

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
        #endregion

        #region Properties
        /// <summary>탑승 중인지 여부.</summary>
        public bool IsRiding { get; private set; }

        /// <summary>현재 적용해야 할 배율. 탑승 중이면 버프 배율, 아니면 같은 대상에 배율 1.</summary>
        public IReadOnlyList<SBuffRatio> BuffRatios => IsRiding ? _buffRatios : _resetRatios;

        /// <summary>코스트는 하차할 때 지불한다.</summary>
        protected override bool IsPayOnExecute => false;
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
        /// 탈것을 생성하고 자원 소모를 시작한 뒤 탑승을 알린다.
        /// </summary>
        private void Mount()
        {
            if (_vehiclePrefab)
            {
                _vehicle = Instantiate(_vehiclePrefab, transform.root);
                _vehicle.transform.localPosition = _vehicleOffset;
                _vehicle.transform.localRotation = Quaternion.identity;
            }

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

            if (_drainCost) _drainCost.StopDrain();
            if (_vehicle) Destroy(_vehicle);
            _vehicle = null;
            IsRiding = false;

            PayCosts();
            base.Execute();
        }
        #endregion
    }
}
