using Attribute.Core;
using Attribute.Effect;
using UnityEngine;

namespace Core
{
    public class MonsterSpawner : SpawnerBase
    {
        [Header("Monster")]
        [SerializeField] private GameObject _monsterPrefab;
        [SerializeField] private SOAttributeEffect[] _monsterEffects;

        private void Start()
        {
            SpawnMonster();
        }

        private void SpawnMonster()
        {
            if (_monsterPrefab == null)
            {
                Debug.LogError("Monster 프리팹이 지정되지 않았습니다.", this);
                return;
            }

            if (!TrySpawnWithAttributes(
                _monsterPrefab,
                transform,
                out GameObject monster,
                out AttributeSet attributes))
            {
                return;
            }

            // TODO: 지연 활성화가 필요하면 프리팹을 비활성 상태로 생성하고 AttributeSet의 Awake 초기화를 분리한다.
            // 활성 프리팹을 Instantiate한 뒤 SetActive(false)하면 Awake/OnEnable은 이미 실행된 상태다.
            // monster.SetActive(false);

            // 프리팹의 AttributeSet이 기본 SO를 읽은 뒤 개체별 Effect 값을 적용한다.
            ApplySpawnEffects(attributes, _monsterEffects);

            // monster.SetActive(true);
        }
    }
}
