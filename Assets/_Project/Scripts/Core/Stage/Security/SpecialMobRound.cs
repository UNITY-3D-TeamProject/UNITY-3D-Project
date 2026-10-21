using System;
using UnityEngine;

namespace Core.Stage.Security
{
    /// <summary>
    /// 2라운드 특수몹 발판. 발판(CombatZone)의 특수몹을 다 잡으면 꺼 두었던 다음 발판이 나타난다.
    /// SecurityApp 이 필드로 들고, 스테이지 시작 때 Begin, 끝날 때 End, 이 라운드 중 죽으면 ResetUnclearedSteps 를 부른다.
    /// </summary>
    [Serializable]
    public class SpecialMobRound
    {
        #region Nested Types
        [Serializable]
        private struct SPlatformStep
        {
            [Tooltip("발판 위 특수몹 구역")]
            public CombatZone Zone;

            [Tooltip("이 발판을 깨면 나타나는 다음 발판. 스테이지가 시작될 때 꺼 둔다.")]
            public GameObject NextPlatform;
        }
        #endregion

        #region Serialized Fields
        [Tooltip("발판마다 특수몹 구역·다음 발판")]
        [SerializeField] private SPlatformStep[] _steps;
        #endregion

        #region Properties
        /// <summary>모든 발판을 깼는지 여부.</summary>
        public bool IsCleared
        {
            get
            {
                if (_steps == null) return true;

                foreach (SPlatformStep step in _steps)
                {
                    if ((step.Zone != null) && !step.Zone.IsCleared) return false;
                }

                return true;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>스테이지가 시작될 때 한 번 호출된다. 다음 발판을 꺼 두고 발판 클리어를 구독한다.</summary>
        public void Begin()
        {
            if (_steps == null) return;

            foreach (SPlatformStep step in _steps)
            {
                if (step.NextPlatform != null) step.NextPlatform.SetActive(false);
                if (step.Zone != null) step.Zone.OnCleared += HandleZoneCleared;
            }
        }

        /// <summary>스테이지가 없어질 때 한 번 호출된다. 구독을 푼다.</summary>
        public void End()
        {
            if (_steps == null) return;

            foreach (SPlatformStep step in _steps)
            {
                if (step.Zone != null) step.Zone.OnCleared -= HandleZoneCleared;
            }
        }

        /// <summary>아직 못 깬 발판만 처음 상태로 돌린다. 이미 나타난 발판은 그대로 둔다.</summary>
        public void ResetUnclearedSteps()
        {
            if (_steps == null) return;

            foreach (SPlatformStep step in _steps)
            {
                if (step.Zone != null) step.Zone.ResetIfNotCleared();
            }
        }
        #endregion

        #region Private Methods
        // 어느 발판이 깨졌는지 이벤트로 알 수 없으므로, 깬 발판의 다음 발판을 모두 켠다.
        private void HandleZoneCleared()
        {
            foreach (SPlatformStep step in _steps)
            {
                if ((step.Zone == null) || !step.Zone.IsCleared) continue;

                if (step.NextPlatform != null) step.NextPlatform.SetActive(true);
            }
        }
        #endregion
    }
}
