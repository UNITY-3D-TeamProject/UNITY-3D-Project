# 플레이어 애니메이션 작업 참고 사항

플레이어 애니메이션 작업 시 사용자가 정한 규칙을 기록한다.
작업 절차는 `.claude/skills/player-animation/SKILL.md` 스킬에 있다. 이 문서는 사실 정보(구조, 결정, 현황)만 다룬다.

## 1. 사용 에셋
- Player는 `Assets/ThirdParty/Tacko3D/` 폴더의 에셋을 사용한다.
- `Assets/ThirdParty/`는 Git에 올라가지 않으므로, 공유가 필요한 결과물(Animator Controller, 스크립트 등)은 `Assets/_Project/` 아래에 만든다.

## 2. 애니메이션 번호 규칙
- 참고 씬: `Assets/ThirdParty/Tacko3D/RadicalRobotsMike/Demo/ZMike_Demo.unity`
- 사용자가 애니메이션을 **번호(N)** 로 알려주면, 위 씬에 있는 `MikeZ (N)` 오브젝트가 사용하는 애니메이션을 뜻한다.
  - 예: "3번" → `MikeZ (3)`가 재생하는 애니메이션
- 씬 오브젝트 이름은 `MikeZ`, `MikeZ (1)`, `MikeZ (2)` … 형식이다.
  - 번호 없는 `MikeZ`가 0번인지는 확인되지 않았다. 필요하면 사용자에게 확인한다.
  - 일부 번호는 씬에 없다(예: 12~14, 16). 없는 번호를 받으면 임의로 추측하지 말고 사용자에게 확인한다.

## 3. 캐릭터 구조

### 3-1. Player 프리팹 (`Assets/_Project/Prefabs/Player/Player.prefab`)
```text
Player  [Animator, PlayerInput, CharacterController, PlayerInputComponent, CharacterMediator, PlayerFacade]
├── MikeZ            [SkinnedMeshRenderer]   ← 외형 메시
├── SK_ZMike/root/…  ← 뼈대 (루트 Animator가 구동)
├── BaseCamera / AimCamera / CameraPivot
├── LookPrefab       [CameraMediator, PlayerBaseCamera]
├── MovePrefab       [MoveMediator, CharacterMotor, MoveDirectionCalculator, RollMover]
├── CombatPrefab     [CombatMediator, CharacterCombat]
├── SkillPrefab      [SkillMediator, SkillController]  ← 자식: DoubleJump, Fire, Interact, Roll, Scan, SpawnVehicle
├── RotatorPrefab    [RotateMediator, CharacterRotator]
├── SpawnPrefab      [SpawnMediator, BulletFactory]
├── AttributePrefab  [AttributeSet, AttributeMediator, AttributeClamper, AttributeRegenerator]
└── AnimationPrefab  [CharacterAnimator, AnimationMediator]
```
- `XxxPrefab` 기능 오브젝트는 모두 `Assets/_Project/Prefabs/Actions/XxxPrefab.prefab`을 Player 안에 **중첩 프리팹(Nested Prefab)** 으로 넣은 것이다. 기능 컴포넌트 구성을 바꿀 때는 Player가 아니라 `Prefabs/Actions/`의 원본 프리팹을 수정한다.
- Player 프리팹에서는 루트 컴포넌트와, 루트에서 중첩 프리팹 내부를 가리키는 참조(예: `CharacterMediator`의 Mediator 필드)만 다룬다.

### 3-2. Animator 현황
- Animator는 Player **루트**에 있다.
- 리그는 **Generic**(Humanoid 아님)이고 Avatar는 `RadicalRobotsMike/Art/Mesh/MikeZ.fbx`이다. Root Motion은 꺼져 있다. 이동은 `CharacterMotor`가 처리한다.
- 컨트롤러는 `Assets/_Project/Animations/Player/PlayerAnimator.controller`이다(5-2 참고). 작업 전에는 ThirdParty의 `Animator_Mike_BusterIdle_R`(Idle 상태 1개)이 연결되어 있었다.

### 3-3. 코드 흐름 (Mediator 패턴)
- 입력: `PlayerInputComponent`가 `IMoveController`, `ICameraController`, `ISkillRequestController`를 구현한다. 각 Mediator가 `GetComponentInParent`로 찾아 콜백을 등록한다.
- 기능 프리팹마다 `XxxMediator`(`MediatorBase` 상속)와 실제 기능 컴포넌트가 함께 붙어 있다. Mediator는 `ResolveComponent`로 같은 오브젝트의 기능 컴포넌트를 찾는다.
- Mediator끼리 직접 참조하지 않는다. 루트의 `CharacterMediator`(`[DefaultExecutionOrder(100)]`)가 각 Mediator 이벤트를 구독해 서로 연결한다.

