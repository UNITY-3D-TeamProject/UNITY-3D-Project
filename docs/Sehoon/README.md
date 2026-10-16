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
| 01 | SO Effect / Context Apply 함수 추가 (CharacterMediator + 퍼사드) | [01-facade-so-apply.md](done/01-facade-so-apply.md) | 완료 |
| 02 | 멀티점프 | [02-multi-jump.md](done/02-multi-jump.md) | 완료 |
| 03 | 점프 중 대쉬 1회 제약 | [03-air-dash-limit.md](done/03-air-dash-limit.md) | 완료 |
| 04 | Fire 구조 차지샷으로 변경 | [04-fire-charge-shot.md](done/04-fire-charge-shot.md) | 완료 |

### 우선순위 높음
| 순서 | 기능 | 문서 | 상태 |
|---|---|---|---|
| 05 | 대쉬 중 사격·Aim 규칙 | [05-dash-fire-aim-rules.md](done/05-dash-fire-aim-rules.md) | 완료 |
| 06 | Heat 게이지 구조 수정 | [06-heat-gauge-fix.md](done/06-heat-gauge-fix.md) | 완료 |
| 07 | 스킬 구현 (블루투스, 상호작용, 스캔) | [07-skill-implementation.md](done/07-skill-implementation.md) | 완료 |
| 07-1 | └ 블루투스 스킬 | [07-1-bluetooth-skill.md](done/07-1-bluetooth-skill.md) | 완료 |
| 07-2 | └ 상호작용 스킬 | [07-2-interact-skill.md](done/07-2-interact-skill.md) | 완료 |
| 07-3 | └ 스캔 스킬 | [07-3-scan-skill.md](done/07-3-scan-skill.md) | 완료 |
| 08 | 블루투스 모션 추가 | [08-bluetooth-motion.md](done/08-bluetooth-motion.md) | 완료 |
| 09 | Hit 시 방향별 모션 | [09-directional-hit-motion.md](done/09-directional-hit-motion.md) | 완료 |
| 21 | 블루투스 디테일 수정 (자동 전진, 요시 점프, 회전 속도) | [21-bluetooth-detail.md](done/21-bluetooth-detail.md) | 완료 |
| 22 | 약점 공격 시 대미지 처리 | [22-weak-point-damage.md](done/22-weak-point-damage.md) | 완료 |

### 우선순위 중간
| 순서 | 기능 | 문서 | 상태 |
|---|---|---|---|
| 10 | 피격 시 깜빡임 + 무적 | [10-hit-blink-invincibility.md](done/10-hit-blink-invincibility.md) | 완료 |
| 11 | 플레이어 회전 속도 체크 | [11-rotation-speed-check.md](done/11-rotation-speed-check.md) | 완료 |
| 12 | Idle 특수 애니메이션 | [12-idle-special-animation.md](done/12-idle-special-animation.md) | 완료 |

### 우선순위 낮음
| 순서 | 기능 | 문서 | 상태 |
|---|---|---|---|
| 13 | 총기 과열 색상 변경 | [13-overheat-color.md](done/13-overheat-color.md) | 완료 |
| 14 | 사격·조준 중 이동 속도 감소 | [14-fire-aim-move-slow.md](done/14-fire-aim-move-slow.md) | 완료 |
| 15 | 캐릭터 사망 시 처리 | [15-character-death.md](done/15-character-death.md) | 완료 |
| 16 | 이동속도에 맞춘 이동 애니메이션 재생 속도 | [16-move-anim-speed.md](done/16-move-anim-speed.md) | 완료 |
| 17 | 조준 중 사격 자세 유지 | [17-aim-fire-pose.md](done/17-aim-fire-pose.md) | 완료 |

### 우선순위 미정
| 순서 | 기능 | 문서 | 상태 |
|---|---|---|---|
| 18 | 플레이어 카메라 뷰 수정 (화면 고정 위치) | [18-camera-fixed-screen-position.md](done/18-camera-fixed-screen-position.md) | 완료 |
| 19 | 펫 시스템 (따라다니기 + 애니메이션 동기화, 스캔 대리 사용은 07-3 으로 이관) | [19-pet-system.md](done/19-pet-system.md) | 완료 |
| 20 | 카메라 가림 처리 (7, 10번 레이어) | [20-camera-occlusion.md](done/20-camera-occlusion.md) | 완료 |
| 23 | 펫 Mediator 구조 정리 (Spawner 는 생성만, Mediator 가 이동·애니메이션 연결) | [23-pet-mediator-refactor.md](done/23-pet-mediator-refactor.md) | 완료 |
| 24 | 코요테 타임 | [24-coyote-time.md](done/24-coyote-time.md) | 완료 |
| 25 | 점프 버퍼링 | [25-jump-buffering.md](done/25-jump-buffering.md) | 완료 |
| 26 | 차지샷 대미지 하드코딩 수정 | [26-charge-shot-damage.md](done/26-charge-shot-damage.md) | 완료 |
| 27 | 차지샷 게이지 퍼사드 노출 (0~1) | [27-charge-gauge-facade.md](done/27-charge-gauge-facade.md) | 완료 |
| 28 | 총알 오브젝트 풀 연동 | [28-bullet-object-pool.md](28-bullet-object-pool.md) | 대기 |
| 29 | 총알 메시 + FX | [29-bullet-mesh-fx.md](29-bullet-mesh-fx.md) | 대기 |
| 30 | 전체 사운드 + VFX 작업 | [30-sound-vfx.md](30-sound-vfx.md) | 대기 |
| 31 | 카메라가 가까워지면 캐릭터 숨김 처리 | [31-camera-near-hide-character.md](done/31-camera-near-hide-character.md) | 완료 |
| 32 | 차지샷 누르고 있으면 자동 재차지 | [32-charge-shot-auto-recharge.md](done/32-charge-shot-auto-recharge.md) | 완료 |
| 33 | 블루투스 배터리 소모 시작 시점 수정 (실제 탑승 후부터 소모) | [33-bluetooth-drain-start-timing.md](33-bluetooth-drain-start-timing.md) | 대기 |

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
  - 21: 08(블루투스 모션)의 후속 조작 다듬기다.
  - 22: 약점 판정(`origin/feature/AI/WeakPoint`)이 develop 에 머지된 뒤 시작한다.
- **중간**: 10은 무적 판정이 밸런스에 직접 영향을 준다. 11은 조작감, 12는 순수 연출이다.
