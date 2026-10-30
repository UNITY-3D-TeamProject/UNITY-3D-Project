using UnityEngine;
using UnityEngine.Events;
using Core.Audio;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 밟으면(닿으면) 연결된 동작을 실행하는 발판. 함정을 작동시키는 데 쓴다.
    /// 예: On Player Entered 에 FallingObstacle.Drop 을 연결
    /// 콜라이더의 Is Trigger를 켜야 한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TriggerPlate : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Event")]
        [Tooltip("플레이어가 닿았을 때 실행할 동작")]
        [SerializeField] private UnityEvent _onPlayerEntered = new UnityEvent();

        [Tooltip("켜면 처음 한 번만 실행한다.")]
        [SerializeField] private bool _isOneShot;

        [Header("Sound")]
        [Tooltip("플레이어가 밟았을 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _pressedSound;
        #endregion

        #region Private Fields
        private bool _hasFired;
        #endregion

        #region Properties
        /// <summary>플레이어가 닿았을 때 실행되는 이벤트.</summary>
        public UnityEvent OnPlayerEntered => _onPlayerEntered;
        #endregion

        #region Unity Lifecycle
        private void OnTriggerEnter(Collider other)
        {
            if (_isOneShot && _hasFired) return;
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            _hasFired = true;
            GimmickUtility.PlaySfx(_pressedSound, transform.position);
            _onPlayerEntered.Invoke();
        }
        #endregion
    }
}
