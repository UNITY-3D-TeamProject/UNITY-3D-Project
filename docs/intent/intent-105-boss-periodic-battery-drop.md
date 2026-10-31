---
id: intent-105
title: 보스전 중 일정 시간마다 배터리 아이템을 떨어뜨린다
part: AI / Boss / Items
status: open   # open | resolved
created: 2026-10-31
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
보스전은 길고 에너지 소모가 큰데, 아레나에는 에너지를 회복할 수단이 없다 (AI_TODO2 보스-37).

## 기대 결과 (Proposed outcome)
- 보스전이 시작된 뒤(`BossEncounter.IsBegun`) 20초마다 아레나의 구멍·예고 중이 아닌 칸 하나에 `Item_ChargeBattery` 가 떨어진다.
- 동시에 최대 2개. 가득 차 있으면 그 주기는 건너뛴다. 안 먹히면 15초 뒤 사라진다.
- 보스전 내내(그로기·단독 패턴 포함) 유지한다. 보스전이 끝나거나 체크포인트 재시작으로 다시 시작되면 타이머를 처음으로 돌리고 남은 아이템을 치운다.

## 영향 범위 (Affected users and systems)
- 신규 `AI/Boss/Fight/BossBatteryDropper`, `Map_4_Security.unity` 의 `BossFight` 오브젝트에 컴포넌트 추가.
- 기존 `BossEncounter`·`BossArena`·아이템 프리팹은 수정하지 않는다.

## 제약 (Constraints)
- 씬은 Unity Editor MCP 로 수정한다.
- 주기·개수·수명·높이는 인스펙터 값(초기값 20초·2개·15초·1m).

## 열린 질문 (Open questions)
- 직접 플레이로 체감 확인(주기·개수 적절한지, 획득 시 에너지 회복, 재시작 시 초기화).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
