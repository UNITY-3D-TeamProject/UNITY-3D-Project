using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Interaction;

namespace Tutorial
{
    /// <summary>
    /// 파이프에서 나오는 흐름 발판에 얹혀 나온 뒤 이 오브젝트 위치(분류장 중앙)로 날아가 대기한다.
    /// 열면 CD가 회전하고 제목이 흐르는 MP3 창을 재생한다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
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
        [SerializeField] private GameObject _contentRoot;
        [SerializeField] private Transform _disc;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private string _songTitle = "다시, 처음부터  ·  마지막 데모 파일";
        [SerializeField, Min(1.0f)] private float _playDuration = 8.0f;
        [SerializeField, Min(0.1f)] private float _discSpeed = 120.0f;
        [SerializeField, Min(0.05f)] private float _scrollInterval = 0.18f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onAcquired = new UnityEvent();
        #endregion

        #region Private Fields
        private AudioSource _audioSource;
        private EFileState _state = EFileState.Hidden;
        private float _scrollTimer;
        private int _scrollOffset;
        #endregion

        #region Properties
        public bool IsAcquired => _state == EFileState.Acquired;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_visualRoot != null, $"[{name}] Visual Root가 연결되지 않았습니다.");
            Debug.Assert(_contentRoot != null, $"[{name}] MP3 창이 연결되지 않았습니다.");
            _audioSource = GetComponent<AudioSource>();
            _visualRoot.gameObject.SetActive(false);
            _contentRoot.SetActive(false);
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
            if ((_titleText == null) || string.IsNullOrEmpty(_songTitle)) return;
            _scrollTimer += Time.deltaTime;
            if (_scrollTimer < _scrollInterval) return;
            _scrollTimer -= _scrollInterval;
            _scrollOffset = (_scrollOffset + 1) % (_songTitle.Length + 6);
            string strip = _songTitle + "      ";
            _titleText.text = (strip + strip).Substring(_scrollOffset, Mathf.Min(13, strip.Length));
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
            StartCoroutine(CoPlay());
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

        private IEnumerator CoPlay()
        {
            _state = EFileState.Playing;
            _visualRoot.gameObject.SetActive(false);
            _contentRoot.SetActive(true);
            _scrollOffset = 0;
            _scrollTimer = 0.0f;
            if (_titleText != null) _titleText.text = _songTitle;
            if (_audioSource.clip != null) _audioSource.Play();
            yield return new WaitForSeconds(_playDuration);
            _state = EFileState.Acquired;
            _onAcquired.Invoke();
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
