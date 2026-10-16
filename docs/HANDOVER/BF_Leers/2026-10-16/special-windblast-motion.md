# Special 바람 마지막 Blast Attack 모션 누락 수정

- 날짜: 2026-10-16 / 브랜치: `feature/AI/Enemy`
- 관련: intent-024(바람 몹), `special-nexus-animation.md`

## 1. 작업 요약 및 방법
- 증상: 바람 스킬에서 날개·손바닥 충전 모션은 나오는데, 마지막 밀어내기(`@Blast Attack`) 모션이 안 나왔다.
- 원인: `Wind` trigger 하나를 Base Layer와 Layer 1(`Wind`)이 같이 쓴다. Base Layer는 `Idle`·`Move`에서만 Wind를 받을 수 있었다. 그래서 `Hit`·`Attack` 상태이거나 전환 중(Idle→Hit 0.05초, Idle↔Move 0.1초 등)일 때는 Base가 Wind를 받지 못했다. 그 사이 Layer 1 `Empty→WindCharge`가 trigger를 먼저 써 버려서, Base는 `WindCharge→WindBlast`로 가지 못했다.
- 확인 방법: Editor MCP `eval`로 Nexus 모델 인스턴스에 컨트롤러를 붙이고 `Animator.Update`로 시뮬레이션했다.
  - 수정 전: Attack 직후, Hit 직후, Hit 반복 상황에서 WindBlast가 없었다.
  - Idle에서 쓸 때는 정상이었다.
- 수정 (`Special.controller`만 수정, `AnimatorController` API로 처리, YAML 직접 편집 없음):
  1. Base Layer에 `AnyState → WindCharge`(조건 `Wind`, Exit Time 없음, Duration 0.1, Can Transition To Self 끔)를 추가했다.
  2. 이와 겹치는 `Idle → WindCharge`, `Move → WindCharge` 전환을 지웠다.
  3. Base Layer 일반 상태 전환 13개의 Interruption Source를 `None` → `Source`로 바꿨다. `None`이면 전환 도중에 AnyState 전환도 끼어들지 못한다.

## 2. 결정 사항
- `Wind` trigger 하나로 두 레이어를 움직이는 구조는 유지한다. C# 코드(`EnemyAnimator` 매핑)는 바꾸지 않았다.
- Base Layer의 바람 진입은 AnyState로 처리한다. 바람이 시작되면 맞는 모션이나 공격 모션을 끊고 바로 충전에 들어간다. 그래야 스킬 타이밍(램프 2.0초)과 Blast 시점이 맞는다.
- Interruption Source가 `Source`로 바뀌었으므로, Base Layer의 다른 전환(Idle→Move 도중 Hit 등)과 AnyState의 `Death`도 전환 도중에 끼어들 수 있다.

## 3. 현재 상태 및 이슈
- 시뮬레이션 7가지 상황에서 모두 같은 흐름이 나왔다: 0.15초에 Base `WindCharge` 진입, 2.20초에 `WindBlast`, 3.00초에 `Idle`. Layer 1도 같은 프레임에 `WindCharge`로 진입했다.
  - 상황: Idle / Attack 중 / Hit 중 / Hit 반복 / Idle→Hit 전환 중 / Idle→Move 전환 중 / Move 중
- 콘솔에 새 에러는 없다. 남은 에러 3개는 진단 중 MCP 조회 실패 로그이고, 경고는 기존 LilRobot Legacy 경고다.
- **Play에서 육안 확인은 아직 하지 않았다.**

## 4. 다음 할 일
- Play에서 Special을 계속 쏘면서 바람을 맞아 보고, 넉백 순간 Blast 모션이 나오는지 확인한다.
- Interruption Source 변경 뒤 Idle/Move/Hit 전환이 어색해지지 않았는지 함께 본다.
