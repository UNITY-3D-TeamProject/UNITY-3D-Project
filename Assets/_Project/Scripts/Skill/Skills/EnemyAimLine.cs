using UnityEngine;
using Skill.Core;

namespace Skill.Skills
{
    /// <summary>
    /// 적의 사격 예고용 조준선 스킬. 발동하면 Stop 될 때까지 발사 지점 정면으로 레이저 선을 그린다.
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
        [Tooltip("조준선이 시작되는 위치. 이 Transform 의 정면으로 그린다. 원거리 공격 스킬과 같은 발사 지점을 연결한다.")]
        [SerializeField] private Transform _firePoint;
        [Tooltip("조준선을 그릴 LineRenderer")]
        [SerializeField] private LineRenderer _lineRenderer;
        #endregion

        #region Private Fields
        private bool _isAiming;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

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
            _isAiming = false;
            if (_lineRenderer) _lineRenderer.enabled = false;
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            _isAiming = true;
            if (_lineRenderer) _lineRenderer.enabled = true;

            base.Execute();
        }
        #endregion
    }
}
