using System.Collections.Generic;
using DTT.AreaOfEffectRegions;
using UnityEngine;

namespace Boss.VFX
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    [AddComponentMenu("VFX/AOE VFX Player")]
    public sealed class AoeVfxPlayer : MonoBehaviour
    {
        [Min(0.01f)] public float duration = 1.5f;
        public bool playOnEnable;
        public bool loop;
        public bool hideOnComplete;

        private readonly List<Material> _instances = new List<Material>();
        private readonly Dictionary<Renderer, Material[]> _originals = new Dictionary<Renderer, Material[]>();
        private MonoBehaviour _region;
        private bool _playing;
        private float _elapsed;

        public MonoBehaviour Region
        {
            get
            {
                if (_region != null) return _region;
                if (TryGetComponent(out CircleRegion circle)) _region = circle;
                else if (TryGetComponent(out ArcRegion arc)) _region = arc;
                else if (TryGetComponent(out ScatterLineRegion scatter)) _region = scatter;
                else if (TryGetComponent(out LineRegion line)) _region = line;
                return _region;
            }
        }

        public float FillProgress
        {
            get
            {
                switch (Region)
                {
                    case CircleRegion circle: return circle.FillProgress;
                    case ArcRegion arc: return arc.FillProgress;
                    case ScatterLineRegion scatter: return scatter.FillProgress;
                    case LineRegion line: return line.FillProgress;
                    default: return 0f;
                }
            }
            set
            {
                switch (Region)
                {
                    case CircleRegion circle: circle.FillProgress = value; break;
                    case ArcRegion arc: arc.FillProgress = value; break;
                    case ScatterLineRegion scatter: scatter.FillProgress = value; break;
                    case LineRegion line: line.FillProgress = value; break;
                }
            }
        }

        private void Awake()
        {
            // DTT writes to sharedMaterial. Each spawned effect needs its own copies.
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                _originals.Add(renderer, (Material[])materials.Clone());
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    materials[i] = new Material(materials[i]);
                    _instances.Add(materials[i]);
                }
                renderer.sharedMaterials = materials;
            }
        }

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        public void Play()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            _elapsed = 0f;
            FillProgress = 0f;
            _playing = true;
        }

        public void Stop()
        {
            _playing = false;
        }

        private void Update()
        {
            if (!_playing) return;
            _elapsed += Time.deltaTime;
            float seconds = Mathf.Max(0.01f, duration);
            FillProgress = Mathf.Clamp01(_elapsed / seconds);
            if (_elapsed < seconds) return;
            if (loop) _elapsed %= seconds;
            else
            {
                _playing = false;
                if (hideOnComplete) gameObject.SetActive(false);
            }
        }

        private void OnDisable() => _playing = false;

        private void OnDestroy()
        {
            foreach (var pair in _originals)
                if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
            foreach (Material material in _instances)
                if (material != null) Destroy(material);
        }
    }
}
