---
id: intent-003
title: PlayMode 테스트로 발견된 이동 자체 버그 4건 수정
part: Movement System
status: open
created: 2026-09-17
resolved: null
---

## 문제 (Problem)
`TestResults_20260917_190135.xml` PlayMode 테스트 결과 총 6개 실패 중, 플랫폼 탑승 관련 2건([[intent-002-movement-core-scope]]에서 이미 담당자 이관 대상으로 분류된 슬라이딩 버그 계열)을 제외한 **이동 코어(`CharacterMotor`, `TransformMotor`) 자체의 버그 4건**이 남아있다.

1. **`CharacterMotor.Move`가 수평 속도를 줄임** — `Direction`에 y 성분이 섞이면(`_direction.normalized`가 y까지 포함해 정규화) 수평 속도가 실제 `Speed`보다 작아진다.
   - 테스트: `CharacterMotorTests.DirectionWithVerticalComponent_KeepsHorizontalSpeed_AndDoesNotLift`
   - 위치: `Assets/_Project/Scripts/Movement/CharacterMotor.cs:85`
   - 기대 5.0 / 실제 3.54

2. **비활성 `CharacterController`에도 `Move` 호출** — `_controller.enabled`가 false여도 매 `FixedUpdate`마다 `_controller.Move`를 호출해 "CharacterController.Move called on inactive controller" 에러 로그가 반복 발생한다.
   - 테스트: `CharacterMotorTests.DisabledCharacterController_LogsNoWarningOrError`
   - 위치: `CharacterMotor.cs:91` (`FixedUpdate` → `Move`)

3. **`transform.position` 순간이동이 되돌려짐** — 외부에서 `transform.position`을 직접 바꿔도 다음 `Move()` 호출 시 `CharacterController`가 내부적으로 들고 있던 이전 위치 기준으로 되돌린다(Physics 트랜스폼 동기화 지연 추정).
   - 테스트: `CharacterMotorTests.TeleportByTransformPosition_IsKept`
   - 위치: `CharacterMotor.cs:91`
   - 기대: 0.01 미만 오차 / 실제: 14.14 되돌아감

4. **`TransformMotor.MoveTo`에 음수 속도를 주면 목표 반대 방향으로 이동** — `Vector3.MoveTowards`는 `maxDistanceDelta`가 음수이면 목표에서 멀어지는 방향으로 이동시킨다. 음수 `speed`를 막는 가드가 없다.
   - 테스트: `TransformMotorTests.MoveTo_NegativeSpeed_DoesNotMoveAwayFromTarget`
   - 위치: `Assets/_Project/Scripts/Movement/TransformMotor.cs:31`
   - 기대: 0 이상 / 실제: -0.28 (반대 방향 이동)

## 기대 결과 (Proposed outcome)
위 4개 테스트가 모두 통과하고, 기존에 통과하던 `Test.Movement` 나머지 테스트가 회귀 없이 유지된다.

- (1) 수평 방향만 정규화: `_direction`의 y를 0으로 만든 뒤 정규화해 수평 속도를 구한다.
- (2) `Move()` 시작 시 `_controller.enabled`가 false면 조기 반환한다.
- (3) `_controller.Move()` 호출 직전 `Physics.SyncTransforms()`를 호출해 외부에서 바뀐 `transform.position`을 `CharacterController`가 먼저 인지하게 한다.
- (4) `TransformMotor.MoveTo`에서 `speed`를 `Mathf.Max(0.0f, speed)`로 클램프한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: `CharacterMotor`/`TransformMotor`를 쓰는 Player/Enemy/Platform/Crowd 파트 전원.
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/Movement/CharacterMotor.cs`, `Assets/_Project/Scripts/Movement/TransformMotor.cs`. 둘 다 시그니처 변경 없이 내부 동작만 고친다.

## 제약 (Constraints)
- 플랫폼 탑승 관련 2건(`PlatformStops_RiderStopsToo`, `DeltaThisFrame_IsZero_WhenMoveToNotCalledThisFrame`)은 이번 범위에서 제외한다. `TransformMotor.DeltaThisFrame`이 `MoveTo` 미호출 프레임에도 이전 값을 유지하는 문제이며, 별도로 다룬다.
- 코어 API(`Direction`/`Speed` 프로퍼티, `MoveTo(targetPosition, speed)` 시그니처)는 변경하지 않는다 — 내부 버그 수정만 한다.
- 최소 수정 원칙: 각 버그당 필요한 라인만 고치고 주변 코드를 리팩토링하지 않는다.

## 열린 질문 (Open questions)
없음.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
