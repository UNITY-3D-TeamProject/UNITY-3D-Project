using System.Collections.Generic;
using UnityEngine;
using Map.Common;

namespace Map.Feed
{
    /// <summary>악성 카드와 일반 피드를 같은 높이 마스크로 교체하고 안쪽 자리로 보낸다.</summary>
    public class FeedPurification : MonoBehaviour
    {
        #region Constants
        private const float CARD_HEIGHT = 4.6f;
        #endregion

        #region Private Fields
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Material[]> _originalMaterials = new List<Material[]>();
        private readonly List<Material> _wipeMaterials = new List<Material>();
        private FeedPost _post;
        private MaliciousFeed _source;
        private Billboard _billboard;
        private bool _wasBillboardEnabled;
        private Vector3 _destination;
        private Vector3 _normalScale;
        private Vector3 _cardScale;
        private float _duration;
        private float _travelDuration;
        private float _elapsed;
        private bool _hasRevealed;
        private bool _isFinished;
        private ParticleSystem _sparkles;
        private Material _sparkMaterial;
        #endregion

        #region Properties
        /// <summary>정화와 측면 이동이 끝났는지 여부. 2라운드 전환에 따른 종료도 포함한다.</summary>
        public bool IsFinished => _isFinished;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (_isFinished) return;

            _elapsed += Time.deltaTime;
            if (!_hasRevealed)
            {
                if (_elapsed < _duration) return;

                RevealNormal();
                _post.MoveTo(_destination, _travelDuration);
                _elapsed = 0.0f;
                return;
            }

            float progress = Mathf.Clamp01(_elapsed / _travelDuration);
            transform.localScale = Vector3.Lerp(_cardScale, _normalScale,
                Mathf.SmoothStep(0.0f, 1.0f, progress));
            if (progress >= 1.0f)
            {
                _isFinished = true;
                Destroy(this);
            }
        }

        private void LateUpdate()
        {
            if (_isFinished || _hasRevealed) return;
            ApplyWipe(Mathf.Clamp01(_elapsed / _duration));
        }

        private void OnDestroy()
        {
            RestoreMaterials();
        }
        #endregion

        #region Public Methods
        public void Begin(
            FeedPost post,
            MaliciousFeed source,
            Transform cardVisual,
            Transform cardBody,
            Material wipeMaterial,
            float duration,
            float travelDuration)
        {
            _post = post;
            _source = source;
            _destination = transform.position;
            _normalScale = transform.localScale;
            _duration = Mathf.Max(0.01f, duration);
            _travelDuration = Mathf.Clamp(travelDuration, 1.0f, 1.5f);
            _billboard = GetComponent<Billboard>();
            if (_billboard != null)
            {
                _wasBillboardEnabled = _billboard.enabled;
                _billboard.enabled = false;
            }

            transform.SetPositionAndRotation(cardBody.position, cardVisual.rotation);
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            Vector3 visualScale = cardVisual.lossyScale;
            _cardScale = new Vector3(visualScale.x / parentScale.x,
                visualScale.y / parentScale.y, visualScale.z / parentScale.z);
            transform.localScale = _cardScale;

            AddWipeMaterials(cardVisual.GetComponentsInChildren<Renderer>(), wipeMaterial, false);
            AddWipeMaterials(GetComponentsInChildren<Renderer>(), wipeMaterial, true);
            CreateSparkles(wipeMaterial);
            ApplyWipe(0.0f);
        }

        /// <summary>2라운드가 시작되면 현재 위치를 유지한 채 정화를 확정한다.</summary>
        public void FinishForRound2()
        {
            if (_isFinished) return;
            _isFinished = true;
            RevealNormal();
            transform.localScale = _normalScale;
            Destroy(this);
        }
        #endregion

