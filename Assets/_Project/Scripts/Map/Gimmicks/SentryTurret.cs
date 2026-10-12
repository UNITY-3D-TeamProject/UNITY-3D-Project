using UnityEngine;
using Attribute.Effect;
using Core;
using Facade.Player;
using Projectile;

namespace Map.Gimmicks
{
    /// <summary>
    /// 사거리 안의 플레이어를 향해 돌면서 일정 간격으로 총알을 쏘는 고정 포탑.
    /// 플레이어가 벽이나 엄폐물 뒤에 있으면 쏘지 않는다.
    /// 컴포넌트를 끄면 멈춘다. GimmickSwitch 의 Enabled While On 에 넣어 전원 스위치로 켜고 끈다.
    /// </summary>
    public class SentryTurret : MonoBehaviour
    {
        #region Constants
        // 포신이 플레이어를 이 각도 안으로 겨눴을 때만 쏜다
        private const float FIRE_ANGLE = 8.0f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("플레이어 쪽으로 도는 부분")]
        [SerializeField] private Transform _head;

        [Tooltip("총알이 생성될 위치")]
        [SerializeField] private Transform _firePoint;

        [Tooltip("생성할 총알 프리팹 (플레이어가 맞는 총알)")]
        [SerializeField] private BulletController _bulletPrefab;

        [Tooltip("맞은 대상에 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect _hitEffect;

        [Header("Attack")]
        [Tooltip("이 거리 안의 플레이어만 노린다.")]
        [SerializeField, Min(0.0f)] private float _range = 15.0f;

        [Tooltip("사격 간격(초)")]
        [SerializeField, Min(0.1f)] private float _fireInterval = 1.2f;

        [Tooltip("초당 회전 각도")]
        [SerializeField, Min(1.0f)] private float _turnSpeed = 180.0f;

        [Tooltip("사격을 가로막는 레이어 (벽, 엄폐물)")]
        [SerializeField] private LayerMask _obstacleLayers = 1;
        #endregion

        #region Private Fields
        private PlayerFacade _playerFacade;
        private CharacterController _playerController;
        private float _nextFireTime;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_head != null, $"[{name}] 도는 부분이 연결되지 않았습니다.");
            Debug.Assert(_firePoint != null, $"[{name}] 발사 지점이 연결되지 않았습니다.");
            Debug.Assert(_bulletPrefab != null, $"[{name}] 총알 프리팹이 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            // 다시 켜진 직후에 바로 쏘지 않는다
            _nextFireTime = Time.time + _fireInterval;
        }

        private void Update()
        {
            if (!TryFindPlayer()) return;

            Vector3 target = _playerController.bounds.center;
            Vector3 toTarget = target - _head.position;
            if (toTarget.magnitude > _range) return;

            Quaternion look = Quaternion.LookRotation(toTarget);
            _head.rotation = Quaternion.RotateTowards(_head.rotation, look, _turnSpeed * Time.deltaTime);

            if (Time.time < _nextFireTime) return;
            if (Quaternion.Angle(_head.rotation, look) > FIRE_ANGLE) return;
            if (Physics.Linecast(_firePoint.position, target, _obstacleLayers, QueryTriggerInteraction.Ignore)) return;

            _nextFireTime = Time.time + _fireInterval;

            BulletController bullet = Instantiate(_bulletPrefab, _firePoint.position, _firePoint.rotation);
            bullet.Initialize(target - _firePoint.position, _hitEffect);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _range);
        }
        #endregion

        #region Private Methods
        // 현재 플레이어를 찾는다. 플레이어가 죽어 다시 스폰되면 새 플레이어로 바뀐다.
        private bool TryFindPlayer()
        {
            GameManager gameManager = GameManager.Instance;
            PlayerFacade playerFacade = (gameManager != null) ? gameManager.CurrentPlayerFacade : null;

            if (playerFacade == null)
            {
                _playerFacade = null;
                _playerController = null;
                return false;
            }

            if ((playerFacade != _playerFacade) || (_playerController == null))
            {
                _playerFacade = playerFacade;
                // 실제로 움직이는 몸체는 플레이어 루트가 아니라 자식의 CharacterController 다
                _playerController = playerFacade.transform.root.GetComponentInChildren<CharacterController>();
            }

            return _playerController != null;
        }
        #endregion
    }
}
