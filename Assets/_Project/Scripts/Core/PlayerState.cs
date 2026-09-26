using Attribute.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class PlayerState
    {
        // SO 에셋원본에 정의된 어트리뷰트 이름이 필요함 (SO 원본을 읽기만함)
        private readonly SOAttributeData _soAttributeData;

        // 지금 씬의 플레이어에 붙은 컴포넌트 참조
        private AttributeSet _currentAttributeSet;

        // PlayerState가 복사해 보관할 저장값
        // 원래는 Dictionary<string,AttributeData>로 선언했지만
        // AttributeData의 Value값 말고 다른 것들은 일체 안쓰기 때문에 float으로 변경
        private readonly Dictionary<string, float> _saveAttributes
            = new(StringComparer.OrdinalIgnoreCase);

        // GameManager에서 SO에셋 원본을 매개변수로 받아옴
        public PlayerState(SOAttributeData attributeData)
        {
            if (attributeData == null)
            {
                // 즉시 오류를 발생시키는 코드
                throw new ArgumentNullException(nameof(attributeData));
            }
            _soAttributeData = attributeData;
        }

        // 혹시 모르니 모든 PlayerSO의 정보를 다 담을 예정
        // 새 플레이어를 기억하고, 저장된 값을 플레이어에게 적용
        // 매개변수는 PlayerSpawner가 방금 생성한 실제 플레이어의 AttributeSet 컴포넌트
        public void RegisterPlayer(AttributeSet attributeSet)
        {
            if (attributeSet == null)
            {
                Debug.LogError("플레이어의 AttributeSet이 없습니다.");
                return;
            }
            // 이 실제 플레이어의 AttributeSet을 _currentAttributeSet 변수로 갖고 있는다. 
            // 나중에 이 값을 가지고 데이터를 저장할 것이기 때문에           
            _currentAttributeSet = attributeSet;

            // 새 씬의 플레이어에게 이전에 저장한 값 적용
            foreach (var pair in _saveAttributes)
            {
                if (attributeSet.IsValidTarget(pair.Key))
                {
                    attributeSet.SetValue(pair.Key, pair.Value);
                }
            }
        }

        public void SaveCurrentAttributes()
        {
            if (_currentAttributeSet == null || _soAttributeData == null)
            {
                Debug.LogError("저장할 플레이어 또는 SOAttributeData가 없습니다.");
                return;
            }

            _saveAttributes.Clear();
            foreach (var entry in _soAttributeData.Attributes)
            {
                // 현재 플레이어의 AttributeSet에 이 이름의 어트리뷰트가 없는가?
                if (!_currentAttributeSet.IsValidTarget(entry.AttributeName))
                {
                    continue;
                }

                _saveAttributes[entry.AttributeName] = _currentAttributeSet.GetValue(entry.AttributeName);
            }
        }
    }
}

