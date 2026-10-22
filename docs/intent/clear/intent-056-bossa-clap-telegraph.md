---
id: intent-056
title: 보스 A A6 박수가 줄 전체를 예고하지만 땅이 사라지지 않음
part: AI / Boss / BossA
status: resolved
created: 2026-10-21
resolved: 2026-10-21
---

## 문제 (Problem)
- AI_TODO [보스-8]: 최종 페이즈에서 패턴 예고는 뜨는데 땅이 사라지지 않는다.
- 원인: 최종 전용 A6 박수(`BossAClap`)는 기획상 칸을 무너뜨리지 않는 패턴인데(`docs/planning/boss-pattern.md` 붕괴 제한), 예고는 기본 동작대로 한 줄 5칸 전체에 띄운다. 실제로 맞는 칸은 1타 양 끝 칸, 2타 가운데 칸뿐이다.

## 기대 결과 (Proposed outcome)
- 사용자 결정(2026-10-21): 칸은 계속 무너뜨리지 않고, 맞는 칸만 예고한다.
  - 1타: 줄 양 끝 칸만 예고(채움 시간 = 패턴 예고 시간).
  - 2타: 1타 직후 양 끝 예고를 끄고 가운데 칸만 예고(채움 시간 = 1타→2타 간격 `_strikeInterval`).
  - 2타가 끝나면 예고를 끈다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스전 플레이어(최종 페이즈)
- 어떤 시스템/모듈이 건드려지는가: `BossAClap`, `BossAPatternBase`(내려침 사이 예고를 바꾸는 protected 메서드 추가)

## 제약 (Constraints)
- 다른 A 패턴(A1·A2·A4·A5)의 예고·붕괴 동작은 바꾸지 않는다.
- 패턴 선택기(`BossAPatternSelector`)의 A6 선택 결과(`Tiles` = 줄 5칸)는 그대로 둔다.

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution) — 해결 후 작성
- 진행(2026-10-21): `BossAPatternBase.ReplaceTelegraph` 추가, `BossAClap` 이 1타 예고를 양 끝 칸으로(`GetTargetTiles`), 1타 직후 가운데 칸으로 바꿈. 리컴파일 통과. Play 강제 실행(줄 10~14)에서 1타 예고 [10,14] → 2타 예고 [12] → 끝나면 없음, A6 로 무너진 칸 없음. 2타 예고는 1.0초에 다 차고 손이 가운데로 옮겨 가느라 약 0.4초 뒤 충돌.
- 최종 결정: A6 는 칸을 무너뜨리지 않고 맞는 칸만 예고(1타 양 끝 → 2타 가운데, `BossAPatternBase.ReplaceTelegraph`). 사용자 육안 확인 완료(2026-10-21).
- 관련 커밋/PR: 커밋 전
