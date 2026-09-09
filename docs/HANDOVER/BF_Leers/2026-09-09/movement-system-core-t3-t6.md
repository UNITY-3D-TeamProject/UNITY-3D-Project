# Movement System 코어 T3-T6 구현

## 작업 요약
- 이전 세션([T1-T2 문서](./movement-system-core-t1-t2.md))에 이어 `/start-feature`로 T3~T6를 구현했다.
- T3: `IMoveMotor` 계약 정의.
- T4: `MovementMotor`(MonoBehaviour 위임자) — `GetComponent<IMoveIntentProvider>()`/`GetComponent<IMoveMotor>()`로 인터페이스 구현체를 자동 해석해 `Update`/`FixedUpdate` 중 `IMoveMotor.UseFixedTick`에 맞는 쪽에서만 위임.
- T5: `CompositeMoveIntentProvider`(여러 provider를 하나의 `SMoveIntent`로 합치는 추상 베이스, 기본 규칙은 override-if-non-default) + 이 과정에서 최초로 EditMode NUnit 테스트를 도입.
- T6: `CharacterControllerMotor`(실제 씬 오브젝트에 붙는 프로덕션 Motor 구현) + 검증용 `DummyMoveIntentProvider`.
- **미해결**: 사용자가 테스트 GameObject에 `CharacterController` + `CharacterControllerMotor` + `MovementMotor` + `DummyMoveIntentProvider`를 붙여 PlayMode로 확인하는 중인데 "테스트가 잘 안 된다"고 보고 — 구체적 증상은 파악 전에 세션이 종료됨. 다음 세션에서 이어서 디버깅 필요.

## 방법/접근
- **T5 asmdef 삽질**: 애초 계획은 "운영 코드(`Scripts/System/`)에는 asmdef를 추가하지 않고, 테스트 asmdef가 `Assembly-CSharp`을 이름으로 참조하면 된다"였는데 이게 틀렸다. Unity는 커스텀 asmdef가 암묵적 기본 어셈블리(`Assembly-CSharp`)를 참조하는 것을 지원하지 않는다(컴파일 순서상 `Assembly-CSharp`이 항상 가장 마지막에 컴파일되어 역참조가 애초에 불가능). 사용자가 Test 폴더를 이동하면서 컴파일 에러를 보고했고, 원인을 진단한 뒤 `Scripts/System/`에도 생산 코드용 `ProjectSystem.asmdef`를 추가해 테스트 asmdef가 이를 GUID로 참조하는 구조로 바꿨다. 그 과정에서 Unity Inspector에서 사용자가 직접 References를 만지다가 `Assembly-CSharp`과 자기 자신을 참조로 넣는 등 상태가 꼬였던 것도 코드로 정리했다.
- **테스트 코드 위치 변경**: 사용자가 Editor에서 `Scripts/Test/EditMode/`를 `Test/EditMode/`(`Assets/_Project/Test/EditMode/`)로 직접 이동함 — `spec.md`, `intent-001` 문서에 반영 완료.
- **T5 설계 의도 재확인**: 사용자가 "왜 여러 provider를 하나로 합치나, 오브젝트가 자기 Intent를 갖고오면 되는 거 아니냐"고 질문 → 한 오브젝트에 이동 의도를 만드는 소스가 여러 개(입력/넉백/외력 등)일 수 있고, `MovementMotor`가 `GetComponent<IMoveIntentProvider>()`로 딱 하나만 찾는 구조이기 때문에 그 소스들을 하나로 합치는 지점이 필요하다고 설명해 확인받음. Player 담당자가 입력/피격/외력 provider를 각각 구현하고, `CompositeMoveIntentProvider`를 상속(또는 합성)해 자기만의 합산 규칙을 정하는 구조로 합의.
- **T6 범위 확인**: T6가 테스트용 더미가 아니라 실제 오브젝트에 붙는 프로덕션 코드임을 설명한 뒤, "중력/점프까지 포함할지" 질문 → 사용자가 포함하기로 결정. 이에 따라 기존 T2 `SOMovementConfig`에 `Gravity`, `JumpForce` 필드를 추가로 얹었다(파일 자체는 삭제/재작성 없이 필드만 추가하는 외과적 변경).
- T6 검증을 위해 `DummyMoveIntentProvider`(Inspector에서 Direction/Speed/IsSprinting/IsJumping을 직접 조절하는 MonoBehaviour)를 별도로 만들어 사용자에게 Editor에서 붙이는 절차를 안내함.

