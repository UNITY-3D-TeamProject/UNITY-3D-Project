using UnityEngine;
using CharacterSound;
using Core.Effect;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 의 도약 내려찍기(1-2 부터). 일반은 플레이어가 선 칸에 원형 예고를 예고 시간 동안 페이드 인으로 띄운 뒤 뛰어올라 내려찍고,
    /// 착지 순간 피해와 칸 주변 충격파 넉백을 주고 경직한다. 착지 칸이 구멍·예고면(시작 때, 예고·체공 중) 가장 가까운 멀쩡한 칸으로 바꾼다.
    /// 도약을 시작해 착지할 때까지 착지 칸을 아레나에 예약해, A 가 그 칸을 노리지 않게 한다.
    /// 회피(<c>_isEvade</c>)는 예고 없이 가장 가까운 멀쩡한 칸으로 뛰어 건너가며, 피해·충격파·경직이 없다.
    /// 포물선은 CharacterMotor 의 점프(중력)와 수평 변위로 만든다. <c>_leapHeight</c> 가 0 보다 크면 체공 동안만
    /// 중력을 8h/T² 로 바꿔 체공 시간 T 와 정점 높이 h 를 따로 정하고, 0 이면 중력을 그대로 두고 초기 속도 = 중력 × T ÷ 2 로 띄운다.
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
        [Tooltip("켜면 회피용 도약. 예고 없이 가장 가까운 멀쩡한 칸으로 건너뛰며 피해·충격파·경직이 없다")]
        [SerializeField] private bool _isEvade;

        [Header("Leap")]
        [Tooltip("착지 칸 원형 예고 시간(초). 회피에서는 쓰지 않는다")]
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.2f;
        [Tooltip("체공 시간(초). 임시값")]
        [SerializeField, Min(0.2f)] private float _flightSeconds = 1.0f;
        [Tooltip("도약 정점 높이(m). 체공 시간과 따로 정한다. 0 이면 쓰지 않고 중력만으로 뜨는 높이(중력 × 체공² ÷ 8)가 된다")]
        [SerializeField, Min(0.0f)] private float _leapHeight = 0.0f;
        [Tooltip("착지 후 경직 시간(초). 회피에서는 쓰지 않는다")]
        [SerializeField, Min(0.0f)] private float _recoverySeconds = 1.5f;

        [Header("Motion")]
        [Tooltip("체공이 시작될 때 요청할 모션 알림 스킬 이름. 비우면 요청하지 않는다. 회피에서는 쓰지 않는다")]
        [SerializeField] private string _flightMotionSkillName = "LeapFlight";

        [SerializeField] private SOEffectData _landingVfx;

        [Header("Landing")]
        [Tooltip("착지점에서 대상까지 수평 거리가 이 안이면 피해를 받는다(m). 임시값")]
        [SerializeField, Min(0.0f)] private float _landingRadius = 4.0f;
        [Tooltip("충격파 반경(칸 수). 이 안의 대상이 착지점에서 바깥으로 밀린다")]
        [SerializeField, Min(0.0f)] private float _shockwaveTiles = 1.0f;
        [Tooltip("착지점에서 충격파가 퍼질 때 재생할 이펙트. 비우면 생략")]
        [SerializeField] private SOEffectData _shockwaveVfx;
        [Tooltip("충격파에 맞은 대상을 착지점에서 바깥으로 밀어내는 거리(m)")]
        [SerializeField, Min(0.0f)] private float _knockbackDistance = 8.0f;
        [Tooltip("밀려나는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.05f)] private float _knockbackSeconds = 0.4f;

        [Header("References")]
        [Tooltip("예고 원판 프리팹(지름 1 m). 회피에서는 쓰지 않는다")]
        [SerializeField] private GameObject _circlePrefab;
        [Tooltip("예고 원판을 바닥에서 띄우는 높이(m)")]
        [SerializeField, Min(0.0f)] private float _circleHeight = 0.05f;
        #endregion

        #region Private Fields
        private SoundController _soundController;
        private ELeapPhase _phase;
        private int _landingTile;
        private float _timer;
        private readonly BossBTelegraph _circle = new BossBTelegraph();
        private bool _isForcedLanding;
        private bool _isGravityOverridden;
        private float _savedGravity;
        #endregion

        #region Properties
        public override bool IsEvade => _isEvade;

        /// <summary>한 번이라도 실제로 도약을 시작했는지. 첫 조우 도약이 시작됐는지 BT 가 확인하는 데 쓴다.</summary>
        public bool HasStarted { get; private set; }

        /// <inheritdoc />
        public override bool IsBodyInFlight => IsRunning && (_phase == ELeapPhase.Flight);
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            if (Boss) _soundController = Boss.GetComponentInChildren<SoundController>();
        }
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

            Arena.SetReservedTile(_landingTile);

            base.Execute();
            HasStarted = true;
            _isForcedLanding = false;

            if (_isEvade)
            {
                StartFlight();
                return;
            }

            _phase = ELeapPhase.Telegraph;
            _timer = _telegraphSeconds;
            if (_soundController) _soundController.PlaySkillSound(SkillName, ESoundPhase.Execute, null);
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
            // 체공 중에 끊겼어도 바꿔 둔 중력이 남지 않게 한다
            RestoreGravity();
            _isForcedLanding = false;
            DestroyCircle();
            Arena?.SetReservedTile(-1);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 회피는 B 에서 가장 가까운 멀쩡한 칸, 일반은 플레이어가 선 칸.
        /// 플레이어 칸이 판 밖이거나 이미 구멍·예고면 가장 가까운 멀쩡한 칸이다. 예고 원이 처음부터 실제 착지 칸에 놓이게 한다.
        /// </summary>
        private int ChooseLandingTile()
        {
            if (_isEvade) return Arena.FindNearestSafe(Controller.Position);

            bool isTargetTileSafe = Arena.TryGetTile(Target.position, out int tile) && !Arena.IsHoleOrTelegraphed(tile);
            if (isTargetTileSafe) return tile;

            return Arena.FindNearestSafe(Target.position);
        }

        private void TickTelegraph(float deltaTime)
        {
            // A 는 예약 칸을 노리지 않지만, 그래도 칸이 구멍·예고로 바뀌었으면 가장 가까운 멀쩡한 칸으로 바로 바꿔 원이 점프 직전에 튀지 않게 한다
            if (!RetargetIfUnsafe())
            {
                EndRun();
                return;
            }

            _timer -= deltaTime;
            // 예고 시간 동안 원이 투명에서 머터리얼 알파까지 나타난다
            float progress = (_telegraphSeconds > 0.0f) ? (1.0f - (_timer / _telegraphSeconds)) : 1.0f;
            _circle.SetFade(progress);
            if (_timer > 0.0f) return;

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

            Arena.SetReservedTile(_landingTile);
            MoveCircle();
            return true;
        }

        private void StartFlight()
        {
            _phase = ELeapPhase.Flight;
            _timer = 0.0f;

            RefreshHold(_flightSeconds + _recoverySeconds);

            // 비행 모션으로 바꾼다(중재자를 거치는 스킬 요청)
            if (!_isEvade && !string.IsNullOrEmpty(_flightMotionSkillName)) Controller.ExecuteSkill(_flightMotionSkillName);

            // 정점 높이를 정했으면 체공 동안만 중력을 바꿔 높이와 체공 시간을 따로 맞춘다.
            // 정점 h, 체공 T 인 포물선은 중력 8h/T², 초기 상승 속도 4h/T 다
            if (_leapHeight > 0.0f)
            {
                _savedGravity = SelfMotor.Gravity;
                _isGravityOverridden = true;
                SelfMotor.Gravity = (8.0f * _leapHeight) / (_flightSeconds * _flightSeconds);
                SelfMotor.Jump((4.0f * _leapHeight) / _flightSeconds);
                return;
            }

            // 체공 시간 T 에 같은 높이로 돌아오는 초기 상승 속도
            SelfMotor.Jump(SelfMotor.Gravity * _flightSeconds * 0.5f);
        }

        private void TickFlight(float deltaTime)
        {
            // 지난 프레임에 남은 거리를 한 번에 옮기라고 했으므로, 그 이동이 반영된 실제 자리에서 착지한다
            if (_isForcedLanding)
            {
                _isForcedLanding = false;
                Land(Controller.Position);
                return;
            }

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
                // 시간 안에 도착하지 못했으면 이번 틱에 더한 몫을 뺀 남은 거리를 한 번에 옮기라고 한다.
                // 그 이동이 무엇에 막힐 수도 있으므로 피해는 착지 칸 중심이 아니라 다음 프레임의 실제 자리에서 준다
                SelfMotor.AddExternalDisplacement(remaining * (1.0f - fraction));
                _isForcedLanding = true;

                float missDistance = remaining.magnitude;
                if (missDistance > ARRIVAL_TOLERANCE)
                {
                    Debug.LogWarning(
                        $"[{name}] 도약이 체공 시간 안에 착지 칸({_landingTile})에 닿지 못했다. 남은 거리={missDistance:F1}m",
                        this);
                }
            }
        }

        private void Land(Vector3 landing)
        {
            RestoreGravity();

            if (_isEvade)
            {
                EndRun();
                return;
            }

            // 내려앉았으므로 경직 동안에는 A 가 이 칸을 노려도 된다
            Arena.SetReservedTile(-1);
            DestroyCircle();

            if (_soundController) _soundController.PlaySkillSound(SkillName, ESoundPhase.Impact, landing);
            if (_landingVfx && _landingVfx.Prefab)
                EffectManager.Instance?.Play(_landingVfx, landing, _landingVfx.Prefab.transform.localRotation);
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
            if (_shockwaveVfx && _shockwaveVfx.Prefab)
                EffectManager.Instance?.Play(_shockwaveVfx, landing, _shockwaveVfx.Prefab.transform.localRotation);

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

        /// <summary>
        /// 체공을 위해 바꿔 둔 중력을 원래 값으로 되돌린다. 바꾸지 않았으면 아무것도 하지 않는다.
        /// </summary>
        private void RestoreGravity()
        {
            if (!_isGravityOverridden) return;

            _isGravityOverridden = false;
            SelfMotor.Gravity = _savedGravity;
        }

        private void ShowCircle()
        {
            if (!_circlePrefab) return;

            _circle.Show(_circlePrefab);
            MoveCircle();
        }

        private void MoveCircle()
        {
            float diameter = Arena.TileSize * _shockwaveTiles * 2.0f;
            _circle.Place(
                Arena.GetTileCenter(_landingTile) + (Vector3.up * _circleHeight),
                Quaternion.identity,
                diameter,
                diameter);
        }

        private void DestroyCircle()
        {
            _circle.Hide();
        }
        #endregion
    }
}
