# 07-1. 블루투스 스킬

- **우선순위**: 높음
- **상태**: 완료
- **상위 작업**: [07. 스킬 구현](../07-skill-implementation.md)
- **후속 작업**: [08. 블루투스 모션 추가](../08-bluetooth-motion.md)

## 목표
블루투스 스킬로 탈것을 소환해 탑승하고, 탑승 중 버프와 배터리 지속 소모가 동작하도록 구현한다.

## 현재 상황
- 기존 `SpawnVehicle` 스킬이 블루투스다. `Debug.Log` 만 찍는 빈 껍데기다 (`Assets/_Project/Scripts/Skill/Skills/SpawnVehicle.cs`).
- 배터리 어트리뷰트 `CurrentBattery` / `MaxBattery` 가 있고 HUD 에 표시된다. `ResourceCost` 는 발동 1회당 고정량 지불만 지원한다 (사용 중 지속 소모 없음).

## 확정 사항
- 탈것을 소환해 탑승한다. 탑승 중 이동속도 증가 + 점프 파워 증가 + 중력 감소.
- 사용하는 동안 배터리를 계속 소모한다.

## 작업 범위 (예상)
- 탈것 소환·탑승, 스탯 버프 적용/해제, 사용 중 배터리 지속 소모.

## 열린 질문
- [x] 이동속도·점프 파워·중력 변화량 → **배율**. 이동속도 ×1.5 (6→9), 점프력 ×1.25 (8→10), 중력 ×0.7 (20→14). Inspector 노출.
- [x] 버프 적용 방식 → **어트리뷰트를 건드리지 않는다**. 탑승 상태를 `SkillMediator` → `CharacterMediator` → `MoveMediator` 로 전달하고, `MoveMediator` 가 모터에 넘기는 속도·점프력·중력에 배율을 곱한다.
  - 이유: `MoveSpeedReducer` 가 `MoveSpeed` 어트리뷰트를 직접 빼고 되돌리므로 겹치면 복구값이 어긋나고, `PlayerState.SaveCurrentAttributes` 가 탑승 중 버프값을 저장해 버릴 수 있다.
  - 영향: HUD·`MapTestHud` 의 `MoveSpeed` 표시에는 버프가 보이지 않는다.
- [x] 배터리 소모 → **초당 10** (가득 찬 상태에서 10초). 지속 소모용 코스트 `DrainCost`(초당 소모량)를 새로 만들고, 탑승 중 스킬이 매 프레임 소모를 요청한다. 탑승 조건은 **배터리 ≥ `DrainCost` 의 초당 소모량**.
- [x] 해제 조건 → 같은 키(F) 재입력 시 하차 (쿨타임·조건 무시), 배터리 0 이면 강제 하차, 사망·스킬 비활성화 시 하차. **피격 시에는 하차하지 않는다**.
- [x] 탈것 모델 → `Prefabs/Object/` 에 **임시 큐브 프리팹**을 만들어 사용한다 (나중에 메쉬 교체 예정).
- [x] 탑승 중 조작 → 사격·구르기·멀티점프 모두 **가능**.
- [x] 입력 키 / 쿨타임 / 코스트 → **F키 (기존 바인딩) / 하차 후 3초 / 발동 코스트 없음 (지속 소모만)**
- [x] 배율 전달 구조 → 배율 3개를 따로 보내지 않고 **(대상, 배율) 구조체 목록**을 보낸다. `CharacterMediator` 가 대상별로 필요한 중재자에 전달한다.
- [x] `DrainCost` 소모 → **코루틴**으로 처리한다 (`SpawnVehicle` 에 `Update` 없음).
- [x] HUD 쿨타임 표시 → 탈것 스킬은 **하차할 때만** `OnSkillUsed` 를 발행한다 (강제 하차 포함).

## 결과 (2026-10-14)
### 흐름
1. F키 → `TryExecuteSkill("SpawnVehicle")`. `DrainCost.CanPay` 는 소모 중이 아니면 `배터리 ≥ 초당 소모량(10)` 을 요구한다. 소모 중이면 항상 통과해 하차 요청을 허용한다.
2. 탑승(`SpawnVehicle.Mount`): 플레이어 루트 아래에 `_vehiclePrefab` 생성 → `DrainCost.StartDrain()` → `base.Execute()` 로 알림.
   - `SkillMediator.OnSkillExecuted` 가 `OnBuffRatiosChanged(BuffRatios)` 를 발행한다.
   - `CharacterMediator` 가 `EBuffTarget` 별로 `MoveMediator.SetMoveSpeedRatio` / `SetJumpPowerRatio` / `SetGravityRatio` 를 호출한다.
   - `MoveMediator` 는 어트리뷰트 값에 배율을 곱해 모터에 넘긴다. 중력은 `Awake` 에서 저장한 기본값에 곱한다.
3. 하차(`SpawnVehicle.Dismount`)는 다음 경우에 일어난다.
   - F키 재입력
   - `DrainCost.OnDepleted` (배터리 0)
   - `OnDisable`
   - `Stop()` (사망 시 `StopAllSkills`)
   - 하차하면 소모를 멈추고, 탈것을 제거하고, `PayCosts()` 로 쿨타임 3초를 시작한 뒤 알린다. 배율 1 목록이 전달되어 버프가 원복되고, `OnSkillUsed` 가 이때 발행된다.

### 변경 파일
- 추가: `Scripts/Skill/Buff/SBuffRatio.cs` (`EBuffTarget`, `SBuffRatio`), `Scripts/Skill/Costs/DrainCost.cs`
- 수정: `Scripts/Skill/Skills/SpawnVehicle.cs`, `Scripts/Mediator/SubMediators/SkillMediator.cs` (`OnBuffRatiosChanged`), `Scripts/Mediator/SubMediators/MoveMediator.cs`, `Scripts/Mediator/CharacterMediator.cs`
- 프리팹
  - `Prefabs/Skills/SpawnVehicle.prefab`: `DrainCost` 추가, `CooldownCost` 1 → 3, 탈것 연결
  - `Prefabs/Object/BluetoothVehicle.prefab` (신규): 임시 큐브, Collider 없음

### 검증 (플레이 모드, `Map_1_Lobby`)
- 탑승 시 이동속도 6→9, 중력 20→14, 점프 배율 1.25. `MoveSpeed` 어트리뷰트는 6 유지.
- 배터리 초당 10 소모, 0 이 되면 강제 하차·버프 원복. 하차 후 3초 쿨타임, HUD 는 하차 시점 기준으로 표시.
- 배터리 9.9 → 탑승 불가, 10 → 가능. 하차 후 탈것 잔존 없음.
- 탑승 중 사격·구르기·멀티점프, 점프 높이 체감은 사용자가 확인.

### 남은 것
- 탈것은 임시 큐브다. 메쉬를 교체할 예정이다.
- `PlayerFacade` 에는 탑승 상태 이벤트가 없다. 08(모션)에서 필요하면 추가한다.
- HUD·`MapTestHud` 의 `MoveSpeed` 표시에는 탑승 버프가 반영되지 않는다 (의도).

## 완료 기준
- 플레이 모드에서 탑승·버프·배터리 소모·하차가 의도대로 동작. 컴파일 에러 없음.
