using System;
using UnityEngine;

namespace Core.Stage
{
    /// <summary>
    /// 스테이지의 한 라운드가 맡는 로직의 공통 부모. MonoBehaviour 가 아니라 스테이지(FileApp 등)가 필드로 들고 인스펙터에서 설정한다.
    /// 스테이지가 Begin·End 를 부르고, 매 프레임 SetActive 로 진행 중인지 알려 주고, 진행 중이면 Tick 을 부른다.
    /// 클리어 조건은 자식이 IsCleared 로 정하고, 깨는 순간 NotifyCleared 를 부른다.
    /// </summary>
    [Serializable]
    public abstract class StageRound
    {
        #region Serialized Fields
        [Tooltip("StageManager.CurrentRoundIndex 가 이 값일 때 진행되는 라운드. 0 부터 시작하므로 2라운드는 1, 3라운드는 2 이다.")]
        [SerializeField, Min(0)] private int _roundIndex;
        #endregion

        #region Properties
        /// <summary>라운드 번호 (0부터).</summary>
        public int RoundIndex => _roundIndex;

        /// <summary>화면에 보일 라운드 번호 (1부터).</summary>
        public int RoundNumber => _roundIndex + 1;

        /// <summary>지금 이 라운드가 진행 중인지 여부.</summary>
        public bool IsActive { get; private set; }

        /// <summary>이 라운드를 깼는지 여부.</summary>
        public abstract bool IsCleared { get; }
        #endregion

        #region Events
        /// <summary>라운드가 시작·종료되거나 진행 상황이 바뀌면 호출된다. 화면 갱신용.</summary>
        public event Action OnProgressChanged;

        /// <summary>라운드를 깬 순간 한 번 호출된다.</summary>
        public event Action OnCleared;
        #endregion

        protected StageRound(int roundIndex)
        {
            _roundIndex = roundIndex;
        }

        #region Public Methods
        /// <summary>스테이지가 시작될 때 한 번 호출된다. 씬에서 필요한 것을 찾는다.</summary>
        public virtual void Begin()
        {
        }

        /// <summary>스테이지가 없어질 때 한 번 호출된다. 구독을 푼다.</summary>
        public virtual void End()
        {
        }

        /// <summary>진행 중일 때 매 프레임 호출된다.</summary>
        public virtual void Tick(float deltaTime)
        {
        }

        /// <summary>
        /// 진행 중인지 알려 준다. 바뀔 때만 OnRoundEntered / OnRoundExited 를 부른다.
        /// </summary>
        public void SetActive(bool isActive)
        {
            if (IsActive == isActive) return;

            IsActive = isActive;
            if (IsActive)
            {
                OnRoundEntered();
            }
            else
            {
                OnRoundExited();
            }

            NotifyProgressChanged();
        }

        /// <summary>
        /// 플레이어 재스폰 요청을 알려 준다. 이 라운드가 진행 중일 때만 OnPlayerRespawnRequested 를 부른다.
        /// </summary>
        public void HandlePlayerRespawnRequested()
        {
            if (!IsActive) return;

            OnPlayerRespawnRequested();
        }
        #endregion

        #region Protected Methods
        /// <summary>이 라운드에 들어왔을 때 한 번 호출된다.</summary>
        protected virtual void OnRoundEntered()
        {
        }

        /// <summary>이 라운드에서 벗어났을 때 한 번 호출된다.</summary>
        protected virtual void OnRoundExited()
        {
        }

        /// <summary>이 라운드 중 플레이어가 죽어 재스폰 요청이 왔을 때 호출된다.</summary>
        protected virtual void OnPlayerRespawnRequested()
        {
        }

        protected void NotifyProgressChanged()
        {
            OnProgressChanged?.Invoke();
        }

        /// <summary>깬 순간 자식이 한 번 부른다.</summary>
        protected void NotifyCleared()
        {
            OnCleared?.Invoke();
            NotifyProgressChanged();
        }
        #endregion
    }
}