### 3-4. 애니메이션에 쓸 수 있는 기존 신호
| 신호 | 위치 |
|---|---|
| 이동 방향 / 속도 | `CharacterMotor.Direction`, `CharacterMotor.Speed` |
| 지면 접촉 | `CharacterMotor.IsGrounded` |
| 점프 시작 / 착지 | `CharacterMotor.OnJumpStarted`, `OnJumpEnded` |
| 구르기 시작 / 종료 | `RollMover.OnRollStarted`, `OnRollEnded` |
| 사격 | `SkillMediator.OnFireRequested` |
| 피격 / 사망 | `CombatMediator.OnHit`, `OnDeath` |

## 4. 애니메이션 구현 규칙 (사용자 확인 완료)
- **코드 구조**: 기존 Mediator 패턴을 따른다.
  - Player 아래에 애니메이션용 자식 프리팹을 두고, `MediatorBase`를 상속한 애니메이션 Mediator와 Animator를 다루는 컴포넌트를 함께 붙인다.
  - 다른 기능(Move, Skill, Combat 등)의 신호는 `CharacterMediator`가 구독해 애니메이션 Mediator로 전달한다.
- **Animator Controller**: `Assets/_Project/Animations/Player/`에 새로 만든다. ThirdParty 폴더의 애니메이션 클립은 참조만 하고, ThirdParty 에셋은 수정하지 않는다.
- 코드는 `docs/coding-convention.md`를 따른다. Animator 파라미터는 해시로 캐싱한다.
- "이동 중" 판단은 **이동 방향 입력** 기준이다. 벽에 막혀도 입력이 있으면 Run을 재생한다.
- **Play 테스트 씬**: 직접 플레이해서 확인해야 하면 `Assets/_Project/Scenes/Map_0_Tutorial.unity`를 열고 플레이한다. 참고 씬(`ZMike_Demo.unity`)에서는 플레이 테스트를 하지 않는다.
  - 테스트 씬으로 전환할 때 `ZMike_Demo.unity`에 저장되지 않은 변경이 있으면 저장하지 않고 버린다(ThirdParty 에셋은 수정하지 않는다).
  - 요청이 들어왔을 때 `ZMike_Demo.unity`가 Play 중이면 Play를 멈춰도 된다.

## 5. 구현 현황

### 5-1. 구조
```text
PlayerInputComponent ─(이동 입력)→ MoveMediator ─ OnMoveDirectionChanged(Vector3)
                                                   │
                         CharacterMediator 가 구독해 연결
                                                   ↓
                     AnimationMediator.SetMoveDirection(Vector3)
                                                   ↓
                     CharacterAnimator.SetMoving(bool)          → IsMoving
                     CharacterAnimator.SetMoveDirection(Vector3) → MoveX / MoveY (Update 에서 매 프레임 계산)

CameraMediator ─ OnViewForwardChanged → CharacterMediator.SendViewForward
                                         → AnimationMediator.SetViewForward → CharacterAnimator.SetViewForward (블렌드 기준 방향, 사격 상체 pitch)

CharacterMotor ─ OnJumpStarted / OnJumpEnded → MoveMediator.OnJumpStarted / OnJumpEnded
                → AnimationMediator.NotifyJumpStarted / NotifyJumpEnded → CharacterAnimator.PlayJump / PlayLand → Jump(Trigger), IsJumping

RollMover ─ OnRollStarted / OnRollEnded → MoveMediator.OnRollStarted(Vector3 구르기 방향) / OnRollEnded
                → AnimationMediator.NotifyRollStarted / NotifyRollEnded → CharacterAnimator.PlayRoll / StopRoll → Roll(Trigger), IsRolling
                → RotateMediator.LockLookDirection / UnlockLookDirection → CharacterRotator (구르는 동안 몸을 구르기 방향으로 고정)

SkillMediator ─ OnFireRequested(Vector3) → AnimationMediator.NotifyFire → CharacterAnimator.PlayFire → IsFiring, Fire(Trigger)
```
| 파일 | 역할 |
|---|---|
| `Scripts/CharacterAnimation/CharacterAnimator.cs` (namespace `CharacterAnimation`) | `GetComponentInParent<Animator>()`로 루트 Animator를 찾아 파라미터를 설정한다. 파라미터 해시는 내부 `AnimHash`에 둔다. 이동 방향은 월드 값으로 저장하고, `Update`에서 시점 방향(`SetViewForward`) 기준 로컬 방향으로 바꿔 `MoveX`/`MoveY`에 넣는다(`_moveDirectionDampTime`, 기본 0.1초). `PlayFire`로 `IsFiring`을 켜고 `Fire` Trigger를 보내며, 마지막 사격 후 `_fireHoldDuration`(3초)이 지나면 끈다. 사격 상체일 때 `LateUpdate`에서 `_aimBoneName`(`upperarm_r`)을 돌려 총신(`_muzzleName` = `FirePosition`의 부모 → `FirePosition`)이 조준점(`SetGetAimPoint`)을 향하게 한다(`_aimBlendTime` 0.15초로 켜고 끔). |
| `Scripts/Mediator/SubMediators/AnimationMediator.cs` | `MediatorBase` 상속. 다른 Mediator에서 온 값을 애니메이션 상태로 바꿔 `CharacterAnimator`에 전달한다. |
| `MoveMediator.OnMoveDirectionChanged` | 이동 방향이 다시 계산될 때마다 발생한다. 구르는 중에도 발생한다. |
| `MoveMediator.OnJumpStarted` / `OnJumpEnded` | `CharacterMotor`의 점프 시작 / 점프 후 착지 이벤트를 그대로 전달한다. |
| `MoveMediator.OnRollStarted(Vector3)` / `OnRollEnded` | `RollMover`의 구르기 시작 / 종료를 전달한다. 시작 시 `CommandRoll`에서 정한 구르기 방향을 함께 넘긴다. |
| `CharacterRotator.LockLookDirection` / `UnlockLookDirection` | 방향을 고정하고 즉시 돌린다 / 해제한다. 고정 중에는 시점 방향과 회전 입력을 무시하고, 해제 후에는 기존 회전 속도로 시점 방향에 복귀한다. |
| `CharacterMediator._animationMediator` | Move(이동·점프·구르기) → Animation, Move(구르기) → Rotate, Camera(시점) → Animation, Skill(사격) → Animation 이벤트를 연결한다. |

