using Core.Audio;
using Mediator;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 맞을 때마다 맞은 지점(없으면 자기 위치)에서 피격음을 한 번 낸다. 연사로 겹치지 않게 재생 간격을 두고,
    /// 약점에 맞은 피격에는 약점 소리만 들리도록 내지 않는다.
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

        [Tooltip("이 시간(초) 안에 다시 맞으면 소리를 내지 않는다. 0 이면 매번 낸다")]
        [SerializeField] private float _minInterval = 0.1f;
        #endregion

        #region Private Fields
        private float _lastPlayTime = float.NegativeInfinity;
        private int _lastCriticalFrame = -1;
        private int _lastHitFromFrame = -1;
        private Vector3 _lastHitPoint;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_characterMediator) _characterMediator = GetComponent<CharacterMediator>();
            Debug.Assert(_characterMediator != null, $"[{name}] CharacterMediator 가 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            if (_characterMediator)
            {
                _characterMediator.OnCriticalHit += OnCriticalHitCallback;
                _characterMediator.OnHitFrom += OnHitFromCallback;
                _characterMediator.OnHit += OnHitCallback;
            }
        }

        private void OnDisable()
        {
            if (_characterMediator)
            {
                _characterMediator.OnCriticalHit -= OnCriticalHitCallback;
                _characterMediator.OnHitFrom -= OnHitFromCallback;
                _characterMediator.OnHit -= OnHitCallback;
            }
        }
        #endregion

        #region Private Methods
        // 약점 명중은 같은 피격에서 OnHit 보다 먼저 오므로, 그 프레임의 OnHit 은 건너뛴다
        private void OnCriticalHitCallback(Vector3? hitPoint)
        {
            _lastCriticalFrame = Time.frameCount;
        }

        // OnHit 보다 먼저 같은 프레임에 오므로, 맞은 지점을 담아 두었다가 OnHit 에서 소리 위치로 쓴다
        private void OnHitFromCallback(Vector3 hitPoint)
        {
            _lastHitFromFrame = Time.frameCount;
            _lastHitPoint = hitPoint;
        }

        private void OnHitCallback()
        {
            if (_lastCriticalFrame == Time.frameCount) return;
            if ((Time.time - _lastPlayTime) < _minInterval) return;
            if (!_sound || (_sound.ClipCount <= 0) || !AudioManager.Instance) return;

            // 덩치가 큰 캐릭터는 루트가 멀어 소리가 작아지므로, 맞은 지점이 있으면 거기서 낸다
            Vector3 position = (_lastHitFromFrame == Time.frameCount) ? _lastHitPoint : transform.position;

            _lastPlayTime = Time.time;
            AudioManager.Instance.PlaySfx(_sound, position);
        }
        #endregion
    }
}
