using System;
using UnityEngine;
using Map.Gimmicks;

namespace Core.Stage.Security
{
    /// <summary>
    /// 1라운드 잡몹 방. 방(CombatZone)의 적을 다 잡으면 그 방의 출구 문을 연다.
    /// 문은 맵의 패널(InteractButton)이 부르던 것과 같은 StateMover.Activate 로 열고, 방을 깨기 전에는 출구 패널 콜라이더를 꺼서 패널(E)로 먼저 여는 것을 막는다.
    /// SecurityApp 이 필드로 들고, 스테이지 시작 때 Begin, 끝날 때 End, 이 라운드 중 죽으면 ResetUnclearedRooms 를 부른다.
    /// </summary>
    [Serializable]
    public class MobRoomRound
    {
        #region Nested Types
        [Serializable]
        private struct SMobRoom
        {
            [Tooltip("방 전체를 덮는 전투 구역")]
            public CombatZone Zone;

            [Tooltip("방을 깨면 Activate 로 여는 출구 문짝 (문 패널의 InteractButton 이 부르던 StateMover)")]
            public StateMover[] Doors;

            [Tooltip("방을 깨기 전에는 콜라이더를 꺼서 조준·상호작용(E)을 막아 둘 출구 문 패널")]
            public InteractButton[] LockedPanels;
        }
        #endregion

        #region Serialized Fields
        [Tooltip("방마다 전투 구역·출구 문짝·출구 패널")]
        [SerializeField] private SMobRoom[] _rooms;
        #endregion

        #region Properties
        /// <summary>모든 방을 깼는지 여부.</summary>
        public bool IsCleared
        {
            get
            {
                if (_rooms == null) return true;

                foreach (SMobRoom room in _rooms)
                {
                    if ((room.Zone != null) && !room.Zone.IsCleared) return false;
                }

                return true;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>스테이지가 시작될 때 한 번 호출된다. 출구 패널을 잠그고 방 클리어를 구독한다.</summary>
        public void Begin()
        {
            if (_rooms == null) return;

            foreach (SMobRoom room in _rooms)
            {
                SetPanelsLocked(room.LockedPanels, true);
                if (room.Zone != null) room.Zone.OnCleared += HandleZoneCleared;
            }
        }

        /// <summary>스테이지가 없어질 때 한 번 호출된다. 구독을 푼다.</summary>
        public void End()
        {
            if (_rooms == null) return;

            foreach (SMobRoom room in _rooms)
            {
                if (room.Zone != null) room.Zone.OnCleared -= HandleZoneCleared;
            }
        }

        /// <summary>아직 못 깬 방만 처음 상태로 돌린다. 깬 방의 문은 열린 채로 둔다.</summary>
        public void ResetUnclearedRooms()
        {
            if (_rooms == null) return;

            foreach (SMobRoom room in _rooms)
            {
                if (room.Zone != null) room.Zone.ResetIfNotCleared();
            }
        }
        #endregion

        #region Private Methods
        // 어느 방이 깨졌는지 이벤트로 알 수 없으므로, 깬 방의 문을 모두 연다. 이미 연 문을 다시 열어도 그대로다.
        private void HandleZoneCleared()
        {
            foreach (SMobRoom room in _rooms)
            {
                if ((room.Zone == null) || !room.Zone.IsCleared) continue;

                OpenDoors(room.Doors);
                SetPanelsLocked(room.LockedPanels, false);
            }
        }

        private static void OpenDoors(StateMover[] doors)
        {
            if (doors == null) return;

            foreach (StateMover door in doors)
            {
                if (door != null) door.Activate();
            }
        }

        // 패널 콜라이더를 끄면 상호작용 레이에 맞지 않아 조준 표시도, E 도 먹지 않는다.
        private static void SetPanelsLocked(InteractButton[] panels, bool isLocked)
        {
            if (panels == null) return;

            foreach (InteractButton panel in panels)
            {
                if (panel == null) continue;

                foreach (Collider panelCollider in panel.GetComponentsInChildren<Collider>(true))
                {
                    panelCollider.enabled = !isLocked;
                }
            }
        }
        #endregion
    }
}
