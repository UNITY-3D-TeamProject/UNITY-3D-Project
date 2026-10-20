using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 체크포인트의 RespawnPoint 에 붙여, 발판 흐름이 움직이는 동안 흘러가는 발판 위를 따라가게 한다.
    /// 흐름이 멈춰 있으면 원래 자리에 그대로 둔다. (멈춘 동안 GetBoardingPosition 은 발판이 없으면 새로 만들기 때문)
    /// </summary>
    [DefaultExecutionOrder(100)]   // 발판(PlatformMover)이 이번 프레임 이동을 마친 뒤 따라간다
    public class BoardingRespawnPoint : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("따라갈 발판을 흘려보내는 스포너")]
        [SerializeField] private PlatformSpawner _spawner;

        [Header("Boarding")]
        [Tooltip("스포너 출구에서 이만큼 나아간 지점에 가장 가까운 발판을 고른다. 최대 이동 거리에 가까울수록 곧 사라질 발판이 된다.")]
        [SerializeField, Min(0.0f)] private float _preferredDistance = 70.0f;

        [Tooltip("발판 중심에서 리스폰 위치까지의 높이")]
        [SerializeField] private float _heightOffset = 0.25f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_spawner != null, $"[{name}] PlatformSpawner가 연결되지 않았습니다.");
        }

        private void LateUpdate()
        {
            if ((_spawner == null) || _spawner.IsPaused) return;

            transform.position = _spawner.GetBoardingPosition(_preferredDistance) + (Vector3.up * _heightOffset);
        }
        #endregion
    }
}
