using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// 소리 하나의 재생 정보. 클립 후보, 기본 음량, 피치 범위를 가진다.
    /// 여러 곳에서 같은 에셋을 공유하므로 재생 중 상태는 여기에 저장하지 않는다.
    /// </summary>
    // 3D 거리는 소리마다 다를 일이 거의 없어서 AudioManager의 3D 소스 공통값을 쓴다.
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
        #endregion

        #region Properties
        public float Volume => _volume;
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

        public float PickPitch() => Random.Range(_minPitch, _maxPitch);
        #endregion
    }
}
