using Attribute.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class PlayerState
    {
        // SO 에셋원본에 정의된 어트리뷰트 이름이 필요함 (SO 원본을 읽기만함)
        private readonly SOAttributeData _playerSOAttributeData;

        // 지금 씬의 플레이어에 붙은 어트리뷰트 컴포넌트 참조
        private AttributeSet _playerCurrentAttributeSet;

        // 씬 전환하기 전에 플레이어의 어트리뷰트 값을 보관하고 있을 저장값
        // 원래는 Dictionary<string,AttributeData>로 선언했지만
        // AttributeData의 Value값 말고 다른 것들은 일체 안쓰기 때문에 float으로 변경
        private readonly Dictionary<string, float> _playerAttributesForSaving
            = new(StringComparer.OrdinalIgnoreCase);

        // 현재 등록된 플레이어의 AttributeSet 조회
        public AttributeSet CurrentAttributeSet => _playerCurrentAttributeSet;


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


        // 새 플레이어를 기억하고, 저장된 값을 플레이어에게 적용
        // 매개변수는 PlayerSpawner가 방금 생성한 실제 플레이어의 AttributeSet 컴포넌트
        public void RegisterPlayer(AttributeSet _spawnedPlayerattributeSet)
        {
            if (_spawnedPlayerattributeSet == null)
            {
                Debug.LogError("플레이어의 AttributeSet이 없습니다.");
                return;
            }

            // 이 실제 플레이어 인스턴스의 AttributeSet(변하는 값이겠지)을 PlayerState에서 _playerCurrentAttributeSet으로 변수로 갖고 있는다. 
            // 나중에 이 값을 가지고 데이터를 저장할 것이기 때문에           
            _playerCurrentAttributeSet = _spawnedPlayerattributeSet;

            // 만약에 이전 플레이어의 Value 값들을 저장해놓은 딕셔너리가 존재하면 현재 플레이어 인스턴스의 Value에 덮어쓰기 해준다.
            foreach (var pair in _playerAttributesForSaving)
            {
                if (_spawnedPlayerattributeSet.IsValidTarget(pair.Key))
                {
                    _spawnedPlayerattributeSet.SetValue(pair.Key, pair.Value);
                }
            }
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
    }
}

