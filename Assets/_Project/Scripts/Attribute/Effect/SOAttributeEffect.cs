using System;
using UnityEngine;
using Attribute.Core;

namespace Attribute.Effect
{
    /// <summary>Effect 에 적용할 수치를 어디서 가져올지.</summary>
    [Serializable]
    public enum EValueSource
    {
        /// <summary>고정 float 값(Amount)을 사용</summary>
        Float,
        /// <summary>cursor(효과 발생 주체)의 어트리뷰트 값을 사용</summary>
        Attribute
    }

    /// <summary>
    /// 대상 어트리뷰트에 연산(EModifier)을 적용하는 효과 데이터 에셋.
    /// 수치는 고정값 또는 효과 발생 주체(cursor)의 어트리뷰트에서 가져온다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOAttributeEffect", menuName = "Attribute/AttributeEffect")]
    public class SOAttributeEffect : ScriptableObject
    {
        #region Serialized Fields
        [Header("Target")]
        [SerializeField] private EModifier _modifier;
        [SerializeField] private string _targetAttribute;

        [Header("Value")]
        [SerializeField] private EValueSource _valueSource;
        [Tooltip("ValueSource 가 Float 일 때 사용")]
        [SerializeField] private float _amount;
        [Tooltip("ValueSource 가 Attribute 일 때 cursor 에서 읽을 어트리뷰트 이름")]
        [SerializeField] private string _cursorAttribute;
        #endregion

        #region Properties
        /// <summary>적용할 연산 종류.</summary>
        public EModifier Modifier => _modifier;

        /// <summary>연산을 적용할 대상 어트리뷰트 이름.</summary>
        public string TargetAttribute => _targetAttribute;

        /// <summary>수치를 가져올 출처.</summary>
        public EValueSource ValueSource => _valueSource;

        /// <summary>ValueSource 가 Float 일 때 적용할 고정 수치.</summary>
        public float Amount => _amount;

        /// <summary>ValueSource 가 Attribute 일 때 cursor 에서 읽을 어트리뷰트 이름.</summary>
        public string CursorAttribute => _cursorAttribute;
        #endregion

        #region Public Methods
        /// <summary>
        /// Effect 적용을 위한 함수
        /// </summary>
        /// <param name="target">effect 를 적용할 객체의 IEffectTarget</param>
        /// <param name="cursor">effect 를 발생시키는 객체의 IEffectTarget (ValueSource 가 Attribute 일 때 필수)</param>
        public void Apply(IEffectTarget target, IEffectTarget cursor = null)
        {
            if (_valueSource == EValueSource.Attribute)
            {
                if (cursor == null) throw new InvalidOperationException("cursor : is null");
                if (!cursor.IsValidTarget(_cursorAttribute))
                {
                    throw new InvalidOperationException($"[{_cursorAttribute}] : is not set in cursor");
                }
            }

            if (!target.IsValidTarget(_targetAttribute))
            {
                throw new InvalidOperationException($"[{_targetAttribute}] : is not set in target");
            }

            float amount = (_valueSource == EValueSource.Float) ? _amount : (cursor?.GetValue(_cursorAttribute) ?? 0);
            float newValue = Modifiers.Modify(_modifier, target.GetValue(_targetAttribute), amount);
            target.SetValue(_targetAttribute, newValue);
        }
        #endregion
    }
}
