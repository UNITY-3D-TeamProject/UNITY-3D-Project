using System.Collections.Generic;
using UnityEngine;
using Attribute.Core;

namespace Core
{
    /// <summary>
    /// 몬스터 종류 하나의 초기 어트리뷰트 값. MonsterSpawner 가 소환한 프리팹으로 이 데이터를 찾아 값을 넣는다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOMonsterData", menuName = "Monster/MonsterData")]
    public class SOMonsterData : ScriptableObject
    {
        #region Serialized Fields
        [Tooltip("이 데이터를 쓸 몬스터 프리팹. 스포너가 소환한 프리팹과 같으면 아래 값을 적용한다.")]
        [SerializeField] private GameObject _monsterPrefab;
        [Tooltip("초기 어트리뷰트 값. 이름은 몬스터 AttributeSet 에 있는 이름과 같아야 한다.")]
        [SerializeField] private SOAttributeData.SAttribute[] _attributes;
        #endregion

        #region Properties
        /// <summary>이 데이터를 쓸 몬스터 프리팹.</summary>
        public GameObject MonsterPrefab => _monsterPrefab;
        /// <summary>초기 어트리뷰트 목록 (읽기 전용).</summary>
        public IReadOnlyList<SOAttributeData.SAttribute> Attributes => _attributes;
        #endregion
    }
}