        #region Private Methods
        private void CreateSparkles(Material template)
        {
            var effect = new GameObject("PurificationSparkles");
            effect.transform.SetParent(transform, false);
            _sparkles = effect.AddComponent<ParticleSystem>();
            _sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = _sparkles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = _duration;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.15f);
            main.startSpeed = 0.0f;
            main.startColor = new Color(0.5f, 1.0f, 0.9f, 1.0f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;

            ParticleSystem.EmissionModule emission = _sparkles.emission;
            emission.rateOverTime = 80.0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.04f, 12) });
            ParticleSystem.ShapeModule shape = _sparkles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3.1f, 0.12f, 0.06f);

            ParticleSystem.VelocityOverLifetimeModule velocity = _sparkles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.85f, -0.35f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.45f, -0.15f);

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.15f), new GradientAlphaKey(0.0f, 1.0f) });
            ParticleSystem.ColorOverLifetimeModule color = _sparkles.colorOverLifetime;
            color.enabled = true;
            color.color = gradient;

            _sparkMaterial = new Material(template);
            _sparkMaterial.name = "FeedPurification Sparkles (Runtime)";
            _sparkMaterial.SetFloat("_SparkMode", 1.0f);
            _sparkMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _sparkMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            _sparkMaterial.SetFloat("_ZWrite", 0.0f);
            _sparkMaterial.renderQueue = 3000;
            ParticleSystemRenderer particleRenderer = _sparkles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = _sparkMaterial;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            _sparkles.Play();
        }

        private void AddWipeMaterials(Renderer[] renderers, Material template, bool shouldReveal)
        {
            foreach (Renderer target in renderers)
            {
                Material[] originals = target.sharedMaterials;
                var replacements = new Material[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    Material original = originals[i];
                    Material material = new Material(template);
                    material.name = "FeedPurification (Runtime)";
                    if (original != null)
                    {
                        if (original.HasProperty("_BaseMap"))
                        {
                            material.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
                            material.SetTextureScale("_BaseMap", original.GetTextureScale("_BaseMap"));
                            material.SetTextureOffset("_BaseMap", original.GetTextureOffset("_BaseMap"));
                        }
                        if (original.HasProperty("_BaseColor"))
                        {
                            material.SetColor("_BaseColor", original.GetColor("_BaseColor"));
                        }
                    }
                    material.SetFloat("_Reveal", shouldReveal ? 1.0f : 0.0f);
                    replacements[i] = material;
                    _wipeMaterials.Add(material);
                }
                _renderers.Add(target);
                _originalMaterials.Add(originals);
                target.sharedMaterials = replacements;
            }
        }

        private void ApplyWipe(float progress)
        {
            if (_sparkles != null)
            {
                float height = (Mathf.Lerp(1.08f, -0.08f, progress) - 0.5f) * CARD_HEIGHT;
                _sparkles.transform.localPosition = new Vector3(0.0f, height, -0.28f);
            }
            Matrix4x4 worldToCard = transform.worldToLocalMatrix;
            foreach (Material material in _wipeMaterials)
            {
                material.SetMatrix("_WipeWorldToCard", worldToCard);
                material.SetFloat("_Progress", progress);
                material.SetFloat("_CardHeight", CARD_HEIGHT);
            }
        }

        private void RevealNormal()
        {
            if (_hasRevealed) return;
            _hasRevealed = true;
            RestoreMaterials();
            if (_billboard != null) _billboard.enabled = _wasBillboardEnabled;
            if (_source != null)
            {
                _source.gameObject.SetActive(false);
                Destroy(_source.gameObject);
            }
        }

        private void RestoreMaterials()
        {
            if (_sparkles != null)
            {
                _sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _sparkles.gameObject.SetActive(false);
                Destroy(_sparkles.gameObject);
                _sparkles = null;
            }
            if (_sparkMaterial != null)
            {
                Destroy(_sparkMaterial);
                _sparkMaterial = null;
            }
            for (int i = 0; i < _renderers.Count; i++)
            {
                if (_renderers[i] != null) _renderers[i].sharedMaterials = _originalMaterials[i];
            }
            foreach (Material material in _wipeMaterials)
            {
                if (material != null) Destroy(material);
            }
            _renderers.Clear();
            _originalMaterials.Clear();
            _wipeMaterials.Clear();
        }
        #endregion
    }
}
