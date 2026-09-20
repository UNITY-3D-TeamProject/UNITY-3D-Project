using System;
using Attribute.Core;
using Movement;
using UnityEngine;

namespace Scripts.Adapter
{
    public class AttributeToMotorAdapter : CharacterMotor
    {
        [SerializeField] private AttributeSet attributeSet;
        [SerializeField] private string valueKey;

        public string ValueKey => valueKey;

        private void OnEnable()
        {
            if (!attributeSet) return;
            Speed = attributeSet.GetValue(valueKey);
        }
    }
}
