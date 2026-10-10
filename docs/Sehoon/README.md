# SeHoon 작업 목록

앞으로 할 기능을 우선순위 순으로 정리한 개인 작업 문서다. 완료된 문서는 `done/` 폴더로 옮긴다.

## 규칙
- 파일명 앞 번호가 작업 순서다. (`01-` 이 가장 먼저)
- 각 문서의 **열린 질문**이 모두 답변되기 전에는 구현을 시작하지 않는다.
- 완료되면 문서 상단 상태를 `완료`로 바꾸고 `done/` 으로 옮긴 뒤, 아래 목록 상태를 갱신한다.
- 작업 세션은 `/sehoon-task {번호}` 로 시작한다 (`.claude/skills/sehoon-task/`).

## 작업 순서

### 최우선 (고정)
| 순서 | 기능 | 문서 | 상태 |
|---|---|---|---|
| 01 | SO Apply 함수 퍼사드에 추가 | [01-facade-so-apply.md](01-facade-so-apply.md) | 대기 |
| 02 | 멀티점프 | [02-multi-jump.md](02-multi-jump.md) | 대기 |
| 03 | 점프 중 대쉬 1회 제약 | [03-air-dash-limit.md](03-air-dash-limit.md) | 대기 |
| 04 | Fire 구조 차지샷으로 변경 | [04-fire-charge-shot.md](04-fire-charge-shot.md) | 대기 |

### 우선순위 높음
| 순서 | 기능 | 문서 | 상태 |
|---|---|---|---|
| 05 | 대쉬 중 사격·Aim 규칙 | [05-dash-fire-aim-rules.md](05-dash-fire-aim-rules.md) | 대기 |
| 06 | Heat 게이지 구조 수정 | [06-heat-gauge-fix.md](06-heat-gauge-fix.md) | 대기 |
| 07 | 스킬 구현 (블루투스, 상호작용) | [07-skill-implementation.md](07-skill-implementation.md) | 대기 |
| 08 | 블루투스 모션 추가 | [08-bluetooth-motion.md](08-bluetooth-motion.md) | 대기 |
| 09 | Hit 시 방향별 모션 | [09-directional-hit-motion.md](09-directional-hit-motion.md) | 대기 |

### 우선순위 중간
| 순서 | 기능 | 문서 | 상태 |
|---|---|---|---|
| 10 | 피격 시 깜빡임 + 무적 | [10-hit-blink-invincibility.md](10-hit-blink-invincibility.md) | 대기 |
| 11 | 플레이어 회전 속도 체크 | [11-rotation-speed-check.md](11-rotation-speed-check.md) | 대기 |
| 12 | Idle 특수 애니메이션 | [12-idle-special-animation.md](12-idle-special-animation.md) | 대기 |

### 우선순위 낮음
| 순서 | 기능 | 문서 | 상태 |
|---|---|---|---|
| 13 | 총기 과열 색상 변경 | [13-overheat-color.md](13-overheat-color.md) | 대기 |

## 같은 우선순위 안에서 순서를 정한 근거
- **최우선** (사용자 지정, 고정)
  - 01: 범위가 작고, 다른 파트(기믹·스테이지)가 바로 쓰는 외부 창구라 먼저 연다.
  - 02 → 03: 대쉬 횟수 초기화 규칙이 멀티점프 구조(공중 상태 판정)와 맞물려 멀티점프를 먼저 한다.
  - 04: 범위가 가장 크다(입력·스킬·투사체). 기존 코스트 구조로 먼저 구현하고 06에서 코스트를 교체한다.
- **높음**
  - 05: 03(대쉬)과 04(차지샷)가 만나는 조작 규칙이다. 두 작업 직후에 붙여 대쉬·사격 조작을 한 번에 마무리한다.
  - 06: 현재 사격 버그이고, 사격 구조 작업(04, 05) 직후에 코스트를 교체한다.
  - 07 → 08: 스킬은 스테이지 진행에 필요한 핵심 플레이다. 08은 07의 블루투스 스킬에 의존한다.
  - 09: 연출이다. 피격 방향 정보를 전투 이벤트에 추가해야 해서 10과 설계를 함께 잡는 편이 낫다.
- **중간**: 10은 무적 판정이 밸런스에 직접 영향을 준다. 11은 조작감, 12는 순수 연출이다.
