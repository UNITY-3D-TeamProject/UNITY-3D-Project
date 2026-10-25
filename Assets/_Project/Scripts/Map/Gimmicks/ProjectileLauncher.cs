using UnityEngine;
using Attribute.Effect;
using Projectile;

namespace Map.Gimmicks
{
    /// <summary>
    /// Fire 가 불릴 때마다 정해진 방향으로 총알을 쏘는 고정 발사기.
    /// 여러 발사 지점과 자동 발사 간격을 설정할 수 있으며, 둘 다 비워 두면 기존처럼 외부 이벤트로 한 발씩 쏜다.
    /// </summary>
    public class ProjectileLauncher : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("총알이 생성될 위치. 이 Transform 의 정면으로 발사한다.")]
        [SerializeField] private Transform _firePoint;

        [Tooltip("동시에 발사할 여러 지점. 하나 이상 연결하면 단일 Fire Point 대신 이 목록을 사용한다.")]
        [SerializeField] private Transform[] _firePoints;

        [Tooltip("생성할 총알 프리팹 (플레이어가 맞는 총알)")]
        [SerializeField] private BulletController _bulletPrefab;

        [Tooltip("맞은 대상에 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect _hitEffect;

        [Header("Options")]
        [Tooltip("자동 발사 간격(초). 0이면 외부에서 Fire를 호출할 때만 발사한다.")]
        [SerializeField, Min(0.0f)] private float _autoFireInterval;

        [Tooltip("총알이 사라질 때까지의 시간(초). 총알이 벽을 통과하므로 통로 폭에 맞춰 짧게 둔다. 0이면 총알 프리팹의 수명을 따른다.")]
        [SerializeField, Min(0.0f)] private float _bulletLifeTime = 0.25f;
        #endregion

        #region Private Fields
        private float _remainingAutoFireTime;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(HasFirePoint(), $"[{name}] 발사 지점이 연결되지 않았습니다.");
            Debug.Assert(_bulletPrefab != null, $"[{name}] 총알 프리팹이 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            _remainingAutoFireTime = _autoFireInterval;
        }

        private void Update()
        {
            if (_autoFireInterval <= 0.0f) return;

            _remainingAutoFireTime -= Time.deltaTime;
            if (_remainingAutoFireTime > 0.0f) return;

            Fire();
            _remainingAutoFireTime += _autoFireInterval;
        }
        #endregion

        #region Public Methods
        /// <summary>연결된 모든 발사 지점에서 총알을 쏜다. (UnityEvent로 연결 가능)</summary>
        public void Fire()
        {
            if (!isActiveAndEnabled || (_bulletPrefab == null)) return;

            if ((_firePoints != null) && (_firePoints.Length > 0))
            {
                foreach (Transform firePoint in _firePoints)
                {
                    if (firePoint != null)
                    {
                        FireFrom(firePoint);
                    }
                }

                return;
            }

            if (_firePoint != null)
            {
                FireFrom(_firePoint);
            }
        }
        #endregion

        #region Private Methods
        private bool HasFirePoint()
        {
            if (_firePoint != null) return true;
            if (_firePoints == null) return false;

            foreach (Transform firePoint in _firePoints)
            {
                if (firePoint != null) return true;
            }

            return false;
        }

        private void FireFrom(Transform firePoint)
        {
            BulletController bullet = Instantiate(_bulletPrefab, firePoint.position, firePoint.rotation);
            bullet.Initialize(firePoint.forward, _hitEffect);

            if (_bulletLifeTime > 0.0f)
            {
                Destroy(bullet.gameObject, _bulletLifeTime);
            }
        }
        #endregion
    }
}