- 프리팹: `Assets/_Project/Prefabs/Actions/AnimationPrefab.prefab`(`CharacterAnimator` + `AnimationMediator`, 내부 참조 연결됨)을 Player 자식으로 중첩해 넣었다.
- 새 애니메이션을 추가하는 순서:
  1. 필요한 신호를 가진 Mediator에 이벤트가 없으면 이벤트를 추가한다.
  2. `CharacterMediator.Subscribe`/`Unsubscribe`에서 그 이벤트를 `AnimationMediator`에 연결한다.
  3. `AnimationMediator` → `CharacterAnimator` 메서드로 Animator 파라미터를 설정한다.

### 5-2. Animator Controller (`Assets/_Project/Animations/Player/PlayerAnimator.controller`)
| 파라미터 | 타입 |
|---|---|
| `IsMoving` | Bool |
| `MoveX` | Float (캐릭터 기준 좌(-1) / 우(+1)) |
| `MoveY` | Float (캐릭터 기준 뒤(-1) / 앞(+1)) |
| `IsFiring` | Bool (사격 상체 유지 중) |
| `Fire` | Trigger (발사 1회. `Fire_R`을 처음부터 1회 재생) |
| `Jump` | Trigger (점프 시작. Any State → JumpStart) |
| `IsJumping` | Bool (점프 시작 ~ 착지) |
| `Roll` | Trigger (구르기 시작. Any State → Roll) |
| `IsRolling` | Bool (구르기 시작 ~ 종료) |

| 레이어 | Weight | Blending | Mask |
|---|---|---|---|
| Base Layer | 1 | - | 없음 (전신) |
| UpperBody | 1 | Override | `UpperBodyMask.mask` (`spine_01` 이하 활성: 척추·팔·머리) |

| 상태 | 번호 | 클립 |
|---|---|---|
| Idle (기본) | 54 | `Anim_ZMIKE_IdleAggro.fbx : Idle_Aggro` |
| Locomotion | - | 2D Blend Tree `Locomotion` (Simple Directional, `MoveX`/`MoveY`) |
| JumpStart | 37 | `Anim_ZMIKE_JumpStart.fbx : Jump_Start` (0.5초) |
| JumpAir | 30 | `Anim_ZMIKE_JumpApex.fbx : Jump_Apex` (루프) |
| JumpLand | 29 | `Anim_ZMIKE_JumpEnd.fbx : Jump_End` (0.5초) |
| Roll | 42 | `Anim_ZMIKE_RunChase.fbx : Run_Chase` (0.9초 루프, **speed 2**) |

| Locomotion 자식 | 번호 | 클립 | 위치 | timeScale |
|---|---|---|---|---|
| 앞 | 41 | `Anim_ZMIKE_Run.fbx : Run` | (0, 1) | 1 |
| 뒤 | 47 | `Anim_ZMIKE_Walk_Back.fbx : Walk_Back` | (0, -1) | 1.5 |
| 왼쪽 | 48 | `Anim_ZMIKE_WalkLeft.fbx : Walk_Left` | (-1, 0) | 1.5 |
| 오른쪽 | 49 | `Anim_ZMIKE_WalkRight.fbx : Walk_Right` | (1, 0) | 1.5 |

