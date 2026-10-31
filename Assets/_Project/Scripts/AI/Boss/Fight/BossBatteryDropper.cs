using System.Collections.Generic;
using UnityEngine;
using AI.Boss.Arena;
using AI.Boss.Logic;

namespace AI.Boss.Fight
{
    /// <summary>
    /// 보스전이 진행되는 동안 일정 시간마다 아레나의 안전한 칸 하나에 배터리 아이템을 떨어뜨린다.
    /// 동시에 존재하는 개수는 제한하고, 안 먹힌 아이템은 일정 시간 뒤 사라진다.
    /// 보스전이 끝나거나 다시 시작되면(BossEncounter.IsBegun 이 꺼지면) 타이머를 처음으로 돌리고 남은 아이템을 치운다.
    /// </summary>
    public class BossBatteryDropper : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private BossEncounter _encounter;
        [SerializeField] private BossArena _arena;
        [Tooltip("떨어뜨릴 배터리 아이템 프리팹 (닿으면 획득되는 것)")]
        [SerializeField] private GameObject _batteryPrefab;

        [Header("Drop")]
        [Tooltip("보스전이 시작된 뒤 다음 드롭까지의 간격(초)")]
        [SerializeField, Min(0.1f)] private float _intervalSeconds = 20.0f;
        [Tooltip("동시에 존재할 수 있는 최대 개수. 가득 차 있으면 그 주기는 건너뛴다")]
        [SerializeField, Min(1)] private int _maxCount = 2;
        [Tooltip("안 먹힌 아이템이 사라지기까지의 시간(초)")]
        [SerializeField, Min(0.1f)] private float _itemLifetime = 15.0f;
        [Tooltip("칸 중심 기준 아이템이 떨어지는 높이(m)")]
        [SerializeField] private float _spawnHeight = 1.0f;
        #endregion

        #region Private Fields
        private readonly List<GameObject> _items = new List<GameObject>();
        private readonly List<int> _safeTileBuffer = new List<int>();
        private float _timer;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!_encounter || !_encounter.IsBegun)
            {
                ClearAll();
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < _intervalSeconds) return;

            _timer = 0.0f;
            TryDrop();
        }

        private void OnDisable()
        {
            ClearAll();
        }
        #endregion

        #region Private Methods
        private void TryDrop()
        {
            // 먹히거나 수명이 다해 사라진 항목을 목록에서 뺀다
            _items.RemoveAll(item => !item);
            if (_items.Count >= _maxCount) return;

            if (!_arena || !_batteryPrefab) return;

            _safeTileBuffer.Clear();
            for (int i = 0; i < ArenaGrid.COUNT; i++)
            {
                if (!_arena.IsHoleOrTelegraphed(i)) _safeTileBuffer.Add(i);
            }

            if (_safeTileBuffer.Count == 0) return;

            int tile = _safeTileBuffer[Random.Range(0, _safeTileBuffer.Count)];
            Vector3 position = _arena.GetTileCenter(tile) + (Vector3.up * _spawnHeight);
            GameObject item = Instantiate(_batteryPrefab, position, Quaternion.identity);
            Destroy(item, _itemLifetime);
            _items.Add(item);
        }

        private void ClearAll()
        {
            _timer = 0.0f;
            if (_items.Count == 0) return;

            foreach (GameObject item in _items)
            {
                if (item) Destroy(item);
            }

            _items.Clear();
        }
        #endregion
    }
}
