# [Codex 작업 요청] 보안 앱 라운드 로직을 Map_4_Security에 적용

요청자: 김성준 · 작성일: 2026-10-21 · 브랜치: `feature/system/stage-rounds`
관련: [intent-057](../../../intent/intent-057-security-app-rounds.md), [작업 기록](./security-app-rounds.md), 커밋 `adee1a19`(코드·프리팹), `59b19ea5`(문서)

## 1. 한 줄 요약
보안 앱 라운드 로직(코드)은 이미 만들어져 커밋돼 있다. **`Map_4_Security.unity` 씬에 오브젝트를 배치하고 인스펙터를 연결하는 일만** 해 달라. 코드는 고치지 않는다.

## 2. 지켜야 할 것
- 작업 전에 `CLAUDE.md`(또는 `AGENTS.md`), `docs/coding-convention.md`, `docs/commit-convention.md`를 읽는다.
- **수정 범위는 `Assets/_Project/Scenes/Map_4_Security.unity`(필요하면 NavMesh 에셋)뿐이다.**
  - 스크립트(`.cs`)는 수정하지 않는다. 로직이 부족하면 멈추고 요청자에게 알린다.
  - `Canvas_InGame` 프리팹은 이미 보스 체력바가 들어가 있으니 수정하지 않는다.
  - 다른 씬(`Map_0_Tutorial`, `Map_3_File` 등)은 열지도, 수정하지도 않는다.
- 판단이 애매한 곳(아래 "6. 확인 필요")은 추측하지 말고 요청자에게 묻는다.
- 커밋·push는 요청자가 따로 말하기 전에는 하지 않는다.

## 3. 만들어진 구조 (읽기용)
```
StageManager        스테이지 전체 진행·체크포인트·사망 복구 (기존, 수정 없음)
└ SecurityApp       (Core/Stage/SecurityApp.cs) 씬의 SecurityApp 오브젝트에 이미 붙어 있음
   │                라운드 3개를 일반 클래스 필드로 들고 인스펙터에서 설정.
   │                지금 라운드 번호로 분기해 사망 리셋·라운드 클리어 판정.
   ├ 1라운드 · 잡몹 방      (MobRoomRound)   방을 깨면 출구 문짝 StateMover.Activate, 깨기 전에는 출구 패널 콜라이더를 꺼 둠
   ├ 2라운드 · 특수몹 발판  (SpecialMobRound) 발판을 깨면 꺼 두었던 다음 발판을 켬
   └ 3라운드 · 보스         (BossRound)       BossFightSpawner.OnBossDefeated → 꺼 두었던 탈출 문을 켬
CombatZone          (Core/Stage/Security/CombatZone.cs) 씬 컴포넌트.
                    플레이어가 트리거에 들어오면 [몬스터 데이터 + 마리 수]대로 적 생성(ObjectPoolManager 사용),
                    만든 적이 모두 죽으면 클리어. 클리어 전 사망 시 그 구역만 리셋.
보스 체력바          Canvas_InGame/BossHealth (이미 프리팹에 있음, 씬의 BossEncounter를 자동으로 찾음. 할 일 없음)
```
- 라운드 번호: 0 = 1라운드(잡몹 방), 1 = 2라운드(특수몹 발판), 2 = 3라운드(보스). StageManager `Round Checkpoint Indices`가 이미 [0, 1, 2], `BossFightSpawner._activeRoundIndex`는 2로 되어 있다.
- 적 프리팹(Melee·Ranged·Special)은 `ObjectPoolManager` 프리팹에 이미 등록돼 있다.

## 4. 맵 현황 (요청자가 씬 파일에서 확인한 것)
- `Stage_BossRun/03_Round1_Battle/Modules`: 1라운드 방 모듈. 문은 `SpaceStation_Doors_A/B/B_Glass`·`SpaceStation_Room_A_Door` 프리팹이다.
  - 현재 문 9곳이 "패널 상호작용(E) → 문 열기"로 연결돼 있다(양준범 커밋 `b2c2d5d1`).
  - 연결 방식: 패널(`SpaceStation_Panel_A`)의 `InteractButton.On Interacted` → 문짝(`Door_Left`/`Door_Right` 등)의 `StateMover.Activate`. InteractButton 14개, StateMover 11개.
- 디자인 의도(요청자 그림 기준): 1라운드는 잡몹 방 4개(작은 방 2개 → L자 통로형 구역 → 큰 방). `Checkpoint_Round1_Start`에서 `Checkpoint_Round2_Start` 쪽으로 진행한다.
- `Stage_BossRun/04_Platforms`: `Platform_1`~`Platform_6`. 2라운드 구간이다. `Checkpoint_Round3_Start`가 `Platform_5`에 있다.
- `Stage_BossRun/05_Boss/BossFight`: `BossFightSpawner`·`BossEncounter`가 있다. 아레나 = `BossArena`.
- 씬 루트에 `MonsterSpawner`, `MonsterSpawner (1)`~`(4)`가 있다. 이 스포너들은 씬 시작(Start) 때 바로 적을 만든다.
- NavMesh 에셋은 `NavMesh-03_Round1_Battle`, `NavMesh-05_Boss`만 있다. **`04_Platforms`에는 NavMesh가 없을 수 있다.**

## 5. 할 일
### 5-1. 1라운드 잡몹 방 4개
1. 방마다 빈 오브젝트를 만든다(예: `03_Round1_Battle/CombatZones/Room_1` ~ `Room_4`).
   - `BoxCollider` 하나로 방 바닥 전체를 덮는다. 높이는 플레이어 키보다 넉넉하게 둔다. `Is Trigger`는 코드가 자동으로 켠다.
   - 입구 문 바로 안쪽까지 덮어서, 문을 지나 들어오면 바로 닿게 한다. 이웃 방과 겹치지 않게 한다.
