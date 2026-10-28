using UnityEngine;
using UnityEngine.Events;
using Core.Audio;
using Interaction;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 가까이에서 상호작용(E)하면 연결된 동작을 실행하는 버튼. DamageButton 의 "가까이서 누르는" 버전이다.
    /// 예: On Interacted 에 GimmickSwitch.Toggle 을 연결
    /// 입력은 플레이어의 Interact 스킬이 카메라 레이로 찾아 넘겨준다. 콜라이더가 있는 오브젝트를 Interact 레이어로 둔다.
    /// </summary>
    public class InteractButton : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Event")]
        [Tooltip("상호작용했을 때 실행할 동작")]
        [SerializeField] private UnityEvent _onInteracted = new UnityEvent();

        [Tooltip("켜면 처음 한 번만 실행한다.")]
        [SerializeField] private bool _isOneShot;

        [Header("Sound")]
        [Tooltip("상호작용했을 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _interactSound;
        #endregion

        #region Private Fields
        private bool _hasFired;
        #endregion

        #region Properties
        /// <summary>상호작용했을 때 실행되는 이벤트.</summary>
        public UnityEvent OnInteracted => _onInteracted;
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            if (_isOneShot && _hasFired) return;

            _hasFired = true;
            GimmickUtility.PlayUi(_interactSound);
            _onInteracted.Invoke();
        }
        #endregion
    }
}
