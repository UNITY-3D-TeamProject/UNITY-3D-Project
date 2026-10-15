# 08. 블루투스 모션 추가

- **우선순위**: 높음
- **상태**: 완료
- **선행 작업**: [07. 스킬 구현](07-skill-implementation.md) → [07-1. 블루투스 스킬](07-1-bluetooth-skill.md)

## 목표
블루투스 스킬(탈것 소환·탑승)을 쓸 때 재생할 플레이어 애니메이션을 추가한다.

## 현재 상황 (작업 전)
- 블루투스 스킬 구현 완료 ([07-1](07-1-bluetooth-skill.md)). 탑승 상태는 `SpawnVehicle.IsRiding` 이고, 탑승·하차 시 `SkillMediator` 가 `OnBuffRatiosChanged` 를 발행한다. 애니메이션 쪽으로 가는 탑승 상태 알림 경로는 없었다.
- 탑승 중 사격·구르기·멀티점프가 모두 가능하다. 탈것은 임시 큐브(`Prefabs/Object/BluetoothVehicle.prefab`)다.

## 열린 질문
- [x] 필요한 모션 구성 → **탑승 시 점프 후 탑승 루프**. 하차 모션은 따로 없이 기존 Idle/Locomotion 으로 블렌드한다.
  - (사용자 피드백으로 변경) 처음에는 애니메이션만 재생했으나, 지상에서 탑승하면 **모터로 실제로 작게 뛰어오르도록** 바꿨다 (`SpawnVehicle._mountJumpPowerRatio` 0.15, 버프 적용 후 약 0.5m, 체공 약 0.56초). 공중 탑승 시에는 뛰지 않는다 (추가 점프 방지).
  - 버프·배터리 소모는 F 입력 즉시 적용한다 (07-1 로직 유지).
  - (사용자 피드백) 탈것 오브젝트는 **착지할 때 생성**한다. 점프 중 탑승도 착지 시 생성. 구르는 중이거나 점프 없이 떨어지는 중이면 착지 알림이 없으므로 즉시 생성.
- [x] 사용할 애니메이션 클립
  - 탑승 점프: **37 `Anim_ZMIKE_JumpStart.fbx : Jump_Start`** (몸이 바로 뜨므로 웅크림을 건너뛰는 Offset 0.6)
  - 탑승 공중: **30 `Anim_ZMIKE_JumpApex.fbx : Jump_Apex`**
  - 탑승 중 하체: **11 `Anim_ZMIKE_Blinking.fbx : Blinking`** (2.5초 루프, 전신, 제자리. `MikeZ (11)` → `Animator_Mike_Blinking.controller`)
  - 탑승 중 상체: **54 `Anim_ZMIKE_IdleAggro.fbx : Idle_Aggro`** (사용자 피드백)
- [x] 탑승 중 기존 이동 블렌드 → **11번으로 고정**한다. 탑승 중 이동·점프·구르기를 해도 Riding 을 유지한다.
- [x] 탑승 중 상체 레이어(사격) → 사격·차지·조준 중에는 Fire/FireHold 가 덮고, 끝나면 54번(RidingUpper)으로 바로 돌아온다.
- [x] (사용자 피드백) 보드 타는 느낌 → 탑승 자세에서 **하체를 보드 방향 기준 왼쪽 90°** 로 돌린다(스탠스). 상체는 시점 정면 유지. 탑승 점프 중에는 돌리지 않는다.
- [x] (사용자 피드백) 보드 방향 → **보드와 하체가 이동 방향을 따라 돈다**.
  - 목표 방향: 이동 방향이 시점 앞쪽 반원(±90° 포함)이면 이동 방향, 뒤쪽 반원이면 반대 방향(앞을 본 채 뒤로 미끄러짐). → W 0°, W+D 45°, D 90°, S+D -45°, S 0°, S+A 45°, A -90°, W+A -45°.
    - 하체 꺾임이 최소가 되는 쪽을 고르는 규칙(R2)으로 먼저 구현했으나, 사용자 플레이 피드백으로 위 규칙(R1)으로 바꿨다. A 에서는 하체가 시점 대비 180° 꺾인다(사용자 수용).
  - 입력이 없으면 마지막 방향 유지, 회전 속도 720°/초.
  - 구르는 중에는 구르기 방향. 탑승 중 뒤쪽 구르기(대쉬)는 몸·보드가 뒤를 보지 않고 앞쪽(반대 방향)을 본다. 실제 구르기 이동 방향은 그대로.
  - (중간 시도) 탈것을 `SK_ZMike` 에 붙여 몸 정면을 따라 돌게 한 방식(`CharacterRotator.AttachToBody`)은 이 방식으로 바뀌며 제거했다.

