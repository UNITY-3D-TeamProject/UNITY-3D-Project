using System;
using UnityEngine;
using Map.Gimmicks;

namespace Core.Stage.Security
{
    /// <summary>
    /// 1라운드 잡몹 방. 방은 순서대로 하나씩 진행한다.
    /// - 지금 방(아직 못 깬 첫 방)의 적은 닫힌 문 뒤에 미리 만들어 제자리에 묶어 둔다. 앞 방을 깨면 다음 방 적을 만든다.
    /// - 플레이어가 방에 들어오면 들어온 문(입구)을 닫고 입구 패널을 잠근다. 입구 패널은 방을 깨도 잠긴 채로 두고, 죽어서 다시 할 때만 푼다.
    /// - 방의 적을 다 잡으면 출구 문을 연다(맵의 패널 InteractButton 이 부르던 것과 같은 StateMover). 깨기 전에는 출구 패널 콜라이더를 꺼서 패널(E)로 먼저 여는 것을 막는다.
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

            [Tooltip("플레이어가 방에 들어오면 Deactivate 로 닫는 입구 문짝 (StateMover)")]
            public StateMover[] EntranceDoors;

            [Tooltip("방에 들어오면 콜라이더를 꺼서 다시 열지 못하게 막는 입구 문 패널. 죽어서 다시 할 때만 풀린다.")]
            public InteractButton[] EntrancePanels;

            [Tooltip("방을 깨면 Activate 로 여는 출구 문짝 (문 패널의 InteractButton 이 부르던 StateMover)")]
            public StateMover[] Doors;

            [Tooltip("방을 깨기 전에는 콜라이더를 꺼서 조준·상호작용(E)을 막아 둘 출구 문 패널")]
            public InteractButton[] LockedPanels;
        }
        #endregion

        #region Serialized Fields
        [Tooltip("진행 순서대로 방마다 전투 구역·입구 문·출구 문")]
        [SerializeField] private SMobRoom[] _rooms;
        #endregion

        #region Properties
        /// <summary>방 수.</summary>
        public int RoomCount => (_rooms != null) ? _rooms.Length : 0;

        /// <summary>지금 방 번호 (1부터, 아직 못 깬 첫 방). 다 깼으면 RoomCount.</summary>
        public int CurrentRoomNumber
        {
            get
            {
                int index = FindCurrentRoomIndex();
                return (index >= 0) ? index + 1 : RoomCount;
            }
        }

        /// <summary>지금 방에 남은 적 수. 다 깼으면 0.</summary>
        public int RemainingEnemyCount
        {
            get
            {
                int index = FindCurrentRoomIndex();
                return ((index >= 0) && (_rooms[index].Zone != null)) ? _rooms[index].Zone.AliveCount : 0;
            }
        }

        /// <summary>모든 방을 깼는지 여부.</summary>
        public bool IsCleared => FindCurrentRoomIndex() < 0;
        #endregion

        #region Events
        /// <summary>방 진행·남은 적 수가 바뀌면 호출된다. 화면 갱신용.</summary>
        public event Action OnProgressChanged;
        #endregion

        #region Public Methods
        /// <summary>스테이지가 시작될 때 한 번 호출된다. 출구 패널을 잠그고, 구독하고, 첫 방 적을 만들어 둔다.</summary>
        public void Begin()
        {
            if (_rooms == null) return;

            foreach (SMobRoom room in _rooms)
            {
                SetPanelsLocked(room.LockedPanels, true);
                if (room.Zone == null) continue;

                room.Zone.OnStarted += HandleZoneStarted;
                room.Zone.OnCleared += HandleZoneCleared;
                room.Zone.OnProgressChanged += HandleZoneProgressChanged;
            }

            PrepareCurrentRoom();
        }

        /// <summary>스테이지가 없어질 때 한 번 호출된다. 구독을 푼다.</summary>
        public void End()
        {
            if (_rooms == null) return;

            foreach (SMobRoom room in _rooms)
            {
                if (room.Zone == null) continue;

                room.Zone.OnStarted -= HandleZoneStarted;
                room.Zone.OnCleared -= HandleZoneCleared;
                room.Zone.OnProgressChanged -= HandleZoneProgressChanged;
            }
        }

        /// <summary>
        /// 아직 못 깬 방만 처음 상태로 돌리고 지금 방 적을 다시 만들어 둔다. 싸우던 방의 입구 패널은 풀어 다시 들어갈 수 있게 한다(문은 닫힌 채).
        /// 깬 방의 문은 그대로 둔다.
        /// </summary>
        public void ResetUnclearedRooms()
        {
            if (_rooms == null) return;

            foreach (SMobRoom room in _rooms)
            {
                if ((room.Zone == null) || room.Zone.IsCleared) continue;

                if (room.Zone.IsStarted) SetPanelsLocked(room.EntrancePanels, false);
                room.Zone.ResetIfNotCleared();
            }

            PrepareCurrentRoom();
        }
        #endregion

        #region Private Methods
        // 아직 못 깬 첫 방. 다 깼으면 -1.
        private int FindCurrentRoomIndex()
        {
            if (_rooms == null) return -1;

            for (int i = 0; i < _rooms.Length; i++)
            {
                if ((_rooms[i].Zone != null) && !_rooms[i].Zone.IsCleared) return i;
            }

            return -1;
        }

        private void PrepareCurrentRoom()
        {
            int index = FindCurrentRoomIndex();
            if (index >= 0) _rooms[index].Zone.Prepare();
        }

        // zone 을 가진 방. 없으면 -1.
        private int FindRoomIndex(CombatZone zone)
        {
            for (int i = 0; i < _rooms.Length; i++)
            {
                if (_rooms[i].Zone == zone) return i;
            }

            return -1;
        }

        // 들어온 방의 입구를 닫고 입구 패널을 잠근다.
        private void HandleZoneStarted(CombatZone zone)
        {
            int index = FindRoomIndex(zone);
            if (index < 0) return;

            CloseDoors(_rooms[index].EntranceDoors);
            SetPanelsLocked(_rooms[index].EntrancePanels, true);
        }

        // 깬 방의 출구를 열고, 다음 방 적을 만들어 둔다.
        // 이 방의 출구가 다음 방의 입구이기도 하므로 깬 방만 처리한다 (앞 방 출구 패널을 다시 풀면 잠가 둔 입구가 풀린다)
        private void HandleZoneCleared(CombatZone zone)
        {
            int index = FindRoomIndex(zone);
            if (index < 0) return;

            OpenDoors(_rooms[index].Doors);
            SetPanelsLocked(_rooms[index].LockedPanels, false);

            PrepareCurrentRoom();
        }

        private void HandleZoneProgressChanged()
        {
            OnProgressChanged?.Invoke();
        }

        private static void OpenDoors(StateMover[] doors)
        {
            if (doors == null) return;

            foreach (StateMover door in doors)
            {
                if (door != null) door.Activate();
            }
        }

        private static void CloseDoors(StateMover[] doors)
        {
            if (doors == null) return;

            foreach (StateMover door in doors)
            {
                if (door != null) door.Deactivate();
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
