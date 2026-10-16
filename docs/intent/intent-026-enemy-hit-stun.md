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

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
