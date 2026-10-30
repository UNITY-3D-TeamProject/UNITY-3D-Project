using Core.Audio;
using Mediator;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 맞을 때마다 자기 위치에서 피격음을 한 번 낸다.
    /// SoundController 가 없는 캐릭터(보스A 등)에 피격음만 붙일 때 쓴다.
    /// </summary>
    public class HitSoundFeedback : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterMediator _characterMediator;

        [Header("Feedback")]
        [Tooltip("맞을 때마다 낼 소리. 비어 있으면 무음")]
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
            if (_characterMediator) _characterMediator.OnHit += OnHitCallback;
        }

        private void OnDisable()
        {
            if (_characterMediator) _characterMediator.OnHit -= OnHitCallback;
        }
        #endregion

        #region Private Methods
        private void OnHitCallback()
        {
            if (_sound && (_sound.ClipCount > 0) && AudioManager.Instance) AudioManager.Instance.PlaySfx(_sound, transform.position);
        }
        #endregion
    }
}
