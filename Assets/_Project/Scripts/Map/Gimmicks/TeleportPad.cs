using UnityEngine;
using PlayerInput;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 올라서면 짝지어진 위치로 옮기는 패드. 서로를 Destination 으로 넣으면 양방향이 된다.
    /// 도착한 패드는 플레이어가 한 번 벗어날 때까지 다시 보내지 않는다.
    /// 콜라이더의 Is Trigger를 켜야 한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TeleportPad : MonoBehaviour
    {
        #region Constants
        // 플레이어가 바닥에 파묻히지 않도록 도착 지점에서 띄우는 높이
        private const float HEIGHT_OFFSET = 0.2f;
        #endregion

        #region Serialized Fields
        [Header("Destination")]
        [Tooltip("플레이어를 옮길 위치. 다른 TeleportPad 를 넣으면 그 패드 위로 간다.")]
        [SerializeField] private Transform _destination;
        #endregion

        #region Private Fields
        private bool _isArrivalLocked;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_destination != null, $"[{name}] 도착 지점이 연결되지 않았습니다.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isArrivalLocked || (_destination == null)) return;
            if (!GimmickUtility.TryGetPlayer(other, out PlayerInputComponent player)) return;

            CharacterController controller = player.GetComponentInChildren<CharacterController>();
            if (controller == null) return;

            // 도착하자마자 되돌아오지 않도록 도착 패드를 잠근다
            if (_destination.TryGetComponent(out TeleportPad arrivalPad))
            {
                arrivalPad._isArrivalLocked = true;
            }

            // CharacterController가 켜져 있으면 위치를 직접 바꿔도 되돌아갈 수 있어 잠시 끈다.
            controller.enabled = false;
            controller.transform.position = _destination.position + (Vector3.up * HEIGHT_OFFSET);
            controller.enabled = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            _isArrivalLocked = false;
        }
        #endregion
    }
}
