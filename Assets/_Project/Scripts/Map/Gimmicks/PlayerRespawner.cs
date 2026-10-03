using UnityEngine;
using Map.Common;
using SceneTransition;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어의 시작 위치와 리스폰 위치를 관리한다. CharacterController가 붙은 플레이어 루트에 붙인다.
    /// - 씬 시작: 직전 씬으로 가는 포탈(IsReturnPoint)이 있으면 그 앞에서, 동적으로 생성된 맵이 있으면 그 시작 지점에서,
    ///   둘 다 없으면 놓인 자리에서 시작한다.
    /// - 리스폰 위치: 처음에는 시작 위치이고, Checkpoint를 밟으면 그 위치로 바뀐다.
    /// 씬이 다시 로드되면 이 컴포넌트도 새로 만들어지므로 리스폰 위치는 씬을 떠나면 초기화된다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerRespawner : MonoBehaviour
    {
        #region Private Fields
        private CharacterController _controller;
        private Vector3 _respawnPosition;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            ScenePortal returnPortal = FindReturnPortal();
            if (returnPortal != null)
            {
                MoveTo(returnPortal.ArrivalPosition);
            }
            else
            {
                // 동적으로 생성된 맵이 있으면 그 맵의 시작 지점에서 시작한다
                MapGeneratorBase generator = FindAnyObjectByType<MapGeneratorBase>();
                if ((generator != null) && generator.IsGenerated)
                {
                    MoveTo(generator.StartPosition);
                }
            }

            _respawnPosition = transform.position;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 리스폰 위치를 바꾼다. (체크포인트에서 호출)
        /// </summary>
        /// <param name="position">다음 리스폰 때 플레이어가 놓일 위치</param>
        public void SetRespawnPosition(Vector3 position)
        {
            _respawnPosition = position;
        }

        /// <summary>
        /// 플레이어를 현재 리스폰 위치로 옮긴다. (낙사 영역에서 호출)
        /// </summary>
        public void Respawn()
        {
            MoveTo(_respawnPosition);
        }
        #endregion

        #region Private Methods
        // 직전 씬으로 가는 포탈 = 플레이어가 들어갔던 포탈
        private static ScenePortal FindReturnPortal()
        {
            if (!SceneLoader.PreviousScene.HasValue) return null;

            ScenePortal[] portals = FindObjectsByType<ScenePortal>(FindObjectsSortMode.None);
            foreach (ScenePortal portal in portals)
            {
                if (portal.IsReturnPoint && (portal.Destination == SceneLoader.PreviousScene.Value))
                {
                    return portal;
                }
            }

            return null;
        }

        private void MoveTo(Vector3 position)
        {
            // CharacterController가 켜져 있으면 위치를 직접 바꿔도 되돌아갈 수 있어 잠시 끈다.
            // TODO: CharacterMotor에 속도 초기화 API가 생기면 호출한다. 지금은 낙하 속도가 남아 있다.
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
        }
        #endregion
    }
}
