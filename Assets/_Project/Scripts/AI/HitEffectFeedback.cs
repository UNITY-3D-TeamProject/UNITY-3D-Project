using Core.Effect;
using Mediator;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 맞을 때마다 맞은 지점에서 피격 이펙트를 한 번 재생한다.
    /// 맞은 지점(또는 공격자 위치)이 없는 피격에는 재생하지 않는다.
    /// </summary>
    public class HitEffectFeedback : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterMediator _characterMediator;

        [Header("Feedback")]
        [Tooltip("맞을 때마다 맞은 지점에서 한 번 재생할 이펙트. 메인 카메라 쪽을 향한다. 비어 있으면 생략")]
        [SerializeField] private SOEffectData _effect;

        [Header("Distance Scale")]
        [Tooltip("이 거리(m)까지는 기본 크기. 넘으면 거리에 비례해 이펙트를 키운다")]
        [SerializeField] private float _scaleStartDistance = 15.0f;

        [Tooltip("거리 비례 확대의 최대 배율. 1 이면 확대하지 않는다")]
        [SerializeField] private float _maxScaleMultiplier = 5.0f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_characterMediator) _characterMediator = GetComponent<CharacterMediator>();
            Debug.Assert(_characterMediator != null, $"[{name}] CharacterMediator 가 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            if (_characterMediator) _characterMediator.OnHitFrom += OnHitFromCallback;
        }

        private void OnDisable()
        {
            if (_characterMediator) _characterMediator.OnHitFrom -= OnHitFromCallback;
        }
        #endregion

        #region Private Methods
        private void OnHitFromCallback(Vector3 hitPoint)
        {
            if (!_effect || !EffectManager.Instance) return;

            Camera mainCamera = Camera.main;
            Vector3 toCamera = mainCamera ? mainCamera.transform.position - hitPoint : Vector3.zero;
            Quaternion rotation = (toCamera.sqrMagnitude > 0.0001f) ? Quaternion.LookRotation(toCamera) : Quaternion.identity;

            float scaleMultiplier = 1.0f;
            if (mainCamera && (_scaleStartDistance > 0.0f))
            {
                float distance = Mathf.Sqrt(toCamera.sqrMagnitude);
                scaleMultiplier = Mathf.Clamp(distance / _scaleStartDistance, 1.0f, Mathf.Max(1.0f, _maxScaleMultiplier));
            }

            EffectManager.Instance.Play(_effect, hitPoint, rotation, null, scaleMultiplier);
        }
        #endregion
    }
}