- 모든 클립은 제자리 클립이다. Walk 계열은 Run 속도로 이동할 때 발이 미끄러져 보여서 1.5배속으로 재생한다(사용자 지정).
- 전환: Idle → Locomotion (`IsMoving` true), Locomotion → Idle (`IsMoving` false). 둘 다 Exit Time 없음, 전환 시간 0.1초. **Interruption Source: Source**: 이 전환 도중에도 Any State 전환(구르기·점프)이 바로 끼어들 수 있게 한다. None이면 0.1초 전환이 끝날 때까지 기다려 0.3초짜리 구르기가 0.16~0.23초 늦게 시작됐다.
- 구르기 전환 (전환 시간은 모두 0.1초, Exit Time 없음):
  | From → To | 조건 |
  |---|---|
  | Any State → Roll | `Roll` |
  | Roll → JumpAir | `IsRolling` false, `IsJumping` true |
  | Roll → Locomotion | `IsRolling` false, `IsJumping` false, `IsMoving` true |
  | Roll → Idle | `IsRolling` false, `IsJumping` false, `IsMoving` false |
- 점프 전환 (따로 적지 않은 전환 시간은 0.1초):
  | From → To | 조건 | Exit Time |
  |---|---|---|
  | Any State → JumpStart | `Jump` (자기 자신으로 전환 허용: 공중 재점프 시 처음부터). **Offset 0.6**: 37을 웅크림이 가장 깊은 0.3초 지점부터 재생 | 없음 |
  | JumpStart → JumpAir | (없음). 전환 시간 **0.05초** | 0.9 |
  | JumpStart / JumpAir → JumpLand | `IsJumping` false | 없음 |
  | JumpLand → Locomotion | `IsMoving` true | 없음 |
  | JumpLand → Idle | `IsMoving` false | 0.8 (클립 끝에서 0.1초 전) |

| UpperBody 상태 | 대상 | 클립 |
|---|---|---|
| Empty (기본) | - | 없음 (Base Layer 상체가 그대로 보인다) |
| Fire | `Prefab_MikeZ_Accessories (6)` | `Anim_ZMIKE_FireR.fbx : Fire_R` (0.67초, 1회 재생) |
| FireHold | - | `Fire_R`, speed 0 (첫 프레임 자세 유지) |

- `Fire_R`은 fbx 임포트 설정이 루프다. ThirdParty 에셋이라 바꾸지 않고, 상태 구성으로 1회 재생을 만든다.
- 전환:
  | From → To | 조건 | Exit Time | 전환 시간 |
  |---|---|---|---|
  | Empty → Fire | `Fire` | 없음 | 0.15초 |
  | Fire → FireHold | (없음) | 1.0 | 0 |
  | Fire → Fire | `Fire` (재발사 시 처음부터) | 없음 | 0.05초 |
  | FireHold → Fire | `Fire` | 없음 | 0.05초 |
  | Fire / FireHold → Empty | `IsFiring` false | 없음 | 0.15초 |

### 5-3. 변경 파일 목록
**조준 (오른팔 총신 IK + 총알 조준점 발사)**
| 구분 | 경로 | 내용 |
|---|---|---|
| 수정 | `Assets/_Project/Scripts/CharacterAnimation/CharacterAnimator.cs` | 시점 pitch 분배 → 총신 IK(`CaptureHoldReference`, `CalculateAimCorrection`). `_aimBoneNames` → `_aimBoneName`/`_muzzleName`, `_viewDirection` 제거, `SetGetAimPoint`/`ClearGetAimPoint` |
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/AnimationMediator.cs` | `SetGetAimPoint`/`ClearGetAimPoint` 전달 |
| 수정 | `Assets/_Project/Prefabs/Actions/AnimationPrefab.prefab` | `_aimBoneName` = `upperarm_r`, `_muzzleName` = `FirePosition` |
| 수정 | `Assets/_Project/Scripts/CameraControl/PlayerBaseCamera.cs` | `GetAimRay()` — 메인 카메라 화면 중앙 레이 (없으면 피벗 레이) |
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/CameraMediator.cs` | `GetAimPoint()`, `_aimMaxDistance`(100), `_aimLayers` |
| 수정 | `Assets/_Project/Prefabs/Actions/LookPrefab.prefab` | `_aimMaxDistance` 100, `_aimLayers` = Default Raycast Layers − Player. 저장 시 FormerlySerializedAs 필드가 새 이름으로 다시 저장됨(값 동일) |
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/SpawnMediator.cs` | `SetGetAimPoint` / `ClearGetAimPoint`, 발사 방향 = 조준점 − 생성 위치 |
| 수정 | `Assets/_Project/Scripts/Mediator/CharacterMediator.cs` | Camera `GetAimPoint` → Spawn, Animation 연결 / 해제 |

**구르기**
| 구분 | 경로 | 내용 |
|---|---|---|
| 수정 | `Assets/_Project/Scripts/Rotation/CharacterRotator.cs` | `LockLookDirection` / `UnlockLookDirection` 추가. yaw 계산·적용을 `CalculateTargetYaw` / `ApplyYaw`로 분리해 공유 |
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/RotateMediator.cs` | `LockLookDirection` / `UnlockLookDirection` 전달 |
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/MoveMediator.cs` | `OnRollStarted(Vector3)` / `OnRollEnded` 이벤트 추가. 기존 private 핸들러 이름을 `HandleRollStarted` / `HandleRollEnded`로 변경(이벤트 이름과 충돌) |
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/AnimationMediator.cs` | `NotifyRollStarted` / `NotifyRollEnded` |
| 수정 | `Assets/_Project/Scripts/Mediator/CharacterMediator.cs` | 구르기 이벤트 → Animation, Rotate 구독 / 해지 |
| 수정 | `Assets/_Project/Scripts/CharacterAnimation/CharacterAnimator.cs` | `PlayRoll` / `StopRoll`, 구르는 동안 조준 pitch 끔 |
| 수정 | `Assets/_Project/Animations/Player/PlayerAnimator.controller` | `Roll`/`IsRolling` 파라미터, Roll 상태, Idle↔Locomotion Interruption Source |

