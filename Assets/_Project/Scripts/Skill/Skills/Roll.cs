using Skill.Conditions;
using Skill.Core;
using UnityEngine;

namespace Skill.Skills
{
    public class Roll : SkillBase
    {
        #region Serialized Fields
        [Header("Roll")]
        [Tooltip("구르기 이동 거리")]
        [SerializeField, Min(0.0f)] private float _distance = 3.0f;
        [Tooltip("구르기 이동에 걸리는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _duration = 0.3f;
        #endregion

        #region Private Fields
        private UseCountCondition _useCountCondition;
        #endregion

        #region Properties
        /// <summary>구르기 이동 거리.</summary>
        public float Distance => _distance;

        /// <summary>구르기 이동에 걸리는 시간(초).</summary>
        public float Duration => _duration;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _useCountCondition = GetComponent<UseCountCondition>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 사용 횟수를 초기화한다.
        /// </summary>
        public override void ResetState()
        {
            if (_useCountCondition) _useCountCondition.ResetCount();
        }
        #endregion

        protected override void Execute()
        {
            Debug.Log("Roll");
            if (_useCountCondition) _useCountCondition.Use();
            base.Execute();
        }
    }
}
