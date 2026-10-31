using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Core.Audio;
using Interaction;
using Map.Gimmicks;

namespace Tutorial
{
    /// <summary>
    /// 파이프에서 나오는 흐름 발판에 얹혀 나온 뒤 이 오브젝트 위치(분류장 중앙)로 날아가 대기한다.
    /// 열면 가운데 CD가 회전하고, 재생이 끝나면 떠올라 플레이어에게 날아가 획득된다.
    /// </summary>
    public class MusicFile : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Ride")]
        [Tooltip("음악 파일을 얹어 내보낼 발판 흐름")]
        [SerializeField] private PlatformSortingFlow _flow;
        [Tooltip("이 지점에 가장 가까운 흐름 발판에 얹는다 (파이프 입구 바로 안쪽)")]
        [SerializeField] private Transform _boardingPoint;
        [Tooltip("발판을 타고 함께 이동할 거리(m). 다 타면 이 오브젝트 위치로 날아간다.")]
        [SerializeField, Min(0.0f)] private float _rideDistance = 12.0f;
        [Tooltip("발판 중심에서 음악 파일까지의 높이")]
        [SerializeField] private float _rideHeight = 1.0f;

        [Header("Drift")]
        [SerializeField] private Transform _visualRoot;
        [Tooltip("발판에서 내려 대기 위치까지 날아가는 속도(m/s)")]
        [SerializeField, Min(0.1f)] private float _driftSpeed = 10.0f;
        [SerializeField, Min(0.0f)] private float _swayAmplitude = 0.6f;
        [SerializeField, Min(0.0f)] private float _hoverAmplitude = 0.2f;

        [Header("MP3 Player")]
        [Tooltip("음악 파일 주변에서 반복 재생할 음원")]
        [SerializeField] private SOAudioData _musicSound;
        [SerializeField] private Transform _disc;
        [SerializeField, Min(1.0f)] private float _playDuration = 8.0f;
        [SerializeField, Min(0.1f)] private float _discSpeed = 120.0f;

        [Header("Collect")]
        [Tooltip("플레이어에게 날아가는 부분. 바닥 링 등 나머지는 제자리에 있다가 획득 순간 함께 사라진다.")]
        [SerializeField] private Transform _collectRoot;
        [Tooltip("떠오르는 높이")]
        [SerializeField, Min(0.0f)] private float _riseHeight = 1.2f;
        [Tooltip("떠오르는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _riseDuration = 0.6f;
        [Tooltip("떠오르는 동안 줄어들 크기 배율")]
        [SerializeField, Min(0.01f)] private float _flyScale = 0.35f;
        [Tooltip("떠오른 뒤 플레이어에게 날아가는 속도")]
        [SerializeField, Min(0.1f)] private float _flySpeed = 14.0f;
        [Tooltip("플레이어 몸에 들어가 획득되는 순간 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _collectSound;

        [Header("Events")]
        [SerializeField] private UnityEvent _onAcquired = new UnityEvent();
        #endregion

        #region Private Fields
        private SAudioHandle _musicHandle;
        private EFileState _state = EFileState.Hidden;
        #endregion

        #region Properties
        public bool IsAcquired => _state == EFileState.Acquired;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_visualRoot != null, $"[{name}] Visual Root가 연결되지 않았습니다.");
            Debug.Assert(_collectRoot != null, $"[{name}] Collect Root가 연결되지 않았습니다.");
            _visualRoot.gameObject.SetActive(false);
        }

        private void Start()
        {
            GimmickUtility.PlaySfxLoop(_musicSound, transform, ref _musicHandle);
        }

        private void OnEnable()
        {
            if (!IsAcquired) GimmickUtility.PlaySfxLoop(_musicSound, transform, ref _musicHandle);
        }

        private void OnDisable()
        {
            GimmickUtility.StopSfxLoop(ref _musicHandle);
        }

        private void Update()
        {
            if (_state == EFileState.Waiting)
            {
                _visualRoot.position = transform.position +
                    (Vector3.up * (Mathf.Sin(Time.time * 1.6f) * _hoverAmplitude));
            }
            if (_state != EFileState.Playing) return;

            if (_disc != null) _disc.Rotate(Vector3.forward, -_discSpeed * Time.deltaTime, Space.Self);
        }
        #endregion

        #region Public Methods
        public void DriftIn()
        {
            if (_state != EFileState.Hidden) return;
            _state = EFileState.Drifting;
            _visualRoot.gameObject.SetActive(true);
            StartCoroutine(CoDriftIn());
        }

        public void Interact(GameObject interactor)
        {
            if (_state != EFileState.Waiting) return;
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider.gameObject.layer == LayerMask.NameToLayer("Interact")) collider.enabled = false;
            }
            StartCoroutine(CoPlay(interactor));
        }
        #endregion

