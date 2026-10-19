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

        [Header("Camera Feedback")]
        [Tooltip("구르는 동안 카메라가 따라오는 Damping 시간(초). 클수록 느리게 따라온다")]
        [SerializeField, Min(0.0f)] private float _cameraFollowDampingTime = 0.1f;
        [Tooltip("구르기가 끝난 뒤 카메라 Damping 이 원래 값으로 돌아오는 시간(초). 0 이면 즉시 복귀")]
        [SerializeField, Min(0.0f)] private float _cameraDampingRecoverTime = 0.15f;
        #endregion

        #region Private Fields
        private UseCountCondition _useCountCondition;
        #endregion

        #region Properties
        /// <summary>구르기 이동 거리.</summary>
        public float Distance => _distance;

        /// <summary>구르기 이동에 걸리는 시간(초).</summary>
        public float Duration => _duration;

        /// <summary>구르는 동안 카메라가 따라오는 Damping 시간(초). 클수록 느리게 따라온다.</summary>
        public float CameraFollowDampingTime => _cameraFollowDampingTime;

        /// <summary>구르는 동안 카메라가 따라오는 Damping (x, y, z). 수직(y)은 0 으로 둔다.</summary>
        public Vector3 CameraFollowDamping => new Vector3(_cameraFollowDampingTime, 0.0f, _cameraFollowDampingTime);

        /// <summary>구르기가 끝난 뒤 카메라 Damping 이 원래 값으로 돌아오는 시간(초).</summary>
        public float CameraDampingRecoverTime => _cameraDampingRecoverTime;
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
