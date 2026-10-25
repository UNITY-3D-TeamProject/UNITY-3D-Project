# 걸어서 떨어질 때 Run/Idle이 유지되는 원인과 수정 방향

- 일시: 2026-10-25 12:55 KST
- 요청: 점프 없이 걸어서 떨어지는 도중 이동 또는 Idle 애니메이션이 나오는 원인을 조사하고 수정 방법 비교.
- 범위: 읽기 전용 조사·설계 검토. 제품 코드·씬·프리팹·Animator Controller 수정 및 git 조작 없음. 이 협의 요약만 Codex가 작성.
- 역할·모델: Codex는 요청 전달·근거 대조·수정안 검토·요약, Claude Code Opus는 코드·컨트롤러 조사 및 대안 정리. 구현 단계와 Sonnet 호출은 없음.

## 확인된 원인 — B: 공중 연출 신호 누락

점프 없이 접지가 끊길 때 Animator에 공중 상태를 전달하는 경로가 없다. 따라서 Animator는 Idle/Locomotion을 유지하고 입력에 따라 Run 또는 Idle을 재생한다.

- `Assets/_Project/Scripts/Movement/CharacterMotor.cs:147`: `Jump()`가 `_isJumping=true`와 `OnJumpStarted`를 설정한다. 단순 낙하는 이 경로를 거치지 않는다.
- 같은 파일 `:200`: 착지 알림도 `_isJumping && isGrounded && verticalVelocity <= 0`을 요구하므로 걸어서 낙하한 뒤에는 `OnJumpEnded`가 발생하지 않는다.
- `Assets/_Project/Scripts/Mediator/SubMediators/MoveMediator.cs:113`: 접지 조회는 코요테 시각 기록에 사용되고, 단순 낙하 애니메이션 신호를 만들지 않는다.
- `Assets/_Project/Scripts/Mediator/CharacterMediator.cs:811`: 점프 시작·종료만 `PlayerAnimationMediator`에 연결한다.
- `Assets/_Project/Scripts/Mediator/SubMediators/PlayerAnimationMediator.cs:65`: `IsMoving`은 전달된 이동 방향 크기로 결정된다. 접지와 독립적이다.
- `Assets/_Project/Scripts/CharacterAnimation/PlayerAnimator.cs:323`: `PlayJump()`가 Animator의 `IsJumping`과 `Jump`를 설정한다. `PlayLand()`는 같은 파일 `:337`에서 해제한다.
- `Assets/_Project/Animations/Player/PlayerAnimator.controller:343`, `:780`: Locomotion과 Idle의 전환 목록에 JumpAir로 나가는 경로가 없다. 접지 파라미터도 없다. 컨트롤러 수정만으로는 판단 신호가 부족하다.

## 주요 대안

아래 ID는 사용자에게 제시할 기준으로 정리했다. Claude 내부 응답의 권장 C안은 여기의 A안과 같다.

| ID | 접근 | 장점 | 단점·예상 영향 |
|---|---|---|---|
| A — 권장 | `MoveMediator`에서 기존 점프 이벤트와 별개로 접지 이탈·재접지 신호를 만들고, `CharacterMediator`를 통해 플레이어 애니메이션에만 전달. 기존 JumpAir/JumpLand 재사용 검토 | 기존 Mediator 흐름 유지, 기존 점프 구독자 동작 보존 | 새 이벤트·메서드와 컨트롤러 전환 필요. 추가 점프와의 상태 이관 설계 필요 |
| B | 애니메이션 경로에 접지 조회를 주입하고 공중 연출을 갱신 | 낙하 이벤트 추가를 줄일 수 있음 | 점프는 이벤트, 낙하는 조회로 판정 경로가 갈라짐. 애니메이션 컴포넌트의 상태 관리 증가 |
| C | 공용 `CharacterMotor`의 공중·착지 의미를 단순 낙하까지 확대 | 모터에서 상태를 일관되게 제공 가능 | Skill·Pet·Enemy 등 기존 구독자에 영향. 이번 애니메이션 증상보다 범위가 넓음 |

## 검토에서 보완한 제약

- 모터의 실제 점프 플래그와 Animator의 공중 연출 플래그를 구분한다. A안에서 기존 `OnJumpStarted/OnJumpEnded`의 의미를 바꾸거나 낙하 신호를 기존 구독자에게 섞지 않는다.
- `SkillMediator.cs:295`의 착지 처리에는 추가 점프·구르기 횟수 초기화와 탈것 부착이 있다. `:491`의 탑승 처리는 단순 낙하에는 기존 착지 알림이 없다는 전제를 포함한다. 공용 이벤트 변경에는 별도 검토가 필요하다.
- Idle/Locomotion 전환 두 개만 추가하면 완결된다고 보지 않는다. SpecialIdle의 해제와 공중 이탈, JumpLand 재생 도중 다시 떨어지는 경우를 함께 확인한다.
- 낙하 추적 중 추가 점프가 발생할 때 상태를 어떻게 넘기고 착지 중복을 어떻게 막을지 명시해야 한다. 중복 `PlayLand()`가 단순 설정이라는 사실만으로 전체 안전성을 확정하지 않는다.
- 구르기 종료·공중 하차는 기존 `IsJumping` 출구 조건과 연동된다. 탑승 자세와 사망·비활성화·초기화 경계도 구현 시 확인 대상이다.

## 추천·사용자 확정·미결정

- 두 도구의 추천: A안. 기존 점프·스킬 정책을 유지하면서 누락된 플레이어 공중 연출 경로를 보완한다.
- 사용자 확정: 원인 조사와 수정 방법 모색만 요청. 수정 방식·구현은 승인되지 않았다.
- 미결정: 방식 선택, 공개 이벤트·메서드 이름, JumpAir 직접 진입 또는 별도 Fall 연출, 탑승 중 적용 방식, 추가 점프 이관 규칙.
- 접지 유예의 필요성과 값은 경사·계단 재현 전 확정하지 않는다. 기존 코요테 시간 재사용 역시 미결정이다.
- 다음 단계: 사용자가 수정 방향을 선택하면 해당 범위의 상세 계획과 필요한 결정을 정리한다. 이번 협의에서는 구현하지 않는다.

## 실제 검증과 한계

- Claude가 관련 C#과 컨트롤러 YAML을 읽어 원인과 대안을 제시했고, Codex가 모터·Mediator·PlayerAnimator·SkillMediator와 상태 전환 목록을 직접 대조했다. 제품 파일 변경 도구는 사용하지 않았다.
- 최초 중계 완료 판정 실패 후 소유 세션을 정리·재개하여 결과를 회수했다. 조사 결과와 검토 반영 응답 모두 최종 JSON `state=completed`, exit 0을 확인했다. 원문은 Claude 데스크톱 앱 Code 탭에서 확인한다.
- 컴파일·Play·실제 낙하 재현은 하지 않았다. 프리팹 실제 배선·클립 설정·MountJump/MountAir 출구·Update 실행 순서는 상세 검증하지 않았다.
- 계단·경사의 일시적 접지 끊김은 가능성으로만 남아 있으며 이번 증상의 추가 원인으로 확정하지 않았다.
- 구현 후 검증 항목: 입력 유지/해제 낙하, 착지, 특수 Idle 중 발판 이탈, JumpLand 중 재낙하, 낙하 중 추가 점프·구르기·하차, 경사·계단, 일반·코요테·버퍼 점프와 Skill·Pet·Enemy 회귀.
- 스킬이 참조한 `docs/player-animation-reference.md`는 현재 경로에 없었으므로 실제 구현을 기준으로 확인했다. 문서 이동·복구는 하지 않았다.
