using UnityEngine;
using Attribute.Core;
using Core.Audio;
using Interaction;

namespace Map.Gimmicks
{
    /// <summary>
    /// 상호작용하면 플레이어의 HP와 배터리를 최대치로 채우는 충전소. 횟수 제한 없이 여러 번 쓸 수 있다.
    /// 입력은 플레이어의 Interact 스킬이 카메라 레이로 찾아 넘겨준다. 콜라이더가 있는 오브젝트를 Interact 레이어로 둔다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ChargeStation : MonoBehaviour, IInteractable
    {
        #region Constants
        private const string CURRENT_HP_NAME = "CurrentHp";
        private const string MAX_HP_NAME = "MaxHp";
        private const string CURRENT_BATTERY_NAME = "CurrentBattery";
        private const string MAX_BATTERY_NAME = "MaxBattery";
        #endregion

        #region Serialized Fields
        [Header("Sound")]
        [Tooltip("충전했을 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _interactSound;
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            // AttributeSet은 플레이어 루트가 아니라 자식(AttributePrefab)에 있다
            IEffectTarget target = interactor.GetComponentInChildren<IEffectTarget>();
            if (target == null) return;

            if (!IsChargeTarget(target)) return;

            target.SetValue(CURRENT_HP_NAME, target.GetValue(MAX_HP_NAME));
            target.SetValue(CURRENT_BATTERY_NAME, target.GetValue(MAX_BATTERY_NAME));
            GimmickUtility.PlayUi(_interactSound);
        }
        #endregion

        #region Private Methods
        private static bool IsChargeTarget(IEffectTarget target)
        {
            return target.IsValidTarget(CURRENT_HP_NAME) &&
                   target.IsValidTarget(MAX_HP_NAME) &&
                   target.IsValidTarget(CURRENT_BATTERY_NAME) &&
                   target.IsValidTarget(MAX_BATTERY_NAME);
        }
        #endregion
    }
}
