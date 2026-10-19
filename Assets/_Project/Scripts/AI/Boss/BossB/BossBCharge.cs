using UnityEngine;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 의 돌진. 제자리에서 예고한 뒤 그때 정한 방향으로 직선 돌진한다.
    /// 앞 칸이 구멍·예고·판 밖이면 칸 가장자리에서 급정거하고 휘청이는 동안 무방비가 된다(공격·이동 없음, 피해 배율 그대로).
    /// 플레이어를 맞히면 피해와 함께 돌진 방향으로 크게 밀어내고 돌진을 멈춘다(휘청 없음).
    /// 예고 시작 때 NotifyChargePrepared 를 알려, A 가 돌진 경로 옆 칸을 칠 수 있게 한다.
    /// </summary>
    public class BossBCharge : BossBSkillBase
    {
        #region Nested Types
        private enum EChargePhase
        {
            Telegraph,
            Charging,
            Stagger,
        }
        #endregion

        #region Serialized Fields
        [Header("Charge")]
        [Tooltip("제자리 예고 시간(초). 임시값")]
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.5f;
        [Tooltip("돌진 속도(m/s). 임시값")]
        [SerializeField, Min(0.1f)] private float _speed = 18.0f;
        [Tooltip("한 번에 돌진하는 최대 거리(m). 이만큼 가면 휘청 없이 끝난다")]
        [SerializeField, Min(1.0f)] private float _maxDistance = 60.0f;
        [Tooltip("B 중심에서 앞쪽 몸 끝까지 거리(m). 이 앞 지점이 구멍이면 멈춘다")]
        [SerializeField, Min(0.0f)] private float _bodyRadius = 2.0f;
        [Tooltip("B 중심에서 대상까지 수평 거리가 이 안이면 맞는다(m)")]
        [SerializeField, Min(0.0f)] private float _hitRadius = 3.0f;
        [Tooltip("급정거 뒤 무방비로 휘청이는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _staggerSeconds = 2.0f;

        [Header("Knockback")]
        [SerializeField, Min(0.0f)] private float _knockbackDistance = 12.0f;
        [SerializeField, Min(0.05f)] private float _knockbackSeconds = 0.4f;

        [Header("Motion")]
        [Tooltip("돌진이 시작될 때 요청할 모션 알림 스킬 이름. 비우면 요청하지 않는다. 예고 모션(Ready)은 이 스킬 자신의 이름으로 재생된다")]
        [SerializeField] private string _runMotionSkillName = "ChargeRun";
        [Tooltip("돌진이 끝날 때(급정거·적중·최대 거리) 요청할 모션 알림 스킬 이름. 비우면 요청하지 않는다")]
        [SerializeField] private string _stopMotionSkillName = "ChargeStop";
        #endregion

        #region Private Fields
        private EChargePhase _phase;
        private Vector3 _direction;
        private float _timer;
        private float _traveled;
        private bool _isPrepared;
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            float holdSeconds = _telegraphSeconds + (_maxDistance / _speed) + _staggerSeconds;
            if (!BeginRun(true, holdSeconds)) return;

            _phase = EChargePhase.Telegraph;
            _direction = GetDirectionToTarget();
            _timer = _telegraphSeconds;
            _traveled = 0.0f;

            _isPrepared = true;
            Boss.NotifyChargePrepared(Controller.Position, _direction);

            base.Execute();
        }

        protected override void Tick(float deltaTime)
        {
            switch (_phase)
            {
                case EChargePhase.Telegraph:
                    TickTelegraph(deltaTime);
                    break;
                case EChargePhase.Charging:
                    TickCharging(deltaTime);
                    break;
                default:
                    TickStagger(deltaTime);
                    break;
            }
        }

        protected override void OnRunEnded()
        {
            if (!_isPrepared) return;

            _isPrepared = false;
            Boss.NotifyChargeEnded();
        }
        #endregion

        #region Private Methods
        private void TickTelegraph(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0.0f) return;

            _phase = EChargePhase.Charging;
            PlayMotion(_runMotionSkillName);
        }

        private void TickCharging(float deltaTime)
        {
            float step = _speed * deltaTime;
            Vector3 front = Controller.Position + (_direction * (_bodyRadius + step));

            if (IsBlocked(front))
            {
                _phase = EChargePhase.Stagger;
                _timer = _staggerSeconds;
                PlayMotion(_stopMotionSkillName);
                return;
            }

            SelfMotor.AddExternalDisplacement(_direction * step);
            _traveled += step;

            if (TryHitTarget(Controller.Position, _hitRadius))
            {
                KnockbackTarget(_direction, _knockbackDistance, _knockbackSeconds);
                PlayMotion(_stopMotionSkillName);
                EndRun();
                return;
            }

            if (_traveled < _maxDistance) return;

            PlayMotion(_stopMotionSkillName);
            EndRun();
        }

        private void TickStagger(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer <= 0.0f) EndRun();
        }

        // 구간 모션으로 바꾼다(중재자를 거치는 스킬 요청)
        private void PlayMotion(string motionSkillName)
        {
            if (!string.IsNullOrEmpty(motionSkillName)) Controller.ExecuteSkill(motionSkillName);
        }

        private bool IsBlocked(Vector3 point)
        {
            if (!Arena.TryGetTile(point, out int tile)) return true;

            return Arena.IsHoleOrTelegraphed(tile);
        }
        #endregion
    }
}
