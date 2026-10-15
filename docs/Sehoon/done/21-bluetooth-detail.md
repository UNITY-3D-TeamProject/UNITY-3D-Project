# 21. 블루투스 디테일 수정

- **우선순위**: 높음
- **상태**: 완료
- **선행 작업**: [08. 블루투스 모션 추가](08-bluetooth-motion.md)

## 목표
블루투스(탈것) 탑승 중 조작을 보드 타는 느낌에 맞게 다듬는다.
1. 탑승 중 **전방 자동 전진**. 카메라를 돌리면 돌린 방향으로 나아간다.
2. 1·2단 점프는 그대로 두고, **2단 점프 이후 스페이스바를 홀드하면 요시 점프**.
   - 요시 점프: 슈퍼마리오의 요시 탑승 점프처럼, 유지 시간 동안 조금씩 위로 올라간다. 유지 시간 1.5~2초 (Inspector 노출).
3. **요시 점프 중 배터리 소모 증가**.
4. 탑승 중 **회전 속도를 Inspector 로 조절**.

## 현재 상황 (작업 전)
- 탑승·하차, 버프(이동속도 ×1.5, 점프력 ×1.25, 중력 ×0.7), 배터리 지속 소모는 [07-1](07-1-bluetooth-skill.md), 모션·보드 방향은 [08](08-bluetooth-motion.md) 에서 구현했다.
- 이동
  - 이동은 입력 방향 기준이다. `MoveMediator` 가 `MoveDirectionCalculator` 로 시점 기준 방향을 계산해 `CharacterMotor.Direction` 에 넣는다. 입력이 없으면 멈춘다.
  - 탑승 중 보드는 이동 방향을 따라 돈다 (`CharacterAnimator._boardTurnSpeed` 720°/초). 앞쪽 반원이면 이동 방향, 뒤쪽이면 반대 방향을 본다.
- 점프
  - 점프 입력은 누르는 순간 한 번만 전달된다 (`PlayerInputComponent.OnJump` → `IMoveController.SetJumpRequest(Action)`). 떼는 시점을 알 수 없다.
  - 홀드는 Fire 처럼 입력 액션 interaction 을 `Press(behavior=2)`(누름·뗌 모두)로 두고 `InputValue.isPressed` 로 구분할 수 있다 (`FID_InputSystem.inputactions` 의 Fire, `PlayerInputComponent.OnFire`).
  - 공중 점프는 `MultiJump` 스킬이다. `UseCountCondition._maxCount` 2 (지상 점프 포함)라서 지상 1회 + 공중 1회다. 착지하면 초기화된다.
- 배터리 소모는 `DrainCost._drainPerSecond`(10/초) 고정값 하나다. 소모량을 바꾸는 경로는 없다.
- 회전
  - 몸은 `CharacterRotator._rotateSpeed`(Player 오버라이드 720°/초)로 시점 정면을 따라 돈다.
  - 보드·하체는 `CharacterAnimator._boardTurnSpeed`(720°/초)로 이동 방향을 따라 돈다.

## 작업 범위 (예상)
- 탑승 중 이동 방향을 입력 대신 시점 전방으로 바꾸는 경로 (`MoveMediator` 또는 `MoveDirectionCalculator`).
- 점프 입력에 뗌(release) 경로 추가, 요시 점프 상태·상승 처리 (`CharacterMotor` 수직 속도 제어), 요시 점프 스킬 또는 `SpawnVehicle` 확장.
- `DrainCost` 소모 배율(또는 추가 소모) 경로.
- 탑승 중 회전 속도 Inspector 값과 적용 경로.
- 요시 점프 애니메이션이 필요하면 `player-animation` 스킬 절차를 따른다.

## 열린 질문
### 자동 전진
- [x] 탑승 중 WASD 입력의 역할 → **무시**. 무조건 전방으로 전진한다.
- [x] 자동 전진 속도 → 탑승 버프가 적용된 이동속도(6 × 1.5 = 9)를 그대로 쓴다.
- [x] 공중(점프·요시 점프 중)에도 전진 → 계속 전진한다.
- [x] 탑승 점프 연출 중 전진 → **착지해 탈것이 생성된 뒤부터** 전진한다.
- [x] 벽에 막혔을 때 → 별도 처리 없음. CharacterController 가 막히는 대로 둔다.
- [x] 보드·하체 방향 → 진행 방향을 따른다. 전방으로만 진행하므로 08 의 이동 방향 규칙으로 자동으로 전방이 된다. 추가 회전 처리는 없다.

