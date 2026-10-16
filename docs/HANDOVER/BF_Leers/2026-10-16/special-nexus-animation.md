# Special(Nexus Robot) 애니메이션 추가

- 날짜: 2026-10-16 / 브랜치: `feature/AI/Enemy`
- 관련: intent-024(바람 몹), `special-prefab-weakpoint.md`

## 1. 작업 요약 및 방법
- Special 프리팹이 ThirdParty `Nexus Robot.controller`(파라미터 0개)를 직접 쓰고 `EnemyAnimator`도 없어서 AI 동작에 애니메이션이 반응하지 않았다. Melee·Ranged와 같은 구조로 맞췄다. C# 코드는 바꾸지 않았다.
- 신규 `Assets/_Project/Animations/Enemy/Special.controller` — 파라미터 `IsMoving`(bool), `MoveAnimSpeed`(float), `Attack`·`Hit`·`Death`·`Wind`(trigger).

  | 상태 | 클립 |
  |---|---|
  | Idle / Move | `Nexus Robot@Idle` / `@Fly Forward In Place` (Move는 `MoveAnimSpeed`로 배속) |
  | Attack | `@Right Claw Attack` |
  | Hit | `@Take Damage` |
  | Death | `@Die` (AnyState, 상태 이름 `Death`는 `EnemyAnimator`가 종료 판정에 씀) |
  | WindCharge → WindBlast | Idle 클립 2.0초 → `@Blast Attack` |

- 바람 준비 모션: 신규 `Clips/Special_WindCharge.anim`(2.0초, 비루프)이 날개 4개(`RigWing*`)는 점점 커지는 흔들림, 손바닥 2개(`RigLArmPalm`·`RigRArmPalm`)는 점점 빨라지는 롤 회전을 한다. 일부 뼈만 건드리는 클립이라 Layer 1 `Wind`(Override, 신규 `Special_WindMask.mask`: 날개 4 + 손바닥 2)에 두고, 몸통은 Base Layer가 Idle을 유지한다.
- `Special.prefab`: Animator 컨트롤러 교체, Apply Root Motion 끔, 같은 오브젝트에 `EnemyAnimator` 추가(`Attack→Attack`, `Wind→Wind`, 둘 다 Trigger), `EnemyAnimationMediator._enemyAnimator` 연결. Editor MCP `run_script`로 `LoadPrefabContents` → 수정 → `SaveAsPrefabAsset` 했고 YAML은 직접 건드리지 않았다.

## 2. 결정 사항
- Attack = Right Claw Attack, Wind = 날개·손바닥 회전 후 Blast Attack (사용자 지정).
- Wind는 Hold(Bool)가 아니라 Trigger다. `EnemyWindBlast`가 끝날 때 `StopSkill`을 부르지 않아 Bool은 켜진 채로 남기 때문이다. 대신 컨트롤러가 시간(램프 2.0초)으로 WindCharge → WindBlast를 이어 붙인다.
- 클립이 ThirdParty FBX를 참조하는 것은 Ranged와 같은 기존 관례를 따랐다.

## 3. 현재 상태 및 이슈
- 컨트롤러 구조를 `get_animator_controller`로 확인했고, 프리팹 YAML에서 컨트롤러·Root Motion 0·`EnemyAnimator` 매핑·미디에이터 연결을 확인했다. 새 컴파일/애니메이션 에러·경고는 없다.
- Nexus 모델 단독 인스턴스에 컨트롤러를 붙여 `Animator.Update`로 시뮬레이션했다. `Wind` trigger 하나로 Base(WindCharge→WindBlast→Idle)와 Layer 1(WindCharge→Empty)이 같이 반응했고, 손바닥 회전량이 1.0초 162°, 1.8초 169°(Idle 기준), 날개는 6.5° → 18.6°로 커지는 것을 확인했다.
- **Play에서 실제 Special 스폰 검증은 미완**(이동·공격·피격·사망·바람 연출을 눈으로 보지 않음). 날개·손바닥 회전 축과 각도(날개 로컬 Z ±30°, 손바닥 로컬 X 720°)는 뼈 축을 보지 않고 정한 1차 값이라 어색할 수 있다.
- `EnemyWindBlast._rampDuration`(현재 2.0)을 바꾸면 `Special_WindCharge.anim` 길이와 Base Layer `WindCharge → WindBlast` Exit Time(= 램프 ÷ Idle 길이 1.0)도 같이 바꿔야 한다. 램프 도중 대상을 놓쳐 스킬이 일찍 끝나도 모션은 끝까지 재생된다.
- 콘솔의 `LilRobot` Legacy 경고는 기존 경고다.

## 4. 다음 할 일
- Play에서 Special을 스폰해 Idle/Move 전환, 근접 공격, 피격, 바람(날개·손바닥 → 미는 동작), 사망 후 풀 반납을 확인한다.
- 회전이 어색하면 `Special_WindCharge.anim` 곡선(또는 마스크 범위)을 에디터에서 조정한다.
