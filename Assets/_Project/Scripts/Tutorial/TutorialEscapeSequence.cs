using UnityEngine;
using Core;
using Facade.Player;
using Map.Gimmicks;
using Movement;
using Skill.Skills;

namespace Tutorial
{
    /// <summary>음악 확인 후 보드 추격을 시작하고 접촉·낙하 시 구간 시작점으로 복귀시킨다.</summary>
    public class TutorialEscapeSequence : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private MusicFile _musicFile;
        [SerializeField] private ScenePortal _portal;
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _finishPoint;
        [SerializeField] private Transform _boulder;
        [SerializeField] private GameObject[] _musicPlatforms;

        [Header("Chase")]
        [SerializeField, Min(0.1f)] private float _speed = 9.5f;
        [SerializeField, Min(0.1f)] private float _radius = 3.0f;
        [SerializeField, Min(1.0f)] private float _headStart = 22.0f;
        [SerializeField, Min(0.0f)] private float _warningTime = 2.0f;
        [SerializeField, Min(1.0f)] private float _halfWidth = 4.5f;
        #endregion

        #region Private Fields
        private PlayerFacade _player;
        private CharacterController _controller;
        private CharacterMotor _motor;
        private SpawnVehicle _board;
        private MultiJump _multiJump;
        private Vector3 _direction;
        private float _length;
        private float _distance;
        private float _warningRemaining;
        private float _nextRecharge;
        private bool _hasChargedAtStart;
        private bool _musicPlatformsUnlocked;
        #endregion

        #region Properties
        public bool IsRunning { get; private set; }
        public bool IsCompleted { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if ((_musicFile == null) || (_portal == null) ||
                (_startPoint == null) || (_finishPoint == null) || (_boulder == null))
            {
                Debug.LogError("추격 구간 참조가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }

            Vector3 route = _finishPoint.position - _startPoint.position;
            route.y = 0.0f;
            _length = route.magnitude;
            _direction = route.normalized;
            _boulder.gameObject.SetActive(false);
            if (_musicPlatforms != null)
            {
                foreach (GameObject platform in _musicPlatforms)
                {
                    if (platform != null) platform.SetActive(false);
                }
            }
        }

        private void Update()
        {
            BindPlayer();
            bool hasMusic = _musicFile.IsAcquired;
            if (hasMusic && !_musicPlatformsUnlocked)
            {
                _musicPlatformsUnlocked = true;
                if (_musicPlatforms != null)
                {
                    foreach (GameObject platform in _musicPlatforms)
                    {
                        if (platform != null) platform.SetActive(true);
                    }
                }
            }

            if (IsCompleted || (_player == null) || (_controller == null)) return;
            if ((GameManager.Instance.CurrentState != GameManager.GameState.Playing) ||
                (_player.GetAttribute("CurrentHp") <= 0.0f)) return;

            Vector3 offset = _controller.transform.position - _startPoint.position;
            float progress = Vector3.Dot(offset, _direction);
            float side = Mathf.Abs(Vector3.Dot(offset, Vector3.Cross(Vector3.up, _direction)));

            if (!IsRunning)
            {
                bool isAtStart = (progress >= -8.0f) && (progress < 8.0f) &&
                    (side < _halfWidth) && (Mathf.Abs(offset.y) < 4.0f);
                if (!hasMusic || !isAtStart) return;
                if (!_hasChargedAtStart || (_player.GetAttribute("CurrentBattery") <= 0.0f))
                {
                    Recharge();
                    _hasChargedAtStart = true;
                }
                if ((_board == null) || !_board.IsRiding || (_board.Vehicle == null) ||
                    (progress < 1.0f)) return;

                BeginChase();
                return;
            }

            if ((offset.y < -5.0f) || (side > _halfWidth + 4.0f) || (progress < -10.0f))
            {
                Retry();
                return;
            }

            if (progress >= _length)
            {
                IsRunning = false;
                IsCompleted = true;
                _boulder.gameObject.SetActive(false);
                _portal.Unlock();
                return;
            }

            if (progress >= _nextRecharge)
            {
                Recharge();
                _nextRecharge += 40.0f;
            }

            if (_warningRemaining > 0.0f)
            {
                _warningRemaining -= Time.deltaTime;
                return;
            }

            _distance += _speed * Time.deltaTime;
            _boulder.position = _startPoint.position + (_direction * _distance) +
                (Vector3.up * (_radius - 0.1f));
            _boulder.Rotate(Vector3.Cross(Vector3.up, _direction),
                (_speed / _radius) * Mathf.Rad2Deg * Time.deltaTime, Space.World);

            Vector3 center = _controller.bounds.center;
            if (Vector3.Distance(center, _boulder.position) < _radius + _controller.radius)
            {
                Retry();
                return;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>구체 접촉 또는 낙하 시 음악 확인 상태를 유지하고 즉시 재도전한다.</summary>
        public void Retry()
        {
            if ((_controller == null) || IsCompleted) return;

            IsRunning = false;
            _boulder.gameObject.SetActive(false);
            if (_board != null)
            {
                _board.StopHover();
                _board.ResetHover();
            }
            if (_multiJump != null) _multiJump.ResetState();
            bool wasMotorEnabled = (_motor != null) && _motor.enabled;
            if (_motor != null) _motor.enabled = false;
            _controller.enabled = false;
            _controller.transform.SetPositionAndRotation(
                _startPoint.position - (_direction * 3.0f) + (Vector3.up * 0.25f),
                Quaternion.LookRotation(_direction));
            _controller.enabled = true;
            if (_motor != null) _motor.enabled = wasMotorEnabled;
            Physics.SyncTransforms();
            Recharge();
            _hasChargedAtStart = true;
        }
        #endregion

        #region Private Methods
        private void BindPlayer()
        {
            PlayerFacade current = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerFacade : null;
            if (current == _player) return;
            _player = current;
            IsRunning = false;
            _hasChargedAtStart = false;
            _boulder.gameObject.SetActive(false);
            _controller = null;
            _motor = null;
            _board = null;
            _multiJump = null;
            if (_player == null) return;
            Transform root = _player.transform.root;
            _controller = root.GetComponentInChildren<CharacterController>();
            _motor = root.GetComponentInChildren<CharacterMotor>();
            _board = root.GetComponentInChildren<SpawnVehicle>(true);
            _multiJump = root.GetComponentInChildren<MultiJump>(true);
        }

        private void BeginChase()
        {
            IsRunning = true;
            _distance = -_headStart;
            _warningRemaining = _warningTime;
            _nextRecharge = 30.0f;
            Recharge();
            _boulder.position = _startPoint.position + (_direction * _distance) + (Vector3.up * _radius);
            _boulder.gameObject.SetActive(true);
        }

        private void Recharge()
        {
            if (_player.HasAttribute("MaxBattery"))
            {
                _player.SetAttribute("CurrentBattery", _player.GetAttribute("MaxBattery"));
            }
        }

        #endregion
    }
}
