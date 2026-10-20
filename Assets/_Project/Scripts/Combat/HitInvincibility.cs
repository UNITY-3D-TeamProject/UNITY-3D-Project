using System;
using UnityEngine;

namespace Combat
{
    /// <summary>
    /// 피격 후 무적 시간을 재는 컴포넌트.
    /// 무적 상태와 종료 알림만 제공하며, 데미지 차단이나 연출은 하지 않는다.
    /// UnityEngine 외 어떤 시스템도 참조하지 않는다.
    /// </summary>
    public class HitInvincibility : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("무적 지속 시간(초)")]
        [SerializeField, Min(0.0f)] private float _duration = 1.0f;
        #endregion

        #region Private Fields
        private float _remainTime;
        #endregion

        #region Properties
        /// <summary>무적 중이면 true.</summary>
        public bool IsInvincible { get; private set; }
        #endregion

        #region Events
        /// <summary>무적 시간이 끝났을 때 발생한다. Stop 으로 멈춘 경우에는 발생하지 않는다.</summary>
        public event Action OnEnded;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!IsInvincible) return;

            _remainTime -= Time.deltaTime;
            if (_remainTime > 0.0f) return;

            IsInvincible = false;
            OnEnded?.Invoke();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 무적을 시작한다. 무적 중이면 남은 시간을 처음부터 다시 잰다.
        /// </summary>
        public void Begin()
        {
            _remainTime = _duration;
            IsInvincible = true;
        }

        /// <summary>
        /// 무적을 즉시 끝낸다. OnEnded 는 발생하지 않는다.
        /// </summary>
        public void Stop()
        {
            _remainTime = 0.0f;
            IsInvincible = false;
        }

        /// <summary>
        /// 무적 지속 시간을 바꾼다. 진행 중인 무적의 남은 시간은 그대로 두고, 다음 Begin 부터 적용한다.
        /// </summary>
        /// <param name="duration">무적 지속 시간(초). 음수는 0 으로 처리한다</param>
        public void SetDuration(float duration)
        {
            _duration = Mathf.Max(0.0f, duration);
        }
        #endregion
    }
}
