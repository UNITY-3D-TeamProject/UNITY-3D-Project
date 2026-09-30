# CharacterMotor 회전 제거 + SOMovementConfig 삭제 (2026-09-14)

## 1. 작업 요약 및 방법
- 회의 결과 플레이어는 **WASD 이동 + 마우스 회전**으로 확정 → `intent-003` 열린 질문 7 해결.
- `CharacterMotor`
  - `Move()`의 이동 방향 회전(`FaceDirection`) 삭제. `Move(direction, speed)` 시그니처는 그대로.
  - `SOMovementConfig` 참조(`_config`, `Config` 프로퍼티, `Debug.Assert`) 삭제, 중력은 `[SerializeField] private float _gravity = -20.0f;`로 이동.
- `SOMovementConfig.cs`(+meta) 삭제. 로컬 `Assets/Test/SOMovementConfig.asset`도 삭제.
- 로컬 스파이크 `Assets/Test/Scripts/NavAgentMotorSpike.cs`: `_motor.Config.RotationSpeed` → 자체 `_rotationSpeed` 필드. Motor가 더 이상 회전하지 않아 필요 없어진 "Move 전 회전 저장/복원" 코드 제거.
- `intent-002`(FaceDirection 유지 / SOMovementConfig 유지 결정)에 취소선 + 대체 결정, `intent-003` 질문 4·7과 영향 범위 갱신.

## 2. 결정 사항
- **컨트롤러는 `_motor.Move(방향, 속도)`만 호출한다.** 중력·지면 부착은 Motor가 내부에서 처리한다 (사용자 요구: 호출 규칙이 복잡한 API 금지).
  - 검토 후 기각: 중력을 컨트롤러가 계산해 `AddExternalDisplacement`로 넣는 방식 / `Move(Vector3 velocity)` / 수직 속도 인자 추가 — 호출자가 알아야 할 것이 늘어난다.
  - 중력은 WASD만 해도 필요하다: 턱에서 떨어지기, 내리막 부착, `isGrounded` 안정화.
- 중력 값은 Motor 인스펙터 필드. `Physics.gravity`는 전역(Rigidbody 영향)이고 기본값이 -9.81이라 쓰지 않는다.
- **회전은 호출자 소유.** Player = 마우스, Enemy = `agent.steeringTarget` 기준. 회전 속도도 호출자가 가진다.
- 이제 코어 두 모터 모두 튜닝 SO가 없다. 이동 속도·회전 속도는 각 파트 컨트롤러/SO 소유.

## 3. 현재 상태 및 이슈
- Grep 기준 `Assets/` 코드에 `SOMovementConfig|RotationSpeed|FaceDirection|.Config` 참조 0건.
- **Unity 컴파일 / PlayMode 확인 미완.** `Test.unity`의 CharacterMotor는 `_gravity` 기본값 -20으로 들어가야 함.
- 커밋 안 함.

## 4. 다음 할 일
- Unity에서 컴파일 에러 없는지, 스파이크 씬에서 적이 바닥에 붙어 이동하고 steeringTarget 기준으로 회전하는지 확인.
- 확인되면 커밋 (`refactor : CharacterMotor 회전 제거 및 SOMovementConfig 삭제` 형식).
- `intent-003` 나머지 열린 질문(2~6) 진행.
