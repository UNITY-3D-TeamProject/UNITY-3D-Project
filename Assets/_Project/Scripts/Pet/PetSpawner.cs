using UnityEngine;

namespace Pet
{
    /// <summary>
    /// 플레이어가 생성되면 펫을 생성해 pivot 을 따라가게 하고, 전달받은 캐릭터 상태를 펫 애니메이션으로 넘긴다.
    /// 펫은 플레이어의 자식이 아닌 월드 오브젝트로 생성하며, 이 컴포넌트가 파괴될 때 함께 파괴한다.
    /// </summary>
    public class PetSpawner : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private PetFollower _petPrefab;
        [Tooltip("펫이 따라갈 위치. 캐릭터와 함께 회전하는 Transform 아래에 둔다")]
        [SerializeField] private Transform _pivot;
        #endregion

        #region Private Fields
        private PetFollower _pet;
        private PetAnimator _petAnimator;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (!_petPrefab || !_pivot) return;

            _pet = Instantiate(_petPrefab, _pivot.position, _pivot.rotation);
            _pet.SetTarget(_pivot);
            _petAnimator = _pet.GetComponent<PetAnimator>();
        }

        private void OnDestroy()
        {
            if (_pet) Destroy(_pet.gameObject);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 펫 점프 애니메이션을 재생한다.
        /// </summary>
        public void PlayJump()
        {
            if (_petAnimator) _petAnimator.PlayJump();
        }

        /// <summary>
        /// 펫 착지 애니메이션을 재생한다.
        /// </summary>
        public void PlayLand()
        {
            if (_petAnimator) _petAnimator.PlayLand();
        }

        /// <summary>
        /// 펫 피격 애니메이션을 재생한다.
        /// </summary>
        /// <param name="direction">캐릭터 기준 피격 방향. 0: 앞, 1: 뒤, 2: 왼쪽, 3: 오른쪽</param>
        public void PlayHit(int direction)
        {
            if (_petAnimator) _petAnimator.PlayHit(direction);
        }

        /// <summary>
        /// 펫 특수 Idle 애니메이션을 재생한다.
        /// </summary>
        /// <param name="specialIdleIndex">캐릭터가 재생한 특수 Idle 인덱스</param>
        public void PlaySpecialIdle(int specialIdleIndex)
        {
            if (_petAnimator) _petAnimator.PlaySpecialIdle(specialIdleIndex);
        }

        /// <summary>
        /// 펫 추종을 멈추고 사망 애니메이션을 재생한다.
        /// </summary>
        public void PlayDeath()
        {
            if (_pet) _pet.StopFollow();
            if (_petAnimator) _petAnimator.PlayDeath();
        }
        #endregion
    }
}
