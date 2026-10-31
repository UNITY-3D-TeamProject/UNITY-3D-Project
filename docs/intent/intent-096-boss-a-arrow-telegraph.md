---
id: intent-096
title: 보스A 박수·그랩 예고를 화살표(WarningSignals_Arrows)로 교체
part: AI / Boss / BossA
status: open   # open | resolved
created: 2026-10-30
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- (AI_TODO2 보스-19) 박수 패턴은 안쪽이 위험한 구간이라는 사실을 알아보기 어렵다.
- (AI_TODO2 보스-23) 박수·그랩 예고가 타일 머티리얼(빨간 칸 채움)과 직선 판이라 어느 방향으로 오는지 읽히지 않는다.
- 맵의 각 보스 타일에는 정적인 `WarningSignals`(화살표 장식)가 붙어 있어 예고 화살표와 겹쳐 보인다.

## 기대 결과 (Proposed outcome)
- `WarningSignals_Arrows.prefab`(Arrow_01~03, 로컬 +X 를 가리키는 ">>>" 띠)을 박수·그랩 예고로 쓴다. 기존 타일 채움·직선 예고는 이 두 패턴에서 화살표로 완전 대체한다(다른 패턴은 그대로).
  - 박수: 1타는 양 끝 칸에 안쪽을 향한 화살표, 2타 예고는 사이 칸들에 가운데를 향한 화살표.
  - 그랩: 날아가는 직선 경로를 따라 진행 방향 화살표를 이어 붙이고, 놓일 칸에도 화살표.
- 화살표는 Arrow_01 → 02 → 03 순으로 하나씩 켜졌다 꺼지며 진행 방향으로 흐르는 깜빡임을 낸다.
- 화살표가 뜬 칸의 맵 `WarningSignals` 는 예고 동안만 끄고, 끝나면 다시 켠다(발판 5개는 건드리지 않음).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스 A 박수·그랩을 상대하는 플레이어
- 어떤 시스템/모듈이 건드려지는가: `IBossArena`/`BossArena`(화살표 풀·표시), `BossTile`(WarningSignals 토글), `BossAPatternBase`(화살표 예고 훅), `BossAClap`, `BossAGrab`, 신규 `BossArrowTelegraph`, `Map_4_Security` 씬(BossArena·BossTile 25개 참조 연결)

## 제약 (Constraints)
- 칸 상태(`ArenaGrid` 의 Telegraphed)는 그대로 유지한다. 바꾸는 것은 겉모습뿐이다.
- 피해 판정·타이밍 수치는 바꾸지 않는다.
- 씬·프리팹은 YAML 직접 수정 대신 Unity Editor MCP 로 바꾼다.
- 기존 `ShowLine`/직선 풀은 다른 곳에서 안 쓰게 되더라도 이번 범위에서는 삭제하지 않는다.

## 열린 질문 (Open questions)
- (해결) 용도: 박수+그랩 모두(사용자 확인).
- (해결) 맵 WarningSignals: 예고가 뜬 타일만, 패턴 중에만 끔(사용자 확인).
- (해결) 기존 예고: 화살표로 완전 대체, 깜빡임/이동 애니메이션 포함(사용자 확인).
- 박수 가운데 칸처럼 방향이 모호한 칸은 줄 방향(왼손→오른손)을 따른다(임시).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