## 결과 (2026-10-14)
### 흐름
1. F키 탑승 → `SkillMediator.OnSkillExecuted` 가 `OnBuffRatiosChanged`, `OnRidingChanged(true)` 를 발행한다.
   - `AnimationMediator.NotifyRidingChanged` → `CharacterAnimator.SetRiding`: `IsRiding` 켜고 `Mount` Trigger. 보드 방향은 몸 정면에서 시작.
   - `BeginMount`: 지상이고 구르는 중이 아니면 `OnMountJumpRequested` → `MoveMediator.CommandAirJump(0.15)`. 점프 중이면 착지를 기다린다. 그 외에는 바로 탈것 생성.
2. 착지(`SkillMediator.NotifyLanded`) → `SpawnVehicle.AttachVehicle()` 로 탈것 생성 → `OnVehicleAttached(Transform)` → `AnimationMediator.SetRidingBoard`.
3. Animator Base Layer: Any State → MountJump(37) → 공중이면 MountAir(30) → `IsJumping` false 면 Riding(11).
4. Riding 자세가 되면 `CharacterAnimator` 가 `IsRidingPose` 를 켠다.
   - UpperBody 레이어 Empty → RidingUpper(54).
   - `LateUpdate` 에서 골반을 "보드 방향 − 몸 정면 + 스탠스(-90°)" 만큼 돌리고 `spine_01` 을 원래 월드 회전으로 되돌린다.
   - 탈것 회전을 보드 방향으로 덮어쓴다.
5. 하차(F, 배터리 0, 사망 등) → `OnRidingChanged(false)`, `OnVehicleAttached(null)`. Riding → 공중이면 JumpAir, 아니면 Locomotion/Idle. RidingUpper → Empty.

### Animator Controller (`PlayerAnimator.controller`)
- 파라미터: `IsRiding`(Bool), `Mount`(Trigger), `IsRidingPose`(Bool)
- Base Layer
  | From → To | 조건 | Exit Time | 전환 시간 |
  |---|---|---|---|
  | Any State → MountJump | `Mount`, Offset 0.6 | 없음 | 0.1 |
  | MountJump → Riding | `IsRiding`, !`IsJumping` | 0.9 | 0.05 |
  | MountJump → MountAir | `IsRiding`, `IsJumping` | 0.9 | 0.05 |
  | MountAir → Riding | `IsRiding`, !`IsJumping` | 없음 | 0.1 |
  | MountJump / MountAir / Riding → JumpAir | !`IsRiding`, `IsJumping` | 없음 | 0.1 |
  | MountJump / MountAir / Riding → Locomotion | !`IsRiding`, !`IsJumping`, `IsMoving` | 없음 | 0.1 |
  | MountJump / MountAir / Riding → Idle | !`IsRiding`, !`IsJumping`, !`IsMoving` | 없음 | 0.1 |