**점프**
| 구분 | 경로 | 내용 |
|---|---|---|
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/MoveMediator.cs` | `OnJumpStarted` / `OnJumpEnded` 이벤트 추가 (`CharacterMotor` 이벤트 전달) |
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/AnimationMediator.cs` | `NotifyJumpStarted` / `NotifyJumpEnded` 추가 |
| 수정 | `Assets/_Project/Scripts/Mediator/CharacterMediator.cs` | Move 점프 이벤트 → Animation 구독 / 해지 |
| 수정 | `Assets/_Project/Scripts/CharacterAnimation/CharacterAnimator.cs` | `PlayJump` / `PlayLand` |
| 수정 | `Assets/_Project/Animations/Player/PlayerAnimator.controller` | `Jump`/`IsJumping` 파라미터, JumpStart / JumpAir / JumpLand 상태 |

**상체 레이어 (사격)**
| 구분 | 경로 | 내용 |
|---|---|---|
| 수정 | `Assets/_Project/Scripts/CharacterAnimation/CharacterAnimator.cs` | `PlayFire`, 사격 유지 타이머, `LateUpdate` 척추 pitch 조준 |
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/AnimationMediator.cs` | `NotifyFire` 추가 |
| 수정 | `Assets/_Project/Scripts/Mediator/CharacterMediator.cs` | `OnFireRequested` → `AnimationMediator.NotifyFire` 구독 / 해지 |
| 수정 | `Assets/_Project/Animations/Player/PlayerAnimator.controller` | `IsFiring`/`Fire` 파라미터, `UpperBody` 레이어(Empty / Fire / FireHold) |
| 신규 | `Assets/_Project/Animations/Player/UpperBodyMask.mask` | 상체 Transform 마스크 |

**방향 이동 (Locomotion 2D Blend Tree)**
| 구분 | 경로 | 내용 |
|---|---|---|
| 수정 | `Assets/_Project/Scripts/CharacterAnimation/CharacterAnimator.cs` | `SetMoveDirection`, `Update`에서 `MoveX`/`MoveY` 설정, `_moveDirectionDampTime` |
| 수정 | `Assets/_Project/Scripts/CharacterAnimation/CharacterAnimator.cs` | `SetViewForward` (블렌드 기준 방향) |
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/AnimationMediator.cs` | `SetMoveDirection`에서 방향도 전달, `SetViewForward` 추가 |
| 수정 | `Assets/_Project/Scripts/Mediator/CharacterMediator.cs` | `SendViewForward`에서 `AnimationMediator`에도 시점 방향 전달 |
| 수정 | `Assets/_Project/Animations/Player/PlayerAnimator.controller` | `MoveX`/`MoveY` 추가, `Run` 상태 → `Locomotion` 2D Blend Tree |

**Idle / Run 적용**
| 구분 | 경로 | 내용 |
|---|---|---|
| 신규 | `Assets/_Project/Scripts/CharacterAnimation/CharacterAnimator.cs` | Animator 파라미터 설정 |
| 신규 | `Assets/_Project/Scripts/Mediator/SubMediators/AnimationMediator.cs` | 애니메이션 Mediator |
| 신규 | `Assets/_Project/Animations/Player/PlayerAnimator.controller` | Idle / Run 상태와 `IsMoving` 파라미터 |
| 수정 | `Assets/_Project/Scripts/Mediator/SubMediators/MoveMediator.cs` | `OnMoveDirectionChanged` 이벤트 추가 (`ApplyDirection`에서 발생) |
| 수정 | `Assets/_Project/Scripts/Mediator/CharacterMediator.cs` | `_animationMediator` 필드, Move → Animation 구독 / 해지 추가 |
| 신규 | `Assets/_Project/Prefabs/Actions/AnimationPrefab.prefab` | 애니메이션 기능 프리팹 (`CharacterAnimator` + `AnimationMediator`) |
| 수정 | `Assets/_Project/Prefabs/Player/Player.prefab` | `AnimationPrefab` 중첩 추가, `CharacterMediator._animationMediator` 연결, 루트 Animator 컨트롤러를 `PlayerAnimator`로 교체 |

