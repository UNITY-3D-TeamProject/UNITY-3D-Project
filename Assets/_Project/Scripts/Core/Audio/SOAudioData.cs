using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// 소리 하나의 재생 정보. 클립 후보, 기본 음량, 피치 범위를 가진다.
    /// 여러 곳에서 같은 에셋을 공유하므로 재생 중 상태는 여기에 저장하지 않는다.
    /// </summary>
    // 한 SO를 BGM·UI·3D 효과음 어디에나 쓰지만, 칸마다 쓰이는 곳이 다르다. (Header와 Tooltip 참고)
    [CreateAssetMenu(fileName = "SOAudioData", menuName = "Audio/AudioData")]
    public class SOAudioData : ScriptableObject
    {
        #region Serialized Fields
        [Tooltip("재생할 때마다 하나를 무작위로 고른다")]
        [SerializeField] private AudioClip[] _clips;

        [Range(0f, 1f)]
        [SerializeField] private float _volume = 1f;

        [Tooltip("재생할 때마다 이 범위에서 피치를 고른다. UI 소리는 피치를 쓰지 않는다")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _minPitch = 1f;

        [Range(0.1f, 3f)]
        [SerializeField] private float _maxPitch = 1f;

        [Tooltip("BGM으로 쓸 때 클립이 2개 이상이면 플레이리스트가 된다. 켜면 무작위 순서, 끄면 위에서부터 차례로 튼다. 효과음·UI는 이 값과 상관없이 매번 무작위로 고른다")]
        [SerializeField] private bool _isShuffle;

        [Header("BGM Fade")]
        [Tooltip("BGM 전용. 곡이 시작할 때 0에서 음량까지 키우는 시간 (초)")]
        [Min(0f)]
        [SerializeField] private float _fadeInDuration = 1f;

        [Tooltip("BGM 전용. 곡이 끝나기 전·다른 BGM으로 바뀔 때 0까지 줄이는 시간 (초)")]
        [Min(0f)]
        [SerializeField] private float _fadeOutDuration = 1f;

        [Header("3D SFX Distance")]
        [Tooltip("3D 효과음 전용. 이 거리 안에서는 최대 음량으로 들린다")]
        [Min(0.01f)]
        [SerializeField] private float _minDistance = 1f;

        [Tooltip("3D 효과음 전용. 이 거리부터는 더 작아지지 않는다 (Logarithmic 감쇠). 폭발처럼 멀리 들려야 하면 크게")]
        [Min(0.01f)]
        [SerializeField] private float _maxDistance = 30f;
        #endregion

        #region Properties
        public float Volume => _volume;
        public bool IsShuffle => _isShuffle;
        public float FadeInDuration => _fadeInDuration;
        public float FadeOutDuration => _fadeOutDuration;
        public float MinDistance => _minDistance;
        public float MaxDistance => Mathf.Max(_maxDistance, _minDistance);
        public int ClipCount => (_clips != null) ? _clips.Length : 0;
        #endregion

        #region Public Methods
        /// <summary>
        /// 클립 후보 중 하나를 무작위로 고른다.
        /// </summary>
        /// <returns>고른 클립. 후보가 없으면 null</returns>
        public AudioClip PickClip()
        {
            if ((_clips == null) || (_clips.Length == 0))
                return null;

            return _clips[Random.Range(0, _clips.Length)];
        }

        // [호출] AudioManager가 BGM 플레이리스트에서 정한 순서대로 꺼낼 때
        public AudioClip GetClip(int index) => _clips[index];

        public float PickPitch() => Random.Range(_minPitch, _maxPitch);
        #endregion
    }
}
