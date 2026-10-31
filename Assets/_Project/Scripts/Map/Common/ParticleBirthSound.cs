using UnityEngine;
using Core.Audio;
using Map.Gimmicks;

namespace Map.Common
{
    /// <summary>
    /// 자식 파티클 시스템에 새 파티클이 생길 때마다 그 파티클 시스템 위치에서 3D 효과음을 낸다. (튜토리얼 경고 패널 등)
    /// 들리는 거리는 SOAudioData 의 Min/Max Distance 로 정한다.
    /// </summary>
    public class ParticleBirthSound : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Sound")]
        [Tooltip("파티클이 새로 생길 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _birthSound;
        [Tooltip("같은 그룹에서 경고음을 다시 낼 때까지의 최소 간격(초)")]
        [SerializeField, Min(0.0f)] private float _minimumInterval;
        #endregion

        #region Private Fields
        private ParticleSystem[] _systems;
        private int[] _lastCounts;
        private float _nextSoundTime;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _systems = GetComponentsInChildren<ParticleSystem>(true);
            _lastCounts = new int[_systems.Length];
        }

        // 켜질 때(Prewarm 포함) 이미 있는 파티클은 새로 생긴 것으로 세지 않도록 첫 프레임 개수를 기준으로 삼는다
        private void OnEnable()
        {
            _nextSoundTime = 0.0f;
            for (int i = 0; i < _lastCounts.Length; i++)
            {
                _lastCounts[i] = -1;
            }
        }

        private void Update()
        {
            for (int i = 0; i < _systems.Length; i++)
            {
                int count = _systems[i].particleCount;
                if ((_lastCounts[i] >= 0) && (count > _lastCounts[i]) && (Time.time >= _nextSoundTime))
                {
                    GimmickUtility.PlaySfx(_birthSound, _systems[i].transform.position);
                    _nextSoundTime = Time.time + _minimumInterval;
                }

                _lastCounts[i] = count;
            }
        }
        #endregion
    }
}
