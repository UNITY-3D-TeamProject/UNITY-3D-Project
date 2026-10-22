---
id: intent-044
title: 보스 B 첫 조우 도약 + 센서로 플레이어 즉시 발견
part: AI / Boss
status: resolved   # open | resolved
created: 2026-10-19
resolved: 2026-10-21
---

## 문제 (Problem)
- B 의 BT(`SmallBoss`)에는 수색 노드가 없어, 센서가 플레이어를 못 잡으면 `VisibleTarget` 이 null 이고 공격을 고르지 않는다. 프리팹 센서가 시야 5m·90도라 스폰 직후 플레이어를 발견하지 못했다.
- 첫 조우에 등장 연출이 없다. 도약은 1-2 부터만 열려 있어(`isLeapOpen`) 1-1 에서는 평범한 거리 판단으로 시작한다.

## 기대 결과 (Proposed outcome)
- B 는 스폰 직후 시야 범위(50m, 360도) 안의 플레이어를 즉시 발견한다.
- 대상을 처음 발견해 첫 공격을 고를 때만 단계와 쿨타임·거리와 무관하게 도약을 쓴다. 이후에는 기존 규칙 그대로이므로 1-1 동안 도약은 다시 나오지 않고(돌진은 1-1 부터), 1-2 부터 일반 규칙으로 열린다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스 B 와 싸우는 플레이어
- 어떤 시스템/모듈이 건드려지는가: `BossBChooseAttackAction`(BT 노드), `BossB.prefab` 의 `Sensor` 시야 각도

## 제약 (Constraints)
- 최소 변경: `BossBAttackRule`(Unity 비참조 순수 규칙)과 BT 그래프는 수정하지 않는다.
- 도약 쿨타임(8초)은 BT 그래프 Leap 가지가 기록하므로 첫 조우 도약도 같은 가지를 탄다.

## 열린 질문 (Open questions)
- 플레이어를 완전히 놓친 뒤 재발견 시 재조우 연출은 하지 않는다(첫 1회만). 필요하면 별도 논의.
- Play 에서 첫 도약 착지/예고 연출의 육안 확인은 사람이 해야 한다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 센서 50m·360도, 대상을 처음 발견해 고르는 첫 공격만 도약. 사용자 육안 확인 통과(2026-10-21).
- 관련 커밋/PR: 커밋 전 → [기록](../../HANDOVER/BF_Leers/2026-10-18/boss-session-c.md)
