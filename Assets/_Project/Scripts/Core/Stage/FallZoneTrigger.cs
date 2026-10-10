using System;
using UnityEngine;

namespace Core.Stage
{
    // 같은 오브젝트에 이 스크립트를 두 개 이상 붙이지 못하게 한다.
    [DisallowMultipleComponent]
    // BoxCollider가 없으면 자동으로 추가한다.
    [RequireComponent(typeof(BoxCollider))]
    public class FallZoneTrigger : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("낙하 구역이 감지할 플레이어 Collider의 레이어")]
        [SerializeField] private LayerMask _playerLayerMask;

        // 진입한 낙하 구역과 감지된 플레이어 Collider를 전달한다.
        // [구독] StageManager.BindFallZones에서 구독하며, OnTriggerEnter에서 발생시킨다.
        public event Action<FallZoneTrigger, Collider> OnPlayerEntered;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
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

        private bool IsPlayerLayer(int layer)
        {
            return (_playerLayerMask.value & (1 << layer)) != 0;
        }
    }
}
