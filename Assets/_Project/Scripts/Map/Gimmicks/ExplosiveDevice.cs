using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Core.Audio;
using PlayerInput;

namespace Map.Gimmicks
{
    /// <summary>
    /// 터지면 주변의 모든 대상(플레이어, 적, 다른 폭발 장치 등 IEffectTarget)에 Effect 를 적용하는 폭발 장치.
    /// 스스로 터지지는 않는다. DamageButton.OnPressed 나 GimmickSwitch 의 이벤트에 Explode 를 연결한다.
    /// 폭발 Effect 가 CurrentHp 를 깎으면, 범위 안의 다른 폭발 장치도 DamageButton 을 거쳐 이어서 터진다.
    /// </summary>
    public class ExplosiveDevice : MonoBehaviour
    {
        #region Constants
        private const int OVERLAP_BUFFER_SIZE = 32;
        #endregion

        #region Serialized Fields
        [Header("Explosion")]
        [Tooltip("폭발 범위(반지름)")]
        [SerializeField, Min(0.1f)] private float _radius = 4.0f;

        [Tooltip("Explode 가 불린 뒤 실제로 터질 때까지의 시간(초). 연쇄 폭발이 차례로 보이게 한다.")]
        [SerializeField, Min(0.0f)] private float _fuseTime = 0.15f;

        [Tooltip("범위 안의 대상에게 위에서부터 순서대로 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect[] _effects;

        [Tooltip("폭발이 닿는 레이어")]
        [SerializeField] private LayerMask _targetLayers = Physics.AllLayers;

        [Header("Visual")]
        [Tooltip("터지면 사라지는 몸체 (선택)")]
        [SerializeField] private GameObject _body;

        [Tooltip("터질 때 범위만큼 커졌다 사라지는 표시 (선택). 지름 1짜리 구를 넣는다.")]
        [SerializeField] private Transform _blastVisual;

        [Tooltip("폭발 표시가 커지는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.05f)] private float _blastDuration = 0.3f;

        [Header("Sound")]
        [Tooltip("터질 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _explodeSound;

        [Header("Respawn")]
        [Tooltip("터진 뒤 다시 생길 때까지의 시간(초). 0이면 다시 생기지 않는다.")]
        [SerializeField, Min(0.0f)] private float _respawnTime;
        #endregion

        #region Private Fields
        private readonly Collider[] _overlapHits = new Collider[OVERLAP_BUFFER_SIZE];
        private readonly HashSet<IEffectTarget> _appliedTargets = new HashSet<IEffectTarget>();
        private Collider[] _ownColliders;
        private IEffectTarget _ownTarget;
        private bool _isExploding;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _ownColliders = GetComponents<Collider>();
            _ownTarget = GetComponent<IEffectTarget>();

            if (_blastVisual != null)
            {
                _blastVisual.gameObject.SetActive(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1.0f, 0.4f, 0.0f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 폭발시킨다. 이미 터지는 중이거나 터져 있으면 무시한다. (UnityEvent로 연결 가능)
        /// </summary>
        public void Explode()
        {
            if (_isExploding || !isActiveAndEnabled) return;

            StartCoroutine(CoExplode());
        }
        #endregion

        #region Private Methods
        private void ApplyEffectsInRange()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _radius,
                _overlapHits,
                _targetLayers,
                QueryTriggerInteraction.Collide);

            _appliedTargets.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                IEffectTarget target = FindEffectTarget(_overlapHits[i]);

                // 콜라이더가 여러 개인 대상에 두 번 적용하지 않는다
                if ((target == null) || (target == _ownTarget) || !_appliedTargets.Add(target)) continue;

                GimmickUtility.ApplyEffects(_effects, target);
            }
        }

        // 플레이어의 AttributeSet 은 콜라이더의 부모가 아니라 플레이어 루트의 자식에 있다
        private static IEffectTarget FindEffectTarget(Collider collider)
        {
            if (GimmickUtility.TryGetPlayer(collider, out PlayerInputComponent player))
            {
                return player.GetComponentInChildren<IEffectTarget>();
            }

            return collider.GetComponentInParent<IEffectTarget>();
        }

        private void SetBodyVisible(bool isVisible)
        {
            if (_body != null)
            {
                _body.SetActive(isVisible);
            }

            foreach (Collider ownCollider in _ownColliders)
            {
                ownCollider.enabled = isVisible;
            }
        }
        #endregion

        #region Coroutines
        private IEnumerator CoExplode()
        {
            _isExploding = true;

            if (_fuseTime > 0.0f)
            {
                yield return new WaitForSeconds(_fuseTime);
            }

            SetBodyVisible(false);
            GimmickUtility.PlaySfx(_explodeSound, transform.position);
            ApplyEffectsInRange();

            if (_blastVisual != null)
            {
                _blastVisual.gameObject.SetActive(true);

                float elapsed = 0.0f;
                while (elapsed < _blastDuration)
                {
                    elapsed += Time.deltaTime;
                    _blastVisual.localScale = Vector3.one * (_radius * 2.0f * Mathf.Clamp01(elapsed / _blastDuration));
                    yield return null;
                }

                _blastVisual.gameObject.SetActive(false);
            }

            if (_respawnTime <= 0.0f)
            {
                Destroy(gameObject);
                yield break;
            }

            yield return new WaitForSeconds(_respawnTime);

            SetBodyVisible(true);
            _isExploding = false;
        }
        #endregion
    }
}
