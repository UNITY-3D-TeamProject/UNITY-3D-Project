---
id: intent-047
title: 보스 A 칸 예고를 중앙에서부터 채워지게 하고, 가득 차면 패턴 발동
part: AI / Boss / Arena
status: open   # open | resolved
created: 2026-10-20
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
보스 A 손 패턴의 칸 예고(`BossTile._telegraph` 반투명 쿼드)가 켜지는 순간 이미 가득 찬 채로 나타난다.
예고 시간이 얼마나 남았는지, 언제 손이 내려칠지 시각적으로 읽을 수 없다.

## 기대 결과 (Proposed outcome)
- 예고가 켜질 때 칸 중앙에서 정사각형이 커지며(스케일 0 → 1) 동시에 알파가 0 → 원래 값으로 올라간다(페이드 인).
- 채움 시간 = 패턴의 예고 시간(`TelegraphSeconds × TelegraphScale`). 가득 차는 순간 손이 움직이고 내려쳐 칸이 붕괴한다(기존 흐름 유지).
- `BossAClap` 의 두 번째 내려침처럼 예고가 이미 켜져 있을 때는 다시 채우지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스 A와 싸우는 플레이어(예고 가독성), 보스 밸런스를 조정하는 팀원.
- 어떤 시스템/모듈이 건드려지는가: `BossTile`, `BossArena`, `IBossArena.SetTelegraph`, `BossAPatternBase.ShowTelegraph`. 씬·프리팹 수정 없음(기존 `_telegraph` 연결 사용).

## 제약 (Constraints)
- 예산/기한: 없음.
- 지켜야 할 정책: 순수 로직(`ArenaGrid`)은 건드리지 않는다. 겉모습은 `BossTile` 이 맡는다.
- 쓰지 말아야 할 것: 새 셰이더. 원형·부드러운 가장자리 연출은 이번 범위가 아니다.

## 열린 질문 (Open questions)
- 정사각형 확장으로 충분한지는 Play 육안 확인 후 판단한다(원형 셰이더는 필요하면 별도 intent).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