## 변경된 파일
- `Assets/_Project/Scripts/System/IMoveMotor.cs` (+ `.meta`) — T3
- `Assets/_Project/Scripts/System/MovementMotor.cs` (+ `.meta`) — T4
- `Assets/_Project/Scripts/System/CompositeMoveIntentProvider.cs` (+ `.meta`) — T5
- `Assets/_Project/Scripts/System/ProjectSystem.asmdef` (+ `.meta`, 신규) — T5 asmdef 수정 과정에서 추가
- `Assets/_Project/Test/EditMode/SystemEditModeTests.asmdef` (+ `.meta`) — T5 (경로는 사용자가 이동)
- `Assets/_Project/Test/EditMode/CompositeMoveIntentProviderTests.cs` (+ `.meta`) — T5
- `Assets/_Project/Scripts/System/SOMovementConfig.cs` — T6에서 `Gravity`/`JumpForce` 필드 추가(기존 파일 수정)
- `Assets/_Project/Scripts/System/CharacterControllerMotor.cs` (+ `.meta`) — T6
- `Assets/_Project/Scripts/System/DummyMoveIntentProvider.cs` (+ `.meta`) — T6 검증용
- `spec.md`, `docs/intent/intent-001-movement-system-core.md` — asmdef 구조 변경, Test 폴더 경로, T6 Gravity/JumpForce 관련 내용 동기화

## 결정 사항
- **네임스페이스**: 여전히 임시 생략 상태 유지(intent-001 참고, `docs/coding-convention.md` 팀 기준 갱신 전까지).
- **asmdef 구조 확정**: `Scripts/System/ProjectSystem.asmdef`(생산 코드) + `Test/EditMode/SystemEditModeTests.asmdef`(테스트, `ProjectSystem`을 GUID로 참조). 커스텀 asmdef는 `Assembly-CSharp`을 참조할 수 없다는 게 이번 세션에서 얻은 핵심 교훈.
- **CharacterControllerMotor 범위**: 수평 이동(가속/감속) + 중력/점프까지 포함하는 완전한 레퍼런스 구현으로 확정. `UseFixedTick = false`(Unity 관례상 `CharacterController.Move()`는 `Update`에서 호출).
- **`IsSprinting`은 Motor가 소비하지 않음**: `Speed` 필드가 이미 provider가 결정한 최종 목표 속도라는 전제(스프린트 반영은 provider 책임).

## 현재 상태 및 이슈
- T3~T6 모두 Editor 컴파일 에러 없음, T5 EditMode 테스트 2개 통과 확인됨(사용자 확인).
- **T6 PlayMode 수동 검증이 안 되고 있음** — 사용자가 `CharacterController` + `CharacterControllerMotor` + `MovementMotor` + `DummyMoveIntentProvider`를 GameObject에 붙여 테스트했는데 "잘 안 된다"고만 보고, 구체적 증상(안 움직임? 튐? 에러 로그?)은 확인하지 못한 채 세션 종료.
  - 다음 세션에서 확인할 만한 포인트:
    - `MovementMotor`의 `Movement Config` 필드에 `SOMovementConfig` 에셋이 실제로 연결됐는지 (`Awake()`의 `Debug.Assert` 경고가 콘솔에 떴는지 확인).
    - `DummyMoveIntentProvider`의 `Direction`/`Speed` 값이 0이 아닌지.
    - 테스트 오브젝트 아래에 바닥(Collider)이 있는지 — 없으면 `CharacterController.isGrounded`가 계속 false라 중력만 누적되며 계속 낙하할 수 있음.
    - `CharacterController`의 `Center`/`Radius`/`Height`가 기본값 그대로라 오브젝트 위치와 안 맞을 가능성(모델 크기에 안 맞으면 계속 충돌 판정이 이상해질 수 있음).
    - Play 진입 시 콘솔에 다른 에러/경고가 없는지.
- `.asset` 인스턴스(`SOMovementConfig`)가 실제로 생성·연결됐는지는 미확인.

## 다음 할 일
- **최우선**: T6 PlayMode 테스트 안 되는 문제 디버깅. 위 "현재 상태 및 이슈"의 체크리스트부터 확인.
- 문제 해결되면 T7(기존 스텁 `Assets/_Project/Scripts/System/IMoveProvider.cs` 삭제, `.meta`도 함께 제거) → T8(intent 문서 최종 정리, `docs/intent/clear/`로 이동)로 진행.
- 새 세션에서는 이 문서와 `spec.md`, `docs/intent/intent-001-movement-system-core.md`를 먼저 읽고 이어서 진행할 것.
