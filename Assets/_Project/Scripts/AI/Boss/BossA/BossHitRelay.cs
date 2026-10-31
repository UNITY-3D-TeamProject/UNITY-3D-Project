using UnityEngine;
using AI.Boss.Logic;
using Attribute.Core;
using Mediator;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A 의 손·몸·머리 콜라이더 오브젝트에 붙어 맞은 효과를 A 본체의 어트리뷰트로 중계한다.
    /// 허용되지 않은 부위의 값 변경은 버린다. 그로기 중에는 HP 하한 아래로 내려가지 않게 값을 자른다.
    /// </summary>
    public class BossHitRelay : MonoBehaviour, IEffectTarget
    {
        #region Constants
        private const string CURRENT_HP_KEY = "CurrentHp";
        private const string MAX_HP_KEY = "MaxHp";
        #endregion

        #region Serialized Fields
        [SerializeField] private CharacterMediator _owner;
        [SerializeField] private EBossHitPart _part;
        #endregion

        #region Private Fields
        private bool _isAllowed;
        private float _hpFloorRatio;
        #endregion

        #region Properties
        public EBossHitPart Part => _part;
        #endregion

        #region Public Methods
        /// <summary>이 부위의 피격 허용 여부를 정한다. 처음에는 허용되지 않는다.</summary>
        public void SetAllowed(bool isAllowed)
        {
            _isAllowed = isAllowed;
        }

        /// <summary>
        /// 최대 HP 에 대한 비율 하한을 정한다. 0 이면 하한 없음.
        /// 하한이 있으면 방향을 빼서 넘긴다. 방향이 없으면 약점(크리티컬) 보정이 붙지 않아 자른 값이 그대로 들어간다.
        /// 맞은 지점은 남겨 둔다. 빼면 피격 이펙트가 공격자 위치에서 나온다.
        /// </summary>
        public void SetHpFloor(float ratio)
        {
            _hpFloorRatio = Mathf.Max(0.0f, ratio);
        }

        /// <inheritdoc />
        public bool IsValidTarget(string targetName)
        {
            IEffectTarget target = GetTarget();
            return target != null && target.IsValidTarget(targetName);
        }

        /// <inheritdoc />
        public float GetValue(string targetName)
        {
            IEffectTarget target = GetTarget();
            return target != null ? target.GetValue(targetName) : 0.0f;
        }

        /// <inheritdoc />
        public void SetValue(string targetName, float value, SEffectContext context = default)
        {
            if (!_isAllowed) return;

            IEffectTarget target = GetTarget();
            if (target == null) return;

            bool isFloorTarget = (_hpFloorRatio > 0.0f) && (targetName == CURRENT_HP_KEY);
            if (isFloorTarget)
            {
                value = Mathf.Max(value, target.GetValue(MAX_HP_KEY) * _hpFloorRatio);
                context = new SEffectContext(context.Cursor, context.Effect, context.HitPoint);
            }

            target.SetValue(targetName, value, context);
        }
        #endregion

        #region Private Methods
        private IEffectTarget GetTarget()
        {
            return _owner ? _owner.EffectTarget : null;
        }
        #endregion
    }
}
