using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.ObjectPool;

namespace Map.Gimmicks
{
    /// <summary>
    /// 켜져 있는 발판 위 무작위 위치로 떨어지는 위험물(FallingObstacle)을 일정 간격으로 꺼낸다.
    /// 위험물 프리팹은 FallingObstacle 의 Is Destroy On Land 를 켜고 Is Loop 를 끈다. 바닥에 닿으면 스스로 풀에 반납된다.
    /// 씬의 ObjectPoolManager 에 위험물 프리팹이 등록돼 있어야 풀을 쓴다. 매니저가 없는 씬에서는 만들고 파괴한다.
    /// 데미지는 위험물 프리팹의 EffectZone(Enter Effects)이 준다.
    /// StartSpawning 으로 시작하고, StopSpawning 으로 멈추며 남은 위험물을 거둔다. ClearHazards 는 생성은 그대로 두고 떨어지는 중인 위험물만 거둔다.
    /// </summary>
    public class FallingHazardSpawner : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Hazards")]
        [Tooltip("떨어뜨릴 위험물 프리팹. 만들 때마다 무작위로 하나를 고른다.")]
        [SerializeField] private FallingObstacle[] _hazardPrefabs;

        [Tooltip("위험물이 떨어질 발판. 켜져 있는 발판 중 하나를 무작위로 골라 콜라이더 윗면 안의 무작위 지점에 떨어뜨린다.")]
        [SerializeField] private Collider[] _platforms;

        [Header("Spawn")]
        [Tooltip("위험물을 만드는 간격(초). 첫 생성도 시작하고 이 시간이 지나서다.")]
        [SerializeField, Min(0.1f)] private float _spawnInterval = 2.0f;

        [Tooltip("한 번에 만드는 위험물 수")]
        [SerializeField, Min(1)] private int _spawnCount = 1;

        [Tooltip("발판 가장자리에서 안쪽으로 비우는 거리. 위험물이 발판 밖에 걸쳐 떨어지지 않게 한다.")]
        [SerializeField, Min(0.0f)] private float _edgeMargin = 1.0f;

        [Tooltip("켜면 발판을 윗면 넓이에 비례해 고른다. 크기가 제각각인 조각을 이어 한 구역을 덮을 때 구역 전체에 고르게 떨어진다. 끄면 켜진 발판 중 하나를 같은 확률로 고른다.")]
        [SerializeField] private bool _isAreaWeighted;
        #endregion

        #region Private Fields
        private readonly List<FallingObstacle> _hazards = new List<FallingObstacle>();
        private readonly List<Collider> _activePlatforms = new List<Collider>();
        private Coroutine _spawnRoutine;
        #endregion

        #region Properties
        /// <summary>위험물을 만드는 중인지 여부.</summary>
        public bool IsSpawning => _spawnRoutine != null;

        /// <summary>떨어지는 중인 위험물 수.</summary>
        public int HazardCount
        {
            get
            {
                RemoveFinishedHazards();
                return _hazards.Count;
            }
        }
        #endregion

        #region Unity Lifecycle
        private void OnDisable()
        {
            // 꺼지면 코루틴도 멈추므로 다시 켰을 때 StartSpawning 이 막히지 않게 비운다
            _spawnRoutine = null;
        }
        #endregion

        #region Public Methods
        /// <summary>위험물 생성을 시작한다. 이미 만드는 중이면 무시한다.</summary>
        public void StartSpawning()
        {
            if (_spawnRoutine != null) return;

            _spawnRoutine = StartCoroutine(CoSpawn());
        }

