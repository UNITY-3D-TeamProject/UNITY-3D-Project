---
id: intent-001
title: 이동 시스템(Movement System) 코어 계약 부재
part: Movement System (Core)
status: open
created: 2026-09-09
resolved: null
---

## 문제 (Problem)
Player, Enemy, MovingPlatform 등 동적 오브젝트마다 "입력/의도"와 "실제 이동 처리"가 분리되어 있지 않아, 오브젝트 종류나 이동 방식(CharacterController/Rigidbody 등)이 늘어날수록 서로 결합되기 쉽다. 현재 `Assets/_Project/Scripts/System/IMoveProvider.cs`는 컨벤션 위반 상태(class로 잘못 선언, 네임스페이스 없음)의 스텁만 존재하고, 재사용 가능한 코어 계약이 없다.

## 기대 결과 (Proposed outcome)
`SMoveIntent`(값 타입 의도), `IMoveIntentProvider`(의도 제공 계약), `SOMovementConfig`(튜닝 SO), `IMoveMotor`/`MovementMotor`(이동 처리 계약과 위임자), `CompositeMoveIntentProvider`(provider 합성 헬퍼), `CharacterControllerMotor`(레퍼런스 구현)로 구성된 코어 골격이 생겨, 각 오브젝트 담당자가 이 계약 위에서 독립적으로 구체 Provider/Motor를 구현할 수 있게 된다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: Player/Enemy/MovingPlatform 등 각 오브젝트를 담당하는 팀원.
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/System/` 폴더(기존 `IMoveProvider.cs` 스텁 대체), 향후 각 오브젝트별 구체 Provider/Motor 구현.

## 제약 (Constraints)
- `Assets/_Project/` 내부에서만 작업, `.asset`/`.prefab`/`.unity`/`.controller` 직접 편집 금지(`[USER]` 티켓으로 분리).
- 신규 파일 위치는 spec.md 원안(`Scripts/Movement/`)이 아니라 기존 스텁이 있는 **`Scripts/System/`**을 재사용한다(2026-09-09 세션에서 결정, spec.md도 함께 갱신).
- **네임스페이스 임시 예외**: `docs/coding-convention.md` 3-6장은 모든 스크립트에 폴더 경로 반영 네임스페이스(`Project.<폴더>`)를 요구하지만, 팀이 새 네임스페이스 기준을 상의했고(2026-09-09) 아직 그 결정이 `docs/coding-convention.md`에 반영되지 않았다. 문서가 갱신되기 전까지 이번 Movement System 코어 파일(`Scripts/System/`의 T1~T7)은 **네임스페이스 없이** 작성한다. `docs/coding-convention.md`가 갱신되면 그 기준에 맞춰 리팩터링한다.
- 프로젝트에 asmdef가 전혀 없는 기존 관례를 유지하려 했으나, EditMode NUnit 테스트가 실제로 필요해진 시점(T5)에 asmdef 예외가 필요했다. **2026-09-09 구현 중 수정**: Unity는 커스텀 asmdef가 암묵적 기본 어셈블리(`Assembly-CSharp`)를 참조하는 것을 지원하지 않는다(컴파일 순서상 `Assembly-CSharp`이 항상 가장 마지막에 컴파일됨). 그 결과 `Assets/_Project/Test/EditMode/`의 테스트 전용 asmdef(`SystemEditModeTests.asmdef`)뿐 아니라, `Scripts/System/`에도 생산 코드용 `ProjectSystem.asmdef`를 추가해 테스트 asmdef가 이를 GUID로 참조하는 구조로 변경했다. 테스트 코드 위치도 `Scripts/Test/EditMode/`가 아닌 `Test/EditMode/`(`Assets/_Project/Test/EditMode/`)이다.
- T6에서 `SOMovementConfig`에 점프/중력 처리를 위한 `Gravity`, `JumpForce` 필드가 추가되었다(CharacterControllerMotor가 중력/점프까지 포함하는 프로덕션 구현으로 확정됨에 따름).
- 그 외 `docs/coding-convention.md`의 명명 규칙/선언 순서/서식은 그대로 따른다.
- `GetIntent()`는 순수 조회(부수효과 없음), Motor는 오브젝트당 1개 고정, Motor가 스스로 틱 타이밍(Update/FixedUpdate)을 선언한다.

## 열린 질문 (Open questions)
- Provider 합성 시 값 합산 규칙(단순합산/우선순위/블렌딩)은 이번 코어 범위가 아니며, 각 오브젝트 담당자가 자신의 구체 Provider 안에서 결정한다.
- 팀의 새 네임스페이스 기준이 확정되어 `docs/coding-convention.md`에 반영되는 시점 — 그때까지 `Scripts/System/`의 네임스페이스 생략 상태가 유지된다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
