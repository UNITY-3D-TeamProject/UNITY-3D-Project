using System;
using UnityEngine;
using AI.Boss.Fight;

namespace Core.Stage.Security
{
    /// <summary>
    /// 3라운드 보스. 보스 생성·단계 진행은 BossFightSpawner·BossEncounter 가 하고(사망 시 재생성도 스포너가 한다),
    /// 이 라운드는 보스(A)를 쓰러뜨리면 꺼 두었던 탈출 문을 켠다.
    /// SecurityApp 이 필드로 들고, 스테이지 시작 때 Begin, 끝날 때 End 를 부른다.
    /// </summary>
    [Serializable]
    public class BossRound
    {
        #region Serialized Fields
        [Tooltip("보스전을 여는 스포너 (05_Boss/BossFight)")]
        [SerializeField] private BossFightSpawner _bossFightSpawner;

        [Tooltip("보스를 쓰러뜨리면 나타나는 탈출 문. 스테이지가 시작될 때 꺼 둔다.")]
        [SerializeField] private GameObject _exitDoor;
        #endregion

        #region Properties
        /// <summary>보스를 쓰러뜨렸는지 여부.</summary>
        public bool IsCleared { get; private set; }
        #endregion

        #region Events
        /// <summary>보스를 쓰러뜨리면 호출된다. 화면 갱신용.</summary>
        public event Action OnProgressChanged;
        #endregion

        #region Public Methods
        /// <summary>스테이지가 시작될 때 한 번 호출된다. 탈출 문을 꺼 두고 보스 격파를 구독한다.</summary>
        public void Begin()
        {
            if (_exitDoor != null) _exitDoor.SetActive(false);

            if (_bossFightSpawner == null)
            {
                Debug.LogWarning("BossFightSpawner가 연결되지 않아 탈출 문을 열 수 없습니다.");
                return;
            }

            _bossFightSpawner.OnBossDefeated += HandleBossDefeated;
        }

        /// <summary>스테이지가 없어질 때 한 번 호출된다. 구독을 푼다.</summary>
        public void End()
        {
            if (_bossFightSpawner != null) _bossFightSpawner.OnBossDefeated -= HandleBossDefeated;
        }
        #endregion

        #region Private Methods
        private void HandleBossDefeated()
        {
            IsCleared = true;
            if (_exitDoor != null) _exitDoor.SetActive(true);

            OnProgressChanged?.Invoke();
        }
        #endregion
    }
}
