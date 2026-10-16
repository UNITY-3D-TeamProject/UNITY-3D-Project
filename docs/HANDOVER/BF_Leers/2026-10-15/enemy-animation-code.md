# AI 애니메이션 코드 1차 (intent-025)

## 작업 요약 및 방법
- 신규: `EnemyAnimator`(이동·피격·사망·스킬 이름→파라미터 매핑), `JumpAnimator`(보행자 점프 전용), `EnemyAnimationMediator`(선택 참조 `_jumpAnimator`).
- 수정(추가만): `CharacterMediator`(`_enemyAnimationMediator` 필드·구독·피격·사망), `SkillMediator`(`OnSkillStopped` 이벤트, 정지 요청이 이 이벤트를 거치도록 `BindRequest` 한 줄 변경).
- 문서: intent-025, `docs/features/enemy-animation-controller-guide.md`(컨트롤러 수작업 가이드).

## 결정 사항
- 객체 전용 로직은 상속이 아니라 기능별 컴포넌트로 분리한다. 스킬별 모션은 데이터 매핑이다.
- 컨트롤러는 베이스 + Override, 전용 상태가 필요한 객체만 복사본. MCP로 만들지 않고 사용자가 직접 만든다.

## 현재 상태 및 이슈
- **컴파일 미확인**(MCP recompile을 사용자가 거절함). 에디터에서 에러 확인 필요.
- 컨트롤러·프리팹 배선은 [USER] 미완. Play 검증 미실시.
- `CharacterMediator`/`SkillMediator`는 이세훈 담당 파일이라 공유 필요. `feature/AI/Enemy`도 `CharacterMediator`를 수정하므로 머지 충돌 주의.

## 다음 할 일
1. 컴파일 에러 0 확인.
2. 가이드대로 컨트롤러·프리팹 작성 후 Melee/Ranged/Pedestrian Play 검증.
3. `feature/AI/Enemy` 머지 후 풀 재사용 시 애니메이션 초기화 확인.
4. 특수·도둑·보스·GC는 프리팹이 생기면 매핑·컨트롤러만 추가.

## 추가: 컨트롤러 MCP 생성 (같은 날)
- `Assets/_Project/Animations/Enemy/`: `EnemyBase.controller`, `Ranged.controller`(IsWindup/Windup, Windup→Attack 우선), `Pedestrian.controller`(IsJumping/Jump, Attack·Hit·Death 제거), `Melee_Override.overrideController`, 자리표시 클립 `Clips/Enemy_*.anim`.
- Move 상태 Speed Multiplier = `MoveAnimSpeed`, Any→Death `canTransitionToSelf=false`를 eval로 설정·확인.
- 남은 [USER]: 실제 클립 교체(Override·컨트롤러 슬롯), 프리팹 배선(Animator 연결·RootMotion 끄기·컴포넌트 추가).
- 컨트롤러 구조는 `get_animator_controller`/eval로 가이드와 대조했으며 Play 검증은 아직 안 했다.
