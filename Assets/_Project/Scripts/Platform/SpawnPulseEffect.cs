using UnityEngine;

namespace Platform
{
    /// <summary>
    /// 발판이 생성될 때마다 그 생성 위치에서 빛 이펙트를 1회 내보낸다. (팽창하며 페이드아웃, 위치 고정)
    /// 스포너의 자식으로 두면 부모에서 스포너를 자동으로 찾는다.
    /// </summary>
    public class SpawnPulseEffect : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("비워두면 부모에서 PlatformSpawner를 자동으로 찾는다.")]
        [SerializeField] private PlatformSpawner _spawner;
        [SerializeField] private ParticleSystem _pulseEffect;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_spawner == null)
            {
                _spawner = GetComponentInParent<PlatformSpawner>();
            }

            Debug.Assert(_spawner != null, $"[{name}] PlatformSpawner를 찾지 못했습니다. 스포너의 자식으로 두거나 직접 연결하세요.");
            Debug.Assert(_pulseEffect != null, $"[{name}] Pulse Effect 파티클이 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            if (_spawner != null)
            {
                _spawner.OnPlatformSpawned += HandlePlatformSpawned;
            }
        }

        private void OnDisable()
        {
            if (_spawner != null)
            {
                _spawner.OnPlatformSpawned -= HandlePlatformSpawned;
            }
        }
        #endregion

        #region Private Methods
        private void HandlePlatformSpawned(Vector3 worldPosition)
        {
            if (_pulseEffect == null) return;

            _pulseEffect.transform.position = worldPosition;
            _pulseEffect.Emit(1);
        }
        #endregion
    }
}
