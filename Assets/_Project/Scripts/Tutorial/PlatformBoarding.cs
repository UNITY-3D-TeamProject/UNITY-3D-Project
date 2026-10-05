using System.Collections;
using UnityEngine;
using Map.Gimmicks;

namespace Tutorial
{
    /// <summary>
    /// 플레이어를 스포너 출구의 발판 위에 올려놓는다. 씬 시작 시, 그리고 BoardingRespawnZone 에 닿았을 때 쓴다.
    /// 올려놓은 자리는 플레이어의 리스폰 위치도 된다.
    /// 플레이어에 PlayerRespawner 가 붙어 있어야 한다.
    /// </summary>
    public class PlatformBoarding : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("플레이어가 올라탈 발판을 내보내는 스포너")]
        [SerializeField] private PlatformSpawner _spawner;

        [Header("Settings")]
        [Tooltip("스포너 출구에서 이만큼 나아간 지점의 발판에 올린다. 출구의 텔레포트 트리거(TeleportZone)에 닿지 않을 만큼 띄운다.")]
        [SerializeField, Min(0.0f)] private float _boardingDistance = 10.0f;

        [Tooltip("발판 중심에서 이 높이만큼 위에 플레이어를 놓는다.")]
        [SerializeField, Min(0.0f)] private float _standHeight = 1.0f;

        [Tooltip("켜면 씬이 시작될 때 플레이어를 발판 위에 올려놓는다.")]
        [SerializeField] private bool _shouldBoardOnStart = true;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_spawner != null, $"[{name}] PlatformSpawner가 연결되지 않았습니다.");
        }

        private void Start()
        {
            if (_shouldBoardOnStart)
            {
                StartCoroutine(CoBoardOnStart());
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 플레이어를 스포너 출구의 발판 위로 옮긴다.
        /// </summary>
        /// <param name="respawner">옮길 플레이어의 PlayerRespawner</param>
        public void Board(PlayerRespawner respawner)
        {
            if ((_spawner == null) || (respawner == null)) return;

            Vector3 standPosition = _spawner.GetBoardingPosition(_boardingDistance) +(Vector3.up * _standHeight);
            respawner.SetRespawnPosition(standPosition);
            respawner.Respawn();
        }
        #endregion

        #region Coroutines
        private IEnumerator CoBoardOnStart()
        {
            // 스포너가 발판을 깔고 PlayerRespawner 가 시작 위치를 정한 다음 프레임에 옮긴다.
            yield return null;

            PlayerRespawner respawner = FindAnyObjectByType<PlayerRespawner>();
            if (respawner == null)
            {
                Debug.LogWarning($"[{name}] 씬에서 PlayerRespawner를 찾지 못해 플레이어를 발판에 올리지 못했습니다.");
                yield break;
            }

            Board(respawner);
        }
        #endregion
    }
}
