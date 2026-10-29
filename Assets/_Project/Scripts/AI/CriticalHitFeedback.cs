using Core.Audio;
using Core.Effect;
using Mediator;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 약점에 명중하면 맞은 지점에서 약점 전용 히트 이펙트와 소리를 한 번 낸다.
    /// 총알의 일반 명중 이펙트·소리는 그대로 두고 그 위에 덧입혀 일반 피격과 구분되게 한다.
    /// </summary>
    public class CriticalHitFeedback : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterMediator _characterMediator;

        [Header("Feedback")]
        [Tooltip("약점 명중 지점에서 한 번 재생할 이펙트. 메인 카메라 쪽을 향한다. 비어 있으면 생략")]
        [SerializeField] private SOEffectData _effect;
        [Tooltip("약점 명중 지점에서 한 번 낼 소리. 비어 있으면 무음")]
        [SerializeField] private SOAudioData _sound;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_characterMediator) _characterMediator = GetComponent<CharacterMediator>();
            Debug.Assert(_characterMediator != null, $"[{name}] CharacterMediator 가 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            if (_characterMediator) _characterMediator.OnCriticalHit += OnCriticalHitCallback;
        }

        private void OnDisable()
        {
            if (_characterMediator) _characterMediator.OnCriticalHit -= OnCriticalHitCallback;
        }
        #endregion

        #region Private Methods
        private void OnCriticalHitCallback(Vector3? hitPoint)
        {
            if (!hitPoint.HasValue) return;

            Vector3 position = hitPoint.Value;
            if (_effect && EffectManager.Instance) EffectManager.Instance.Play(_effect, position, GetFacingRotation(position));
            if (_sound && (_sound.ClipCount > 0) && AudioManager.Instance) AudioManager.Instance.PlaySfx(_sound, position);
        }

        /// <summary>
        /// position 에서 메인 카메라를 향하는 회전을 돌려준다. 카메라가 없거나 같은 위치면 기본 회전.
        /// </summary>
        private static Quaternion GetFacingRotation(Vector3 position)
        {
            Camera mainCamera = Camera.main;
            if (!mainCamera) return Quaternion.identity;

            Vector3 toCamera = mainCamera.transform.position - position;
            return (toCamera.sqrMagnitude > 0.0001f) ? Quaternion.LookRotation(toCamera) : Quaternion.identity;
        }
        #endregion
    }
}
