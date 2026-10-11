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
        #endregion

        #region Private Fields
        private UseCountCondition _useCountCondition;
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
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (_useCountCondition) _useCountCondition.Use();
            base.Execute();
        }
        #endregion
    }
}
