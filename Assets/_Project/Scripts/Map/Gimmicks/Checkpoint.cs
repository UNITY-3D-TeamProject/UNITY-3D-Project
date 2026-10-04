using UnityEngine;
using PlayerInput;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 닿으면 리스폰 위치를 이곳으로 바꾸는 중간 저장 지점.
    /// 콜라이더의 Is Trigger를 켜야 한다. 플레이어에 PlayerRespawner가 붙어 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("리스폰될 위치. 비워 두면 이 오브젝트의 위치를 쓴다.")]
        [SerializeField] private Transform _respawnPoint;
        #endregion

        #region Unity Lifecycle
        private void OnTriggerEnter(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out PlayerInputComponent player)) return;

            PlayerRespawner respawner = player.GetComponentInChildren<PlayerRespawner>();
            if (respawner == null) return;

            Vector3 position = (_respawnPoint != null) ? _respawnPoint.position : transform.position;
            respawner.SetRespawnPosition(position);
        }
        #endregion
    }
}
