using System.Collections;
using UnityEngine;
using Core.ObjectPool;
using DTT.AreaOfEffectRegions;

namespace Map.Gimmicks
{
    /// <summary>
    /// 위에서 떨어졌다가 다시 올라가는 장애물의 움직임.
    /// 놓인 자리가 꼭대기이며, 대기 → 낙하 → 바닥에서 정지 → 상승을 한 번 또는 반복한다.
    /// 풀에서 꺼낸 위험물은 DropOnto(착지점)으로 시작하고, 바닥에 닿으면 풀에 반납된다.
    /// 대기~낙하 동안 떨어질 자리의 DTT 원형 예고가 채워지거나 기존 표시가 점점 짙은 빨간색으로 변하고, 바닥에 닿는 순간 다 찬다.
    /// 데미지는 같은 오브젝트에 EffectZone(Enter Effects)과 Trigger 콜라이더를 붙여서 준다.
    /// 바닥에 닿아 사라질 때 착지 연출(_landEffect)이 있으면 착지점 바닥에 만든다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FallingObstacle : MonoBehaviour
    {
        #region Constants
        private const float WARNING_GROUND_OFFSET = 0.03f;
        private const float MIN_GRAVITY = 0.1f;
        #endregion

        #region Serialized Fields
        [Header("Fall")]
        [Tooltip("꼭대기에서 바닥까지 떨어지는 거리")]
        [SerializeField, Min(0.1f)] private float _fallDistance = 7.0f;

        [Tooltip("낙하 가속도")]
        [SerializeField, Min(0.1f)] private float _gravity = 20.0f;

        [Tooltip("낙하 가속도에 떨어질 때마다 더하는 무작위 오프셋의 최대 크기. -값~+값 사이에서 골라 더하므로 여러 개를 동시에 만들어도 바닥에 닿는 시각이 제각각이 된다. 0이면 쓰지 않는다.")]
        [SerializeField, Min(0.0f)] private float _gravityOffsetRange;

        [Tooltip("떨어질 때마다 세로(Y) 축으로 돌려 놓는 무작위 각도의 최대 크기(도). -값~+값 사이에서 골라 처음 놓인 각도에 더하므로 여러 개가 서로 다른 각도로 떨어진다. 낙하 중에는 돌지 않는다. 0이면 쓰지 않는다.")]
        [SerializeField, Range(0.0f, 180.0f)] private float _yawOffsetRange;

        [Tooltip("떨어지기 전 대기 시간(초). 예고는 이 대기 시간과 낙하 시간을 합친 동안 채워진다.")]
        [SerializeField, Min(0.0f)] private float _waitBeforeFall = 1.0f;

        [Header("Return")]
        [Tooltip("바닥에 머무는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _restTime = 1.0f;

        [Tooltip("꼭대기로 돌아가는 속도")]
        [SerializeField, Min(0.1f)] private float _riseSpeed = 4.0f;

        [Header("Warning")]
        [Tooltip("떨어질 자리에 깔리는 예고 표시 (선택). 시작할 때 바닥 위치로 옮겨지고, 대기~낙하 동안에만 보인다.")]
        [SerializeField] private Renderer _warningMarker;

        [Tooltip("보스와 같은 DTT 원형 예고 (선택). AoeVfxPlayer가 붙은 원형 예고를 연결하면 대기~낙하 동안 채워져 바닥에 닿을 때 다 찬다.")]
        [SerializeField] private CircleRegion _warningCircle;

        [Tooltip("예고가 시작될 때의 색")]
        [SerializeField] private Color _warningStartColor = new Color(1.0f, 0.6f, 0.6f, 0.15f);

        [Tooltip("떨어지기 직전의 색")]
        [SerializeField] private Color _warningEndColor = new Color(0.7f, 0.0f, 0.0f, 0.9f);

        [Header("Land Effect")]
        [Tooltip("바닥에 닿아 사라질 때 착지점 바닥에 만들 연출 (선택). Is Destroy On Land 가 켜졌을 때만 쓴다.")]
        [SerializeField] private GameObject _landEffect;

        [Header("Options")]
        [Tooltip("켜면 시작하자마자 떨어지기 시작한다. 끄면 Drop()이 호출될 때까지 기다린다. (TriggerPlate 등에서 호출)")]
        [SerializeField] private bool _isAutoStart = true;

        [Tooltip("켜면 꼭대기로 돌아온 뒤 다시 떨어진다.")]
        [SerializeField] private bool _isLoop = true;

        [Tooltip("켜면 바닥에 닿는 순간 사라진다. 다시 올라가지 않는다. 풀에서 꺼낸 것이면 풀에 반납된다. (FallingHazardSpawner 로 만드는 위험물)")]
        [SerializeField] private bool _isDestroyOnLand;
        #endregion

        #region Private Fields
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Vector3 _topPosition;
        private Quaternion _baseRotation;
        private bool _isRunning;
        private MaterialPropertyBlock _warningBlock;
        #endregion

        #region Properties
        /// <summary>꼭대기에서 바닥까지 떨어지는 거리.</summary>
        public float FallDistance => _fallDistance;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Rigidbody 없이 Trigger 콜라이더를 움직이면 충돌 판정이 불안정해진다.
            GetComponent<Rigidbody>().isKinematic = true;
        }

        private void Start()
        {
            // DropOnto 로 이미 시작했으면 꼭대기와 예고 표시를 다시 잡지 않는다
            if (_isRunning) return;

            _topPosition = transform.position;
            _baseRotation = transform.rotation;
            SetUpWarningMarker();

            if (_isAutoStart)
            {
                Drop();
            }
        }

        private void OnDisable()
        {
            _isRunning = false;

            SetWarningVisible(false);
        }

        private void OnDestroy()
        {
            // 예고 표시는 시작할 때 자식에서 분리했으므로 직접 지운다
            if (_warningMarker != null)
            {
                Destroy(_warningMarker.gameObject);
            }

            if (_warningCircle != null)
            {
                Destroy(_warningCircle.gameObject);
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

        /// <summary>
        /// 다 떨어졌을 때 아랫면이 착지점에 닿도록 꼭대기를 정하고 낙하를 시작한다.
        /// 풀에서 꺼낸 위험물은 켜질 때마다 이 메서드로 시작한다.
        /// </summary>
        /// <param name="landingPoint">떨어질 바닥 위 지점 (콜라이더 아랫면이 닿을 높이)</param>
        public void DropOnto(Vector3 landingPoint)
        {
            float halfHeight = TryGetComponent(out Collider ownCollider) ? ownCollider.bounds.extents.y : 0.0f;
            _topPosition = landingPoint + (Vector3.up * (_fallDistance + halfHeight));
            transform.position = _topPosition;
            _baseRotation = transform.rotation;

            SetUpWarningMarker();
            Drop();
        }

        /// <summary>
        /// 장애물을 없앤다. 풀에서 꺼낸 것이면 풀에 반납하고, 아니면 파괴한다.
        /// </summary>
        public void Despawn()
        {
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 예고 표시를 장애물에서 분리해 떨어질 바닥 위치에 고정한다.
        /// </summary>
        private void SetUpWarningMarker()
        {
            if ((_warningMarker == null) && (_warningCircle == null)) return;

            Vector3 groundPosition = GetGroundPosition();

            // 자식으로 두면 장애물과 함께 떨어지므로 분리한다
            if (_warningCircle != null)
            {
                Transform circle = _warningCircle.transform;
                circle.SetParent(transform.parent, true);
                circle.position = groundPosition;
                _warningCircle.FillProgress = 0.0f;
            }

            if (_warningMarker != null)
            {
                Transform marker = _warningMarker.transform;
                marker.SetParent(transform.parent, true);
                marker.position = groundPosition;
                _warningBlock ??= new MaterialPropertyBlock();
            }

            SetWarningVisible(false);
        }

        /// <summary>
        /// 떨어질 바닥 위 지점. 다 떨어졌을 때의 중심 높이 - 콜라이더 절반 높이에서 겹쳐 보이지 않게 살짝 띄운다.
        /// </summary>
        private Vector3 GetGroundPosition()
        {
            float halfHeight = TryGetComponent(out Collider ownCollider) ? ownCollider.bounds.extents.y : 0.0f;
            return _topPosition + (Vector3.down * (_fallDistance + halfHeight - WARNING_GROUND_OFFSET));
        }

        private void SpawnLandEffect()
        {
            if (_landEffect == null) return;

            Instantiate(_landEffect, GetGroundPosition(), Quaternion.identity);
        }

        private void SetWarningColor(float progress)
        {
            if (_warningCircle != null)
            {
                _warningCircle.FillProgress = progress;
            }

            if (_warningMarker == null) return;

            _warningBlock.SetColor(BaseColorId, Color.Lerp(_warningStartColor, _warningEndColor, progress));
            _warningMarker.SetPropertyBlock(_warningBlock);
        }

        private void SetWarningVisible(bool isVisible)
        {
            if (_warningCircle != null)
            {
                _warningCircle.gameObject.SetActive(isVisible);
            }

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
                // 이번 낙하의 각도. 오프셋이 있으면 낙하마다 달라진다
                transform.rotation = Quaternion.AngleAxis(Random.Range(-_yawOffsetRange, _yawOffsetRange), Vector3.up) * _baseRotation;

                // 대기~낙하: 바닥에 닿는 순간 다 차도록 DTT 예고가 채워지거나 기존 예고 표시가 점점 짙어진다
                SetWarningColor(0.0f);
                SetWarningVisible(true);

                // 이번 낙하의 가속도. 오프셋이 있으면 낙하마다 달라진다
                float gravity = Mathf.Max(MIN_GRAVITY, _gravity + Random.Range(-_gravityOffsetRange, _gravityOffsetRange));

                // 정지 상태에서 등가속도로 떨어지는 시간 = √(2 × 거리 ÷ 가속도)
                float warningDuration = _waitBeforeFall + Mathf.Sqrt((2.0f * _fallDistance) / gravity);
                float elapsed = 0.0f;
                while (elapsed < _waitBeforeFall)
                {
                    elapsed += Time.deltaTime;
                    SetWarningColor(elapsed / warningDuration);
                    yield return null;
                }

                // 낙하
                Vector3 bottomPosition = _topPosition + (Vector3.down * _fallDistance);
                float speed = 0.0f;
                while (transform.position != bottomPosition)
                {
                    speed += gravity * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, bottomPosition, speed * Time.deltaTime);
                    elapsed += Time.deltaTime;
                    SetWarningColor(Mathf.Min(elapsed / warningDuration, 1.0f));
                    yield return null;
                }

                SetWarningColor(1.0f);

                SetWarningVisible(false);

                if (_isDestroyOnLand)
                {
                    SpawnLandEffect();
                    Despawn();
                    yield break;
                }

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