### 5-4. 결정 사항
- 애니메이션 제어는 `AnimationMediator`(+ `CharacterAnimator`)가 맡는다. Player 루트에 별도 애니메이션 제어 스크립트를 추가하지 않는다.
- 스크립트 폴더와 네임스페이스는 `CharacterAnimation`이다. `Animation`으로 지으면 `UnityEngine.Animation` 타입과 이름이 겹친다. `CameraControl` 폴더와 같은 방식이다.
- TPS 이동: 몸 전체의 yaw는 `CharacterRotator`가 카메라 정면으로 맞추고, 다리는 캐릭터 기준 이동 방향 2D Blend Tree로 표현한다(언리얼의 Controller Yaw + 방향 BlendSpace 구성). 상체는 `UpperBody` 레이어(AvatarMask, 언리얼 Layered Blend per Bone 대응)로 분리한다.
- 상체 기본값은 Base Layer의 상체(Idle/Locomotion)를 그대로 쓴다. 사격 시작부터 `Fire_R` 상체를 쓰고, 3초 동안 사격이 없으면 원래대로 돌아간다(사용자 지정). `Fire_R`은 발사할 때마다 1회만 재생하고, 사이에는 첫 프레임 자세(FireHold)를 유지한다(사용자 지정).
- 사격 상체일 때만 카메라 방향을 본다. 좌우는 몸 회전(`CharacterRotator`)이 맞춘다. 조준 방식은 다음 순서로 바꿨다. ① spine_01~03에 시점 pitch 분배: 상체 전체가 휨. ② `upperarm_r` pitch만: `Fire_R` 자세의 총신이 정면보다 오른쪽 약 35°, 아래 약 15°를 향해 총알 방향과 약 38° 어긋남. ③ **총신 IK**(사용자 결정 B): `upperarm_r`를 돌려 총신이 조준점을 향하게 한다.
  - 반동 처리(사용자 선택): **유지 자세 기준 고정 보정**. 상체가 FireHold일 때 총구 위치·총신 방향을 `upperarm_r` 부모(`clavicle_r`) 기준으로 저장하고, 그 기준 자세가 조준점을 향하게 하는 회전을 현재 포즈에 더한다. 그래서 Fire 재생 중 반동은 살아 있고 유지 자세에서 정확히 맞는다. 기준을 저장하기 전(스폰 후 첫 Fire 재생 중)에는 현재 포즈로 보정한다.
  - 회전 계산: 총신 직선 위에서 어깨와 조준점 사이 거리만큼 떨어진 점을 2차 방정식으로 구한 뒤 조준점 쪽으로 돌린다(정확한 해). 반복 근사(FromToRotation 반복)는 팔 길이 0.75m에 조준점이 1.6m로 가까우면 8회 반복해도 9° 이상 남아서 쓰지 않았다.
  - 뼈·총구는 이름으로 찾는다(중첩 프리팹 원본에서 Player 뼈를 참조할 수 없음). `FirePosition`은 Fire 스킬이 `Buster_R` 뼈 아래에 둔 오브젝트라 이름에 의존한다.
  - 이동 중 상체 비틀림 보정(사용자 결정 A): UpperBody 마스크가 `spine_01`부터라 골반은 이동 클립을 따른다. 측면·후진 클립은 골반째 이동 방향으로 돌아가서 상체가 함께 돌았다(가슴 yaw: W -20°, S -22°, A -115°, D +74°). 사격 상체일 때 `LateUpdate`에서 골반이 Idle 기준 자세보다 돌아간 yaw만큼 `_twistBoneName`(`spine_01`)을 반대로 돌린다(조준 강도 적용, 팔 IK보다 먼저). 골반 기준은 Base Layer가 Idle(전환 중 아님)일 때 골반 로컬 회전으로 저장한다.
  - 벽에 붙어 사격할 때(사용자 결정 B): 조준 뼈→조준점 거리가 `_minAimDistance`(1.0m) 이하이면 보정을 끄고, 그 위 `_aimFadeDistance`(0.5m) 구간에서 서서히 켠다. 총신 직선 위에서 해를 못 찾으면(조준점이 팔 길이 안쪽이나 총구 뒤) 보정하지 않는다. 전에는 이때 `총신 → 조준점` 예비 회전으로 팔을 90° 이상 꺾었다. 보정이 꺼지면 팔은 `Fire_R` 원래 자세(오른쪽 약 35°)로 돌아간다.
  - 첫 발: 팔을 들기 전(상체 Empty)에 총알이 생성되므로 첫 발은 내린 손 위치에서 조준점으로 나간다. 이후 단발(차지/노차지) 사격으로 바뀔 예정이라 이번에는 그대로 둔다(사용자 결정).
