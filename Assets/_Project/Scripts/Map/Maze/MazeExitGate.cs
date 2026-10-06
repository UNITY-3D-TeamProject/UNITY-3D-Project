using System;
using UnityEngine;
using Map.Gimmicks;

namespace Map.Maze
{
    /// <summary>
    /// 미로의 출구. 플레이어가 출구 칸에 들어오면 문(출구 벽)이 사라져 다음 미로로 가는 다리로 나갈 수 있다.
    /// 문이 열린 자리는 플레이어의 리스폰 위치가 된다. (다리에서 떨어지면 여기로 돌아온다)
    /// 출구 칸 중앙에 놓고, 콜라이더의 Is Trigger를 켠다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MazeExitGate : MonoBehaviour
    {
        #region Constants
        private const float RESPAWN_HEIGHT_OFFSET = 0.1f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("열릴 때 사라지는 문 오브젝트 (콜라이더 포함)")]
        [SerializeField] private GameObject _door;
        #endregion

        #region Properties
        /// <summary>문이 열렸는지 여부.</summary>
        public bool IsOpen { get; private set; }
        #endregion

        #region Events
        /// <summary>문이 열릴 때 호출된다.</summary>
        public event Action OnOpened;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_door != null, $"[{name}] 문 오브젝트가 연결되지 않았습니다.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsOpen) return;

            PlayerRespawner respawner = other.GetComponentInParent<PlayerRespawner>();
            if (respawner == null) return;

            respawner.SetRespawnPosition(transform.position + (Vector3.up * RESPAWN_HEIGHT_OFFSET));
            Open();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 문을 연다.
        /// </summary>
        public void Open()
        {
            if (IsOpen) return;

            IsOpen = true;
            if (_door != null)
            {
                _door.SetActive(false);
            }

            OnOpened?.Invoke();
        }
        #endregion
    }
}
