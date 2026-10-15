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

        private const string MOVE_SPEED_KEY = "MoveSpeed";

        // 몬스터 프리팹은 씬의 ObjectPoolManager에도 같은 프리팹으로 등록돼 있어야 한다. (Get(프리팹)으로 꺼낸다)
        [Header("소환 방식 : Fixed = 아래 프리팹 하나 / Random = 랜덤 목록 중 하나")]
        [Tooltip("소환 방식.\nFixed: 아래 Monster Prefab 하나만 소환\nRandom: Random Monster Prefabs 목록 중 하나를 무작위로 골라 소환")]
        [SerializeField] private ESpawnMode _spawnMode = ESpawnMode.Fixed;
        [Header("Fixed일 때 필수 (몬스터 프리팹 ← Prefabs/AI/Enemy)")]
        [Tooltip("Spawn Mode가 Fixed일 때 소환할 몬스터 프리팹. Random이면 쓰지 않는다.\nObjectPoolManager와 Monster Database에 같은 프리팹이 있어야 한다.")]
        [SerializeField] private GameObject _monsterPrefab;
        [Header("Random일 때 필수 (몬스터 프리팹 여러 개, 빈 칸 금지)")]
        [Tooltip("Spawn Mode가 Random일 때 이 중 하나를 골라 소환한다. Fixed면 쓰지 않는다.\n오른쪽 숫자는 목록 칸 수. 칸을 비워 두면 안 된다.\n넣은 프리팹은 모두 ObjectPoolManager와 Monster Database에 있어야 한다.")]
        [SerializeField] private GameObject[] _randomMonsterPrefabs;
        [Header("필수 : 몬스터 초기값 DB (SOMonsterData ← ScriptableObjects/MonsterData)")]
        [Tooltip("몬스터 종류별 초기값 데이터(SOMonsterData) 목록. 오른쪽 숫자는 목록 칸 수.\n소환한 프리팹과 같은 Monster Prefab을 가진 데이터의 값을 SetValue로 넣는다.\n없으면 경고 후 프리팹 기본값으로 소환한다.")]
        [SerializeField] private SOMonsterData[] _monsterDatabase;
        [Header("안 해도 됨 : DB 값 뒤에 덧붙일 추가 Effect (비우면 0)")]
        [Tooltip("DB 값을 넣은 뒤 어떤 몬스터가 소환되든 똑같이 적용할 추가 Effect 목록. 오른쪽 숫자는 목록 칸 수.\n비워 둬도 된다(0). 같은 어트리뷰트를 Set하면 DB 값을 덮어쓴다.")]
        [SerializeField] private SOAttributeEffect[] _monsterEffects;
        [Header("MoveSpeed 오차 : 0.15 = 85%~115%, 0 = 오차 없음")]
        [Tooltip("같은 종류 몬스터라도 개체마다 MoveSpeed가 다르도록 줄 비율 오차. DB 값과 Effect를 적용한 뒤 곱한다.\n0.15면 85%~115% 사이 무작위, 0이면 오차 없음")]
        [SerializeField, Range(0.0f, 1.0f)] private float _moveSpeedVariance;

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

            // DB 값 → 추가 Effect → MoveSpeed 오차 순서로 적용한다.
            // DB 값을 매번 SetValue로 다시 넣으므로 재사용(Release 후 다시 Get)해도 값이 누적되지 않고, 모터 등에도 값이 전달된다.
            ApplyMonsterData(monsterPrefab, attributes);
            ApplySpawnEffects(attributes, _monsterEffects);
            ApplyMoveSpeedVariance(attributes);

            // monster.SetActive(true);
        }

        // _monsterDatabase에서 monsterPrefab의 데이터를 찾아 초기값을 넣는다. 없으면 프리팹 기본 SO 값을 그대로 둔다.
        private void ApplyMonsterData(GameObject monsterPrefab, AttributeSet attributes)
        {
            SOMonsterData monsterData = FindMonsterData(monsterPrefab);
            if (monsterData == null)
            {
                Debug.LogWarning($"[{monsterPrefab.name}] Monster Database에 데이터가 없어 프리팹 기본값으로 소환합니다.", this);
                return;
            }

            foreach (SOAttributeData.SAttribute entry in monsterData.Attributes)
            {
                if (!attributes.IsValidTarget(entry.AttributeName))
                {
                    Debug.LogWarning($"[{monsterData.name}] 어트리뷰트 '{entry.AttributeName}'가 몬스터에 없어 건너뜁니다.", monsterData);
                    continue;
                }

                attributes.SetValue(entry.AttributeName, entry.Value);
            }
        }

        private SOMonsterData FindMonsterData(GameObject monsterPrefab)
        {
            if (_monsterDatabase == null)
            {
                return null;
            }

            foreach (SOMonsterData monsterData in _monsterDatabase)
            {
                if (monsterData != null && monsterData.MonsterPrefab == monsterPrefab)
                {
                    return monsterData;
                }
            }

            return null;
        }

        // MoveSpeed를 값 × Random(1 - 오차, 1 + 오차)로 덮어쓴다.
        private void ApplyMoveSpeedVariance(AttributeSet attributes)
        {
            if (_moveSpeedVariance <= 0.0f)
            {
                return;
            }

            if (!attributes.IsValidTarget(MOVE_SPEED_KEY))
            {
                Debug.LogWarning($"[{name}] 어트리뷰트 '{MOVE_SPEED_KEY}'가 몬스터에 없어 속도 오차를 주지 않습니다.", this);
                return;
            }

            float ratio = UnityEngine.Random.Range(1.0f - _moveSpeedVariance, 1.0f + _moveSpeedVariance);
            attributes.SetValue(MOVE_SPEED_KEY, attributes.GetValue(MOVE_SPEED_KEY) * ratio);
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
