# 03. 점프 중 대쉬 1회만 가능하게 제약 추가

- **우선순위**: 최우선
- **상태**: 완료

## 목표
공중에서는 대쉬를 1회만 쓸 수 있게 하고, 착지하면 다시 쓸 수 있게 한다.

## 현재 상황
- 대쉬 = `Roll` 스킬 (확정). `Roll.cs` 는 거리/시간 설정, 실제 이동은 `RollMover.cs`.
- 접지 판정: `CharacterMotor.IsGrounded` (`CharacterController.isGrounded`, `Assets/_Project/Scripts/Movement/CharacterMotor.cs:64`).
- 스킬 상태 초기화 (02 멀티점프에서 추가): `SkillBase.ResetState()`(virtual, 기본 동작 없음)와 `SkillController.ResetSkill(string skillName)` 이 있다. `StopSkill` / `Stop()` 과 같은 패턴이다.
- 착지 경로 (02 멀티점프에서 추가): `CharacterMotor.OnJumpEnded` → `MoveMediator.OnJumpEnded` → `CharacterMediator` → `SkillMediator.NotifyLanded()` → `SkillController.ResetSkill("MultiJump")`. 현재는 멀티점프만 초기화한다.
  - `OnJumpEnded` 는 점프한 뒤 착지할 때만 발생한다. 점프 없이 걸어서 떨어진 뒤 착지하면 발생하지 않는다.
- 공중 점프는 `MultiJump` 스킬이 판단하고 `MoveMediator.CommandAirJump` 가 실행한다. 공중 점프를 할 때도 `OnJumpStarted` 가 발생한다.

## 확정 사항
- 제한 판단은 코스트 지불 전(스킬 조건)에 한다. `Roll.prefab` 에 `ResourceCost(CurrentBattery 1)` 가 있어 `MoveMediator` 에서 막으면 배터리만 소모된다.
- 횟수 조건은 기존 `Skill/Conditions/UseCountCondition` 을 재사용한다 (`_maxCount = 1`). 새 조건 스크립트를 만들지 않는다.
- 공중 여부: `CharacterMotor.IsGrounded` 를 조회한다. `MoveMediator.IsGrounded()` → `CharacterMediator` 가 `SkillMediator.SetGetGrounded(Func<bool>)` 로 주입 (`SpawnMediator.SetGetFireDirection` 과 같은 패턴). `CharacterMotor` 이벤트 추가 없음.
- 초기화 지점: 접지 중 대쉬 요청(발동 전), 접지 중 대쉬 발동(지상 사용은 횟수에서 제외), 접지 중 점프 시작(`Jump()` 시점엔 아직 `isGrounded == true` 라 멀티점프와 구분됨), 점프 후 착지(`OnJumpEnded`).
- 걸어서 떨어지는 중에도 공중 대쉬는 1회로 제한된다.
- 알려진 한계(수용): 걸어서 떨어짐 → 공중 대쉬 → 걸어서 착지 → 점프·지상 대쉬 없이 다시 걸어서 떨어지면 공중 대쉬가 막힌다. 경사·계단에서 `isGrounded` 가 잠깐 false 가 되는 순간의 대쉬는 공중 대쉬로 센다.
- 멀티점프로는 초기화하지 않는다.

## 작업 범위 (예상)
- 공중 사용 횟수를 세고 착지 시 초기화.

## 열린 질문
- [x] 여기서 말하는 "대쉬"가 현재 `Roll` 스킬과 같은 것인가? → 같다.
- [x] 초기화 시점: 착지할 때만인가? 멀티점프(02)를 쓸 때도 초기화되는가? → **착지할 때만**. 멀티점프를 써도 초기화하지 않는다.
- [x] 구현 위치: 재사용 가능한 `ISkillCondition`(공중 사용 횟수 조건)으로 만들지, `Roll` 내부 로직으로 둘지? → **G1안**. `Roll.prefab` 에 기존 `UseCountCondition` 을 붙이고, 공중 여부는 `SkillMediator` 가 주입받은 접지 조회 함수(`CharacterMotor.IsGrounded` 기반)로 판단한다. (A안의 점프 이벤트 추적은 폐기)
- [x] 공중 대쉬 횟수를 인스펙터 설정값으로 열어둘지, 1회로 고정할지? → **인스펙터 값** (`UseCountCondition._maxCount`, 기본 1).

## 완료 기준
- 플레이 모드에서 점프 후 대쉬가 1회만 발동되고, 착지 후 다시 가능한지 확인. → 확인 완료 (2026-10-11). 막힌 대쉬는 배터리가 소모되지 않고, 멀티점프로는 초기화되지 않으며, 걸어서 떨어진 경우도 1회로 제한됨.
