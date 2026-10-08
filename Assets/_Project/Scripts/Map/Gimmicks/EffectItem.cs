using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Interaction;
using PlayerInput;

namespace Map.Gimmicks
{
    public enum EItemPickupType
    {
        Touch,      // 플레이어가 닿으면 획득 (Trigger 콜라이더 필요)
        Interact,   // 플레이어가 상호작용하면 획득
    }

    /// <summary>
    /// 획득하면 Effect 목록을 플레이어에게 적용하고 사라지는 아이템.
    /// 예: 배터리 충전(Add CurrentBattery), 체력 회복(Add CurrentHp)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EffectItem : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Pickup")]
        [Tooltip("Touch: 닿으면 획득 (콜라이더의 Is Trigger를 켜야 한다) / Interact: 상호작용하면 획득")]
        [SerializeField] private EItemPickupType _pickupType = EItemPickupType.Touch;

        [Header("Effects")]
        [Tooltip("획득 시 위에서부터 순서대로 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect[] _effects;
        #endregion

        #region Unity Lifecycle
        private void OnTriggerEnter(Collider other)
        {
            if (_pickupType != EItemPickupType.Touch) return;
            if (!GimmickUtility.TryGetPlayer(other, out PlayerInputComponent player)) return;

            PickUp(player.GetComponentInChildren<IEffectTarget>());
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            if (_pickupType != EItemPickupType.Interact) return;

            // AttributeSet은 플레이어 루트가 아니라 자식(AttributePrefab)에 있다
            PickUp(interactor.GetComponentInChildren<IEffectTarget>());
        }
        #endregion

        #region Private Methods
        private void PickUp(IEffectTarget target)
        {
            if (target == null) return;

            GimmickUtility.ApplyEffects(_effects, target);
            Destroy(gameObject);
        }
        #endregion
    }
}
