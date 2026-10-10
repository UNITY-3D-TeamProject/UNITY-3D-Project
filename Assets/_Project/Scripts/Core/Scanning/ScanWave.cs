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
        // 한 번에 담을 수 있는 콜라이더 최대 수
        private const int MAX_HIT_COUNT = 256;
        #endregion

        #region Serialized Fields
        // Iscannable 붙은것들을 GetComponentInParent로 걸러야하는데 결과 배열이 256칸으로 고정이라
        // 범위 안에 대상이 아닌 콜라이더가 많으면 칸이 차서 진짜 대상이 누락될 수 있다.
        // 레이어를 만들면 대상이 아닌 콜라이더들은 배열에 안들어온다.
        [Header("Settings")]
        [Tooltip("스캔에 닿을 수 있는 대상이 속한 레이어. 필요한 레이어만 고르면 판정 비용이 줄어든다.")]
        [SerializeField] private LayerMask _scannableLayers = ~0;

        [Header("References")]
        [Tooltip("파동으로 쓰는 구 모양 파티클. Begin 때 지속 시간과 크기가 주입된다.")]
        [SerializeField] private ParticleSystem _scanParticle;
        #endregion

        #region Private Fields
        // 스캔 번호 발급기. static 이라 ScanWave 가 여러 개여도 번호가 겹치지 않는다.
        private static int _nextScanId;

        // 범위 판정 결과를 담는 배열. 미리 만들어 재사용해 매 프레임 메모리 할당이 없다.
        private readonly Collider[] _hitColliders = new Collider[MAX_HIT_COUNT];
        // 콜라이더에서 찾은 IScannable 을 저장해 두는 캐시. 못 찾은 콜라이더는 null 로 저장한다.
        private readonly Dictionary<Collider, IScannable> _scannableCache = new Dictionary<Collider, IScannable>();
        // 파동 파티클 1개의 상태를 읽어 오는 배열(구가 하나뿐이라 크기 1).
        private readonly ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[1];
        // 아래 세 값은 Begin 때 정해져 스캔이 끝날 때까지 바뀌지 않는다.
        private Vector3 _origin;
        private float _duration;
        private int _scanId;
        // 콜라이더 수 초과 경고를 스캔당 한 번만 내기 위한 플래그
        private bool _hasWarnedOverflow;
        #endregion

        #region Properties
        /// <summary>파동이 확산 중이면 true.</summary>
        public bool IsPlaying => _scanParticle && _scanParticle.isPlaying;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 파티클이 없으면 파동 자체가 불가능하므로 개발 중에 바로 알 수 있게 한다.
            Debug.Assert(_scanParticle != null, $"[{name}] 파동 파티클이 연결되지 않았습니다.");
            if (!_scanParticle) return;

            // 스킬 아래에 두어도 플레이어를 따라가지 않고, 시작하자마자 재생되지 않게 한다.
            ParticleSystem.MainModule main = _scanParticle.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            // 에디터에서 켜진 채 저장됐더라도 첫 프레임에 남은 파티클이 없도록 비운다.
            _scanParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            // 재생 중일 때만 판정한다. 평소에는 이 줄에서 바로 빠져나가 비용이 거의 없다.
            if (!IsPlaying) return;

            // 구 파티클 1개의 현재 상태를 읽는다. 아직 방출 전이면 비어 있다.
            int particleCount = _scanParticle.GetParticles(_particles);
            if (particleCount == 0) return;

            // 파티클 지름이 곧 파동의 지름이다.
            float radius = _particles[0].GetCurrentSize(_scanParticle) * 0.5f;

            // 이번 프레임에 닿는 모든 대상에게 같은 정보를 전달한다.
            SScanHit hit = new SScanHit(_scanId, _origin, radius, _duration);

            // 일반 Physics.OverlapSphere는 호출할 때마다 결과 배열을 새로 만들어서 GC 부담이 생긴다.
            // 파동 중심에서 현재 반경 안에 있는 콜라이더를 미리 만든 배열에 담는다(GC 할당 없음).
            // 트리거 콜라이더도 스캔 대상이 될 수 있도록 Collide 로 둔다.
            int hitCount = Physics.OverlapSphereNonAlloc(
                _origin,
                radius,
                _hitColliders,
                _scannableLayers,
                QueryTriggerInteraction.Collide);

            // 배열이 가득 찼다면 범위 안에 더 있어도 놓쳤을 수 있다. 스캔당 한 번만 경고한다.
            if ((hitCount == MAX_HIT_COUNT) && !_hasWarnedOverflow)
            {
                _hasWarnedOverflow = true;
                Debug.LogWarning($"[{name}] 스캔 범위 안의 콜라이더가 {MAX_HIT_COUNT}개를 넘어 일부가 누락될 수 있습니다. Scannable Layers 를 좁혀 주세요.", this);
            }

            // 스캔 가능한 대상만 골라 닿았음을 알린다. 매 프레임 호출되므로 받는 쪽은 중복 호출에 안전해야 한다.
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

            // 중심과 지속 시간은 시작 시점 값으로 고정한다. 이후 플레이어가 움직여도 파동은 제자리에 남는다.
            _origin = origin;
            _duration = duration;

            // 새 번호를 부여해 이전 스캔과 구분한다. 대상은 이 번호가 바뀌면 새 스캔으로 인식한다.
            _scanId = ++_nextScanId;
            _hasWarnedOverflow = false;

            // 이전 스캔에서 찾아 둔 대상 캐시는 버린다(대상이 바뀌었을 수 있다).
            _scannableCache.Clear();

            // 진행 중이던 파동이 있으면 지우고 처음부터 다시 시작한다.
            // true => 자식 파티클까지 포함 
            _scanParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 파티클 수명이 곧 확산 시간, 파티클 크기가 곧 최대 지름이 된다.
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
        /// 전에 한번 조사한 콜라이더면 다시 조사하지말고 메모해둔 답을 쓰기
        private bool TryGetScannable(Collider collider, out IScannable scannable)
        {
            // 이미 찾아 둔 콜라이더면 캐시에서 바로 꺼내 쓴다.
            if (_scannableCache.TryGetValue(collider, out scannable))
            {
                // 스캔 대상이 아니라고 이미 판정된 콜라이더다.
                if (scannable == null) return false;

                // 인터페이스 변수는 유니티의 파괴 여부를 모르므로, 유니티 오브젝트면 살아 있는지 따로 확인한다.
                if (!(scannable is Object unityObject) || unityObject)
                {
                    return true;
                }

                // 파괴된 대상은 캐시에서 지우고 다시 찾는다.
                _scannableCache.Remove(collider);
            }

            // 콜라이더가 자식에 있어도 부모의 IScannable 을 찾을 수 있다.
            // 못 찾은 결과(null)도 캐시해 두어 매 프레임 같은 탐색을 반복하지 않는다.
            scannable = collider.GetComponentInParent<IScannable>();
            _scannableCache[collider] = scannable;
            return scannable != null;
        }
        #endregion
    }
}
