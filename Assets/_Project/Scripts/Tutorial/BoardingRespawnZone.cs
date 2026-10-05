using UnityEngine;
using Map.Gimmicks;

namespace Tutorial
{
    /// <summary>
    /// 플레이어가 닿으면 스포너 출구의 발판 위로 되돌린다. (소각로 입구, 발판 구간 아래 낙사 영역용)
    /// 콜라이더의 Is Trigger를 켜야 한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BoardingRespawnZone : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private PlatformBoarding _boarding;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_boarding != null, $"[{name}] PlatformBoarding이 연결되지 않았습니다.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_boarding == null) return;

            PlayerRespawner respawner = other.GetComponentInParent<PlayerRespawner>();
            if (respawner == null) return;

            _boarding.Board(respawner);
        }
        #endregion
    }
}
