using UnityEngine;
using UnityEngine.Serialization;
using Attribute.Core;
using Mediator;
using Skill.Costs;

namespace Bridge.SkillAttribute
{
    /// <summary>
    /// IEffectTarget(AttributeSet) 의 어트리뷰트를 자원으로 사용하는 ResourceCost.
    /// 대상 IEffectTarget 은 외부(SkillMediator)에서 주입받는다.
    /// </summary>
    public class AttributeResourceCost : ResourceCost, IEffectTargetReceiver
    {
        #region Serialized Fields
        [FormerlySerializedAs("_batteryKey")]
        [Header("Attribute")]
        [Tooltip("자원으로 사용할 어트리뷰트 이름")]
        [SerializeField] private string _resourceKey = "CurrentBattery";
        #endregion

        #region Private Fields
        private IEffectTarget _target;
        #endregion

        #region IEffectTargetReceiver
        /// <summary>
        /// 자원을 읽고 쓸 대상을 설정한다. 대상에 자원 어트리뷰트가 없으면 경고를 남긴다.
        /// </summary>
        /// <param name="target">자원 어트리뷰트를 가진 IEffectTarget</param>
        public void SetEffectTarget(IEffectTarget target)
        {
            _target = target;

            if (_target != null && !_target.IsValidTarget(_resourceKey))
            {
                Debug.LogWarning($"[{name}] 대상에 '{_resourceKey}' 어트리뷰트가 없습니다.", this);
            }
        }

        /// <summary>
        /// 설정된 대상을 해제한다. 해제 후에는 자원을 0 으로 취급한다.
        /// </summary>
        public void ClearEffectTarget()
        {
            _target = null;
        }
        #endregion

        #region Protected Methods
        protected override float GetResource()
        {
            return _target?.GetValue(_resourceKey) ?? 0.0f;
        }

        protected override void SetResource(float value)
        {
            _target?.SetValue(_resourceKey, value);
        }
        #endregion
    }
}
