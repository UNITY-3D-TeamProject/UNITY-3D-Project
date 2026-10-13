using System.Collections.Generic;
using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 파동. 구 모양 파티클이 확산하는 동안, 등록된 Scan 대상(ScanTarget.Registered) 중
    /// 파티클 크기에서 계산한 파동 구에 닿은 대상에 알린다. 물리 탐색을 쓰지 않으므로 대상이 아닌 콜라이더는 보지 않는다.
    /// 스킬 아래에 상주하다가 Begin 으로 재생되고, 스킬과 대상은 서로를 모른다.
    /// 파티클은 월드 공간으로 재생되고 중심은 Begin 시점의 위치로 고정되므로 플레이어가 움직여도 파동은 제자리에 남는다.
    /// 동시에 하나의 파동만 쓰는 것이 전제다. 대상(ScanTarget)이 마지막 스캔 번호 하나만 기억하므로,
    /// 두 파동이 같은 대상에 번갈아 닿으면 매번 새 스캔으로 보고 유지 시간이 계속 갱신된다.
    /// </summary>
    public class ScanWave : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("파동으로 쓰는 구 모양 파티클. Begin 때 지속 시간과 크기가 주입된다.")]
        [SerializeField] private ParticleSystem _scanParticle;
        #endregion

        #region Private Fields
        // 스캔 번호 발급기. static 이라 ScanWave 가 여러 개여도 번호가 겹치지 않는다.
        private static int _nextScanId;

        // 파동 파티클 1개의 상태를 읽어 오는 배열(구가 하나뿐이라 크기 1).
        private readonly ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[1];
        // 아래 세 값은 Begin 때 정해져 스캔이 끝날 때까지 바뀌지 않는다.
        private Vector3 _origin;
        private float _duration;
        private int _scanId;
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

            // 등록된 Scan 대상만 돌며 파동 구에 닿은 대상에 알린다. 매 프레임 호출되므로 받는 쪽은 중복 호출에 안전해야 한다.
            // 대상마다 한 번씩 판정하므로 콜라이더가 여러 개여도 프레임당 한 번만 알린다.
            // 알림 중에 목록이 바뀌어도 안전하도록 뒤에서부터 돈다.
            IReadOnlyList<ScanTarget> targets = ScanTarget.Registered;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                ScanTarget target = targets[i];

                // 풀에 반납된 몬스터처럼 꺼져 있는 대상은 건너뛴다.
                if (!target.gameObject.activeInHierarchy) continue;

                if (target.IsTouchedBy(_origin, radius)) target.OnScanned(hit);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 파동을 시작한다. 진행 중이던 파동은 버리고 새 스캔으로 다시 시작한다.
        /// </summary>
        /// <param name="origin">파동의 중심(월드 좌표)</param>
        /// <param name="duration">파동이 확산하는 시간이자, 닿은 대상이 활성화되는 시간(초)</param>
        /// <param name="maxDiameter">파동의 최대 지름(m)</param>
        public void Begin(Vector3 origin, float duration, float maxDiameter)
        {
            if (!_scanParticle) return;

            // 중심과 지속 시간은 시작 시점 값으로 고정한다. 이후 플레이어가 움직여도 파동은 제자리에 남는다.
            _origin = origin;
            _duration = duration;

            // 새 번호를 부여해 이전 스캔과 구분한다. 대상은 이 번호가 바뀌면 새 스캔으로 인식한다.
            _scanId = ++_nextScanId;

            // 진행 중이던 파동이 있으면 지우고 처음부터 다시 시작한다.
            // true => 자식 파티클까지 포함 
            _scanParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 파티클 수명이 곧 확산 시간, 파티클 크기가 곧 최대 지름이 된다.
            ParticleSystem.MainModule main = _scanParticle.main;
            main.startLifetime = duration;
            main.startSize = maxDiameter;

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
    }
}
