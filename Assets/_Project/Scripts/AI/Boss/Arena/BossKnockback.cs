using UnityEngine;
using Movement;

namespace AI.Boss.Arena
{
    /// <summary>
    /// 대상 모터를 정해진 거리만큼 ease-out 으로 밀어내는 공용 도우미. MonoBehaviour 가 아니라 호출 측이 들고 쓴다.
    /// 호출 측 Update 에서 매 프레임 Tick 을 부르고, 호출 측 클래스는 [DefaultExecutionOrder(-50)] 이어야 이번 프레임 이동에 합쳐진다.
    /// </summary>
    public class BossKnockback
    {
        #region Private Fields
        private CharacterMotor _motor;
        private Vector3 _displacement;
        private float _duration;
        private float _elapsed;
        #endregion

        #region Properties
        public bool IsActive { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// 넉백을 시작한다. 진행 중이면 새 넉백으로 바꾼다.
        /// </summary>
        /// <param name="motor">밀어낼 대상 모터</param>
        /// <param name="displacement">duration 동안 움직일 총 변위(m)</param>
        /// <param name="duration">걸리는 시간(초). 0 이하면 다음 Tick 에 한 번에 민다</param>
        public void Start(CharacterMotor motor, Vector3 displacement, float duration)
        {
            _motor = motor;
            _displacement = displacement;
            _duration = duration;
            _elapsed = 0.0f;
            IsActive = motor;
        }

        /// <summary>
        /// 이번 프레임 몫만큼 민다. 모터가 파괴되었으면 끝낸다.
        /// </summary>
        /// <returns>아직 진행 중이면 true</returns>
        public bool Tick(float deltaTime)
        {
            if (!IsActive) return false;

            if (!_motor)
            {
                Cancel();
                return false;
            }

            // 처음에 빠르고 끝으로 갈수록 느려지게 미는 증분 방식 (EnemyWindBlast 와 같다)
            float previous = _elapsed;
            _elapsed = Mathf.Min(_elapsed + deltaTime, _duration);
            float previousRatio = (_duration > 0.0f) ? previous / _duration : 0.0f;
            float currentRatio = (_duration > 0.0f) ? _elapsed / _duration : 1.0f;
            float moved = EaseOut(currentRatio) - EaseOut(previousRatio);
            _motor.AddExternalDisplacement(_displacement * moved);

            if (currentRatio >= 1.0f) Cancel();

            return IsActive;
        }

        public void Cancel()
        {
            IsActive = false;
            _motor = null;
        }
        #endregion

        #region Private Methods
        private static float EaseOut(float t)
        {
            return 1.0f - ((1.0f - t) * (1.0f - t));
        }
        #endregion
    }
}
