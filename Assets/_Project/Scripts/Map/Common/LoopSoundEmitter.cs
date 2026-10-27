using UnityEngine;
using Core.Audio;
using Map.Gimmicks;

namespace Map.Common
{
    /// <summary>
    /// 켜져 있는 동안 이 위치에서 3D 반복음을 낸다. (튜토리얼 분쇄기 기계음, 스캔 구역 처리음 등)
    /// 들리는 거리는 SOAudioData 의 Min/Max Distance 로 정한다.
    /// </summary>
    public class LoopSoundEmitter : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Sound")]
        [Tooltip("켜져 있는 동안 반복할 소리")]
        [SerializeField] private SOAudioData _loopSound;
        #endregion

        #region Private Fields
        private SAudioHandle _loopHandle;
        #endregion

        #region Unity Lifecycle
        // AudioManager 는 Awake 에서 준비되므로 처음에는 Start 에서 튼다
        private void Start()
        {
            GimmickUtility.PlaySfxLoop(_loopSound, transform, ref _loopHandle);
        }

        private void OnEnable()
        {
            GimmickUtility.PlaySfxLoop(_loopSound, transform, ref _loopHandle);
        }

        private void OnDisable()
        {
            GimmickUtility.StopSfxLoop(ref _loopHandle);
        }
        #endregion
    }
}
