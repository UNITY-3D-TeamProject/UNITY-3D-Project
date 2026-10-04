using System;
using UnityEngine;
using Attribute.Core;

namespace Attribute.ProjectSpecific
{
    /// <summary>
    /// 인스펙터에 정의한 규칙에 따라 어트리뷰트 값을 최솟값 ~ 최댓값 사이로 보정하는 컴포넌트.
    /// 최솟값/최댓값은 다른 어트리뷰트에서 읽으며, 키가 비어 있거나 없는 어트리뷰트면 대체값을 사용한다.
    /// </summary>
    public class AttributeClamper : MonoBehaviour
    {
        #region Nested Types
        /// <summary>어트리뷰트 하나에 대한 클램핑 규칙.</summary>
        [Serializable]
        public struct SClampRule
        {
            [Tooltip("보정할 어트리뷰트 이름")]
            public string TargetKey;
            [Tooltip("최솟값을 읽을 어트리뷰트 이름")]
            public string MinKey;
            [Tooltip("최댓값을 읽을 어트리뷰트 이름")]
            public string MaxKey;
            [Tooltip("MinKey 가 비어 있거나 없는 어트리뷰트일 때 사용할 최솟값")]
            public float FallbackMin;
            [Tooltip("MaxKey 가 비어 있거나 없는 어트리뷰트일 때 사용할 최댓값")]
            public float FallbackMax;
        }
        #endregion

        #region Serialized Fields
        [SerializeField] private SClampRule[] _rules;
        #endregion

        #region Public Methods
        /// <summary>
        /// attributeName 을 대상으로 하는 규칙으로 newValue 를 보정한다.
        /// </summary>
        /// <param name="source">최솟값/최댓값을 읽을 대상</param>
        /// <param name="attributeName">변경되는 어트리뷰트 이름</param>
        /// <param name="newValue">반영될 값. 보정된 값으로 바뀐다.</param>
        public void Clamp(IEffectTarget source, string attributeName, ref float newValue)
        {
            if (_rules == null) return;

            foreach (var rule in _rules)
            {
                if (!IsSameKey(rule.TargetKey, attributeName)) continue;

                newValue = ClampByRule(source, rule, newValue);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 규칙의 최솟값/최댓값으로 value 를 보정한다.
        /// </summary>
        private float ClampByRule(IEffectTarget source, SClampRule rule, float value)
        {
            float min = GetBound(source, rule.MinKey, rule.FallbackMin);
            float max = GetBound(source, rule.MaxKey, rule.FallbackMax);

            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// key 어트리뷰트 값을 반환한다. key 가 비어 있거나 없는 어트리뷰트면 fallback 을 반환한다.
        /// </summary>
        private float GetBound(IEffectTarget source, string key, float fallback)
        {
            if (string.IsNullOrEmpty(key) || !source.IsValidTarget(key)) return fallback;

            return source.GetValue(key);
        }

        /// <summary>
        /// key 가 비어 있지 않고 attributeName 과 같은지(대소문자 무시) 확인한다.
        /// </summary>
        private bool IsSameKey(string key, string attributeName)
        {
            return !string.IsNullOrEmpty(key) &&
                   string.Equals(key, attributeName, StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }
}
