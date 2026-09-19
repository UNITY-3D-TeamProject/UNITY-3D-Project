# 이동 로직 코드 리뷰 반영 + PlayMode 테스트 추가

## 1. 작업 요약 및 방법
`/code-review` 스킬로 이동 로직(`CharacterMotor`, `TransformMotor`)을 Standards(코딩 규칙)/Spec(intent 문서) 두 축으로 리뷰했다. 기준점은 `main` + 미커밋 변경(당시 `CharacterMotor.cs`에 `Direction`/`Speed` 프로퍼티 + `FixedUpdate` 구동으로 바꾸는 미커밋 수정이 있었다).

리뷰에서 나온 지적을 사용자와 하나씩 확인해 반영 범위를 정했다:
- **네임스페이스 규칙(`Project.<폴더>`) 위반** — 코드가 아니라 **문서 쪽이 실제 관례와 달랐던 것**으로 결론. `docs/coding-convention.md`의 네임스페이스 규칙 예시를 `Project.` 접두사 없는 실제 관례(`Movement`, `Player`, `Enemy.AI`)로 고쳤다.
- **`FixedUpdate` 구동, `Direction`/`Speed` 프로퍼티, `_gravity` 부호(`20`, `-=`)** — **유지 결정.** Player/Enemy 둘 다 CharacterController 이동을 FixedUpdate에서 처리하기 때문. 이 방향으로 미커밋 변경을 그대로 커밋 대상에 남기고, 대신 이 설계와 어긋난 주석/문서만 고쳤다.
  - `CharacterMotor.cs`: `AddExternalDisplacement` 주석의 잘못된 설명("한 프레임에 2번 부르지 않게끔") 제거, `_gravity`에 Tooltip 추가.
  - `docs/features/movement-system.md`: 사용 예시·API 표·주의사항을 `Move(direction, speed)` 호출형에서 `Direction`/`Speed` 프로퍼티 + FixedUpdate 자동 구동형으로 갱신.
  - `docs/intent/intent-002-movement-core-scope.md` 48행: 옛 API를 취소선 처리하고 변경 사유를 덧붙였다.

추가로 사용자가 "이동 관련 버그가 날 만한 모든 상황"을 테스트해보고 싶다고 해서, Unity Test Runner PlayMode 자동 테스트를 작성했다. 코드가 아니라 로컬 검증 도구라 `Assets/Test/`(Git 미포함) 아래 두었다.

## 2. 결정 사항
- **네임스페이스는 `Project.` 접두사 없이 `Scripts/` 폴더명 그대로** 짓는다. `docs/coding-convention.md` 3-6절, using 예시, 요약표, 전체 예시를 모두 이 방식으로 갱신했다.
- **`CharacterMotor`는 `FixedUpdate` 구동 + `Direction`/`Speed` 프로퍼티 방식이 최종 설계다.** 이전 intent 문서의 `Move(direction, speed)` 호출형은 폐기됐다.
- **테스트를 실행하려면 이동 스크립트가 별도 어셈블리에 있어야 한다** (PlayMode 테스트 asmdef는 `Assembly-CSharp`를 참조 못 함). 그래서 `Assets/_Project/Scripts/Movement/Movement.asmdef`(신규, Git에 올라감, `autoReferenced: true`)를 추가했다. 기존에 `using Movement`를 쓰던 `Assets/Test/PlayerTestController.cs`, `Assets/Test/Scripts/NavAgentMotorSpike.cs`는 `autoReferenced` 덕분에 수정 없이 그대로 컴파일되어야 한다(미확인 — 아래 이슈 참고).

## 3. 현재 상태 및 이슈
- **컴파일/PlayMode 실행 미확인.** 에이전트가 Unity를 실행할 수 없어서, 다음 세션(또는 사용자)이 에디터에서 직접 확인해야 한다. 확인 항목:
  1. `Movement.asmdef` 추가 후 콘솔에 컴파일 에러 없음.
  2. 기존 씬/프리팹의 `CharacterMotor` 참조 유지(MonoBehaviour는 GUID 기반이라 유지되어야 하나 실물 확인 필요).
  3. Window > General > Test Runner > PlayMode > Run All 결과.
- **리뷰에서 나온 버그 의심 사항은 이번 범위에서 고치지 않고, `Assets/Test/Movement/`에 `[Category("KnownBug")]` 테스트로만 남겼다.** 실행 결과로 실제 버그 여부를 판단할 것:
  - `TransformMotor.DeltaThisFrame`이 `MoveTo` 미호출 프레임에 초기화되지 않음 → 플랫폼 정지 후 탑승자가 계속 밀림 (`PlatformRideTests.PlatformStops_RiderStopsToo`, `TransformMotorTests.DeltaThisFrame_IsZero_WhenMoveToNotCalledThisFrame`).
  - `CharacterMotor.Direction`에 y 성분이 있으면 `normalized`가 이를 포함해 수평 속도가 줄고 위로 뜨는 힘이 생김 (`DirectionWithVerticalComponent_KeepsHorizontalSpeed_AndDoesNotLift`).
  - `TransformMotor.MoveTo`에 음수 speed를 주면 목표 반대 방향으로 이동 (`MoveTo_NegativeSpeed_DoesNotMoveAwayFromTarget`).
  - `CharacterController.enabled = false` 상태에서 FixedUpdate가 `Move`를 호출하면 Unity 경고 로그 발생 가능 (`DisabledCharacterController_LogsNoWarningOrError`).
  - `transform.position` 직접 대입이 `autoSyncTransforms` 설정에 따라 되돌려질 가능성 (`TeleportByTransformPosition_IsKept`).
- **탑승 스니펫(`Update` + `DefaultExecutionOrder`)과 모터의 `FixedUpdate` 구동이 실행 순서상 맞는지 확인되지 않았다.** `docs/features/movement-system.md` 5-6장의 탑승 관련 서술은 이번에 손대지 않았다 — 실제 정합성 확인 후 갱신 필요.

## 4. 다음 할 일 (Next Steps)
1. Unity 에디터에서 컴파일 확인 → PlayMode Test Runner 실행 → 결과(특히 KnownBug 6종) 공유.
2. KnownBug 테스트 결과에 따라 각각 "실제 버그로 수정" vs "의도된 동작이니 테스트를 현재 동작에 맞게 고침"을 결정.
3. 탑승 스니펫과 `FixedUpdate` 구동 방식의 실행 순서 정합성을 별도로 검토(발판 탑승 시 슬라이딩 회귀 가능성).
4. `docs/features/movement-system.md` 5장의 "이동 Platform에 Kinematic Rigidbody 필수" 등 세팅 요구사항이 `FixedUpdate` 구동 방식에서도 그대로 유효한지 확인.
