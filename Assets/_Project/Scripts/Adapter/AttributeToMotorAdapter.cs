using System;
using UnityEngine;
using UnityEngine.Serialization;
using Attribute.Core;
using Movement;

namespace Adapter
{
    /// <summary>
    /// AttributeSet의 속도/점프 어트리뷰트 값을 CharacterMotor.Speed/JumpSpeed에 연결하는 접착 컴포넌트.
    /// AttributeSet과 CharacterMotor는 서로를 알지 못하며, 이 어댑터가 둘 사이의 값 동기화만 담당한다.
    /// </summary>
    public class AttributeToMotorAdapter : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [FormerlySerializedAs("attributeSet")]
        [SerializeField] private AttributeSet _attributeSet;
        [FormerlySerializedAs("characterMotor")]
        [SerializeField] private CharacterMotor _characterMotor;

        [Header("Settings")]
        [Tooltip("CharacterMotor.Speed 로 연결할 어트리뷰트 이름")]
        [FormerlySerializedAs("speedValueKey")]
        [SerializeField] private string _speedValueKey;
        [Tooltip("CharacterMotor.JumpSpeed 로 연결할 어트리뷰트 이름")]
        [SerializeField] private string _jumpSpeedValueKey;
        #endregion

        #region Properties
        /// <summary>CharacterMotor.Speed 로 연결되는 어트리뷰트 이름.</summary>
        public string SpeedValueKey => _speedValueKey;

        /// <summary>CharacterMotor.JumpSpeed 로 연결되는 어트리뷰트 이름.</summary>
        public string JumpSpeedValueKey => _jumpSpeedValueKey;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_attributeSet) _attributeSet = GetComponent<AttributeSet>();

            if (!_attributeSet)
            {
                Debug.LogError($"[{name}] AttributeSet is not found", this);
            }
        }

        private void OnEnable()
        {
            if (!_attributeSet || !_characterMotor) return;

            // 활성화 시점의 값을 먼저 반영하고, 이후 변경은 콜백으로 동기화
            _characterMotor.Speed = _attributeSet.GetValue(_speedValueKey);
            _characterMotor.JumpSpeed = _attributeSet.GetValue(_jumpSpeedValueKey);
            _attributeSet.AddOnAttributeChangedCallback(OnAttributeChanged);
        }

        private void OnDisable()
        {
            if (!_attributeSet) return;
            _attributeSet.RemoveOnAttributeChangedCallback(OnAttributeChanged);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 어트리뷰트 변경 콜백. 속도/점프 키에 해당하는 변경만 Motor 에 반영한다.
        /// </summary>
        /// <param name="attributeKey">변경된 어트리뷰트 이름</param>
        /// <param name="newValue">변경 후 값</param>
        /// <param name="oldValue">변경 전 값</param>
        private void OnAttributeChanged(string attributeKey, float newValue, float oldValue)
        {
            if (string.Equals(attributeKey, _speedValueKey, StringComparison.OrdinalIgnoreCase))
            {
                _characterMotor.Speed = newValue;
            }
            else if (string.Equals(attributeKey, _jumpSpeedValueKey, StringComparison.OrdinalIgnoreCase))
            {
                _characterMotor.JumpSpeed = newValue;
            }
        }
        #endregion
    }
}
