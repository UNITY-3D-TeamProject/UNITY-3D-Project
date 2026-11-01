using UnityEngine;

namespace Projectile
{
    /// <summary>풀에서 다시 꺼낸 발사체에 이전 비행 궤적이 남지 않도록 지운다.</summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class ProjectileTrailReset : MonoBehaviour
    {
        private TrailRenderer _trail;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
        }

        private void OnEnable()
        {
            _trail.Clear();
        }
    }
}
