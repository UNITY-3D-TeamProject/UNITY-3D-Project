---
id: intent-085
title: 스테이지2 특수몹이 HP·에너지 아이템을 발판 순서대로 번갈아 떨어뜨린다
part: Stage / SecurityApp / Items
status: open   # open | resolved
created: 2026-10-29
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
스테이지2(`Map_4_Security`) 2라운드 특수몹 발판 5곳의 특수몹은 다른 잡몹과 같이 `EnemyItemDropper`(intent-083)로 25% 확률에 HP·에너지 중 하나를 무작위로 떨어뜨린다. 그래서 특수몹을 잡아도 아이템이 안 나오거나 같은 종류만 이어서 나올 수 있다 (AI_TODO2 [스테이지2-3]).

## 기대 결과 (Proposed outcome)
- 특수몹은 잡을 때마다 아이템을 반드시 떨어뜨린다.
- 종류는 발판 순서대로 HP → 에너지 → HP → 에너지 → HP 로 번갈아 나온다.
- 순서는 발판 번호 기준이다. 플레이어가 죽어 못 깬 발판이 리셋돼도 그 발판은 같은 아이템을 떨어뜨리고, 스테이지를 다시 시작하면 처음부터 나온다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(2라운드 보상), 레벨 담당(발판별 아이템 지정)
- 어떤 시스템/모듈이 건드려지는가:
  - `Core.Stage.Security.CombatZone`: 선택 항목 `_clearDropItem` 추가. 구역의 마지막 적이 죽으면 그 자리에 지정한 아이템을 떨어뜨린다. 비우면 지금과 같다 (1라운드 몹 방 영향 없음).
  - `Map_4_Security` 특수몹 발판 구역 5곳에 아이템 지정
  - `Special.prefab`: `EnemyItemDropper` 제거 (무작위 드롭과 겹치지 않게)

## 제약 (Constraints)
- 지켜야 할 정책: 씬·프리팹은 YAML 직접 수정 금지, Unity Editor MCP로 수정한다.
- 아이템은 `EffectItem`이 획득 시 스스로 `Destroy` 하므로 `EnemyItemDropper`와 같이 `Instantiate` 후 수명이 지나면 `Destroy` 한다.
- 실행자: Codex CLI가 이 PC에 설치돼 있지 않아 Claude가 직접 구현한다.

## 열린 질문 (Open questions)
- (해결) 순서: HP·에너지 번갈아 / 확률: 항상 드롭 / 리셋: 발판 번호 기준 — 2026-10-29 사용자 확인

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
