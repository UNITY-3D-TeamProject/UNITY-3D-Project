using Attribute.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class PlayerState
    {
        private const string CURRENT_HP_KEY = "CurrentHp";
        private const string CURRENT_BATTERY_KEY = "CurrentBattery";


        private readonly struct SRoundSnapshot
        {
            public float Health { get; }
            public float Battery { get; }

            public SRoundSnapshot(float health, float battery)
            {
                Health = health;
                Battery = battery;
            }
        }

        // SO 에셋원본에 정의된 어트리뷰트 이름이 필요함 (SO 원본을 읽기만함)
        private readonly SOAttributeData _playerSOAttributeData;

        // 지금 씬의 플레이어에 붙은 어트리뷰트 컴포넌트 참조
        private AttributeSet _playerCurrentAttributeSet;

        // 씬 전환하기 전에 플레이어의 어트리뷰트 값을 보관하고 있을 저장값
        // 원래는 Dictionary<string,AttributeData>로 선언했지만
        // AttributeData의 Value값 말고 다른 것들은 일체 안쓰기 때문에 float으로 변경
        private readonly Dictionary<string, float> _playerAttributesForSaving
            = new(StringComparer.OrdinalIgnoreCase);

        // Key: 라운드 인덱스, Value: 해당 라운드의 최초 능력치.
        // 기존 씬 전환용 _playerAttributesForSaving과 별도로 관리한다.
        private readonly Dictionary<int, SRoundSnapshot> _roundSnapshots = new();

        // 현재 등록된 플레이어의 AttributeSet 조회
        public AttributeSet CurrentAttributeSet => _playerCurrentAttributeSet;

        // 플레이어가 스폰할 때 넘겨줄 데이터가 존재하는지 존재 여부 조회 변수
        public bool HasSavedAttributes => _playerAttributesForSaving.Count > 0;

        // GameManager에서 SO에셋 원본을 매개변수로 받아옴 (GameManager에서 new로 만들어서 갖고있고 초기화해줌)
        public PlayerState(SOAttributeData playerSOattributeData)
        {
            if (playerSOattributeData == null)
            {
                // 즉시 오류를 발생시키는 코드
                throw new ArgumentNullException(nameof(playerSOattributeData));
            }
            // 플레이어 SO 원본 데이터를 알 수 있게 된다.
            _playerSOAttributeData = playerSOattributeData;
        }

        // 현재 씬에서 사용할 플레이어 참조를 연결한다.
        // 매개변수는 PlayerSpawner가 방금 생성한 실제 플레이어의 AttributeSet 컴포넌트
        public void SetCurrentPlayer(AttributeSet spawnedAttributeSet)
        {
            if (spawnedAttributeSet == null)
            {
                Debug.LogError("플레이어의 AttributeSet이 없습니다.");
                return;
            }

            _playerCurrentAttributeSet = spawnedAttributeSet;
        }

        // 씬 전환 하기 전에 플레이어의 어트리뷰트셋 값들을 저장하는 로직
        public void SaveCurrentAttributes()
        {
            if (_playerCurrentAttributeSet == null || _playerSOAttributeData == null)
            {
                Debug.LogError("저장할 플레이어 또는 SOAttributeData가 없습니다.");
                return;
            }

            // 저장하기전에 일단 저장칸을 비워둔다.(이전 값들이 있을 수 있으니)
            _playerAttributesForSaving.Clear();


            // 현재 플레이어 인스턴스의 어트리뷰트를 순회하면서 저장하는데
            // 이 때 SO플레이어 에셋에서 각 항목의 이름을 보면서 존재하는지 확인하고 모든 항목들을 저장해준다. 
            foreach (var entry in _playerSOAttributeData.Attributes)
            {
                // 현재 플레이어의 AttributeSet에 이 이름의 어트리뷰트가 없는가?
                if (!_playerCurrentAttributeSet.IsValidTarget(entry.AttributeName))
                {
                    continue;
                }

                // 현재 플레이어의 어트리뷰트 값들을 다 복사한다.
                _playerAttributesForSaving[entry.AttributeName] = _playerCurrentAttributeSet.GetValue(entry.AttributeName);
            }
        }

        // 저장된 능력치를 현재 플레이어에게 복원한다.
        public void RestoreSavedAttributes()
        {
            if (_playerCurrentAttributeSet == null)
            {
                Debug.LogError("능력치를 복원할 플레이어가 없습니다.");
                return;
            }

            foreach (var pair in _playerAttributesForSaving)
            {
                if (_playerCurrentAttributeSet.IsValidTarget(pair.Key))
                {
                    _playerCurrentAttributeSet.SetValue(pair.Key, pair.Value);
                }
            }
        }

        // 보관 중인 능력치만 비우며 현재 플레이어의 값은 변경하지 않는다.
        public void ClearSavedAttributes()
        {
            _playerAttributesForSaving.Clear();
        }

        // 해당 라운드의 최초 저장값이 있는지 확인한다.
        public bool HasRoundSnapshot(int roundIndex)
        {
            return _roundSnapshots.ContainsKey(roundIndex);
        }

        // 해당 라운드에 처음 진입했을 때만 저장한다.
        public void SaveRoundSnapshot(int roundIndex)
        {
            if (roundIndex < 0)
            {
                Debug.LogError("저장할 라운드 인덱스가 올바르지 않습니다.");
                return;
            }

            // 재접촉하거나 사망 후 재시도해도 최초 값을 유지한다.
            if (HasRoundSnapshot(roundIndex))
            {
                return;
            }

            if (!HasRoundAttributes())
            {
                Debug.LogError("라운드 능력치를 저장할 플레이어 또는 능력치가 없습니다.");
                return;
            }

            float health = _playerCurrentAttributeSet.GetValue(CURRENT_HP_KEY);
            float battery = _playerCurrentAttributeSet.GetValue(CURRENT_BATTERY_KEY);

            // 초기화 전 또는 사망한 상태의 체력을 복구 기준으로 저장하지 않는다.
            if (health <= 0f)
            {
                Debug.LogError("라운드 스냅샷은 능력치 초기화 후 생존 상태에서 저장해야 합니다.");
                return;
            }

            _roundSnapshots.Add(
                roundIndex,
                new SRoundSnapshot(health, battery));
        }

        // 해당 라운드에 최초 저장했던 체력·배터리를 복원한다.
        public void RestoreRoundSnapshot(int roundIndex)
        {
            if (!_roundSnapshots.TryGetValue(roundIndex, out SRoundSnapshot snapshot))
            {
                Debug.LogError($"라운드 {roundIndex}의 스냅샷이 없습니다.");
                return;
            }

            if (!HasRoundAttributes())
            {
                Debug.LogError("라운드 능력치를 복원할 플레이어 또는 능력치가 없습니다.");
                return;
            }

            _playerCurrentAttributeSet.SetValue(CURRENT_HP_KEY, snapshot.Health);
            _playerCurrentAttributeSet.SetValue(CURRENT_BATTERY_KEY, snapshot.Battery);
        }

        // 새 스테이지 시도를 시작할 때 기존 라운드 기록을 비운다.
        // 같은 스테이지에서 사망 후 재시도할 때는 호출하지 않는다.
        public void ClearRoundSnapshots()
        {
            _roundSnapshots.Clear();
        }

        // 저장·복원에 필요한 플레이어 참조와 능력치가 있는지 확인한다.
        private bool HasRoundAttributes()
        {
            return _playerCurrentAttributeSet != null &&
                _playerCurrentAttributeSet.IsValidTarget(CURRENT_HP_KEY) &&
                _playerCurrentAttributeSet.IsValidTarget(CURRENT_BATTERY_KEY);
        }

    }
}

