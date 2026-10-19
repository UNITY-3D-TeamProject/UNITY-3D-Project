using UnityEngine;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 의 도약 내려찍기(1-2 부터). 일반은 플레이어가 선 칸에 원형 예고를 띄운 뒤 뛰어올라 내려찍고,
    /// 착지 순간 피해와 칸 주변 충격파 넉백을 주고 경직한다. 착지 직전에 그 칸이 구멍·예고면 가장 가까운 멀쩡한 칸으로 바꾼다.
    /// 회피(<c>_isEvade</c>)는 예고 없이 가장 가까운 멀쩡한 칸으로 뛰어 건너가며, 피해·충격파·경직이 없다.
    /// 포물선은 CharacterMotor 의 점프(중력)와 수평 변위로 만든다: 체공 시간 T 에 맞춰 초기 속도 = 중력 × T ÷ 2.
    /// </summary>
    public class BossBLeap : BossBSkillBase
    {
        #region Constants
        /// <summary>체공 시간이 지나도 착지하지 못할 때 강제로 끝내기까지의 여유(초).</summary>
        private const float FLIGHT_OVERTIME_SECONDS = 1.0f;

        /// <summary>체공 시간이 끝난 뒤 착지 칸이 바뀌었을 때 남은 거리를 따라잡는 시간(초).</summary>
        private const float CATCH_UP_SECONDS = 0.2f;

        /// <summary>착지 칸 중심까지 이 수평 거리(m) 안이어야 착지로 본다.</summary>
        private const float ARRIVAL_TOLERANCE = 1.0f;
        #endregion

        #region Nested Types
        private enum ELeapPhase
        {
            Telegraph,
            Flight,
            Recovery,
        }
        #endregion

        #region Serialized Fields
        [SerializeField] private bool _isEvade;

        [Header("Leap")]
        [Tooltip("착지 칸 원형 예고 시간(초). 회피에서는 쓰지 않는다")]
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.2f;
        [Tooltip("체공 시간(초). 임시값")]
        [SerializeField, Min(0.2f)] private float _flightSeconds = 1.0f;
        [Tooltip("착지 후 경직 시간(초). 회피에서는 쓰지 않는다")]
        [SerializeField, Min(0.0f)] private float _recoverySeconds = 1.5f;

        [Header("Landing")]
        [Tooltip("착지점에서 대상까지 수평 거리가 이 안이면 피해를 받는다(m). 임시값")]
        [SerializeField, Min(0.0f)] private float _landingRadius = 4.0f;
        [Tooltip("충격파 반경(칸 수). 이 안의 대상이 착지점에서 바깥으로 밀린다")]
        [SerializeField, Min(0.0f)] private float _shockwaveTiles = 1.0f;
        [SerializeField, Min(0.0f)] private float _knockbackDistance = 8.0f;
        [SerializeField, Min(0.05f)] private float _knockbackSeconds = 0.4f;

        [Header("References")]
        [Tooltip("예고 원판 프리팹(지름 1 m). 회피에서는 쓰지 않는다")]
        [SerializeField] private GameObject _circlePrefab;
        [Tooltip("예고 원판을 바닥에서 띄우는 높이(m)")]
        [SerializeField, Min(0.0f)] private float _circleHeight = 0.05f;
        #endregion

        #region Private Fields
        private ELeapPhase _phase;
        private int _landingTile;
        private float _timer;
        private GameObject _circle;
        #endregion

        #region Properties
        public override bool IsEvade => _isEvade;
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            float holdSeconds = (_isEvade ? 0.0f : _telegraphSeconds) + _flightSeconds + _recoverySeconds;
            if (!BeginRun(!_isEvade, holdSeconds)) return;

            _landingTile = ChooseLandingTile();
            if (_landingTile < 0)
            {
                EndRun();
                return;
            }

            base.Execute();

            if (_isEvade)
            {
                StartFlight();
                return;
            }

            _phase = ELeapPhase.Telegraph;
            _timer = _telegraphSeconds;
            ShowCircle();
        }

        protected override void Tick(float deltaTime)
        {
            switch (_phase)
            {
                case ELeapPhase.Telegraph:
                    TickTelegraph(deltaTime);
                    break;
                case ELeapPhase.Flight:
                    TickFlight(deltaTime);
                    break;
                default:
                    TickRecovery(deltaTime);
                    break;
            }
        }

        protected override void OnRunEnded()
        {
            DestroyCircle();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 회피는 B 에서 가장 가까운 멀쩡한 칸, 일반은 플레이어가 선 칸(판 밖이면 가장 가까운 멀쩡한 칸).
        /// </summary>
        private int ChooseLandingTile()
        {
            if (_isEvade) return Arena.FindNearestSafe(Controller.Position);

            if (Arena.TryGetTile(Target.position, out int tile)) return tile;

            return Arena.FindNearestSafe(Target.position);
        }

        private void TickTelegraph(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0.0f) return;

            // 예고하는 사이 칸이 구멍·예고로 바뀌었으면 가장 가까운 멀쩡한 칸으로 바꾼다
            if (!RetargetIfUnsafe())
            {
                EndRun();
                return;
            }

            StartFlight();
        }

        /// <summary>
        /// 착지 칸이 구멍·예고면 가장 가까운 멀쩡한 칸으로 바꾼다. 회피는 B 위치, 일반은 대상 위치 기준이다.
        /// </summary>
        /// <returns>멀쩡한 착지 칸이 있으면 true</returns>
        private bool RetargetIfUnsafe()
        {
            if (!Arena.IsHoleOrTelegraphed(_landingTile)) return true;

            Vector3 reference = _isEvade ? Controller.Position : Target.position;
            _landingTile = Arena.FindNearestSafe(reference);
            if (_landingTile < 0) return false;

            MoveCircle();
            return true;
        }

        private void StartFlight()
        {
            _phase = ELeapPhase.Flight;
            _timer = 0.0f;

            RefreshHold(_flightSeconds + _recoverySeconds);

            // 체공 시간 T 에 같은 높이로 돌아오는 초기 상승 속도
            SelfMotor.Jump(SelfMotor.Gravity * _flightSeconds * 0.5f);
        }

        private void TickFlight(float deltaTime)
        {
            // 체공 중에도 착지 칸이 구멍·예고로 바뀔 수 있다
            if (!RetargetIfUnsafe())
            {
                EndRun();
                return;
            }

            // 남은 수평 거리를 남은 비율만큼 나눠 가므로 도중에 착지 칸이 바뀌어도 새 칸에 맞는다.
            // 체공 시간이 끝난 뒤에 바뀌었으면 짧은 시간 안에 따라잡는다
            Vector3 remaining = Vector3.ProjectOnPlane(
                Arena.GetTileCenter(_landingTile) - Controller.Position,
                Vector3.up);
            float previousRatio = Mathf.Min(_timer / _flightSeconds, 1.0f);
            _timer += deltaTime;
            float currentRatio = Mathf.Min(_timer / _flightSeconds, 1.0f);
            float fraction = (previousRatio < 1.0f)
                ? (currentRatio - previousRatio) / (1.0f - previousRatio)
                : Mathf.Min(deltaTime / CATCH_UP_SECONDS, 1.0f);
            SelfMotor.AddExternalDisplacement(remaining * fraction);

            // 점프 직후에는 아직 지면에 닿은 것으로 남아 있으므로 체공 절반이 지난 뒤부터 착지를 본다.
            // 착지 칸에 가까이 도착하지 못했으면 아직 착지로 보지 않는다
            bool isDescending = _timer >= (_flightSeconds * 0.5f);
            bool isArrived = remaining.magnitude <= ARRIVAL_TOLERANCE;
            bool isLanded = isDescending && isArrived && SelfMotor.IsGrounded;
            bool isOvertime = _timer >= (_flightSeconds + FLIGHT_OVERTIME_SECONDS);
            if (isLanded)
            {
                Land(Controller.Position);
            }
            else if (isOvertime)
            {
                // 시간 안에 도착하지 못했으면 이번 틱에 더한 몫을 뺀 남은 거리를 한 번에 옮기고,
                // 피해는 안전한 착지 칸 중심 기준으로 준다
                SelfMotor.AddExternalDisplacement(remaining * (1.0f - fraction));
                Land(Arena.GetTileCenter(_landingTile));
            }
        }

        private void Land(Vector3 landing)
        {
            if (_isEvade)
            {
                EndRun();
                return;
            }

            DestroyCircle();

            TryHitTarget(landing, _landingRadius);
            ApplyShockwave(landing);

            _phase = ELeapPhase.Recovery;
            _timer = _recoverySeconds;
        }

        /// <summary>
        /// 착지점에서 충격파 반경 안의 대상을 바깥으로 민다. 겹쳐 있으면 B 정면 쪽으로 민다.
        /// </summary>
        private void ApplyShockwave(Vector3 landing)
        {
            float radius = Arena.TileSize * _shockwaveTiles;
            if (GetFlatDistanceToTarget(landing) > radius) return;

            Vector3 away = Vector3.ProjectOnPlane(Target.position - landing, Vector3.up);
            Vector3 direction = (away.sqrMagnitude > Mathf.Epsilon) ? away.normalized : Controller.transform.forward;
            KnockbackTarget(direction, _knockbackDistance, _knockbackSeconds);
        }

        private void TickRecovery(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer <= 0.0f) EndRun();
        }

        private void ShowCircle()
        {
            if (!_circlePrefab) return;

            _circle = Instantiate(_circlePrefab);
            MoveCircle();
        }

        private void MoveCircle()
        {
            if (!_circle) return;

            float diameter = Arena.TileSize * _shockwaveTiles * 2.0f;
            Vector3 scale = _circle.transform.localScale;
            _circle.transform.localScale = new Vector3(diameter, scale.y, diameter);
            _circle.transform.position = Arena.GetTileCenter(_landingTile) + (Vector3.up * _circleHeight);
        }

        private void DestroyCircle()
        {
            if (!_circle) return;

            Destroy(_circle);
            _circle = null;
        }
        #endregion
    }
}
