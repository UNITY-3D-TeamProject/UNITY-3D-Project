using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Core.Effect
{
    /// <summary>
    /// 씬 단위 VFX 재생 창구. SO별 풀을 처음 Play할 때 만들고 시간으로 반납한다.
    /// 언제 어떤 효과를 낼지는 사용하는 쪽이 정한다.
    /// </summary>
    // 사용 흐름 (AudioManager와 같은 씬 단위 방식)
    // 1. 효과가 필요한 씬에 EffectManager 프리팹을 하나 배치한다. 없는 씬에서는 Instance가 null이다.
    // 2. 사용하는 쪽이 [SerializeField] SOEffectData를 들고 Start 이후 Play에 넘긴다. 사전 등록은 없다.
    //    EffectManager.Instance?.Play(_hitVfx, hitPoint, rotation);
    // 3. 루프는 PlayLoop로 내고, 반환된 핸들을 보관했다가 끝날 때 Stop한다. 소유자 OnDisable에서도 Stop해 준다.
    //    _handle = EffectManager.Instance.PlayLoop(_chargeVfx, position, rotation, muzzle);
    //    EffectManager.Instance?.Stop(_handle); _handle = default;
    // 4. Play는 Duration 뒤 반납한다. Stop하면 Play는 즉시, PlayLoop는 방출 중단 후 StopFadeDuration 뒤 반납한다.
    //    같은 핸들로 다시 Stop해도 반납 시각은 바뀌지 않는다.
    // 5. 부모를 지정하면 방출 위치가 따라간다. 이미 나온 입자는 프리팹의 Simulation Space 설정을 따른다.
    //    PlayLoop는 자식 파티클 Loop를 모두 켜고, Play는 프리팹의 원래 Loop 값으로 되돌린다.
    //    자체 Destroy/반납 스크립트가 있는 프리팹은 사용하지 않는다.
    // 6. Time.time으로 수명을 재고 파티클은 scaled time으로 재생하므로 timeScale=0에서 함께 멈춘다.
    //    매니저가 파괴되면 외부 부모 아래 재생 중인 효과와 보관된 효과도 정리한다.
    public class EffectManager : MonoBehaviour
    {
        #region Private Fields
        private readonly Dictionary<SOEffectData, ObjectPool<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, ParticleSystem[]> _particlesByInstance = new();

        // 인스턴스 자식 파티클의 프리팹 원래 Loop 값. PlayLoop로 켠 Loop를 Play 때 되돌리는 데 쓴다.
        private readonly Dictionary<GameObject, bool[]> _prefabLoopsByInstance = new();
        private readonly List<SActiveEffect> _actives = new();
        private readonly object _handleOwner = new();

        // 활성 프리팹도 최초 OnEnable 전에 파티클 설정과 위치를 준비할 수 있게 한다.
        private Transform _creationRoot;
        private int _nextId = 1;
        private bool _isDestroying;
        #endregion

        #region Properties
        /// <summary>지금 씬의 매니저. 배치하지 않은 씬이거나 Awake 전이면 null이다.</summary>
        // DontDestroyOnLoad를 쓰지 않으므로 공용 Singleton<T>을 상속하지 않는다.
        public static EffectManager Instance { get; private set; }
        #endregion

        #region Nested Types
        private struct SActiveEffect
        {
            public int Id;
            public SOEffectData Data;
            public GameObject Instance;
            public ParticleSystem[] Particles;
            public float EndTime;
            public bool IsLoop;
            public bool IsStopping;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if ((Instance != null) && (Instance.gameObject.scene == gameObject.scene))
                Debug.LogError("[EffectManager] 한 씬에 매니저가 두 개 있습니다. 나중에 Awake된 쪽이 Instance가 됩니다.", this);

            Instance = this;

            GameObject creationRoot = new GameObject("EffectCreationRoot");
            creationRoot.SetActive(false);
            creationRoot.transform.SetParent(transform, false);
            _creationRoot = creationRoot.transform;
        }

        private void Update()
        {
            float now = Time.time;
            for (int i = _actives.Count - 1; i >= 0; i--)
            {
                SActiveEffect active = _actives[i];
                if ((active.Instance == null) || (active.EndTime <= now))
                    ReleaseAt(i);
            }
        }

        private void OnDestroy()
        {
            _isDestroying = true;
            if (Instance == this)
                Instance = null;

            // 풀의 Dispose는 대여 중 인스턴스를 모른다. 다른 부모로 옮긴 효과도 직접 정리한다.
            foreach (SActiveEffect active in _actives)
            {
                if (active.Instance != null)
                    Destroy(active.Instance);
            }

            _actives.Clear();
            foreach (ObjectPool<GameObject> pool in _pools.Values)
                pool.Dispose();

            _pools.Clear();
            _particlesByInstance.Clear();
            _prefabLoopsByInstance.Clear();
        }
        #endregion

        #region Public Methods
        /// <summary>월드 위치·회전에서 한 번 재생하고 Duration 뒤 반납한다. parent가 있으면 현재 월드 배치를 유지하며 붙인다.</summary>
        /// <param name="scaleMultiplier">프리팹과 SO 기본 크기에 추가로 곱할 이번 재생의 배율.</param>
        /// <returns>이번 재생의 핸들. 잘못된 요청이면 default이며 중간에 끊을 일이 없으면 무시해도 된다.</returns>
        // [호출] 피격·폭발 등 VFX가 필요한 기능. 다른 컴포넌트의 Start 이후에 사용한다.
        public SEffectHandle Play(
            SOEffectData data,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            float scaleMultiplier = 1f)
        {
            return PlayEffect(data, position, rotation, parent, false, scaleMultiplier);
        }

        /// <summary>자식 파티클 Loop를 켜고 반복 재생한다. Stop을 부를 때까지 반납하지 않는다.</summary>
        /// <returns>이번 재생의 핸들. 잘못된 요청이면 default이다. 보관했다가 Stop에 넘긴다.</returns>
        // [호출] 차지 등 지속 VFX. 소유자 OnDisable에서도 Stop한다.
        public SEffectHandle PlayLoop(
            SOEffectData data,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
        {
            return PlayEffect(data, position, rotation, parent, true, 1f);
        }

        /// <summary>Play로 낸 효과는 즉시 반납하고, PlayLoop로 낸 효과는 방출을 멈춘 뒤 잔여 파티클 대기 시간 후 반납한다.</summary>
        // [호출] 루프 종료, 소유자 OnDisable, 일회성 조기 취소. 종료된 핸들·다른 매니저의 핸들은 무시한다.
        public void Stop(SEffectHandle handle)
        {
            if ((_isDestroying) || (!handle.IsValid) ||
                (!handle.IsOwnedBy(_handleOwner)))
                return;

            for (int i = _actives.Count - 1; i >= 0; i--)
            {
                SActiveEffect active = _actives[i];
                if (active.Id != handle.Id) continue;

                if (active.Instance == null)
                {
                    ReleaseAt(i);
                    return;
                }

                if (active.IsStopping) return;
                if (!active.IsLoop)
                {
                    ReleaseAt(i);
                    return;
                }

                active.IsStopping = true;
                active.EndTime = Time.time + active.Data.StopFadeDuration;
                _actives[i] = active;
                foreach (ParticleSystem particle in active.Particles)
                {
                    if (particle != null)
                        particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
                return;
            }
        }
        #endregion

        #region Private Methods
        // Play·PlayLoop 공통. isLoop면 자식 파티클 Loop를 켜고 Stop 전까지 반납하지 않는다.
        private SEffectHandle PlayEffect(
            SOEffectData data,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            bool isLoop,
            float scaleMultiplier)
        {
            if (_isDestroying || (_creationRoot == null))
                return default;

            if ((data == null) || (data.Prefab == null))
            {
                Debug.LogWarning($"[EffectManager] 재생할 프리팹이 없습니다: '{(data != null ? data.name : "null")}'", this);
                return default;
            }

            // 같은 매니저에서 번호를 재사용하지 않는다. 오래된 핸들과 충돌하는 순환도 허용하지 않는다.
            if (_nextId == int.MaxValue)
            {
                Debug.LogError("[EffectManager] 재생 번호를 모두 사용했습니다.", this);
                return default;
            }

            if (!_pools.TryGetValue(data, out ObjectPool<GameObject> pool))
            {
                GameObject prefab = data.Prefab;
                pool = new ObjectPool<GameObject>(
                    createFunc: () => CreateInstance(prefab, data.PlaybackSpeed),
                    actionOnGet: null,
                    actionOnRelease: ReleaseInstance,
                    actionOnDestroy: DestroyInstance,
                    collectionCheck: true,
                    defaultCapacity: Mathf.Min(10, data.MaxSize),
                    maxSize: data.MaxSize);
                _pools.Add(data, pool);
            }

            GameObject instance = pool.Get();
            Transform effectTransform = instance.transform;
            effectTransform.SetParent(transform, false);
            effectTransform.localScale = data.Prefab.transform.localScale * data.Scale * Mathf.Max(0.01f, scaleMultiplier);
            effectTransform.SetPositionAndRotation(position, rotation);
            if (parent != null)
                effectTransform.SetParent(parent, true);

            ParticleSystem[] particles = _particlesByInstance[instance];
            bool[] prefabLoops = _prefabLoopsByInstance[instance];
            instance.SetActive(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle == null) continue;
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

                // 같은 인스턴스를 Play·PlayLoop가 번갈아 쓰므로 매번 정한다.
                ParticleSystem.MainModule main = particle.main;
                main.loop = isLoop || prefabLoops[i];
                particle.Play(false);
            }

            int id = _nextId++;
            _actives.Add(new SActiveEffect
            {
                Id = id,
                Data = data,
                Instance = instance,
                Particles = particles,
                EndTime = isLoop ? float.MaxValue : Time.time + data.Duration,
                IsLoop = isLoop,
            });
            return new SEffectHandle(id, _handleOwner);
        }

        // [호출] 해당 SO의 풀이 비었을 때만. 자식 파티클은 생성 시 한 번 수집한다.
        private GameObject CreateInstance(GameObject prefab, float playbackSpeed)
        {
            GameObject instance = Instantiate(prefab, _creationRoot);
            instance.SetActive(false);
            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            bool[] prefabLoops = new bool[particles.Length];
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.stopAction = ParticleSystemStopAction.None;
                main.useUnscaledTime = false;
                main.playOnAwake = false;
                main.simulationSpeed *= playbackSpeed;
                prefabLoops[i] = main.loop;
            }
            _particlesByInstance.Add(instance, particles);
            _prefabLoopsByInstance.Add(instance, prefabLoops);
            return instance;
        }

        // [호출] Update의 수명 종료·부모 파괴 감지 또는 Stop의 즉시 반납.
        private void ReleaseAt(int index)
        {
            SActiveEffect active = _actives[index];
            // OnDisable에서 같은 핸들을 Stop해도 다시 반납하지 않도록 먼저 목록에서 뺀다.
            _actives.RemoveAt(index);
            if (active.Instance == null)
            {
                // 부모와 함께 파괴된 대여 객체는 풀에 넣지 않는다. 풀 CountActive는 실제 생존 수가 아니다.
                _particlesByInstance.Remove(active.Instance);
                _prefabLoopsByInstance.Remove(active.Instance);
                return;
            }

            _pools[active.Data].Release(active.Instance);
        }

        // [호출] 풀 Release. 잔여 파티클을 지우고 끈 뒤 매니저 아래에 보관한다.
        private void ReleaseInstance(GameObject instance)
        {
            foreach (ParticleSystem particle in _particlesByInstance[instance])
            {
                if (particle != null)
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            instance.SetActive(false);
            instance.transform.SetParent(transform, false);
        }

        // [호출] 보관 상한 초과 또는 풀 Dispose.
        private void DestroyInstance(GameObject instance)
        {
            _particlesByInstance.Remove(instance);
            _prefabLoopsByInstance.Remove(instance);
            if (instance != null)
                Destroy(instance);
        }
        #endregion
    }
}