- 블렌드 기준 방향은 시점 방향(카메라 forward의 수평 성분)이다. `CharacterRotator`가 회전시키는 대상은 Player 루트가 아니라 `SK_ZMike`(기본 자세 X 270°)이므로, Animator(루트) transform을 기준으로 계산하면 안 된다. 몸이 시점과 따로 도는 기능(프리룩 등)이 생기면 실제 몸 방향을 기준으로 바꿔야 한다.
- 방향 클립: 앞은 41번 Run, 뒤·좌·우는 47~49번 Walk 계열을 쓴다(46번 Walk는 쓰지 않는다).
- 점프: 37 → 공중 30(루프) → 착지 29 (사용자 지정). 31(`Jump`, 1.13초)은 웅크림-점프-웅크림 전체 동작이라 실제 체공(약 1.0초) 안에 끝나지 않아 30이 재생되지 않았다. 그래서 사용자 결정으로 뺐다. 공중 판단은 점프 이벤트(점프 시작 ~ 착지) 기준이다. 점프 없이 난간에서 떨어지는 경우는 점프 애니메이션을 재생하지 않는다(사용자 선택).
- 점프 입력 즉시 몸이 뜨므로(입력 지연 없음, 조작감 우선 — 사용자 선택) 37은 앞쪽 웅크림 구간을 건너뛰고 0.3초 지점(Offset 0.6)부터 재생한다. 37 → 30 Exit Time을 0.8로 두면 들어오는 전환(0.1초) 도중에 0.8을 지나 버려 37이 한 바퀴 더 돌았다. 그래서 0.9로 옮겼다.
- 37 재생 중에 착지하면 바로 29로 간다. 착지 시 이동 입력이 있으면 29를 끝까지 재생하지 않고 바로 Locomotion으로 간다(사용자 선택).
- 총알 조준: 총알은 총구(`FirePosition`, `Buster_R` 뼈 하위)에서 **화면 중앙 조준점**을 향해 발사한다(사용자 승인). 기존의 수평 시점 방향 발사는 Fire 기능 구현용 임시였다(사용자 확인).
  - 조준점: 메인 카메라 화면 중앙 레이를 레이캐스트해서 구한다(최대 100m, Player 레이어 제외). 맞는 것이 없으면 100m 지점이다.
  - 카메라와 캐릭터 사이 물체 무시: 레이캐스트 시작점을 카메라 피벗을 레이에 투영한 깊이로 앞당긴다(사용자 제안 반영).
  - 조준점이 기존 발사 방향(수평 시점 방향) 기준으로 총구 뒤에 있으면 기존 방향으로 쏜다(안전장치).
  - 알려진 한계: 총구 바로 앞에 벽이 있으면 조준점과 실제 탄착이 어긋난다.
- 구르기: 42번(`Run_Chase`)을 2배속으로 재생한다(사용자 지정). 구르는 동안 몸은 구르기 방향을 본다(사용자 선택 B). 시작 프레임에 즉시 돌리고(사용자 선택), 끝나면 기존 회전 속도로 카메라 정면에 복귀한다.
- 구르는 동안에는 사격 상체의 조준 보정(총신 IK)을 끈다. 몸이 구르기 방향을 보는 동안 팔이 조준점 쪽으로 크게 꺾이지 않게 한다. 상체 사격 레이어 자체는 그대로 덮는다.
- 구르기가 시작되면 Motor 방향은 0이 되지만, 이동 입력이 남아 있으면 `IsMoving`은 true로 유지된다. 그래서 Roll이 끝날 때 `IsMoving`으로 Idle/Locomotion을 고른다.

