using Attribute.Core;
using Attribute.Effect;
using Core.ObjectPool;
using UnityEngine;

namespace Core
{
    public class MonsterSpawner : SpawnerBase
    {
        public enum ESpawnMode
        {
            Fixed,
            Random,
        }

        // 몬스터 프리팹은 씬의 ObjectPoolManager에도 같은 프리팹으로 등록돼 있어야 한다. (Get(프리팹)으로 꺼낸다)
        [Header("Monster")]
        [Tooltip("Fixed: Monster Prefab 하나를 소환 / Random: Random Monster Prefabs 중 하나를 골라 소환")]
        [SerializeField] private ESpawnMode _spawnMode = ESpawnMode.Fixed;
        [Tooltip("Fixed일 때 소환할 프리팹. ObjectPoolManager에 등록돼 있어야 한다.")]
        [SerializeField] private GameObject _monsterPrefab;
        [Tooltip("Random일 때 이 중 하나를 골라 소환한다. 모두 ObjectPoolManager에 등록돼 있어야 한다.")]
        [SerializeField] private GameObject[] _randomMonsterPrefabs;
        [Tooltip("어떤 몬스터가 소환되든 똑같이 적용할 Effect")]
        [SerializeField] private SOAttributeEffect[] _monsterEffects;

        // ObjectPoolManager.Instance는 Awake에서 채워지므로 Start에서 꺼낸다.
        private void Start()
        {
            SpawnMonster();
        }

        private void SpawnMonster()
        {
            GameObject monsterPrefab = SelectMonsterPrefab();
            if (monsterPrefab == null)
            {
                Debug.LogError($"Monster 프리팹이 지정되지 않았습니다. ({_spawnMode}: 칸이 비었거나 랜덤 목록에 빈 칸이 있음)", this);
                return;
            }

            ObjectPoolManager poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("씬에 ObjectPoolManager가 없어 몬스터를 생성할 수 없습니다.", this);
                return;
            }

            // 등록되지 않은 프리팹이면 매니저가 에러를 남기고 null을 돌려준다.
            GameObject monster = poolManager.Get(monsterPrefab, transform.position, transform.rotation);
            if (monster == null)
            {
                return;
            }

            AttributeSet attributes = monster.GetComponentInChildren<AttributeSet>(true);
            if (attributes == null)
            {
                Debug.LogError($"[{monsterPrefab.name}] AttributeSet이 없습니다.", monster);
                poolManager.Release(monster);
                return;
            }

            // TODO: 지연 활성화가 필요하면 프리팹을 비활성 상태로 생성하고 AttributeSet의 Awake 초기화를 분리한다.
            // 활성 프리팹을 Instantiate한 뒤 SetActive(false)하면 Awake/OnEnable은 이미 실행된 상태다.
            // monster.SetActive(false);

            // 프리팹의 AttributeSet이 기본 SO를 읽은 뒤 개체별 Effect 값을 적용한다.
            // 몬스터를 재사용(Release 후 다시 Get)하게 되면 Attribute를 기본값으로 되돌린 뒤 적용해야 효과가 겹치지 않는다.
            ApplySpawnEffects(attributes, _monsterEffects);

            // monster.SetActive(true);
        }

        // Fixed면 _monsterPrefab, Random이면 _randomMonsterPrefabs 중 하나를 고른다. 고를 수 없으면 null.
        private GameObject SelectMonsterPrefab()
        {
            if (_spawnMode == ESpawnMode.Fixed)
            {
                return _monsterPrefab;
            }

            if (_randomMonsterPrefabs == null || _randomMonsterPrefabs.Length == 0)
            {
                return null;
            }

            return _randomMonsterPrefabs[UnityEngine.Random.Range(0, _randomMonsterPrefabs.Length)];
        }
    }
}
