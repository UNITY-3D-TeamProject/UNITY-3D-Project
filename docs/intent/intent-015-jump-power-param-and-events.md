---
id: intent-015
title: 점프를 점프력 매개변수 + 무조건 실행으로 바꾸고 시작/착지 이벤트 발행
part: Movement System
status: open
created: 2026-10-07
resolved: null
---

## 문제 (Problem)
`CharacterMotor`가 점프력(`JumpSpeed`)을 직접 들고 있고, `Jump()`는 요청 플래그만 세운 뒤 다음 Update에서 접지 상태일 때만 소비한다.
- 점프력을 호출 시점마다 다르게 줄 수 없다 (스킬·점프대 등).
- 공중에서 요청하면 모터가 무시하므로 더블점프 같은 스킬을 만들 수 없다.
- 점프 시작/종료를 외부(애니메이션 등)가 알 방법이 없다.

## 기대 결과 (Proposed outcome)
- `CharacterMotor.Jump(float jumpPower)` — 호출되면 접지 여부와 관계없이 즉시 실행한다.
- 점프 시작 시 `OnJumpStarted`, 점프 후 착지 시 `OnJumpEnded`를 발행한다.
- 점프력은 `MoveMediator`가 어트리뷰트에서 받아 들고 있다가 넘긴다.
- 일반 점프 입력의 "지상에서만" 정책은 `MoveMediator.CommandJump()`가 `IsGrounded`로 판단한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: `MoveMediator`를 쓰는 모든 캐릭터(Player/Enemy).
- 어떤 시스템/모듈이 건드려지는가: `Movement/CharacterMotor.cs`, `Mediator/SubMediators/MoveMediator.cs`.

## 제약 (Constraints)
- 지켜야 할 정책: 컴포넌트는 판단하지 않는다(접지 정책은 중재자), 캐릭터 아키텍처 문서의 이벤트 방향(컴포넌트 → 중재자).
- 쓰지 말아야 할 것: 별도 점프 컴포넌트 신설(사용자 결정: 모터 유지, API만 변경). 직렬화 필드명(`_jumpSpeedValueKey`) 변경 — 프리팹 값이 깨진다.

## 열린 질문 (Open questions)
- 공중 재점프 시 `OnJumpStarted`가 매번 발행된다(현재 구현). 더블점프 애니메이션 요구가 생기면 이 정책이 맞는지 확인.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