        /// <summary>위험물 생성을 멈추고 떨어지는 중인 위험물을 거둔다.</summary>
        public void StopSpawning()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            ClearHazards();
        }

        /// <summary>떨어지는 중인 위험물을 모두 거둔다. 생성은 그대로 계속한다.</summary>
        public void ClearHazards()
        {
            // 이미 반납된 위험물을 또 거두지 않는다
            RemoveFinishedHazards();

            foreach (FallingObstacle hazard in _hazards)
            {
                hazard.Despawn();
            }

            _hazards.Clear();
        }

        /// <summary>켜져 있는 발판 위 무작위 위치에 위험물을 하나 꺼낸다. 켜진 발판이나 프리팹이 없으면 꺼내지 않는다.</summary>
        public void SpawnHazard()
        {
            if ((_hazardPrefabs == null) || (_hazardPrefabs.Length == 0)) return;
            if (!TryPickLandingPoint(out Vector3 landingPoint)) return;

            FallingObstacle prefab = _hazardPrefabs[Random.Range(0, _hazardPrefabs.Length)];
            if (prefab == null) return;

            // 반납된 위험물이 다시 나올 수 있으므로 꺼내기 전에 목록에서 뺀다. 안 그러면 같은 위험물이 두 번 들어간다
            RemoveFinishedHazards();

            // 발판 묶음이 커져 있어도 프리팹 크기 그대로 떨어지도록 부모 없이 꺼낸다
            FallingObstacle hazard = GetHazard(prefab, landingPoint);
            if (hazard == null) return;

            hazard.DropOnto(landingPoint);
            _hazards.Add(hazard);
        }
        #endregion

        #region Private Methods
        // 켜져 있는 발판 하나를 무작위로 골라 윗면 안의 무작위 지점을 정한다. 켜진 발판이 없으면 false.
        private bool TryPickLandingPoint(out Vector3 landingPoint)
        {
            landingPoint = Vector3.zero;
            if (_platforms == null) return false;

            _activePlatforms.Clear();
            foreach (Collider platform in _platforms)
            {
                if ((platform != null) && platform.enabled && platform.gameObject.activeInHierarchy) _activePlatforms.Add(platform);
            }

            if (_activePlatforms.Count == 0) return false;

            Bounds bounds = PickPlatform().bounds;

            // 발판이 여백보다 좁으면 가운데에 떨어뜨린다
            float marginX = Mathf.Min(_edgeMargin, bounds.extents.x);
            float marginZ = Mathf.Min(_edgeMargin, bounds.extents.z);

            landingPoint = new Vector3(
                Random.Range(bounds.min.x + marginX, bounds.max.x - marginX),
                bounds.max.y,
                Random.Range(bounds.min.z + marginZ, bounds.max.z - marginZ));
            return true;
        }

        // 켜진 발판 중 하나를 고른다. 넓이 가중이 켜져 있으면 윗면 넓이에 비례해 고른다
        private Collider PickPlatform()
        {
            if (!_isAreaWeighted) return _activePlatforms[Random.Range(0, _activePlatforms.Count)];

            float totalArea = 0.0f;
            foreach (Collider platform in _activePlatforms)
            {
                totalArea += GetTopArea(platform);
            }

            float pick = Random.Range(0.0f, totalArea);
            foreach (Collider platform in _activePlatforms)
            {
                pick -= GetTopArea(platform);
                if (pick <= 0.0f) return platform;
            }

            return _activePlatforms[_activePlatforms.Count - 1];
        }

        private static float GetTopArea(Collider platform)
        {
            Vector3 size = platform.bounds.size;
            return size.x * size.z;
        }

        // 풀 매니저가 있으면 풀에서 꺼내고, 없으면 새로 만든다. 꺼낸 직후 위치는 DropOnto 가 다시 정한다
        private FallingObstacle GetHazard(FallingObstacle prefab, Vector3 position)
        {
            if (ObjectPoolManager.Instance == null)
            {
                return Instantiate(prefab, position, prefab.transform.rotation);
            }

            GameObject hazardObject = ObjectPoolManager.Instance.Get(prefab.gameObject, position, prefab.transform.rotation);
            return (hazardObject != null) ? hazardObject.GetComponent<FallingObstacle>() : null;
        }

        // 바닥에 닿아 스스로 반납되거나 파괴된 위험물을 목록에서 뺀다
        private void RemoveFinishedHazards()
        {
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                if ((_hazards[i] == null) || !_hazards[i].gameObject.activeSelf) _hazards.RemoveAt(i);
            }
        }
        #endregion

        #region Coroutines
        private IEnumerator CoSpawn()
        {
            WaitForSeconds wait = new WaitForSeconds(_spawnInterval);

            while (true)
            {
                yield return wait;

                for (int i = 0; i < _spawnCount; i++)
                {
                    SpawnHazard();
                }
            }
        }
        #endregion
    }
}
