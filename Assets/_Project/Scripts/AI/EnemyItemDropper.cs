using System;
using Mediator;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 몬스터가 죽으면 설정한 확률로 아이템을 한 개 떨어뜨린다.
    /// 드랍 여부를 먼저 굴리고, 드랍이 정해지면 가중치로 아이템 하나를 고른다.
    /// 떨어진 아이템은 닿으면 획득되는 아이템 프리팹(EffectItem)이며, 안 먹히면 일정 시간 뒤 사라진다.
    /// </summary>
    public class EnemyItemDropper : MonoBehaviour
    {
        #region Types
        [Serializable]
        public struct SDropEntry
        {
            [Tooltip("떨어뜨릴 아이템 프리팹 (닿으면 획득되는 것)")]
            public GameObject Prefab;

            [Tooltip("다른 항목 대비 뽑힐 비율. 0이면 뽑히지 않는다.")]
            [Min(0.0f)] public float Weight;
        }
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterMediator _characterMediator;

        [Header("Drop")]
        [Tooltip("죽었을 때 아이템이 떨어질 확률 (0~1)")]
        [SerializeField, Range(0.0f, 1.0f)] private float _dropChance = 0.25f;
        [Tooltip("드랍이 정해졌을 때 가중치로 하나를 고르는 아이템 목록")]
        [SerializeField] private SDropEntry[] _entries;
        [Tooltip("아이템이 떨어지는 위치의 높이 보정(m). 몬스터 발 위치 기준")]
        [SerializeField] private float _spawnHeight = 0.5f;
        [Tooltip("안 먹힌 아이템이 사라지기까지의 시간(초)")]
        [SerializeField, Min(0.1f)] private float _itemLifetime = 15.0f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_characterMediator) _characterMediator = GetComponent<CharacterMediator>();
        }

        private void OnEnable()
        {
            _characterMediator.OnDeath += OnDeathCallback;
        }

        private void OnDisable()
        {
            _characterMediator.OnDeath -= OnDeathCallback;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 드랍 판정에 따라 뽑을 항목의 인덱스를 돌려준다. 드랍하지 않거나 뽑을 항목이 없으면 -1.
        /// </summary>
        /// <param name="entries">가중치 목록</param>
        /// <param name="dropChance">드랍 확률 (0~1)</param>
        /// <param name="chanceRoll">드랍 여부 주사위 [0,1). dropChance 보다 작으면 드랍</param>
        /// <param name="weightRoll">항목 선택 주사위 [0,1)</param>
        public static int SelectEntry(SDropEntry[] entries, float dropChance, float chanceRoll, float weightRoll)
        {
            if (entries == null || chanceRoll >= dropChance) return -1;

            float totalWeight = 0.0f;
            foreach (SDropEntry entry in entries)
            {
                if (entry.Prefab != null) totalWeight += entry.Weight;
            }

            if (totalWeight <= 0.0f) return -1;

            float target = weightRoll * totalWeight;
            int lastIndex = -1;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Prefab == null || entries[i].Weight <= 0.0f) continue;

                lastIndex = i;
                target -= entries[i].Weight;
                if (target < 0.0f) return i;
            }

            // 부동소수 오차로 끝까지 못 뺐으면 마지막 유효 항목
            return lastIndex;
        }
        #endregion

        #region Private Methods
        private void OnDeathCallback()
        {
            int index = SelectEntry(_entries, _dropChance, UnityEngine.Random.value, UnityEngine.Random.value);
            if (index < 0) return;

            Vector3 position = transform.position + (Vector3.up * _spawnHeight);
            GameObject item = Instantiate(_entries[index].Prefab, position, Quaternion.identity);
            Destroy(item, _itemLifetime);
        }
        #endregion
    }
}