- UpperBody 레이어
  | From → To | 조건 | 전환 시간 |
  |---|---|---|
  | Empty → RidingUpper | `IsRidingPose`, !`IsFiring` | 0.15 |
  | RidingUpper → Fire / FireHold | `Fire` / `IsFiring` | 0.05 / 0.15 |
  | Fire / FireHold → RidingUpper (Empty 전환보다 먼저 검사) | !`IsFiring`, `IsRidingPose` | 0.15 |
  | RidingUpper → Empty | !`IsRidingPose` | 0.15 |
- 탑승 중 점프·구르기는 `CharacterAnimator` 가 `IsJumping`/`IsRolling` 만 갱신하고 Trigger 를 보내지 않아 Riding 을 유지한다.

### 변경 파일
- `Scripts/Skill/Skills/SpawnVehicle.cs`: 탈것 생성을 `AttachVehicle()` 로 분리, `Vehicle`, `MountJumpPowerRatio`(`_mountJumpPowerRatio` 0.15)
- `Scripts/Mediator/SubMediators/SkillMediator.cs`: `OnRidingChanged`, `OnMountJumpRequested`, `OnVehicleAttached`, `BeginMount`, 착지 시 탈것 생성, 점프·구르기 상태 기록
- `Scripts/Mediator/CharacterMediator.cs`: 위 이벤트 연결, 탑승 중 뒤쪽 구르기 방향 변환(`GetRollLookDirection`)
- `Scripts/Mediator/SubMediators/AnimationMediator.cs`: `NotifyRidingChanged`, `SetRidingBoard`, 구르기 방향 전달
- `Scripts/CharacterAnimation/CharacterAnimator.cs`: `SetRiding`, `SetRidingBoard`, `PlayRoll(Vector3)`, 보드 방향 계산, 하체 회전, `IsRidingPose`. Inspector: `_ridingLowerBodyYaw`(-90), `_ridingTurnBlendTime`(0.15), `_boardTurnSpeed`(720), `_bodyName`(`SK_ZMike`)
- `Animations/Player/PlayerAnimator.controller`: 위 파라미터·상태·전환
- 프리팹 변경 없음

### 검증 (플레이 모드, `Map_1_Lobby`)
- 탑승: MountJump → MountAir → 착지 후 Riding. 높이 약 0.51m, 체공 약 0.56초. 공중에서는 탈것 없음, 착지 후 생성. 점프 중 탑승도 착지 후 생성.
- 탑승 중 점프·구르기에도 Riding 유지. 공중 하차 시 JumpAir, 지상 하차·배터리 고갈 시 Idle.
- 상체: 착지 후 RidingUpper, 사격 시 Fire → 끝나면 RidingUpper, 하차 시 Empty.
- 하체: 보드 방향 기준 왼쪽 90°, 가슴·머리는 정면 유지.
- 보드 방향: 8방향 목표값 일치(계산 함수 직접 호출. 동시 키 입력은 테스트 입력이 유지되지 않음). A, W+A, D, S 는 실제 키 입력으로도 확인. 탑승 중 S + Shift 구르기 시 몸 고정 방향이 앞(0°).
- 이동 방향 동작은 사용자가 플레이로 확인.
- 화면 품질(골반 한 곳에서 꺾는 모습, A 이동 시 180° 꺾임)은 확인하지 못했다.

### 남은 것
- 탈것은 임시 큐브다. 메쉬를 교체할 때 보드 방향 기준(긴 축 = 진행 방향)에 맞춘다.
- 하체 회전은 골반 한 곳에서 한다. 부자연스러우면 여러 뼈로 나눠 돌리는 것을 검토한다.
- 보드 회전은 연출용으로 `CharacterAnimator` 가 맡는다. 탈것에 충돌·물리가 생기면 위치를 옮겨야 한다.
- `docs/player-animation-reference.md` 는 `eecd50d` 에서 삭제된 뒤 복원되지 않아, `player-animation` 스킬의 문서 갱신 단계는 하지 못했다.

## 완료 기준
- 플레이 모드에서 블루투스 스킬 발동 시 모션 재생 확인.
