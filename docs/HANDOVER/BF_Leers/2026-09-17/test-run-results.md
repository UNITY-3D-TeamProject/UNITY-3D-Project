# PlayMode 테스트 실행 결과 — KnownBug 6종 원인 확정

## 1. 작업 요약 및 방법
[이동 로직 리뷰 반영 + PlayMode 테스트](./movement-review-and-tests.md)에서 "컴파일/PlayMode 실행 미확인"으로 남겨뒀던 항목을 사용자가 Unity 에디터에서 직접 실행하고, 결과 XML(`TestResults_20260917_190135.xml`)을 전달받아 분석했다. `CharacterMotor.cs`, `TransformMotor.cs` 코드를 실패 스택트레이스와 대조해 각 실패의 원인 코드 라인을 특정했다.

## 2. 결과 요약
- 총 25개 중 19 통과 / 6 실패, 실패 전부 `[Category("KnownBug")]` 태그 — 지난 세션에 리뷰로 의심만 해뒀던 버그 후보 5개가 **전부 실제 버그로 확정**됐다(6개 실패 중 2개는 원인이 하나로 겹침).
- `Movement.asmdef` 분리 자체는 문제없이 컴파일/실행됨 (별도 컴파일 에러 없음).

## 3. 실패 원인 (확정)

**CharacterMotor.cs**
1. `DirectionWithVerticalComponent_KeepsHorizontalSpeed_AndDoesNotLift` — `CharacterMotor.cs:85` `_direction.normalized * _speed`. Direction에 y 성분이 섞이면 정규화가 이를 포함해 수평 속도가 줄어듦(기대 5.0 vs 실측 3.54 ≈ 1/√2배).
2. `DisabledCharacterController_LogsNoWarningOrError` — `CharacterMotor.cs:91`(`Move()`, `FixedUpdate`에서 호출). `_controller.enabled` 체크 없이 매 프레임 `Move()`를 호출해 비활성 상태에서도 Unity 에러 로그 발생.
3. `TeleportByTransformPosition_IsKept` — `transform.position` 직접 대입 후 다음 프레임 `_controller.Move()`가 위치를 되돌림(CharacterController 내부 캐시 위치 문제). Teleport를 위한 별도 처리(`controller.enabled` 토글 등) 없음.

**TransformMotor.cs**
4. `DeltaThisFrame_IsZero_WhenMoveToNotCalledThisFrame` — `TransformMotor.cs:18,32`. `DeltaThisFrame`이 `MoveTo()` 내부에서만 갱신되고 프레임마다 초기화하는 로직이 없어, 그 프레임에 `MoveTo`를 안 불러도 이전 값이 남음.
5. `MoveTo_NegativeSpeed_DoesNotMoveAwayFromTarget` — `TransformMotor.cs:31` `Vector3.MoveTowards(..., speed * Time.deltaTime)`. 음수 speed가 그대로 들어가면 Unity 스펙상 목표 반대 방향으로 이동 — speed 음수/0 가드 없음.

**PlatformRideTests (파급 효과)**
6. `PlatformStops_RiderStopsToo` — 4번과 동일 원인. 플랫폼이 멈춰 `MoveTo`를 더 이상 안 불러도 `DeltaThisFrame`이 리셋되지 않아 탑승자가 계속 밀림. **4번을 고치면 6번도 함께 해결될 가능성이 큼.**

## 4. 현재 상태 및 이슈
- 6개 실패는 전부 원인이 규명됐지만 **아직 코드 수정은 하지 않았다** (사용자가 평가만 요청).
- 4번·6번은 사실상 같은 버그(`DeltaThisFrame` 리셋 누락)이므로 이슈를 하나로 묶어 추적하는 게 맞다.

## 5. 다음 할 일 (Next Steps)
1. 5개 버그(4·6 통합) 각각에 대해 "실제 버그로 수정" 여부를 사용자와 확정 — [이동 로직 리뷰 반영 + PlayMode 테스트](./movement-review-and-tests.md) 3장에서 이미 "의도된 동작일 수도 있으니 판단 필요"라고 열어뒀던 질문의 답.
2. 수정하기로 하면 작업 전 `docs/intent/`에 각 버그 수정 의도를 정리(3장 "코딩 전 생각하기" 원칙).
3. `CharacterMotor` 쪽 3개(#1~#3)를 먼저 볼지, `TransformMotor`/탑승 쪽 2개(#4/#6, #5)를 먼저 볼지 우선순위 결정.