2. 각 구역에 `CombatZone`을 붙인다.
   - `Enemies`: `Monster`에 `ScriptableObjects/MonsterData/SOMonsterData_Melee`/`_Ranged`, `Count`에 마리 수. 마리 수는 6번 참고.
   - `Spawn Points`: 방 안의 빈 오브젝트 2~4개를 만들어 연결한다. 반드시 NavMesh 위, 입구에서 떨어진 곳에 둔다.
3. `SecurityApp` 인스펙터 → `1라운드 · 잡몹 방` → `Rooms`에 방 4개를 진행 순서대로 넣는다.
   - `Zone`: 그 방의 `CombatZone`
   - `Doors`: 그 방의 **출구** 문짝 `StateMover`. 그 문 패널의 `InteractButton.On Interacted`에 걸려 있는 대상을 그대로 넣는다(보통 문짝 2개).
   - `Locked Panels`: 그 출구 문의 패널 `InteractButton`(문 양쪽에 있으면 둘 다)
   - **입구 문은 넣지 않는다.** 플레이어가 기존처럼 패널(E)로 열고 들어온다.

### 5-2. 2라운드 특수몹 발판
1. 특수몹이 서 있을 발판마다 구역 오브젝트 + `BoxCollider` + `CombatZone`을 만든다.
   - `Enemies`: `SOMonsterData_Special`, `Count` 1
   - `Spawn Points`: 그 발판 위 한 곳
2. `SecurityApp` → `2라운드 · 특수몹 발판` → `Steps`에 진행 순서대로 넣는다.
   - `Zone`: 발판 구역
   - `Next Platform`: 이 발판을 깨면 나타날 다음 발판(`Platform_N` 루트). 스테이지가 시작될 때 코드가 끈다.
   - 플레이어가 처음 밟는 발판(2라운드 시작 발판)과 `Checkpoint_Round3_Start`가 있는 `Platform_5`가 사라지면 안 되는지 확인한다. 어느 발판을 숨길지는 6번 참고.
3. 특수몹이 움직이려면 발판에 NavMesh가 필요하다. `04_Platforms`에 NavMesh가 없으면 `NavMeshSurface`를 추가해 굽는다.
   - 숨길 발판도 **켜진 상태로** 굽는다.
   - 굽는 방식은 `05_Boss`의 NavMeshSurface 설정을 참고한다.

### 5-3. 3라운드 보스
1. `SecurityApp` → `3라운드 · 보스` → `Boss Fight Spawner`에 `05_Boss/BossFight`의 `BossFightSpawner`를 연결한다.
2. 탈출 문 오브젝트를 만들어 `Exit Door`에 연결한다. 스테이지가 시작될 때 코드가 끄고, 큰 보스(A)가 쓰러지면 켠다.
   - 씬 전환이 필요하면 탈출 문 안에 `ScenePortal`(트리거 콜라이더, `Destination`)을 둔다. 목적지는 6번 참고.

### 5-4. 기존 MonsterSpawner 5개
- 구역 방식과 별개로 씬 시작 때 적을 만들기 때문에, 1라운드 방 클리어 판정에 들어가지 않는 적이 생긴다.
- **삭제·비활성화 전에 요청자에게 묻는다.**

## 6. 확인 필요 (요청자에게 물을 것)
1. 방별·발판별 적 수. 정해 주지 않으면 **임시값**으로 넣고 기록에 "임시"라고 남긴다.
   - 작은 방 1·2: 근접 3
   - L자 구역 3: 근접 3 + 원거리 1
   - 큰 방 4: 근접 4 + 원거리 2
   - 발판마다: 특수몹 1
2. 2라운드에서 특수몹을 놓을 발판과 숨겼다가 나타나게 할 발판(`Platform_1`~`6` 중 어느 것)
3. 탈출 문의 위치와 목적지 씬(`ESceneType`)
4. 루트 `MonsterSpawner` 5개를 지울지 끌지 남길지

## 7. 검증 기준
- Unity 컴파일 에러 0, 씬을 열었을 때 Missing Script·Missing Reference 없음.
- Play에서 다음을 확인한다.
  - 시작 직후: 각 방 출구 패널을 조준해도 `[E] 상호작용`이 안 뜬다. 숨긴 발판과 탈출 문이 안 보인다.
  - 방에 들어가면 정한 수만큼 적이 생기고, 다 잡으면 출구 문이 열리고 패널이 다시 조준된다.
  - 방에서 일부만 잡고 죽으면 1라운드 시작점에서 다시 시작하고, 그 방 적이 다시 처음 수로 생긴다. 이미 연 문은 열려 있다.
  - 발판 특수몹을 잡으면 다음 발판이 나타나고 건너갈 수 있다. 특수몹이 NavMesh 밖 경고 없이 움직인다.
  - 보스전: 위쪽 체력바가 때릴 수 있는 보스로 바뀐다(1-1 필드 보스 → 그로기 거대 보스 → 1-2 필드 보스 → 최종 거대 보스). 큰 보스가 쓰러지면 체력바가 사라지고 탈출 문이 나타난다.
- 직접 플레이로 확인하지 못한 항목은 "미확인"으로 기록에 남긴다.

## 8. 작업이 끝나면
- `docs/HANDOVER/SungJun/Codex/2026-10-21/`에 작업 기록(작업 요약, 결정 사항, 현재 상태·이슈, 다음 할 일)을 쓰고 Codex `HANDOFF_POINTER.md` 맨 위에 한 줄을 추가한다.
- intent-057의 "진행 상황"에 맵 연결 결과를 한 줄 추가한다.
