---
id: intent-026
title: 적 피격 경직 (맞으면 잠깐 멈추고 공격 못 함)
part: AI / Enemy / Combat
status: open   # open | resolved
created: 2026-10-16
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
적이 맞아도 Hit 애니메이션만 재생되고 이동·공격은 그대로 이어진다. `AIController.OnHitFromCallback`은 대상이 보이는 중(전투 중)에는 피격을 무시하므로, 추격 중인 적을 맞혀도 아무런 디메리트가 없다.

## 기대 결과 (Proposed outcome)
- 맞으면 짧게(기본 0.4초) 제자리에 멈추고, 그동안 공격(스킬 요청)도 하지 못한다. 목적지는 유지되므로 풀리면 하던 추격을 이어간다.
- 경직이 끝난 뒤 일정 시간(기본 1.5초)은 다시 경직되지 않는다. 연사로 계속 묶이지 않게 하기 위함이며, 피격 애니메이션은 이 동안에도 재생된다.
- 스킬 사용 중(Special 바람 Hold, 원거리 예비동작)에는 경직하지 않는다(슈퍼아머).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 모든 적(Melee·Ranged·Special) 상대 플레이어
- 어떤 시스템/모듈이 건드려지는가: `AI/AIController.cs`, `AI/BT/AIRangedAttackAction.cs`

## 제약 (Constraints)
- 지켜야 할 정책: 기존 피격 돌아보기(대상이 안 보일 때) 동작은 유지한다. 프리팹·BT 에셋·애니메이터는 건드리지 않는다.
- 쓰지 말아야 할 것: `HitInvincibility` 를 적에 붙이는 방식(피해 자체를 막아 버림)

## 열린 질문 (Open questions)
- 없음 (디메리트 형태·연사 대응·스킬 중 처리 모두 2026-10-16 사용자 확정)

## 2026-10-21 추가 — 잡몹-3·잡몹-2 (AI_TODO)

### 재현 결과 (원거리, 0.2초 간격 연사, Play 기록)
- 피해·조준선·발사는 정상이다. **모션만** 어긋난다.
- 원인 1: 피격마다 `Hit` 트리거를 켠다(경직·면역과 무관). `Attack`·`Windup`·`Hit` 상태에는 Hit 전이가 없어 트리거가 남아 있다가, 상태가 끝난 뒤 늦게 소비된다 → 공격이 끝난 뒤 뒤늦은 힛모션, Hit → Idle → Hit 반복.
- 원인 2: `Hit` 상태에서 `Windup`·`Attack`으로 가는 전이가 없고, Idle 전이 우선순위가 Hit > Windup 이다 → 힛모션 중 조준을 시작하면 조준 모션이 빠지고, 발사 모션이 약 0.5초 늦게 나온다.
- 원인 3: 공격 모션 구간에 슈퍼아머가 없다(원거리는 발사 직후 해제, 근접은 `_holdDuration` 0) → 공격 모션 중 경직이 걸려 힛모션이 예약된다.

### 바뀐 기대 결과 (사용자 확정 2026-10-21)
- 힛모션은 **경직이 걸릴 때만** 재생한다. 면역(재경직 방지) 시간의 피격은 피해·명중 이펙트·사운드만 적용한다. (위 "피격 애니메이션은 이 동안에도 재생된다"를 대체)
- 공격 모션(근접 공격 동작, 원거리 조준~발사 동작) 동안에는 경직하지 않는다. 피해는 그대로 적용된다.
- 경직이 끝나 공격·조준을 시작하면 남은 힛모션을 끊고 바로 공격 모션으로 넘어간다.
- 경직 중 추가 피격으로 경직을 연장하거나 힛모션을 처음부터 재생하지 않는다(기존 면역 타이머 유지).

### 영향 범위 추가
- `Mediator/CharacterMediator.cs`(경직 요청 콜백), `AI/AIController.cs`, `AI/BT/AIRangedAttackAction.cs`
- `EnemyBase.controller`·`Ranged.controller`: Hit → Attack / Hit → Windup 전이 추가 (Unity MCP)
- `Melee.prefab`: `EnemyMeleeAttack._holdDuration` = 공격 모션 길이
- AIController 를 쓰는 다른 캐릭터(보스 B·GC·도둑·Special)도 힛모션이 경직과 같이 움직인다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
