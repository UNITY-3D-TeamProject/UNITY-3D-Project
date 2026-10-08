# 점프 API 변경: Jump(float jumpPower) + 시작/착지 이벤트 (intent-015)

## 1. 작업 요약 및 방법
- `CharacterMotor`
  - `JumpSpeed` 프로퍼티, `_jumpPower`/`_jumpRequested` 필드, `ConsumeJumpRequest()` 제거.
  - `Jump()` → `Jump(float jumpPower)`: 접지 체크 없이 즉시 `_verticalVelocity = jumpPower`, `_isJumping = true`, `OnJumpStarted` 발행. (`_controller.enabled` 가드만 유지 — 비활성이면 Move가 안 돌기 때문)
  - `Move()`에서 `_controller.Move` 직후 `_isJumping && isGrounded && _verticalVelocity <= 0`이면 `OnJumpEnded` 발행.
  - `event Action OnJumpStarted`, `event Action OnJumpEnded` 추가.
- `MoveMediator`
  - `_jumpPower` 필드 추가. 어트리뷰트 콜백/`InitValue`가 모터 대신 여기에 저장.
  - `CommandJump()`: 모터 없음 / 구르는 중 / 공중이면 무시, 아니면 `_motor.Jump(_jumpPower)`.
  - `_jumpSpeedValueKey` 필드명 유지(프리팹 직렬화 보존), Tooltip만 수정.
- 검증: `unity run` 배치 모드로 Assembly-CSharp 재컴파일, `error CS` 0건.

## 2. 결정 사항
- 점프는 모터에 유지하고 API만 변경(사용자 결정). 별도 Jumper 컴포넌트 만들지 않음.
- 모터의 `Jump`는 무조건 실행, "지상에서만" 정책은 중재자가 판단 → 더블점프 스킬 등은 접지 무관하게 `Jump` 호출 가능.
- "점프 종료" = 착지 시점(사용자 결정). 점프 없이 떨어진 경우는 이벤트 없음.
- 공중 재점프 시 `OnJumpStarted`는 매번 발행, `OnJumpEnded`는 착지 시 1회.
- 이벤트를 중재자에서 구독/중계하는 코드는 소비자가 없어 넣지 않음.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. **Play 모드 검증 미완**.
- 이전에는 접지 판정을 Update로 미뤄 입력 씹힘을 막았는데(intent-007 시절 FixedUpdate 문제), 지금은 모터가 Update에서 Move하므로 입력 시점의 `IsGrounded`는 직전 프레임 Move 결과다. Play에서 Space 입력이 씹히는지 확인 필요.

## 4. 다음 할 일 (Next Steps)
- Play 검증: 지상 점프 / 공중 입력 무시 / 구르기 중 무시 / `OnJumpStarted`·`OnJumpEnded` 발행 타이밍.
- 애니메이션·더블점프 스킬(`DoubleJump.cs`, 현재 Debug.Log만)에서 필요해지면 중재자가 이벤트 구독 + 공중 `Jump` 호출 경로 추가.
- 검증 후 intent-015 resolved 처리.
