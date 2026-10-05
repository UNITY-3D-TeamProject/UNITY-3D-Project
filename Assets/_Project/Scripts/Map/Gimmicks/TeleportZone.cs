using UnityEngine;
using PlayerInput;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 닿으면 도착 지점으로 즉시 옮긴다. 도착 지점은 리스폰 위치도 된다.
    /// 콜라이더의 Is Trigger를 켜야 한다. 플레이어에 PlayerRespawner가 붙어 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TeleportZone : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Destination")]
        [Tooltip("플레이어가 놓일 위치. 이 트리거 밖에 둔다.")]
        [SerializeField] private Transform _arrivalPoint;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_arrivalPoint != null, $"[{name}] 도착 지점이 연결되지 않았습니다.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_arrivalPoint == null) return;
            if (!GimmickUtility.TryGetPlayer(other, out PlayerInputComponent player)) return;

            PlayerRespawner respawner = player.GetComponentInChildren<PlayerRespawner>();
            if (respawner == null)
            {
                Debug.LogWarning($"[{name}] 플레이어에 PlayerRespawner가 없어 이동시킬 수 없습니다.");
                return;
            }

            respawner.SetRespawnPosition(_arrivalPoint.position);
            respawner.Respawn();
        }
        #endregion
    }
}
