---
id: intent-091
title: 보스A 패턴 예고 시간 0.5초 증가
part: AI / Boss / BossA
status: open   # open | resolved
created: 2026-10-29
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
보스A 패턴의 예고가 짧아 플레이어가 보고 피할 여유가 부족하다 (AI_TODO2 보스-22).

## 기대 결과 (Proposed outcome)
`BossA.prefab` 의 예고 시간 값만 각각 +0.5초 올린다. 코드는 바꾸지 않는다.

| 패턴 | 컴포넌트·필드 | 변경 |
|---|---|---|
| A1 | `BossASlam._telegraphSeconds` | 1.2 → 1.7 |
| A1Double | `BossASlam._telegraphSeconds` | 1.2 → 1.7 |
| A2 | `BossALineSlam._telegraphSeconds` | 1.5 → 2.0 |
| A4 조준 | `BossAGrab._aimSeconds` | 1.0 → 1.5 |
| A4 내려치기 | `BossAGrab._slamTelegraphSeconds` | 1.5 → 2.0 |
| A5 | `BossAChessSlam._telegraphSeconds` | 1.5 → 2.0 |
| A6 | `BossAClap._telegraphSeconds` | 1.5 → 2.0 |
| 어깨포(좌·우) | `BossShoulderCannon._telegraphSeconds` | 1.0 → 1.5 |

최종 단계는 기존대로 `TelegraphScale`(0.8)이 곱해져 실제로는 +0.4초가 된다(어깨포는 배율을 곱하지 않아 +0.5초).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안 스테이지 보스전 플레이어
- 어떤 시스템/모듈이 건드려지는가: `Prefabs/AI/Boss/BossA.prefab` 값만. 씬(`Map_4_Security`)에는 이 값들의 오버라이드가 없음을 확인

## 제약 (Constraints)
- 지켜야 할 정책: 프리팹은 YAML 직접 수정 금지, Unity Editor MCP 로 수정
- 쓰지 말아야 할 것: 보스B 값, `GroggyPalm`(예고 0초 유지형), A6 `_strikeInterval`(1타→2타 간격, 예고가 아니라 제외)

## 열린 질문 (Open questions)
- (해결) 범위: 보스A 예고 시간만. 2026-10-29 사용자 결정.
- (해결) 최종 단계: 기본값에 +0.5, 배율 0.8 유지. 2026-10-29 사용자 결정.
- 보스-20(1단계 패턴 사이 텀, `BossController._handStayPhase1`)은 이 변경을 플레이해 본 뒤 값을 정한다. 2026-10-29 사용자 결정.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