### 요시 점프
- [x] 시작 조건 → 2단 점프 이후 스페이스를 **다시 눌러** 누르고 있는 동안 진행한다.
- [x] 시작 시점 → 누르는 즉시 시작한다. 상승 중·하강 중과 관계없이 수직 속도를 상승 속도로 바꾼다.
- [x] 상승 방식 → 일정 속도 상승. 기본 1.5 m/s (Inspector 노출).
- [x] 유지 시간 기본값 → 1.5초 (Inspector 노출).
- [x] 중간에 떼면 → 즉시 끝난다. 같은 체공 중에는 다시 쓸 수 없고, 착지하면 초기화된다.
- [x] 사용 조건 → 탑승 중 전용.
- [x] 사격·대쉬 → 사격 가능. 대쉬하면 요시 점프가 끝난다.
- [x] 애니메이션 → 전용 모션 없음. 기존 Riding(11번)을 유지한다.

### 배터리 소모
- [x] 증가 방식 → **초당 추가량**. 요시 점프 중에는 기본 소모량(10/초)에 노출한 값을 더한다. 기본값 10/초 (float, Inspector 노출).
  - 추가 방식이 어려우면 배율(기본 ×2)로 대체한다.
- [x] 요시 점프 중 배터리 0 → 현재 규칙대로 강제 하차한다.

### 회전 속도
- [x] 조절 대상 → **자동 전진 방향이 카메라 방향을 따라 도는 속도** 하나. 기본 720°/초. 몸(`CharacterRotator`)·보드(`_boardTurnSpeed`) 회전은 그대로 둔다.
- [x] Inspector 위치 → `SpawnVehicle` (탈것 설정에 모음).

### 탑승 연출 중 입력 (사용자 피드백)
- [x] 탑승 시작(F) ~ 탈것 생성까지 막을 입력 → **사격, 구르기(대쉬), 공중 점프, 요시 점프**. 조준·상호작용·스캔·F(하차)는 허용한다.
  - 이 구간은 항상 공중이라 지상 점프는 발생하지 않는다. 요시 점프는 탈것이 생성된 뒤에만 시작한다.
  - 하차하면 막은 입력을 바로 푼다 (탈것이 생성되지 않는 경우 대비).
- [x] 탑승 시 차지 중이었다면 → 취소한다 (구르기 시작 시와 같은 동작).
- [x] 막은 스킬을 풀 때 → 막기 직전에 켜져 있던 스킬만 다시 켠다 (외부에서 `SetSkillEnabled` 로 꺼 둔 스킬 유지).
  - 같은 방식으로 구르기 종료 시 Attack 도 구르기 전에 켜져 있었을 때만 다시 켜도록 함께 고쳤다 (기존에는 무조건 켬).

## 결과 (2026-10-14)
### 흐름
- **자동 전진**: 착지해 탈것이 생성되면 `SkillMediator.OnAutoForwardChanged(true, ForwardTurnSpeed)` → `MoveMediator.SetAutoForward` → `MoveDirectionCalculator.SetAutoForward`.
  - 계산기는 `Update` 에서 전진 방향을 시점 방향 쪽으로 `RotateTowards`(720°/초)로 돌려 알린다. WASD 입력은 저장만 하고, 끄면(하차) 저장된 입력으로 다시 계산한다.
  - 보드는 기존 `OnMoveDirectionChanged` 를 따라 전진 방향을 본다. 구르기는 `_lastDirection`(전진 방향)으로 구른다.
  - 자동 전진 중에는 `CharacterMediator.GetRollLookDirection` 이 뒤쪽 구르기 반전을 하지 않는다.
