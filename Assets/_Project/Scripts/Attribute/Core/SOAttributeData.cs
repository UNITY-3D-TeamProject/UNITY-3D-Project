using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Attribute.Core
{
    /// <summary>
    /// AttributeSet 의 초기 어트리뷰트 목록(이름 + 초기값)을 정의하는 데이터 에셋.
    /// </summary>
    [CreateAssetMenu(fileName = "SOAttributeData", menuName = "Attribute/AttributeData")]
    public class SOAttributeData : ScriptableObject
    {
        #region Nested Types
        /// <summary>어트리뷰트 하나의 이름과 초기값.</summary>
        [Serializable]
        public struct SAttribute
        {
            public string AttributeName;
            public float Value;
        }
        #endregion

        #region Serialized Fields
        [Tooltip("어트리뷰트 목록. 이름은 비어 있거나 중복될 수 없다.")]
        [SerializeField] private SAttribute[] _attributes;
        #endregion

        #region Properties
        /// <summary>정의된 어트리뷰트 목록 (읽기 전용).</summary>
        public IReadOnlyList<SAttribute> Attributes => _attributes;
        #endregion

        #region Unity Lifecycle
        private void OnValidate()
        {
            if (_attributes == null) return;

            // 빈 이름 / 중복 이름을 에디터에서 즉시 검출
            var seenNames = new HashSet<string>();
            foreach (var entry in _attributes)
            {
                if (string.IsNullOrWhiteSpace(entry.AttributeName))
                {
                    Assert.IsTrue(false, $"[{name}] : AttributeName은 비어있을 수 없습니다");
                    continue;
                }

                if (!seenNames.Add(entry.AttributeName))
                {
                    Assert.IsTrue(false, $"[{name}] : AttributeName '{entry.AttributeName}' 이(가) 중복되었습니다");
                }
            }
        }
        #endregion
    }
}
