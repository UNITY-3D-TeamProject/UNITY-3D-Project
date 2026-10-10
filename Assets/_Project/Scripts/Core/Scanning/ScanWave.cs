using System.Collections.Generic;
using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 파동. 구 모양 파티클이 확산하는 동안, 파티클 크기에서 계산한 반경 안의 콜라이더에서
    /// IScannable 을 찾아 닿았음을 알린다. 스킬 아래에 상주하다가 Begin 으로 재생되고, 스킬과 대상은 서로를 모른다.
    /// 파티클은 월드 공간으로 재생되고 중심은 Begin 시점의 위치로 고정되므로 플레이어가 움직여도 파동은 제자리에 남는다.
    /// </summary>
    public class ScanWave : MonoBehaviour
    {
        #region Constants
        private const int MAX_HIT_COUNT = 256;
        #endregion

        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("스캔에 닿을 수 있는 대상이 속한 레이어. 필요한 레이어만 고르면 판정 비용이 줄어든다.")]
        [SerializeField] private LayerMask _scannableLayers = ~0;

        [Header("References")]
        [Tooltip("파동으로 쓰는 구 모양 파티클. Begin 때 지속 시간과 크기가 주입된다.")]
        [SerializeField] private ParticleSystem _scanParticle;
        #endregion

        #region Private Fields
        private static int _nextScanId;

        private readonly Collider[] _hitColliders = new Collider[MAX_HIT_COUNT];
        private readonly Dictionary<Collider, IScannable> _scannableCache = new Dictionary<Collider, IScannable>();
        private readonly ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[1];
        private Vector3 _origin;
        private float _duration;
        private int _scanId;
        private bool _hasWarnedOverflow;
        #endregion

        #region Properties
        /// <summary>파동이 확산 중이면 true.</summary>
        public bool IsPlaying => _scanParticle && _scanParticle.isPlaying;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_scanParticle != null, $"[{name}] 파동 파티클이 연결되지 않았습니다.");
            if (!_scanParticle) return;

            // 스킬 아래에 두어도 플레이어를 따라가지 않고, 시작하자마자 재생되지 않게 한다.
            ParticleSystem.MainModule main = _scanParticle.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            _scanParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (!IsPlaying) return;

            int particleCount = _scanParticle.GetParticles(_particles);
            if (particleCount == 0) return;

            // 파티클 지름이 곧 파동의 지름이다.
            float radius = _particles[0].GetCurrentSize(_scanParticle) * 0.5f;
            SScanHit hit = new SScanHit(_scanId, _origin, radius, _duration);

            int hitCount = Physics.OverlapSphereNonAlloc(
                _origin,
                radius,
                _hitColliders,
                _scannableLayers,
                QueryTriggerInteraction.Collide);

            if ((hitCount == MAX_HIT_COUNT) && !_hasWarnedOverflow)
            {
                _hasWarnedOverflow = true;
                Debug.LogWarning($"[{name}] 스캔 범위 안의 콜라이더가 {MAX_HIT_COUNT}개를 넘어 일부가 누락될 수 있습니다. Scannable Layers 를 좁혀 주세요.", this);
            }

            for (int i = 0; i < hitCount; i++)
            {
                if (TryGetScannable(_hitColliders[i], out IScannable scannable))
                {
                    scannable.OnScanned(hit);
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 파동을 시작한다. 진행 중이던 파동은 버리고 새 스캔으로 다시 시작한다.
        /// </summary>
        /// <param name="origin">파동의 중심(월드 좌표)</param>
        /// <param name="duration">파동이 확산하는 시간이자, 닿은 대상이 활성화되는 시간(초)</param>
        /// <param name="size">파동의 최대 지름(m)</param>
        public void Begin(Vector3 origin, float duration, float size)
        {
            if (!_scanParticle) return;

            _origin = origin;
            _duration = duration;
            _scanId = ++_nextScanId;
            _hasWarnedOverflow = false;
            _scannableCache.Clear();

            _scanParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = _scanParticle.main;
            main.startLifetime = duration;
            main.startSize = size;

            // 월드 공간 파티클은 재생 시점의 방출 위치에 남으므로, 재생 전에 중심으로 옮긴다.
            _scanParticle.transform.position = origin;
            _scanParticle.Play();
        }

        /// <summary>
        /// 파동을 즉시 멈춘다. 이미 닿은 대상은 각자의 타이머대로 진행한다.
        /// </summary>
        public void Stop()
        {
            if (!_scanParticle) return;

            _scanParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 콜라이더에 연결된 IScannable 을 찾는다. 같은 스캔 안에서는 결과를 캐시해 매 프레임 탐색하지 않는다.
        /// </summary>
        /// <param name="collider">닿은 콜라이더</param>
        /// <param name="scannable">찾은 대상</param>
        /// <returns>스캔 가능한 대상이면 true</returns>
        private bool TryGetScannable(Collider collider, out IScannable scannable)
        {
            if (_scannableCache.TryGetValue(collider, out scannable))
            {
                // 파괴된 대상은 캐시에서 지우고 다시 찾는다.
                if (scannable == null) return false;
                if (!(scannable is Object unityObject) || unityObject)
                {
                    return true;
                }

                _scannableCache.Remove(collider);
            }

            scannable = collider.GetComponentInParent<IScannable>();
            _scannableCache[collider] = scannable;
            return scannable != null;
        }
        #endregion
    }
}
