using System;
using Attribute.Core;
using Movement;
using UnityEngine;

namespace Scripts.Adapter
{
    public class AttributeToMotorAdapter : MonoBehaviour
    {
        [SerializeField] private AttributeSet attributeSet;
        [SerializeField] private CharacterMotor characterMotor;
        [SerializeField] private string speedValueKey;

        public string SpeedValueKey => speedValueKey;

        private void Awake()
        {
            if(!attributeSet) attributeSet = GetComponentInParent<AttributeSet>();
            
            if (!attributeSet)
            {
                Debug.LogError($"[{name}] AttributeSet is not found", this);
            }
        }

        private void OnEnable()
        {
            if (!attributeSet || !characterMotor) return;
            characterMotor.Speed = attributeSet.GetValue(speedValueKey);
            attributeSet.AddOnAttributeChangedCallback(OnSpeedChanged);
        }

        private void OnDisable()
        {
            if (!attributeSet) return;
            attributeSet.RemoveOnAttributeChangedCallback(OnSpeedChanged);
        }
        
        private void OnSpeedChanged(string attributeKey, float newValue, float oldValue)
        {
            if (string.Equals(attributeKey, speedValueKey, StringComparison.OrdinalIgnoreCase))
            {
                characterMotor.Speed = newValue; 
            }
        }
    }
}
