using UnityEngine;
using Attribute.Effect;
using Core;
using Facade.Player;
using Map.Gimmicks;
using UI;

namespace Tutorial
{
    /// <summary>
    /// 음악 확인 후 숨은 발판을 열고, 플레이어가 출발 트리거를 지나면 미리 놓인 공을 코스 방향으로 굴린다.
    /// 공에 닿거나 코스에서 떨어지면 사망 처리하고, 재스폰하면 공을 처음 자리로 되돌린다. 결승점에 닿으면 포탈을 연다.
    /// 추격 중 배터리가 기준 이하로 떨어지면 펫 안내를 띄우고 한 번만 배터리를 채워 준다.
    /// </summary>
    public class TutorialEscapeSequence : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private MusicFile _musicFile;
        [SerializeField] private ScenePortal _portal;
        [Tooltip("코스 기준점. 진행 거리와 이탈 판정의 기준")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _finishPoint;
        [Tooltip("씬에 미리 놓아 둔 공. 추격이 시작되면 이 자리에서 코스 방향으로 굴러간다.")]
        [SerializeField] private Transform _boulder;
        [SerializeField] private GameObject[] _musicPlatforms;
        [Tooltip("공에 닿거나 코스에서 떨어졌을 때 플레이어에게 적용할 Effect (CurrentHp 0)")]
        [SerializeField] private SOAttributeEffect _killEffect;

        [Header("Chase")]
        [SerializeField, Min(0.1f)] private float _speed = 10.5f;
        [SerializeField, Min(0.1f)] private float _radius = 3.0f;
        [SerializeField, Min(1.0f)] private float _halfWidth = 4.5f;

        [Header("Low Battery")]
        [Tooltip("배터리가 최대치의 이 비율 이하로 떨어지면 펫 안내를 띄운다")]
        [SerializeField, Range(0.0f, 1.0f)] private float _lowBatteryRatio = 0.2f;
        [Tooltip("배터리가 부족하다고 알리는 펫 안내 번호")]
        [SerializeField] private int _lowBatteryGuideId = 13;
        [Tooltip("임시로 충전해 준다고 알리는 펫 안내 번호. 이 안내를 띄울 때 배터리를 채운다")]
        [SerializeField] private int _rechargeGuideId = 14;
        #endregion

        #region Private Fields
        private PlayerFacade _player;
        private CharacterController _controller;
        private Vector3 _direction;
        private float _length;
        private Vector3 _boulderHomePosition;
        private Quaternion _boulderHomeRotation;
        private float _travelled;
        private bool _musicPlatformsUnlocked;
        private bool _hasUsedRecharge;
        private bool _isWaitingRechargeGuide;
        #endregion

        #region Properties
        public bool IsRunning { get; private set; }
        public bool IsCompleted { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if ((_musicFile == null) || (_portal == null) ||
                (_startPoint == null) || (_finishPoint == null) ||
                (_boulder == null) || (_killEffect == null))
            {
                Debug.LogError("추격 구간 참조가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }

            Vector3 route = _finishPoint.position - _startPoint.position;
            route.y = 0.0f;
            _length = route.magnitude;
            _direction = route.normalized;
            _boulderHomePosition = _boulder.position;
            _boulderHomeRotation = _boulder.rotation;
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
            if (_musicFile.IsAcquired && !_musicPlatformsUnlocked)
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

            if (!IsRunning || (_player == null) || (_controller == null)) return;
            if ((GameManager.Instance.CurrentState != GameManager.GameState.Playing) ||
                (_player.GetAttribute("CurrentHp") <= 0.0f)) return;

            Vector3 offset = _controller.transform.position - _startPoint.position;
            float progress = Vector3.Dot(offset, _direction);
            float side = Mathf.Abs(Vector3.Dot(offset, Vector3.Cross(Vector3.up, _direction)));

            if ((offset.y < -5.0f) || (side > _halfWidth + 4.0f) || (progress < -10.0f))
            {
                Kill();
                return;
            }

            if (progress >= _length)
            {
                IsRunning = false;
                IsCompleted = true;
                _portal.Unlock();
                return;
            }

            UpdateLowBattery();

            _travelled += _speed * Time.deltaTime;
            _boulder.position = _boulderHomePosition + (_direction * _travelled);
            _boulder.Rotate(Vector3.Cross(Vector3.up, _direction),
                (_speed / _radius) * Mathf.Rad2Deg * Time.deltaTime, Space.World);

            Vector3 center = _controller.bounds.center;
            if (Vector3.Distance(center, _boulder.position) < _radius + _controller.radius)
            {
                Kill();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>출발 트리거에서 호출한다. 진행 중이거나 이미 끝났으면 무시한다.</summary>
        public void BeginChase()
        {
            if (!enabled || IsRunning || IsCompleted || (_player == null)) return;

            ResetBoulder();
            IsRunning = true;
            _hasUsedRecharge = false;
            _isWaitingRechargeGuide = false;
            Recharge();
        }
        #endregion

        #region Private Methods
        private void BindPlayer()
        {
            PlayerFacade current = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerFacade : null;
            if (current == _player) return;
            _player = current;
            IsRunning = false;
            ResetBoulder();
            _controller = null;
            if (_player == null) return;
            _controller = _player.transform.root.GetComponentInChildren<CharacterController>();
        }

        // 공을 처음 놓인 자리로 되돌린다. 결승 뒤에는 멈춘 자리에 둔다.
        private void ResetBoulder()
        {
            if (IsCompleted) return;
            _travelled = 0.0f;
            _boulder.SetPositionAndRotation(_boulderHomePosition, _boulderHomeRotation);
        }

        // 공은 그 자리에 멈추고, 재스폰으로 플레이어가 바뀌면 BindPlayer 가 공을 되돌린다.
        private void Kill()
        {
            IsRunning = false;
            if (_player.EffectTarget != null) _killEffect.Apply(_player.EffectTarget);
        }

        // 배터리가 기준 이하면 부족 안내를 띄우고, 그 안내를 닫으면 충전 안내와 함께 배터리를 채운다. 추격마다 한 번만.
        private void UpdateLowBattery()
        {
            UIManager uiManager = UIManager.Instance;
            if (_hasUsedRecharge || (uiManager == null) || uiManager.IsPetGuideOpen) return;

            if (_isWaitingRechargeGuide)
            {
                _isWaitingRechargeGuide = false;
                _hasUsedRecharge = true;
                uiManager.ShowPetGuide(_rechargeGuideId);
                Recharge();
                return;
            }

            if (!_player.HasAttribute("MaxBattery")) return;
            float maxBattery = _player.GetAttribute("MaxBattery");
            if ((maxBattery <= 0.0f) ||
                (_player.GetAttribute("CurrentBattery") > maxBattery * _lowBatteryRatio)) return;

            // 대화 중이라 안내가 안 뜨면 다음 프레임에 다시 시도한다.
            uiManager.ShowPetGuide(_lowBatteryGuideId);
            _isWaitingRechargeGuide = uiManager.IsPetGuideOpen;
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
