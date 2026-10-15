# 24. 코요테 타임

- **우선순위**: 미정
- **상태**: 대기
- **관련 작업**: [25. 점프 버퍼링](25-jump-buffering.md) — 둘 다 `MoveMediator.CommandJump` 의 접지 판정을 바꾼다. [02. 멀티점프](done/02-multi-jump.md)

## 목표
발판 끝에서 떨어진 직후 짧은 시간 동안은 지상 점프를 허용한다.

## 현재 상황
- 지상/공중 판정: `MoveMediator.CommandJump` (`Assets/_Project/Scripts/Mediator/SubMediators/MoveMediator.cs:295`) 가 `_motor.IsGrounded` 로 바로 나눈다. 공중이면 `OnAirJumpRequested` 를 발행해 `MultiJump` 가 판단한다.
- 접지 판정: `CharacterMotor.IsGrounded` = `CharacterController.isGrounded` (`Assets/_Project/Scripts/Movement/CharacterMotor.cs:66`).
- 02 확정 사항: 걸어서 떨어지면 지상 점프를 **사용한 것으로 간주**한다. 코요테 타임 안의 점프는 이 규칙의 예외가 된다.

## 작업 범위 (예상)
- 마지막 접지 시각 기록, 유예 시간 안의 점프는 지상 점프로 처리.
- 유예 시간 Inspector 노출.

## 열린 질문
- [ ] 유예 시간 값 (예: 0.1~0.15초)
- [ ] 값을 어디에 두는가 (`CharacterMotor` / `MoveMediator`)
- [ ] 코요테 점프 후 `MultiJump` 횟수: 지상 점프로 쳐서 공중 점프를 그대로 남기는가
- [ ] 구르기·탈것 탑승 중에도 적용하는가
- [ ] AI 에도 적용되는가 (`MoveMediator` 공용 여부)