- **요시 점프**: 공중 점프 입력 → `SkillMediator.RequestMultiJump` 에서 MultiJump 가 발동하지 못하면 `SpawnVehicle.TryStartHover`.
  - 조건: 탑승 중, 탈것 생성됨, 이번 체공에 미사용. 시작하면 `OnHoverChanged(true)` → `MoveMediator.SetHover` → `CharacterMotor.StartHover`(수직 속도 고정).
  - 종료: 유지 시간 경과, 점프 뗌(`IJumpReleaseController` → `MoveMediator.OnJumpReleased` → `SkillMediator.NotifyJumpReleased`), 구르기 시작, 하차. 착지 시 `ResetHover`.
  - 요시 점프 중 `DrainCost.SetExtraDrainPerSecond` 로 기본 소모량에 추가 소모량을 더한다.
- **탑승 연출 중 입력 차단**: 탑승 점프·착지 대기 중 `SkillMediator.SetMountBlocked` 가 Attack·Roll·MultiJump 를 끄고, 탈것 생성 또는 하차 시 막기 직전 켜져 있던 스킬만 다시 켠다.
  - 구르기 종료 시 Attack 도 구르기 전에 켜져 있었을 때만 다시 켠다.

### Inspector (`SpawnVehicle`)
| 필드 | 기본값 | 설명 |
|---|---|---|
| `_forwardTurnSpeed` | 720 | 자동 전진 방향이 카메라를 따라 도는 속도(도/초) |
| `_hoverDuration` | 1.5 | 요시 점프 최대 유지 시간(초) |
| `_hoverRiseSpeed` | 1.5 | 요시 점프 상승 속도(m/s) |
| `_hoverExtraDrainPerSecond` | 10 | 요시 점프 중 초당 추가 배터리 소모량 |

### 변경 파일
- `FID_InputSystem.inputactions`: Jump interaction `Press(behavior=2)`
- `PlayerInputComponent.cs`: `IJumpReleaseController` 구현, `OnJump` 누름/뗌 분리
- `MoveMediator.cs`: `IJumpReleaseController` 정의, `SetAutoForward`, `SetHover`, `OnJumpReleased` (전달만)
- `MoveDirectionCalculator.cs`: 자동 전진
- `CharacterMotor.cs`: `StartHover`, `StopHover`
- `SpawnVehicle.cs`: 위 Inspector 값, 요시 점프 상태·타이머, `OnHoverChanged`
- `DrainCost.cs`: `SetExtraDrainPerSecond`
- `SkillMediator.cs`: 요시 점프 시작·종료·초기화, `OnAutoForwardChanged`, `OnHoverChanged`, 탑승 연출 중 스킬 차단, 구르기 종료 시 Attack 복원 조건
- `CharacterMediator.cs`: 이벤트 연결, 자동 전진 상태 기록
- 프리팹 변경 없음 (새 필드는 코드 기본값 사용)

### 검증 (플레이 모드, `Map_1_Lobby`)
- 탑승 후 입력 없이 이동속도 9 로 전진, 시점 변경 시 3프레임(약 0.047초)에 약 34° 회전 (720°/초).
- 요시 점프 수직 속도 1.5 유지, 1.5초 후 자동 종료. 해당 구간 배터리 감소량이 기본 소모 + 10×1.5 와 일치. 착지 후 재사용 가능.
- 점프 뗌 종료·같은 체공 재사용 차단은 메서드 직접 호출로 확인 (키 시뮬레이션 지연으로 체공 중 연속 입력이 어려움).
- 탑승 연출 중 Attack·Roll·MultiJump 비활성, 요시 점프 불가. 탈것 생성·하차 시 복원. 외부에서 꺼 둔 Roll·Attack 은 탑승·구르기 후에도 꺼진 채 유지.

### 알려진 사항
- 탑승 연출 중(약 0.5초) 외부에서 스킬을 끄면, 연출이 끝날 때 막기 직전 상태로 되돌린다.

## 완료 기준
- 플레이 모드 확인
  - 탑승 중 입력 없이 카메라 전방으로 전진하고, 카메라를 돌리면 진행 방향이 따라 돈다.
  - 1·2단 점프 후 홀드 시 요시 점프로 유지 시간 동안 상승하고, 그동안 배터리 소모가 늘어난다.
  - 탑승 중 회전 속도가 Inspector 값대로 바뀐다.
- 컴파일 에러 없음.
