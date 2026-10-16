# 25. 점프 버퍼링

- **우선순위**: 미정
- **상태**: 완료
- **관련 작업**: [24. 코요테 타임](24-coyote-time.md), [02. 멀티점프](02-multi-jump.md)

## 목표
착지 직전에 누른 점프 입력을 기억했다가 착지하는 순간 점프한다.

## 현재 상황
- 점프 입력은 `MoveMediator.CommandJump` (`Assets/_Project/Scripts/Mediator/SubMediators/MoveMediator.cs:295`) 에서 즉시 처리한다. 공중이면 `OnAirJumpRequested` → `MultiJump` 판단으로 넘어가고, 입력은 저장하지 않는다.
- 착지 알림: `CharacterMotor.OnJumpEnded` → `MoveMediator.OnJumpEnded`. 착지 시 `MultiJump` 횟수가 초기화된다.
- 24(코요테 타임) 이후 `CommandJump` 는 `_motor.IsGrounded` 이거나 `IsInCoyoteTime()` 이면 지상 점프한다. 코요테 판정은 `_lastGroundedTime`(Update 에서 기록)과 `_hasJumpedSinceGrounded`(점프 시작 시 true, 착지 시 false)를 쓴다.
- 공중 점프 횟수가 남아 있으면 착지 직전 입력이 공중 점프로 소모된다. 버퍼와 어떻게 나눌지 정해야 한다.

## 작업 범위 (예상)
- 입력 시각 기록, 착지 시 버퍼 시간 안이면 지상 점프 실행.
- 버퍼 시간 Inspector 노출.

## 열린 질문
- [x] 버퍼 시간 값 → **0.1초**, Inspector 노출.
- [x] 공중 점프가 남아 있을 때 → **공중 점프 우선** (기존 동작 유지). 공중 점프·요시 점프가 발동하지 않은 입력만 버퍼에 저장한다.
- [x] 요시 점프(탈것 홀드 상승)·구르기 중 입력도 버퍼에 넣는가 → **둘 다 넣지 않는다**. 요시 점프로 쓰인 입력은 버퍼를 비우고, 구르기 중 입력은 기존처럼 무시한다.
- [x] 버퍼 점프 시 점프 키를 이미 뗐다면 → **그래도 점프**. 지상 점프 높이는 홀드와 무관하고, 요시 점프는 공중 입력으로만 시작하므로 버퍼 점프와 엮이지 않는다 (코드 확인).
- [x] AI 에도 적용되는가 → 24 와 같이 `MoveMediator` 공용 기본값 그대로 적용.