### 5-5. 검증 상태
- 컴파일 에러 0, 프리팹 참조 연결과 컨트롤러 설정은 에셋 기준으로 확인했다.
- 2026-10-09 `Map_0_Tutorial` Play 확인: W/S/A/D 입력 시 `MoveX`/`MoveY`가 (0,1)/(0,-1)/(-1,0)/(1,0)으로 수렴하고 Locomotion 상태, 키를 떼면 Idle로 복귀. 콘솔 에러 없음.
- 2026-10-09 `Map_0_Tutorial` Play 확인(상체): 클릭 사격 시 `IsFiring` true → UpperBody `Fire` 상태, 마지막 사격 후 약 3초에 `Empty`로 복귀. 시점에 아래 30° / 위 30°를 넣었을 때 가슴이 각각 앞으로 숙여지고 뒤로 젖혀짐(pelvis→neck 방향 z 0.16 → 0.52 / 0.00). 콘솔 에러 없음.
- 2026-10-09 1회 재생 확인: 발사 → Fire 1회(normalizedTime 0→1) → FireHold 유지 → 마지막 발사 후 3초에 Empty. 재생 중 재발사 시 처음부터 다시 재생, FireHold 중 재발사 시 Fire로 재진입(`PlayFire` 직접 호출로 확인).
- 2026-10-09 점프 확인 (31 제거 후): Space 점프 시 Idle → JumpStart(0.13초) → JumpAir(0.52초) → JumpLand(1.11초) → Idle(1.51초). 이동 중 착지 시 바로 Locomotion으로 가는 것은 31 제거 전에 `PlayJump`/`PlayLand`/`SetMoving` 직접 호출로 확인했다(해당 전환은 바뀌지 않음).
- 2026-10-09 Offset 적용 확인: Space 점프 시 JumpStart가 normalizedTime 0.66부터 시작해 약 0.2초 뒤 JumpAir로 넘어감.
- 2026-10-09 구르기 확인: Shift 정면 구르기 → Roll(speed 2) → 종료 후 Idle. 오른쪽 구르기(`CommandRoll` 직접 호출, 이동 입력 (1,0))에서 몸 yaw가 시작 프레임에 0° → 90°로 바뀌고, 종료 후 0°로 복귀. Interruption Source 적용 후 Idle→Locomotion 전환 중 구르기 요청 시 0.015초 안에 Roll 전환 시작. 콘솔 에러 없음.
- 2026-10-09 조준 확인: 위 50° 사격 시 총알이 10.4m 앞 천장 조준점을, 아래 약 45° 사격 시 캐릭터 1.5m 앞 바닥 조준점을 향함(발사 방향과 총구→조준점 사이 각도 0.00°). 조준점은 일반 카메라 레이캐스트 지점과 같았다(카메라-캐릭터 사이 물체 없음). 콘솔 에러 없음.
- 2026-10-09 총신 IK 확인: FireHold에서 총신과 총구→조준점 방향 사이 각도가 정면, 위 50°, 아래 50° 모두 0.00°(시점 변경 직후 약 0.2초는 Cinemachine 지연으로 14~16°). 기준 자세 저장 전 첫 Fire 재생 중에도 0.00°. IK 적용 전에는 유지 자세 38°, 발사 순간 80° 차이였다.
- 2026-10-09 벽 예외 확인: 캐릭터를 앞 벽에서 0.05m 거리로 옮겨 사격하면 어깨→조준점 1.0m, 보정 강도 0, 총신은 애니메이션 자세(yaw 35°, pitch -15°)를 유지한다. 뒤로 0.2/0.4/0.6/1.0m 물러나면 강도가 0.01/0.08/0.37/1.00으로 올라가고 총신 yaw가 34°→32°→18°로 꺾임 없이 변한다.
- 2026-10-09 이동 중 상체 보정 확인: 이동 입력을 직접 넣고 사격 상태를 유지한 채, 정지 자세에서 몸 정면에 해당하는 가슴(`spine_03`) 축의 yaw를 0.9초 평균으로 쟀다. 보정 끔: W -20.4°, S -21.6°, A -114.5°, D +73.7°. 보정 켬: 네 방향 모두 ±0.3° 이내. (처음 체크 때 보고한 13~14°는 측정 축을 잘못 잡은 값이었다.)
- 확인하지 못한 것(이동 중 상체): 측면 이동 시 허리(`spine_01`) 한 곳에서 최대 약 115°를 비트는 모습이 자연스러운지(필요하면 spine_01~03에 나눠 비틀기).
- 확인하지 못한 것(조준): 카메라와 캐릭터 사이에 벽이 있는 상황, 총구 앞 벽, 실제 마우스 조작, 팔 pitch의 시각적 품질.
- 확인하지 못한 것(구르기): 실제 방향키 + Shift 입력(테스트 입력이 겹쳐 방향키가 빠짐), 공중 구르기 후 JumpAir 복귀, 사격 중 구르기의 시각적 품질. JumpLand → Idle/Locomotion 전환 도중의 구르기는 아직 Interruption Source가 None이라 늦을 수 있다.
- 확인하지 못한 것(점프): 실제 이동 입력과 점프를 함께 했을 때(테스트 입력이 겹쳐 확인 실패), 공중 재점프(DoubleJump 스킬이 아직 실제로 점프하지 않음).
- 확인하지 못한 것(상체): 실제 마우스 pitch 조작, 이동하면서 사격할 때의 시각적 품질, 팔(Buster_R)이 조준점을 정확히 향하는지.
- 확인하지 못한 것: 마우스로 시점을 돌리며 이동하는 경우, 대각선 입력, 블렌드 결과의 시각적 품질(발 미끄러짐).
- 측면·후진은 Walk 클립이라 Run 속도에서 발이 미끄러져 보였다(사용자 플레이 확인). Walk 계열 timeScale을 1.5로 올렸고, 개선됐는지는 다시 확인해야 한다.
