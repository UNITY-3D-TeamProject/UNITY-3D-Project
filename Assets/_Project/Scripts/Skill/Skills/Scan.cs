using Core.Scanning;
using Skill.Core;
using UnityEngine;

namespace Skill.Skills
{
    /// <summary>
    /// 스캔 스킬. 발동하면 알리기만 하고, 파동은 발동 위치가 정해진 뒤 BeginWave 로 시작한다.
    /// 파동(ScanWave)은 이 스킬 아래에 상주하며 월드 공간으로 재생되므로 플레이어를 따라가지 않는다.
    /// </summary>
    public class Scan : SkillBase
    {
        #region Serialized Fields
        [Header("Scan")]
        [Tooltip("스캔 파동. 이 스킬 아래에 켜진 상태로 둔다.")]
        [SerializeField] private ScanWave _scanWave;
        [Tooltip("파동이 확산하는 시간이자, 닿은 대상이 활성화되는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _duration = 6.0f;
        [Tooltip("파동의 최대 지름(m)")]
        [SerializeField, Min(0.0f)] private float _maxDiameter = 30.0f;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            if (!_scanWave) _scanWave = GetComponentInChildren<ScanWave>(true);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// origin 에서 스캔 파동을 시작한다. 진행 중이던 파동은 새 스캔으로 다시 시작한다.
        /// </summary>
        /// <param name="origin">파동의 중심(월드 좌표)</param>
        public void BeginWave(Vector3 origin)
        {
            TryBeginWave(origin);
        }

        /// <summary>
        /// BeginWave 와 같이 파동을 시작하고, 실제로 시작했는지 알린다.
        /// </summary>
        /// <param name="origin">파동의 중심(월드 좌표)</param>
        /// <returns>파동을 시작했으면 true. 파동이 없거나 시작하지 못했으면 false</returns>
        public bool TryBeginWave(Vector3 origin)
        {
            if (!_scanWave) return false;

            _scanWave.Begin(origin, _duration, _maxDiameter);
            // 파티클이 없거나 재생되지 않으면 파동 판정도 진행되지 않으므로 시작하지 못한 것으로 본다
            return _scanWave.IsPlaying;
        }
        #endregion

        #region Protected Methods
        // 파동 위치는 펫 이동 후 정해지므로 여기서는 발동만 알리고, 파동은 BeginWave 로 시작한다
        protected override void Execute()
        {
            base.Execute();
        }
        #endregion
    }
}
