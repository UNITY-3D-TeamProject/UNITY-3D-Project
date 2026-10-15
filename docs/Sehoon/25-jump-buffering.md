# 25. 점프 버퍼링

- **우선순위**: 미정
- **상태**: 대기
- **관련 작업**: [24. 코요테 타임](24-coyote-time.md), [02. 멀티점프](done/02-multi-jump.md)

## 목표
착지 직전에 누른 점프 입력을 기억했다가 착지하는 순간 점프한다.

## 현재 상황
- 점프 입력은 `MoveMediator.CommandJump` (`Assets/_Project/Scripts/Mediator/SubMediators/MoveMediator.cs:295`) 에서 즉시 처리한다. 공중이면 `OnAirJumpRequested` → `MultiJump` 판단으로 넘어가고, 입력은 저장하지 않는다.
- 착지 알림: `CharacterMotor.OnJumpEnded` → `MoveMediator.OnJumpEnded`. 착지 시 `MultiJump` 횟수가 초기화된다.
- 공중 점프 횟수가 남아 있으면 착지 직전 입력이 공중 점프로 소모된다. 버퍼와 어떻게 나눌지 정해야 한다.

## 작업 범위 (예상)
- 입력 시각 기록, 착지 시 버퍼 시간 안이면 지상 점프 실행.
- 버퍼 시간 Inspector 노출.

## 열린 질문
- [ ] 버퍼 시간 값 (예: 0.1~0.15초)
- [ ] 공중 점프가 남아 있을 때: 공중 점프를 우선하는가, 지면이 가까우면 버퍼로 미루는가
- [ ] 요시 점프(탈것 홀드 상승)·구르기 중 입력도 버퍼에 넣는가
- [ ] 버퍼 점프 시 점프 키를 이미 뗐다면 요시 점프 홀드는 시작하지 않는가
