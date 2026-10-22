using System;
using UnityEngine;

namespace Core.Stage
{
    // 같은 오브젝트에 이 스크립트를 두 개 이상 붙이지 못하게 한다.
    [DisallowMultipleComponent]
    // BoxCollider가 없으면 자동으로 추가한다.
    [RequireComponent(typeof(BoxCollider))]
    public class CheckpointTrigger : MonoBehaviour
    {
        private const float ARROW_LENGTH = 1.5f;
        private const float ARROW_HEAD_LENGTH = 0.4f;
        private const float ARROW_HEAD_ANGLE = 30f;

        [Header("Detection")]
        [Tooltip("체크포인트가 감지할 플레이어 Collider의 레이어")]
        [SerializeField] private LayerMask _playerLayerMask;

        [Header("Respawn")]
        [Tooltip("최초 생성 또는 낙하·사망 복구에 사용할 위치와 회전")]
        [SerializeField] private Transform _respawnPoint;

        public Transform RespawnPoint => _respawnPoint;

        // 도달한 체크포인트와 감지된 플레이어 Collider를 전달한다.
        public event Action<CheckpointTrigger, Collider> OnPlayerEntered;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;

            if (_respawnPoint == null)
            {
                Debug.LogError("체크포인트의 RespawnPoint가 없습니다.", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (!IsPlayerLayer(other.gameObject.layer))
            {
                return;
            }

            OnPlayerEntered?.Invoke(this, other);
        }

        // 씬 뷰에 RespawnPoint의 정면(파란 Z축)을 화살표로 그린다. 플레이어는 이 방향을 보고 생성된다.
        private void OnDrawGizmos()
        {
            if (_respawnPoint == null)
            {
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(_respawnPoint.forward, Vector3.up).normalized;
            if (forward == Vector3.zero)
            {
                return;
            }

            Vector3 start = _respawnPoint.position;
            Vector3 end = start + (forward * ARROW_LENGTH);
            Vector3 left = Quaternion.AngleAxis(180f - ARROW_HEAD_ANGLE, Vector3.up) * forward;
            Vector3 right = Quaternion.AngleAxis(180f + ARROW_HEAD_ANGLE, Vector3.up) * forward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(start, 0.15f);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawLine(end, end + (left * ARROW_HEAD_LENGTH));
            Gizmos.DrawLine(end, end + (right * ARROW_HEAD_LENGTH));
        }

        private bool IsPlayerLayer(int layer)
        {
            return (_playerLayerMask.value & (1 << layer)) != 0;
        }
    }
}