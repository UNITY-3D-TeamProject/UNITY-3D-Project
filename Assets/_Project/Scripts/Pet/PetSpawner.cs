using System;
using UnityEngine;

namespace Pet
{
    /// <summary>
    /// 플레이어가 생성되면 펫을 생성해 pivot 을 따라가게 하고, 생성한 펫을 OnPetSpawned 로 알린다.
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

        #region Properties
        /// <summary>생성된 펫. 아직 생성 전이거나 생성하지 못했으면 null.</summary>
        public PetFollower Pet { get; private set; }

        /// <summary>펫이 따라가는 위치.</summary>
        public Transform Pivot => _pivot;
        #endregion

        #region Events
        /// <summary>펫을 생성했을 때 생성한 펫과 함께 발생한다.</summary>
        public event Action<PetFollower> OnPetSpawned;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (!_petPrefab || !_pivot) return;

            Pet = Instantiate(_petPrefab, _pivot.position, _pivot.rotation);
            Pet.SetTarget(_pivot);
            OnPetSpawned?.Invoke(Pet);
        }

        private void OnDestroy()
        {
            if (Pet) Destroy(Pet.gameObject);
        }
        #endregion
    }
}
