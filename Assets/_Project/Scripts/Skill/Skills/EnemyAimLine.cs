using UnityEngine;
using AI;
using Skill.Core;

namespace Skill.Skills
{
    /// <summary>
    /// 적의 사격 예고용 조준선 스킬. 발동하면 Stop 될 때까지 발사 지점 정면으로 레이저 선을 그린다.
    /// 발동 순간 보이는 대상의 몸통 중심으로 발사 지점을 돌려 그 방향을 고정한다(조준 중 대상을 따라가지 않음).
    /// 선은 장애물에 닿으면 거기서 끊긴다. 총알도 같은 발사 지점 정면으로 나가므로 선이 곧 탄도다.
    /// </summary>
    public class EnemyAimLine : SkillBase
    {
        #region Serialized Fields
        [Header("Aim Line")]
        [Tooltip("조준선의 최대 길이(m)")]
        [SerializeField, Min(0.0f)] private float _maxDistance = 30.0f;
        [Tooltip("조준선을 끊는 장애물 레이어")]
        [SerializeField] private LayerMask _obstacleLayers;

        [Header("References")]
        [Tooltip("조준선이 시작되는 위치. 발동 순간 대상 쪽으로 돌려 고정하고 그 정면으로 그린다. 원거리 공격 스킬과 같은 발사 지점을 연결한다.")]
        [SerializeField] private Transform _firePoint;
        [Tooltip("조준선을 그릴 LineRenderer")]
        [SerializeField] private LineRenderer _lineRenderer;
        #endregion

        #region Private Fields
        private bool _isAiming;
        private AIController _controller;
        private Vector3 _aimDirection;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _controller = GetComponentInParent<AIController>();

            Debug.Assert(_firePoint != null, $"[{name}] 발사 지점이 연결되지 않았습니다.");
            Debug.Assert(_lineRenderer != null, $"[{name}] LineRenderer 가 연결되지 않았습니다.");

            if (_lineRenderer)
            {
                _lineRenderer.positionCount = 2;
                _lineRenderer.enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (!_isAiming || !_firePoint || !_lineRenderer) return;

            // 몸통이 돌아도 월드 기준 조준 방향이 유지되도록 매 프레임 다시 맞춘다
            _firePoint.rotation = Quaternion.LookRotation(_aimDirection);

            Vector3 origin = _firePoint.position;
            Vector3 direction = _firePoint.forward;

            float distance = _maxDistance;
            bool isBlocked = Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                _maxDistance,
                _obstacleLayers,
                QueryTriggerInteraction.Ignore);
            if (isBlocked)
            {
                distance = hit.distance;
            }

            _lineRenderer.SetPosition(0, origin);
            _lineRenderer.SetPosition(1, origin + direction * distance);
        }

        private void OnDisable()
        {
            Stop();
        }
        #endregion

        #region Public Methods
        public override void Stop()
        {
            // 발사 지점 회전은 되돌리지 않는다. 같은 프레임에 이어지는 사격이 고정된 방향으로 나가야 한다
            _isAiming = false;
            if (_lineRenderer) _lineRenderer.enabled = false;
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (_firePoint)
            {
                _aimDirection = _firePoint.forward;

                Transform target = _controller ? _controller.VisibleTarget : null;
                if (target)
                {
                    // 발밑이 아니라 몸통 중심을 노린다
                    Vector3 aimPoint = target.TryGetComponent(out Collider targetCollider)
                        ? targetCollider.bounds.center
                        : target.position;
                    Vector3 toAimPoint = aimPoint - _firePoint.position;
                    if (toAimPoint.sqrMagnitude > Mathf.Epsilon) _aimDirection = toAimPoint.normalized;
                }
            }

            _isAiming = true;
            if (_lineRenderer) _lineRenderer.enabled = true;

            base.Execute();
        }
        #endregion
    }
}
