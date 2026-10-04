using UnityEngine;
using PlayerInput;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 닿으면 리스폰 위치(시작 위치 또는 마지막으로 밟은 Checkpoint)로 되돌린다.
    /// 맵 아래에 깔아 낙사 처리에 쓴다. 콜라이더의 Is Trigger를 켜야 한다.
    /// 플레이어에 PlayerRespawner가 붙어 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RespawnZone : MonoBehaviour
    {
        #region Unity Lifecycle
        private void OnTriggerEnter(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out PlayerInputComponent player)) return;

            PlayerRespawner respawner = player.GetComponentInChildren<PlayerRespawner>();
            if (respawner == null)
            {
                Debug.LogWarning($"[{name}] 플레이어에 PlayerRespawner가 없어 리스폰할 수 없습니다.");
                return;
            }

            respawner.Respawn();
        }
        #endregion
    }
}