        #region Coroutines
        private IEnumerator CoDriftIn()
        {
            yield return CoRidePlatform();
            yield return CoDriftTo(transform.position);
            _state = EFileState.Waiting;
        }

        // 탑승 지점에 가장 가까운 흐름 발판 위에 얹혀 _rideDistance 만큼 함께 이동한다. 발판이 없으면 바로 끝난다.
        private IEnumerator CoRidePlatform()
        {
            if ((_flow == null) || (_boardingPoint == null)) yield break;

            Transform platform = _flow.FindNearestPlatform(_boardingPoint.position);
            if (platform == null) yield break;

            Vector3 lastPosition = platform.position;
            float riddenDistance = 0.0f;

            // 발판이 그라인더에서 사라지면(null) 그 자리에서 내린다.
            while ((platform != null) && (riddenDistance < _rideDistance))
            {
                riddenDistance += Vector3.Distance(lastPosition, platform.position);
                lastPosition = platform.position;
                _visualRoot.position = lastPosition + (Vector3.up * _rideHeight);
                yield return null;
            }
        }

        private IEnumerator CoDriftTo(Vector3 target)
        {
            Vector3 start = _visualRoot.position;
            float distance = Vector3.Distance(start, target);
            if (distance < 0.001f) yield break;
            Vector3 side = Vector3.Cross(Vector3.up, (target - start).normalized);
            for (float travelled = 0.0f; travelled < distance; travelled += _driftSpeed * Time.deltaTime)
            {
                float t = travelled / distance;
                float sway = Mathf.Sin(travelled * 0.5f) * _swayAmplitude * Mathf.Sin(t * Mathf.PI);
                _visualRoot.position = Vector3.Lerp(start, target, t) + (side * sway);
                yield return null;
            }
            _visualRoot.position = target;
        }

        private IEnumerator CoPlay(GameObject interactor)
        {
            _state = EFileState.Playing;
            yield return new WaitForSeconds(_playDuration);
            yield return CoFlyToPlayer(interactor);
            GimmickUtility.StopSfxLoop(ref _musicHandle);
            GimmickUtility.PlaySfx(_collectSound, _collectRoot.position);
            _visualRoot.gameObject.SetActive(false);
            _state = EFileState.Acquired;
            _onAcquired.Invoke();
        }

        // ItemBox 와 같이 위로 떠오른 뒤 플레이어 몸으로 날아간다. CD는 그동안에도 계속 돈다.
        private IEnumerator CoFlyToPlayer(GameObject interactor)
        {
            Vector3 start = _collectRoot.position;
            Vector3 top = start + (Vector3.up * _riseHeight);
            Vector3 startScale = _collectRoot.localScale;
            Vector3 flyScale = startScale * _flyScale;
            for (float time = 0.0f; time < _riseDuration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0.0f, 1.0f, time / _riseDuration);
                _collectRoot.position = Vector3.Lerp(start, top, t);
                _collectRoot.localScale = Vector3.Lerp(startScale, flyScale, t);
                yield return null;
            }
            _collectRoot.localScale = flyScale;

            // 실제로 움직이는 몸체는 플레이어 루트가 아니라 자식의 CharacterController 다
            CharacterController body = (interactor != null) ? interactor.GetComponentInChildren<CharacterController>() : null;
            while (body != null)
            {
                Vector3 target = body.bounds.center;
                _collectRoot.position = Vector3.MoveTowards(_collectRoot.position, target, _flySpeed * Time.deltaTime);
                if ((_collectRoot.position - target).sqrMagnitude < 0.01f) break;

                yield return null;
            }
        }
        #endregion

        #region Nested Types
        private enum EFileState
        {
            Hidden,
            Drifting,
            Waiting,
            Playing,
            Acquired,
        }
        #endregion
    }
}
