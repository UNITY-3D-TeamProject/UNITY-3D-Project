---
id: intent-050
title: 보스 B 도약 착지 칸 예약 — 보스 A 가 그 칸을 피한다
part: AI / Boss / BossA·BossB
status: resolved   # open | resolved
created: 2026-10-20
resolved: 2026-10-21
---

## 문제 (Problem)
- 1-2 에서 B 가 도약 원을 띄운 뒤 약 1초 지나 원이 이웃 칸으로 옮겨 가고, B 는 옮겨 간 칸으로 뛴다.
- 원인: B 가 예고하는 동안 A 가 새 패턴으로 같은 칸(대개 플레이어 칸)에 예고를 띄우면, `BossBLeap.RetargetIfUnsafe` 가 착지 칸을 가장 가까운 멀쩡한 칸으로 바꾼다. 처음 본 원과 실제 착지 칸이 달라져 예고를 믿을 수 없다.

## 기대 결과 (Proposed outcome)
- B 가 도약을 시작하면(예고·회피 모두) 착지할 칸을 아레나에 예약하고, 착지하거나 도약이 끊기면 예약을 푼다.
- A 는 예약된 칸을 노리지 않는다(사용자 결정: "그 칸만 빼고 공격").
  - 한 칸 표적(A1·A1Double): 예약 칸 대신 가장 가까운 멀쩡한 칸을 친다. 대신할 칸이 없거나 이미 표적이면 그 칸만 뺀다.
  - 여러 칸 패턴(A2·A5): 예약 칸만 빼고 예고·붕괴한다. 손이 예약 칸을 칠 차례면 남은 칸 중 가장 가까운 칸으로 옮긴다.
  - A4 그랩: 끌려올 칸(가장 가까운 멀쩡한 칸)을 고를 때 예약 칸을 건너뛴다.
- 그래서 B 의 원은 처음 놓인 칸에서 움직이지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 1-2 단계(A·B 동시) 플레이어.
- 어떤 시스템/모듈이 건드려지는가: `ArenaGrid`(예약 칸·`FindNearestSafe`), `IArenaQuery`/`BossArena`(예약 창구), `BossAPatternSelector`(선택 결과에서 예약 칸 제외), `BossBLeap`(예약·해제).

## 제약 (Constraints)
- 지켜야 할 정책: 외과적 변경. 기존 `RetargetIfUnsafe` 는 예약 전에 이미 위험했던 칸 등 남은 경우의 안전장치로 둔다.
- 쓰지 말아야 할 것: 씬·프리팹 YAML 직접 수정.
- A6 박수는 칸 순서로 손 자리를 정하고 최종(A 단독, B 없음) 전용이라 손대지 않는다.

## 열린 질문 (Open questions)
- 없음(겹칠 때 A 의 동작은 사용자가 "그 칸만 빼고 공격"으로 결정).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: B 도약 착지 칸 예약(`ArenaGrid.ReservedTile`), A 는 예약 칸을 빼고 공격. 사용자 육안 확인 통과(2026-10-21).
- 관련 커밋/PR: 커밋 전 → [기록](../../HANDOVER/BF_Leers/2026-10-20/boss-b-leap-reserved-tile.md)
