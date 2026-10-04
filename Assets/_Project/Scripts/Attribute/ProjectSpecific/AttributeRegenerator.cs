using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Attribute.Core;
using Attribute.Effect;

namespace Attribute.ProjectSpecific
{
    /// <summary>
    /// 인스펙터에 정의한 규칙에 따라 일정 주기마다 SOAttributeEffect 를 대상에 적용하는 컴포넌트.
    /// 효과의 값 출처가 Attribute 면 대상 자신을 cursor 로 사용하며, 정지되면 ResumeDelay 후 주기를 처음부터 다시 시작한다.
    /// </summary>
    public class AttributeRegenerator : MonoBehaviour
    {
        #region Nested Types
        /// <summary>어트리뷰트 하나에 대한 주기 회복 규칙.</summary>
        [Serializable]
        public struct SRegenRule
        {
            [Tooltip("주기마다 적용할 효과. 비어 있거나 대상/cursor 어트리뷰트가 없으면 규칙에서 제외된다.")]
            public SOAttributeEffect Effect;
            [Tooltip("적용 주기(초). 0 이하면 매 프레임 실행한다.")]
            public float Duration;
            [Tooltip("정지 후 재개까지 대기 시간(초). 0 이하면 다음 프레임에 재개한다.")]
            public float ResumeDelay;
        }
        #endregion

        #region Serialized Fields
        [SerializeField] private SRegenRule[] _rules;
        #endregion

        #region Private Fields
        private readonly List<SRegenRule> _activeRules = new();
        private readonly List<Coroutine> _routines = new();
        private IEffectTarget _target;
        #endregion

        #region Public Methods
        /// <summary>
        /// 회복을 적용할 대상을 설정하고, 유효한 규칙의 주기를 시작한다.
        /// </summary>
        /// <param name="target">값을 읽고 쓸 대상</param>
        public void SetTarget(IEffectTarget target)
        {
            if (target == null) return;

            ClearTarget();
            _target = target;

            if (_rules == null) return;

            foreach (var rule in _rules)
            {
                if (!IsValidRule(rule))
                {
                    Debug.LogWarning($"[{name}] {nameof(AttributeRegenerator)} : rule [{(rule.Effect ? rule.Effect.name : "null")}] is invalid, skipped", this);
                    continue;
                }

                _activeRules.Add(rule);
                _routines.Add(StartCoroutine(CoRegen(rule)));
            }
        }

        /// <summary>
        /// 모든 주기를 멈추고 대상을 제거한다.
        /// </summary>
        public void ClearTarget()
        {
            StopAllCoroutines();
            _activeRules.Clear();
            _routines.Clear();
            _target = null;
        }

        /// <summary>
        /// 효과의 대상 어트리뷰트가 targetKey 인 규칙을 정지한다. ResumeDelay 후 주기를 처음부터 다시 시작한다.
        /// 정지 중에 다시 호출되면 대기 시간이 처음부터 다시 흐른다.
        /// </summary>
        /// <param name="targetKey">정지할 어트리뷰트 이름</param>
        public void Pause(string targetKey)
        {
            for (int i = 0; i < _activeRules.Count; i++)
            {
                if (!string.Equals(_activeRules[i].Effect.TargetAttribute, targetKey, StringComparison.OrdinalIgnoreCase)) continue;

                if (_routines[i] != null) StopCoroutine(_routines[i]);
                _routines[i] = StartCoroutine(CoResume(i));
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 효과가 있고, 대상 어트리뷰트와 (값 출처가 Attribute 면) cursor 어트리뷰트가 대상에 있는지 확인한다.
        /// </summary>
        private bool IsValidRule(SRegenRule rule)
        {
            if (!rule.Effect) return false;
            if (!_target.IsValidTarget(rule.Effect.TargetAttribute)) return false;
            if (rule.Effect.ValueSource == EValueSource.Attribute && !_target.IsValidTarget(rule.Effect.CursorAttribute)) return false;

            return true;
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// 주기마다 대상에 효과를 적용한다. 주기가 0 이하면 매 프레임 실행한다.
        /// </summary>
        private IEnumerator CoRegen(SRegenRule rule)
        {
            WaitForSeconds wait = (rule.Duration > 0.0f) ? new WaitForSeconds(rule.Duration) : null;

            while (true)
            {
                yield return wait;

                rule.Effect.Apply(_target, _target);
            }
        }

        /// <summary>
        /// ResumeDelay 동안 기다린 뒤 index 규칙의 주기를 처음부터 다시 시작한다.
        /// </summary>
        private IEnumerator CoResume(int index)
        {
            float delay = _activeRules[index].ResumeDelay;
            if (delay <= 0.0f) yield return null;
            else yield return new WaitForSeconds(delay);

            _routines[index] = StartCoroutine(CoRegen(_activeRules[index]));
        }
        #endregion
    }
}
