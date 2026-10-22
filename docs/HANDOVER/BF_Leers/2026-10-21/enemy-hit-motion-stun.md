# 잡몹 피격 모션·경직 정리 (AI_TODO 잡몹-3 → 잡몹-2)

- 날짜: 2026-10-21
- 관련 intent: [intent-026](../../../intent/intent-026-enemy-hit-stun.md) (2026-10-21 추가 절)
- 실행자: Claude 직접 (Codex CLI 미설치 — `codex` 명령 없음. 도구 미지원 예외). 프리팹·컨트롤러는 YAML 직접 수정 없이 Unity Editor API(MCP eval)로 수정.

## 1. 작업 요약 및 방법

### 재현 (수정 전, 보스 테스트 씬 `Map_4_Security_Boss_Phase_T`)
- 보스 A·B 를 끄고 원거리 적 앞에 플레이어를 둔 뒤, 0.2초 간격으로 피해 효과를 적용하며 Animator 상태를 프레임 단위로 기록했다.
- 피해·조준선·발사는 정상, **모션만 어긋남**: 힛모션 중 조준이 시작돼 Windup 모션이 빠지고, 발사 모션이 약 0.5초 늦게 나옴. 공격이 끝난 뒤 남아 있던 Hit 트리거로 뒤늦은 힛모션, Hit → Idle → Hit 반복.
- 원인
  1. 피격마다 `Hit` 트리거를 켬(경직·면역과 무관). `Attack`·`Windup`·`Hit` 상태에 Hit 전이가 없어 트리거가 남았다가 늦게 소비됨.
  2. `Hit` 상태에서 `Attack`·`Windup` 으로 가는 전이가 없음 + Idle 전이 우선순위 Hit > Windup.
  3. 공격 모션 구간 슈퍼아머 없음(원거리는 발사 직후 해제, 근접 `_holdDuration` 0).

### 수정
| 파일 | 내용 |
|---|---|
| `Mediator/CharacterMediator.cs` | `SetRequestHitStun(Func<bool>)`/`ClearRequestHitStun()` 추가. 피격 시 콜백으로 경직을 요청하고, **경직이 걸렸을 때만** 적 힛모션(`EnemyAnimationMediator.NotifyHit`)을 재생. 콜백이 없으면 기존처럼 매번 재생(보스 A 등). |
| `AI/AIController.cs` | 경직 판정을 `OnHitFromCallback` 에서 `TryStartHitStun()` 로 분리해 중재자에 등록(OnEnable/OnDisable). 돌아보기 동작은 그대로. 툴팁 갱신. |
| `AI/BT/AIRangedAttackAction.cs` | 발사 후 `AttackMotionDuration`(기본 0.85초, 그래프 값 없으면 상수) 동안 슈퍼아머 유지, `UpdateWait` 에서 해제. |
| `EnemyBase.controller` | Hit → Attack (Attack 트리거, 종료 시간 없음, 0.05초) |
| `Ranged.controller` | Hit → Attack, Hit → Windup (IsWindup) |
| `Melee.prefab` | `EnemyMeleeAttack._holdDuration` 0 → 1.2 (모션 1.0초 + Move→Idle→Attack 전이 지연 약 0.2초) |

### 검증 (수정 후, 같은 방법)
- 리컴파일: 에러 0, 경고 0 (`gate.cjs check recompile`, 로그 `.claude/codex-review/check-6s4w8B/`).
- 원거리: 6초 연사(피격 30회) 동안 힛모션 2회(경직 시에만), 조준→발사 모션 순서 정상, 발사 후 0.05초 만에 Attack 모션. 공격 중 Hit 트리거 예약 없음. 피격 종료 후 2~3초 간격 반복 정상.
- 근접: Hold 1.0 에서는 모션 끝 0.2초에 경직이 걸려 공격 직후 힛모션이 나옴 → 1.2 로 조정 후 6초 연사(피격 30회) 동안 공격 9회, 공격 상태 중 경직 0회. 경직 직후 공격 시 Hit → Attack 전이로 바로 공격 모션 확인(1.0 단계).
- 콘솔: 새 에러·경고 없음(기존 보스 컴포넌트 누락·ThirdParty·NavMesh 에이전트 경고만).

## 2. 결정 사항
- 힛모션 = 경직. 면역(재경직 방지) 시간의 피격은 피해·이펙트·사운드만 (사용자 확정 2026-10-21).
- 경직 결정은 조종부(`AIController`)가, 힛모션 재생은 중재자가 결과를 받아 처리(`SetRequestHitStun` 콜백). 컴포넌트끼리 직접 참조하지 않음.
- 공격 모션 슈퍼아머는 기존 방식(근접 = Hold, 원거리 = BT 노드 `IsSuperArmor`)을 그대로 씀.

## 3. 현재 상태 및 이슈
- **육안 확인 미완(waiting_user)**: 실제 사격으로 원거리·근접·Special 의 모션 자연스러움.
- **근접 공격 모션 밀림(기존 문제, 이번 수정과 무관)**: 쿨타임 1.0초 < 모션 1.0초 + 전이 약 0.1~0.2초라, 연속 공격 시 실제 판정보다 모션이 점점 늦어짐(9회째 약 1.1초 지연). 또 사거리 안에서는 Hold 가 계속 이어져 경직이 거의 걸리지 않음. 쿨타임 조정(예: 1.3~1.5초) 여부는 사용자 결정 필요.
- AIController 를 쓰는 보스 B·GC·도둑·Special 도 힛모션이 경직과 같이 움직임(보스 B 는 1.9초에 최대 1회). 보스 A 는 AIController 가 없어 기존과 동일.
- 에디터가 Search 인덱스(LMDB) 증분 병합 중 2회 크래시 → 사용자 허락 후 `Library/Search/*.index` 삭제·재실행으로 해결.
- 작업과 무관하게 에디터가 바꾼 파일: `M_BossBTelegraph.mat`, `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset` — 이번 작업 범위 아님, 커밋 시 제외 검토.
- 게이트 루프·커밋 전.

## 4. 다음 할 일 (Next Steps)
1. 사용자 육안 확인(실제 연사로 원거리·근접 모션).
2. 근접 쿨타임 조정 결정 → 조정 시 `Melee.prefab` 의 CooldownCost 값 변경.
3. 게이트 루프 통과 후 커밋, intent-026 resolved 처리.
4. 다음 TODO: 보스-4 → 잡몹-1 (약점 표시 묶음).
