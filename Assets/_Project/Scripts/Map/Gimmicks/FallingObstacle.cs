using System.Collections;
using UnityEngine;

namespace Map.Gimmicks
{
    /// <summary>
    /// 위에서 떨어졌다가 다시 올라가는 장애물의 움직임.
    /// 놓인 자리가 꼭대기이며, 대기 → 낙하 → 바닥에서 정지 → 상승을 한 번 또는 반복한다.
    /// 대기하는 동안 떨어질 자리의 예고 표시가 점점 짙은 빨간색으로 변한다.
    /// 데미지는 같은 오브젝트에 EffectZone(Enter Effects)과 Trigger 콜라이더를 붙여서 준다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FallingObstacle : MonoBehaviour
    {
        #region Constants
        private const float WARNING_GROUND_OFFSET = 0.03f;
        #endregion

        #region Serialized Fields
        [Header("Fall")]
        [Tooltip("꼭대기에서 바닥까지 떨어지는 거리")]
        [SerializeField, Min(0.1f)] private float _fallDistance = 7.0f;

        [Tooltip("낙하 가속도")]
        [SerializeField, Min(0.1f)] private float _gravity = 20.0f;

        [Tooltip("떨어지기 전 대기 시간(초). 이 시간 동안 예고 표시가 짙어진다.")]
        [SerializeField, Min(0.0f)] private float _waitBeforeFall = 1.0f;

        [Header("Return")]
        [Tooltip("바닥에 머무는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _restTime = 1.0f;

        [Tooltip("꼭대기로 돌아가는 속도")]
        [SerializeField, Min(0.1f)] private float _riseSpeed = 4.0f;

        [Header("Warning")]
        [Tooltip("떨어질 자리에 깔리는 예고 표시 (선택). 시작할 때 바닥 위치로 옮겨지고, 대기~낙하 동안에만 보인다.")]
        [SerializeField] private Renderer _warningMarker;

        [Tooltip("예고가 시작될 때의 색")]
        [SerializeField] private Color _warningStartColor = new Color(1.0f, 0.6f, 0.6f, 0.15f);

        [Tooltip("떨어지기 직전의 색")]
        [SerializeField] private Color _warningEndColor = new Color(0.7f, 0.0f, 0.0f, 0.9f);

        [Header("Options")]
        [Tooltip("켜면 시작하자마자 떨어지기 시작한다. 끄면 Drop()이 호출될 때까지 기다린다. (TriggerPlate 등에서 호출)")]
        [SerializeField] private bool _isAutoStart = true;

        [Tooltip("켜면 꼭대기로 돌아온 뒤 다시 떨어진다.")]
        [SerializeField] private bool _isLoop = true;
        #endregion

        #region Private Fields
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Vector3 _topPosition;
        private bool _isRunning;
        private MaterialPropertyBlock _warningBlock;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Rigidbody 없이 Trigger 콜라이더를 움직이면 충돌 판정이 불안정해진다.
            GetComponent<Rigidbody>().isKinematic = true;
        }

        private void Start()
        {
            _topPosition = transform.position;
            SetUpWarningMarker();

            if (_isAutoStart)
            {
                Drop();
            }
        }

        private void OnDisable()
        {
            _isRunning = false;

            if (_warningMarker != null)
            {
                _warningMarker.enabled = false;
            }
        }

        private void OnDestroy()
        {
            // 예고 표시는 시작할 때 자식에서 분리했으므로 직접 지운다
            if (_warningMarker != null)
            {
                Destroy(_warningMarker.gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 top = Application.isPlaying ? _topPosition : transform.position;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(top, top + (Vector3.down * _fallDistance));
            Gizmos.DrawWireSphere(top + (Vector3.down * _fallDistance), 0.3f);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 낙하를 시작한다. 이미 움직이는 중이면 무시한다. (UnityEvent로 연결 가능)
        /// </summary>
        public void Drop()
        {
            if (_isRunning) return;

            StartCoroutine(CoFallCycle());
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 예고 표시를 장애물에서 분리해 떨어질 바닥 위치에 고정한다.
        /// </summary>
        private void SetUpWarningMarker()
        {
            if (_warningMarker == null) return;

            // 바닥 높이 = 다 떨어졌을 때의 중심 높이 - 콜라이더 절반 높이
            float halfHeight = TryGetComponent(out Collider ownCollider) ? ownCollider.bounds.extents.y : 0.0f;
            Vector3 groundPosition = _topPosition + (Vector3.down * (_fallDistance + halfHeight - WARNING_GROUND_OFFSET));

            // 자식으로 두면 장애물과 함께 떨어지므로 분리한다
            Transform marker = _warningMarker.transform;
            marker.SetParent(transform.parent, true);
            marker.position = groundPosition;

            _warningBlock = new MaterialPropertyBlock();
            _warningMarker.enabled = false;
        }

        private void SetWarningColor(float progress)
        {
            if (_warningMarker == null) return;

            _warningBlock.SetColor(BaseColorId, Color.Lerp(_warningStartColor, _warningEndColor, progress));
            _warningMarker.SetPropertyBlock(_warningBlock);
        }

        private void SetWarningVisible(bool isVisible)
        {
            if (_warningMarker != null)
            {
                _warningMarker.enabled = isVisible;
            }
        }
        #endregion

        #region Coroutines
        private IEnumerator CoFallCycle()
        {
            _isRunning = true;

            do
            {
                // 대기: 예고 표시가 점점 짙어진다
                SetWarningColor(0.0f);
                SetWarningVisible(true);

                float elapsed = 0.0f;
                while (elapsed < _waitBeforeFall)
                {
                    elapsed += Time.deltaTime;
                    SetWarningColor(elapsed / _waitBeforeFall);
                    yield return null;
                }

                SetWarningColor(1.0f);

                // 낙하
                Vector3 bottomPosition = _topPosition + (Vector3.down * _fallDistance);
                float speed = 0.0f;
                while (transform.position != bottomPosition)
                {
                    speed += _gravity * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, bottomPosition, speed * Time.deltaTime);
                    yield return null;
                }

                SetWarningVisible(false);

                yield return new WaitForSeconds(_restTime);

                // 상승
                while (transform.position != _topPosition)
                {
                    transform.position = Vector3.MoveTowards(transform.position, _topPosition, _riseSpeed * Time.deltaTime);
                    yield return null;
                }
            }
            while (_isLoop);

            _isRunning = false;
        }
        #endregion
    }
}
