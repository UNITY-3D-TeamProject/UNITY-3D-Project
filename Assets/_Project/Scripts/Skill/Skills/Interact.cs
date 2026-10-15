using System;
using Interaction;
using Skill.Conditions;
using Skill.Core;
using UnityEngine;

namespace Skill.Skills
{
    /// <summary>
    /// 카메라 레이에 처음 맞은 IInteractable 을 상호작용 대상으로 삼고, 발동 시 그 대상의 Interact 를 호출한다.
    /// 활성화된 동안 매 프레임 대상을 검사하며, 대상 유무가 바뀌면 알린다.
    /// 대상이 없을 때 발동하지 않도록 같은 GameObject 의 HasTargetCondition 에 대상 유무를 전달한다.
    /// </summary>
    public class Interact : SkillBase
    {
        #region Serialized Fields
        [Header("Interact")]
        [Tooltip("상호작용 가능 거리. 카메라 위치부터 잰다.")]
        [SerializeField, Min(0.0f)] private float _interactDistance = 6.0f;
        [Tooltip("상호작용 대상 레이어")]
        [SerializeField] private LayerMask _interactLayers = 1 << 9;
        [Tooltip("대상을 가리는 레이어. 레이에 이 레이어가 먼저 맞으면 대상으로 보지 않는다.")]
        [SerializeField] private LayerMask _blockingLayers = (1 << 0) | (1 << 7) | (1 << 10);
        #endregion

        #region Private Fields
        private HasTargetCondition _hasTargetCondition;
        private Func<Ray> _getAimRay;
        private Action<bool> _notifyInteractableChanged;
        // 현재 상호작용 대상 (IInteractable 이 붙은 GameObject). 없으면 null
        private GameObject _target;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _hasTargetCondition = GetComponent<HasTargetCondition>();
        }

        private void Update()
        {
            SetTarget(FindTarget());
        }

        private void OnDisable()
        {
            // 꺼져 있는 동안은 검사하지 않으므로 대상 없음으로 알린다
            SetTarget(null);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 카메라 조준 레이를 얻기 위한 델리게이트 주입
        /// </summary>
        public void SetGetAimRay(Func<Ray> getAimRay)
        {
            if (getAimRay == null) return;
            _getAimRay = getAimRay;
        }

        /// <summary>
        /// 카메라 조준 레이 델리게이트 제거
        /// </summary>
        public void ClearGetAimRay()
        {
            _getAimRay = null;
        }

        /// <summary>
        /// 상호작용 가능 여부 변경을 알리기 위한 델리게이트 주입
        /// </summary>
        public void SetNotifyInteractableChanged(Action<bool> notifyInteractableChanged)
        {
            if (notifyInteractableChanged == null) return;
            _notifyInteractableChanged = notifyInteractableChanged;
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (!_target) return;

            GameObject interactor = transform.root.gameObject;
            Debug.Log($"[Interact] Interact : {_target.name} (interactor : {interactor.name})"); // TODO: 테스트용 로그, 검증 후 제거
            // 대상에 IInteractable 이 여러 개여도 처음 찾은 하나만 실행한다
            if (_target.TryGetComponent(out IInteractable interactable)) interactable.Interact(interactor);
            base.Execute();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 카메라 레이에 처음 맞은 것이 대상 레이어의 IInteractable 이면 그 GameObject 를 반환한다.
        /// </summary>
        /// <returns>상호작용 대상. 없으면 null</returns>
        private GameObject FindTarget()
        {
            if (_getAimRay == null) return null;

            Ray aimRay = _getAimRay();

            // 대상은 트리거 콜라이더도 맞히고, 가림은 트리거(체크포인트 등 판정 영역)를 무시한다
            bool hasHit = Physics.Raycast(
                aimRay,
                out RaycastHit hit,
                _interactDistance,
                _interactLayers,
                QueryTriggerInteraction.Collide);
            if (!hasHit) return null;

            // 대상보다 가림 레이어가 먼저 맞으면 대상이 가려진 것이다
            bool isBlocked = Physics.Raycast(
                aimRay,
                hit.distance,
                _blockingLayers,
                QueryTriggerInteraction.Ignore);
            if (isBlocked) return null;

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            return (interactable is Component component) ? component.gameObject : null;
        }

        /// <summary>
        /// 대상을 갱신한다. 대상 유무가 바뀌었으면 조건에 전달하고 알린다.
        /// </summary>
        /// <param name="target">새 대상. 없으면 null</param>
        private void SetTarget(GameObject target)
        {
            bool hadTarget = _target;
            _target = target;

            bool hasTarget = _target;
            if (hadTarget == hasTarget) return;

            Debug.Log($"[Interact] OnInteractableChanged : {hasTarget} ({(_target ? _target.name : "none")})"); // TODO: 테스트용 로그, 검증 후 제거
            if (_hasTargetCondition) _hasTargetCondition.SetHasTarget(hasTarget);
            _notifyInteractableChanged?.Invoke(hasTarget);
        }
        #endregion
    }
}
