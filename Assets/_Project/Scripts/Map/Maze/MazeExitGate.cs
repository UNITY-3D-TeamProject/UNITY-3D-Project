using System;
using UnityEngine;
using Core.Audio;
using Map.Gimmicks;

namespace Map.Maze
{
    /// <summary>
    /// 미로의 출구. 플레이어가 출구 칸에 들어오면 문(출구 벽)이 사라져 다음 미로로 가는 다리로 나갈 수 있다.
    /// 마지막 미로의 문은 라운드를 클리어해야 열린다.
    /// 플레이어가 문을 지나가면 Close 로 다시 닫으며, 닫힌 문은 다시 열리지 않는다. (문 밖 TriggerPlate 에 연결)
    /// 출구 칸 중앙에 놓고, 콜라이더의 Is Trigger를 켠다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MazeExitGate : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("열릴 때 사라지는 문 오브젝트 (콜라이더 포함)")]
        [SerializeField] private GameObject _door;

        [Header("Condition")]
        [Tooltip("켜면 라운드를 클리어해야 열린다. 마지막 미로의 문은 생성기가 켠다.")]
        [SerializeField] private bool _shouldRequireRoundClear;

        [Header("Sound")]
        [Tooltip("문이 열릴 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _openSound;
        #endregion

        #region Properties
        /// <summary>문이 열렸는지 여부.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>플레이어가 지나간 뒤 다시 닫혔는지 여부.</summary>
        public bool IsClosedBehind { get; private set; }

        /// <summary>라운드를 클리어해야 열리는 문인지 여부.</summary>
        public bool ShouldRequireRoundClear
        {
            get => _shouldRequireRoundClear;
            set => _shouldRequireRoundClear = value;
        }
        #endregion

        #region Events
        /// <summary>문이 열릴 때 호출된다.</summary>
        public event Action OnOpened;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_door != null, $"[{name}] 문 오브젝트가 연결되지 않았습니다.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsOpen || IsClosedBehind) return;

            if (!GimmickUtility.TryGetPlayer(other, out _)) return;
            if (_shouldRequireRoundClear && !IsRoundCleared()) return;

            Open();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 문을 연다.
        /// </summary>
        public void Open()
        {
            if (IsOpen) return;

            IsOpen = true;
            if (_door != null)
            {
                GimmickUtility.PlaySfx(_openSound, _door.transform.position);
                _door.SetActive(false);
            }

            OnOpened?.Invoke();
        }

        /// <summary>
        /// 열린 문을 다시 닫는다. 플레이어가 문을 지나간 뒤 되돌아오지 못하게 할 때 쓴다. (UnityEvent로 연결 가능)
        /// </summary>
        public void Close()
        {
            if (!IsOpen) return;

            IsOpen = false;
            IsClosedBehind = true;
            if (_door != null)
            {
                _door.SetActive(true);
            }
        }
        #endregion

        #region Private Methods
        // TODO: 라운드 클리어 판정이 생기면 그 값을 돌려준다. 지금은 항상 클리어한 것으로 본다.
        private bool IsRoundCleared()
        {
            return true;
        }
        #endregion
    }
}
