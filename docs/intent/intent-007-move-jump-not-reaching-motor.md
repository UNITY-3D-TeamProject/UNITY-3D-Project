---
id: intent-007
title: Player_test 이동/점프 입력이 CharacterMotor까지 도달하지 않음
part: Movement System
status: open
created: 2026-09-22
resolved: null
---

## 문제 (Problem)
Cube를 바닥으로 두고 `Player_test` 프리팹으로 재생하면 WASD/Space 입력에 캐릭터가 전혀 반응하지 않는다.
원인을 역추적한 결과 서로 다른 계층에 걸친 결손 3건이 확인되었다.

1. **이동 — 프리팹 참조 끊김**: `Assets/Test/PlayerTest/Player_test.prefab`의 MoveMediator 인스턴스 오버라이드가 `_motor: {fileID: 0}`(None)으로 박혀 있다. 베이스 `MovePrefab.prefab`은 정상 참조를 갖지만 인스턴스 오버라이드가 이를 덮어쓴다. `motor` → `_motor` 필드 리네임([FormerlySerializedAs]) 때 에셋 필드는 마이그레이션됐지만 이 프리팹 인스턴스의 오버라이드 항목은 마이그레이션되지 않은 잔재로 추정.
   - 결과: `MoveMediator.Update()`의 `if (!_motor) return;`에서 매 프레임 조기 반환 → `CharacterMotor.Direction`이 항상 0.
2. **점프 — 명령 미전달**: `MoveMediator.CommandJump()`가 `//todo` 주석만 있는 빈 메서드. `CharacterMotor.Jump()`는 구현되어 있으나 호출자가 없다.
3. **점프 — 속도 미설정**: `CharacterMotor.JumpSpeed`에 값을 넣는 주체가 프로젝트 전체에 없어 항상 0. `AttributeToMotorAdapter`는 `MoveSpeed`만 연결하고 있다. `SOAttributeData_Player_T.asset`에는 `JumpPower: 30`이 이미 정의되어 있다.

입력 경로(PlayerInput SendMessages → PlayerInputComponent → CharacterMediator → MoveMediator)와 `CharacterController` 탐색(`GetComponentInParent`)은 정상 확인됨.

## 기대 결과 (Proposed outcome)
- `MoveMediator.CommandJump()`가 `CharacterMotor.Jump()`를 호출한다.
- `AttributeToMotorAdapter`가 `MoveSpeed`와 동일한 패턴으로 `JumpPower` 어트리뷰트를 `CharacterMotor.JumpSpeed`에 연결한다.
- `Player_test.prefab`의 MoveMediator `Motor` 참조가 복구된다 (USER 작업).
- WASD 이동 및 Space 점프가 정상 동작한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: MoveMediator/AttributeToMotorAdapter를 쓰는 모든 캐릭터(Player/Enemy 등).
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/Mediator/MoveMediator.cs`, `Assets/_Project/Scripts/Adapter/AttributeToMotorAdapter.cs` (코드), `Assets/Test/PlayerTest/Player_test.prefab` / `Assets/_Project/Prefabs/Actions/MovePrefab.prefab` (USER, 에디터 작업).

## 제약 (Constraints)
- `.prefab`은 AI가 직접 편집하지 않는다 — `[USER]` 티켓으로 분리.
- 코어 API 시그니처(`Direction`/`Speed`/`JumpSpeed` 프로퍼티)는 변경하지 않는다.
- 최소 수정 원칙: 기존 `_speedValueKey` 패턴을 그대로 확장, 새 컴포넌트/파일 생성하지 않는다.

## 열린 질문 (Open questions)
없음.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
