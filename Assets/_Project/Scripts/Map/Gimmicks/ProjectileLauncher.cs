using UnityEngine;
using Attribute.Effect;
using Projectile;

namespace Map.Gimmicks
{
    /// <summary>
    /// Fire 가 불릴 때마다 정해진 방향으로 총알 한 발을 쏘는 고정 발사기. 스스로 쏘지는 않는다.
    /// 예: CycleTimer.OnPhaseOn 에 Fire 를 연결하면 일정 간격으로 쏘는 화살 함정,
    /// TriggerPlate.OnPlayerEntered 에 연결하면 밟았을 때 쏘는 함정
    /// </summary>
    public class ProjectileLauncher : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("총알이 생성될 위치. 이 Transform 의 정면으로 발사한다.")]
        [SerializeField] private Transform _firePoint;

        [Tooltip("생성할 총알 프리팹 (플레이어가 맞는 총알)")]
        [SerializeField] private BulletController _bulletPrefab;

        [Tooltip("맞은 대상에 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect _hitEffect;

        [Header("Options")]
        [Tooltip("총알이 사라질 때까지의 시간(초). 총알이 벽을 통과하므로 통로 폭에 맞춰 짧게 둔다. 0이면 총알 프리팹의 수명을 따른다.")]
        [SerializeField, Min(0.0f)] private float _bulletLifeTime = 0.25f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_firePoint != null, $"[{name}] 발사 지점이 연결되지 않았습니다.");
            Debug.Assert(_bulletPrefab != null, $"[{name}] 총알 프리팹이 연결되지 않았습니다.");
        }
        #endregion

        #region Public Methods
        /// <summary>총알 한 발을 쏜다. (UnityEvent로 연결 가능)</summary>
        public void Fire()
        {
            if (!isActiveAndEnabled || (_bulletPrefab == null) || (_firePoint == null)) return;

            BulletController bullet = Instantiate(_bulletPrefab, _firePoint.position, _firePoint.rotation);
            bullet.Initialize(_firePoint.forward, _hitEffect);

            if (_bulletLifeTime > 0.0f)
            {
                Destroy(bullet.gameObject, _bulletLifeTime);
            }
        }
        #endregion
    }
}
