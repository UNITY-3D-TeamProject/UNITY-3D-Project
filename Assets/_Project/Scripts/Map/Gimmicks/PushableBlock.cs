using UnityEngine;
using Core;
using Facade.Player;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어 기준으로 한 칸씩 밀리거나 당겨지는 블록. 블록의 앞뒤·좌우 네 방향으로만 움직인다.
    /// 스스로 입력을 받지는 않는다. InteractButton.OnInteracted 에 Push, DamageButton.OnPressed 에 Pull 을 연결한다.
    /// 가려는 자리가 벽이나 다른 블록에 막혀 있으면 움직이지 않는다.
    /// 예: 미로 통로를 막은 상자를 옆 칸으로 치우기, 감압판(TriggerPlate) 위로 옮기기
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class PushableBlock : MonoBehaviour
    {
        #region Constants
        // 바닥이나 옆 벽에 스치는 것을 막힌 것으로 보지 않도록 검사 상자를 조금 줄인다
        private const float CAST_SHRINK_RATIO = 0.9f;

        // 당길 때 블록이 플레이어와 겹치지 않도록 두는 여유
        private const float PLAYER_CLEARANCE = 0.5f;
        #endregion

        #region Serialized Fields
        [Header("Movement")]
        [Tooltip("한 번에 움직이는 거리. 미로에서는 타일 한 칸 크기와 같게 둔다.")]
        [SerializeField, Min(0.1f)] private float _stepDistance = 5.0f;

        [Tooltip("이동 속도")]
        [SerializeField, Min(0.1f)] private float _speed = 6.0f;

        [Tooltip("블록의 길을 막는 레이어 (플레이어 레이어는 뺀다)")]
        [SerializeField] private LayerMask _blockingLayers = Physics.DefaultRaycastLayers;
        #endregion

        #region Private Fields
        private BoxCollider _collider;
        private Vector3 _targetPosition;
        private bool _isMoving;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _collider = GetComponent<BoxCollider>();

            // Rigidbody 없이 콜라이더를 움직이면 정적 콜라이더 이동으로 처리되어 충돌 판정이 불안정해진다.
            GetComponent<Rigidbody>().isKinematic = true;
        }

        private void Update()
        {
            if (!_isMoving) return;

            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _speed * Time.deltaTime);
            _isMoving = transform.position != _targetPosition;
        }
        #endregion

        #region Public Methods
        /// <summary>플레이어 반대쪽으로 한 칸 민다. (UnityEvent로 연결 가능)</summary>
        public void Push()
        {
            if (!TryGetPlayerPosition(out Vector3 playerPosition)) return;

            TryMove(-GetAxisToward(playerPosition), playerPosition);
        }

        /// <summary>플레이어 쪽으로 한 칸 당긴다. 플레이어와 겹치게 되면 움직이지 않는다. (UnityEvent로 연결 가능)</summary>
        public void Pull()
        {
            if (!TryGetPlayerPosition(out Vector3 playerPosition)) return;

            TryMove(GetAxisToward(playerPosition), playerPosition);
        }
        #endregion

        #region Private Methods
        private static bool TryGetPlayerPosition(out Vector3 position)
        {
            position = Vector3.zero;

            GameManager gameManager = GameManager.Instance;
            PlayerFacade playerFacade = (gameManager != null) ? gameManager.CurrentPlayerFacade : null;
            if (playerFacade == null) return false;

            // 실제로 움직이는 몸체는 플레이어 루트가 아니라 자식의 CharacterController 다
            CharacterController controller = playerFacade.transform.root.GetComponentInChildren<CharacterController>();
            if (controller == null) return false;

            position = controller.bounds.center;
            return true;
        }

        // 블록에서 본 플레이어 방향을 블록의 앞뒤·좌우 네 방향 중 가장 가까운 것으로 맞춘다
        private Vector3 GetAxisToward(Vector3 position)
        {
            Vector3 local = transform.InverseTransformDirection(position - transform.position);
            Vector3 axis = (Mathf.Abs(local.x) > Mathf.Abs(local.z))
                ? Vector3.right * Mathf.Sign(local.x)
                : Vector3.forward * Mathf.Sign(local.z);

            return transform.TransformDirection(axis);
        }

        private void TryMove(Vector3 direction, Vector3 playerPosition)
        {
            if (_isMoving) return;

            Vector3 destination = transform.position + (direction * _stepDistance);
            if (IsBlocked(direction) || IsOverlappingPlayer(destination, playerPosition)) return;

            _targetPosition = destination;
            _isMoving = true;
        }

        private bool IsBlocked(Vector3 direction)
        {
            Vector3 halfExtents = Vector3.Scale(_collider.size, transform.lossyScale) * (0.5f * CAST_SHRINK_RATIO);

            // 자기 콜라이더가 검사에 걸리지 않도록 잠시 끈다
            _collider.enabled = false;
            bool isBlocked = Physics.BoxCast(
                transform.TransformPoint(_collider.center),
                halfExtents,
                direction,
                transform.rotation,
                _stepDistance,
                _blockingLayers,
                QueryTriggerInteraction.Ignore);
            _collider.enabled = true;

            return isBlocked;
        }

        private bool IsOverlappingPlayer(Vector3 destination, Vector3 playerPosition)
        {
            Vector3 local = Quaternion.Inverse(transform.rotation) * (playerPosition - destination);
            Vector3 halfExtents = Vector3.Scale(_collider.size, transform.lossyScale) * 0.5f;

            return (Mathf.Abs(local.x) < (halfExtents.x + PLAYER_CLEARANCE)) &&
                   (Mathf.Abs(local.z) < (halfExtents.z + PLAYER_CLEARANCE));
        }
        #endregion
    }
}
