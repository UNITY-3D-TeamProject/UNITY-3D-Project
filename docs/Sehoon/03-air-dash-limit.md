# 03. 점프 중 대쉬 1회만 가능하게 제약 추가

- **우선순위**: 최우선
- **상태**: 대기

## 목표
공중에서는 대쉬를 1회만 쓸 수 있게 하고, 착지하면 다시 쓸 수 있게 한다.

## 현재 상황
- 대쉬 = `Roll` 스킬 (확정). `Roll.cs` 는 거리/시간 설정, 실제 이동은 `RollMover.cs`.
- 접지 판정: `CharacterMotor.IsGrounded` (`CharacterController.isGrounded`, `Assets/_Project/Scripts/Movement/CharacterMotor.cs:64`).
- 스킬 상태 초기화 (02 멀티점프에서 추가): `SkillBase.ResetState()`(virtual, 기본 동작 없음)와 `SkillController.ResetSkill(string skillName)` 이 있다. `StopSkill` / `Stop()` 과 같은 패턴이다.
- 착지 경로 (02 멀티점프에서 추가): `CharacterMotor.OnJumpEnded` → `MoveMediator.OnJumpEnded` → `CharacterMediator` → `SkillMediator.NotifyLanded()` → `SkillController.ResetSkill("MultiJump")`. 현재는 멀티점프만 초기화한다.
  - `OnJumpEnded` 는 점프한 뒤 착지할 때만 발생한다. 점프 없이 걸어서 떨어진 뒤 착지하면 발생하지 않는다.
- 공중 점프는 `MultiJump` 스킬이 판단하고 `MoveMediator.CommandAirJump` 가 실행한다. 공중 점프를 할 때도 `OnJumpStarted` 가 발생한다.

## 작업 범위 (예상)
- 공중 사용 횟수를 세고 착지 시 초기화.

## 열린 질문
- [x] 여기서 말하는 "대쉬"가 현재 `Roll` 스킬과 같은 것인가? → 같다.
- [ ] 초기화 시점: 착지할 때만인가? 멀티점프(02)를 쓸 때도 초기화되는가?
- [ ] 구현 위치: 재사용 가능한 `ISkillCondition`(공중 사용 횟수 조건)으로 만들지, `Roll` 내부 로직으로 둘지?
- [ ] 공중 대쉬 횟수를 인스펙터 설정값으로 열어둘지, 1회로 고정할지?

## 완료 기준
- 플레이 모드에서 점프 후 대쉬가 1회만 발동되고, 착지 후 다시 가능한지 확인.
