using Core.Effect;
using Skill.Conditions;
using Skill.Core;
using UnityEngine;

namespace Skill.Skills
{
    /// <summary>
    /// 공중 점프 가능 여부를 판단하는 스킬. 실제 점프는 하지 않는다.
    /// 횟수는 같은 GameObject 의 UseCountCondition 으로 세며, 최대 횟수는 지상 점프를 포함한다.
    /// 이 스킬은 공중에서만 요청되므로 초기화 시 지상 점프를 사용한 상태로 둔다.
    /// </summary>
    public class MultiJump : SkillBase
    {
        #region Serialized Fields
        [Header("MultiJump")]
        [Tooltip("공중 점프 높이 비율. 지상 점프 높이 대비 값이다. (0.5 = 절반 높이)")]
        [SerializeField, Min(0.0f)] private float _jumpPowerRatio = 1.0f;

        [Header("Vfx")]
        [Tooltip("공중 점프할 때 한 번 재생할 VFX. 비어 있으면 재생하지 않는다")]
        [SerializeField] private SOEffectData _jumpVfx;
        [Tooltip("점프 VFX 를 붙일 위치. 재생 방향은 이 Transform 의 회전을 따른다")]
        [SerializeField] private Transform _jumpVfxPoint;
        #endregion

        #region Private Fields
        private UseCountCondition _useCountCondition;
        private bool _isJumpVfxEnabled = true;
        #endregion

        #region Properties
        /// <summary>공중 점프 높이 비율. 지상 점프 높이 대비 값이다.</summary>
        public float JumpPowerRatio => _jumpPowerRatio;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _useCountCondition = GetComponent<UseCountCondition>();
            ResetState();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 사용 횟수를 초기화하고, 지상 점프를 사용한 상태로 둔다.
        /// </summary>
        public override void ResetState()
        {
            if (!_useCountCondition) return;

            _useCountCondition.ResetCount();
            _useCountCondition.Use();
        }

        /// <summary>
        /// 공중 점프 VFX 재생 여부를 정한다. 보드 탑승 중처럼 VFX 를 내지 않아야 할 때 끈다.
        /// </summary>
        /// <param name="isEnabled">재생하려면 true</param>
        public void SetJumpVfxEnabled(bool isEnabled)
        {
            _isJumpVfxEnabled = isEnabled;
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (_useCountCondition) _useCountCondition.Use();
            PlayJumpVfx();
            base.Execute();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 점프 VFX 위치를 따라가는 점프 VFX 를 한 번 재생한다. 꺼져 있거나 VFX, 위치, EffectManager 중 하나라도 없으면 생략한다.
        /// </summary>
        private void PlayJumpVfx()
        {
            if (!_isJumpVfxEnabled) return;
            if ((_jumpVfx == null) || (_jumpVfxPoint == null) || (EffectManager.Instance == null)) return;

            EffectManager.Instance.Play(
                _jumpVfx,
                _jumpVfxPoint.position,
                _jumpVfxPoint.rotation,
                _jumpVfxPoint);
        }
        #endregion
    }
}
