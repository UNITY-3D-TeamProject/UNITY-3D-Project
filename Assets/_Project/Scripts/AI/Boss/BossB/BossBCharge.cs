using UnityEngine;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 의 돌진. 제자리에서 예고한 뒤 그때 정한 방향으로 직선 돌진한다.
    /// 예고 동안 B 앞에서 돌진이 멈출 지점까지 직선 예고가 페이드 인으로 나타나고, 돌진이 끝나면 사라진다.
    /// 앞 칸이 구멍·예고·판 밖이면 칸 가장자리에서 급정거하고 휘청이는 동안 무방비가 된다(공격·이동 없음, 피해 배율 그대로).
    /// 플레이어를 맞히면 피해와 함께 돌진 방향으로 크게 밀어내고 돌진을 멈춘다(휘청 없음).
    /// 예고 시작 때 NotifyChargePrepared 를 알려, A 가 돌진 경로 옆 칸을 칠 수 있게 한다.
    /// </summary>
    public class BossBCharge : BossBSkillBase
    {
        #region Constants
        /// <summary>직선 예고 길이를 잴 때 앞으로 한 번에 살펴보는 거리(m).</summary>
        private const float LINE_PROBE_STEP = 0.5f;
        #endregion

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
        [Tooltip("돌진에 맞은 대상을 돌진 방향으로 밀어내는 거리(m)")]
        [SerializeField, Min(0.0f)] private float _knockbackDistance = 12.0f;
        [Tooltip("밀려나는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.05f)] private float _knockbackSeconds = 0.4f;

        [Header("Motion")]
        [Tooltip("돌진이 시작될 때 요청할 모션 알림 스킬 이름. 비우면 요청하지 않는다. 예고 모션(Ready)은 이 스킬 자신의 이름으로 재생된다")]
        [SerializeField] private string _runMotionSkillName = "ChargeRun";
        [Tooltip("돌진이 끝날 때(급정거·적중·최대 거리) 요청할 모션 알림 스킬 이름. 비우면 요청하지 않는다")]
        [SerializeField] private string _stopMotionSkillName = "ChargeStop";

        [Header("References")]
        [Tooltip("직선 예고 판 프리팹(가로·세로 1 m). 로컬 X 가 폭, Z 가 길이다. 비우면 예고를 띄우지 않는다")]
        [SerializeField] private GameObject _linePrefab;
        [Tooltip("직선 예고 판을 B 발밑에서 띄우는 높이(m)")]
        [SerializeField, Min(0.0f)] private float _lineHeight = 0.05f;
        #endregion

        #region Private Fields
        private EChargePhase _phase;
        private Vector3 _direction;
        private float _timer;
        private float _traveled;
        private bool _isPrepared;
        private readonly BossBTelegraph _line = new BossBTelegraph();
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

            _line.Show(_linePrefab);
            PlaceLine();

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
            _line.Hide();

            if (!_isPrepared) return;

            _isPrepared = false;
            Boss.NotifyChargeEnded();
        }
        #endregion

        #region Private Methods
        private void TickTelegraph(float deltaTime)
        {
            _timer -= deltaTime;
            // 예고하는 사이 앞 칸이 구멍·예고로 바뀔 수 있어 멈출 지점을 매 프레임 다시 잰다
            PlaceLine();
            float progress = (_telegraphSeconds > 0.0f) ? (1.0f - (_timer / _telegraphSeconds)) : 1.0f;
            _line.SetFade(progress);
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
                _line.Hide();
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

        /// <summary>
        /// B 발밑에서 돌진이 멈출 지점의 몸 앞 끝까지 직선 예고를 놓는다. 폭은 적중 반경 × 2 다.
        /// </summary>
        private void PlaceLine()
        {
            Vector3 origin = Controller.Position;
            float length = _bodyRadius + MeasureChargeDistance(origin);
            Vector3 center = origin + (_direction * (length * 0.5f)) + (Vector3.up * _lineHeight);
            _line.Place(center, Quaternion.LookRotation(_direction, Vector3.up), _hitRadius * 2.0f, length);
        }

        /// <summary>
        /// 지금 칸 상태로 origin 에서 돌진하면 멈추기까지 갈 거리(m). 최대 거리를 넘지 않는다.
        /// </summary>
        private float MeasureChargeDistance(Vector3 origin)
        {
            float distance = 0.0f;
            while (distance < _maxDistance)
            {
                float next = Mathf.Min(distance + LINE_PROBE_STEP, _maxDistance);
                if (IsBlocked(origin + (_direction * (_bodyRadius + next)))) break;

                distance = next;
            }
            return distance;
        }

        private bool IsBlocked(Vector3 point)
        {
            if (!Arena.TryGetTile(point, out int tile)) return true;

            return Arena.IsHoleOrTelegraphed(tile);
        }
        #endregion
    }
}
