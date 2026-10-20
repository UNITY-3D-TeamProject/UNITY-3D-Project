---
id: intent-046
title: 보스 아레나 타일을 3×3에서 5×5로 확장 (4×4 시도 후 5×5로 변경)
part: AI / Boss / Arena
status: open   # open | resolved
created: 2026-10-20
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
테스트 씬 `Map_4_Security_Boss_T`의 `05_Boss` 보스 아레나가 3×3(칸 20 m)이다. 4×4로 먼저 배치했다가 가운데 칸이 없는 점 때문에 5×5로 바꿨다.
`ArenaGrid.SIZE`가 3으로 고정이고 `BossArena`가 `_tiles` 9개가 아니면 에러를 내므로 씬만 바꿔서는 동작하지 않는다.

## 기대 결과 (Proposed outcome)
- 전체 아레나 60×60 m를 유지하고 칸 크기를 12 m로 줄여 5×5(25칸)로 배치한다.
- `ArenaGrid.SIZE = 5`, `BossArena._tileSize = 12`, `_tiles` 25개(`BossFloor_0~24`, 인덱스 = row × 5 + col).
- 낙사 존·체크포인트 등 아레나 바깥 배치는 그대로 둔다. NavMesh는 다시 굽는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스 A·B 전투 설계자/플레이어
- 어떤 시스템/모듈이 건드려지는가: `ArenaGrid`(상수), `BossArena`(주석), 테스트 씬 `05_Boss` 타일·NavMesh

## 제약 (Constraints)
- `Assets/Test/`는 Git 제외 영역이라 씬 변경은 공유되지 않는다.
- 보스 A 패턴 로직에 3×3 전제가 남아 있다(아래 열린 질문). 이번 작업에서는 임의로 바꾸지 않는다.

## 열린 질문 (Open questions)
- (임시 결정, 사용자 확인 필요) 보스 A 패턴을 `SIZE` 기준으로 일반화했다: 가운데 칸 `ArenaGrid.CENTER`(12), 모서리 4곳, A2/A6 은 줄 양 끝 칸이 손 칸·A6 2타는 줄의 가운데 칸, A5 +자 = 가운데 가로줄+세로줄(9칸, 손 = 가운데 줄 양 끝), X자 = 두 대각선(9칸, 손 = 윗줄 양 끝 모서리). 이 모양이 의도에 맞는지.
- A5 가 3×3 의 5칸에서 5×5 의 9칸이 되어 판의 36% 가 한 번에 무너진다. 난이도상 괜찮은지.
- `CreateSingleHand`의 손 배정(col 0 → 왼손, col SIZE-1 → 오른손, 그 사이 교대)이 5열에서 의도에 맞는지.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
