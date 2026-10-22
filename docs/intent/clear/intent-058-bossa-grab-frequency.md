---
id: intent-058
title: 보스 A 그랩(A4)이 거의 나오지 않음 — A4 보장 규칙 추가
part: AI / Boss / BossA
status: resolved
created: 2026-10-21
resolved: 2026-10-21
---

## 문제 (Problem)
- AI_TODO 보스-10. 그랩은 확률이 아니라 기본 순서(A1 → A2 → A4 → A5 → A6) 차례로만 나온다.
- 그런데 상황 규칙 5("한 줄에 3초 이상 머물면 그 줄에 A2")가 거의 매번 먼저 걸린다. 패턴 하나가 4초 넘게 걸리고, 가로줄·세로줄 중 하나만 3초 유지해도 걸리기 때문이다. 상황 규칙은 기본 순서를 넘기지 않으므로 순서가 A4 차례까지 가지 못한다.
- 선택기 시뮬레이션(실제 `BossAPatternSelector`, 플레이어는 일정 간격마다 이웃 칸으로 이동, 점프 15%, B 돌진 준비 25%):
  - 1-2: 3초마다 이동 0%, 1.5초마다 8%, 0.7초마다 17%
  - 최종: 6%
  - 1-1: 원래 A4 없음(기획)

## 기대 결과 (Proposed outcome)
- A4 가 아닌 패턴이 N번(임시값 3) 끝까지 이어지면, 상황 규칙보다 먼저 A4 를 고른다(1-2·최종).
- N은 `BossController` 인스펙터에서 조정한다.

## 영향 범위 (Affected users and systems)
- `AI.Boss.Logic.BossAPatternSelector`, `AI.Boss.BossA.BossController`

## 제약 (Constraints)
- 붕괴 제한(무너진 칸이 많으면 쉼/A6)은 그대로 먼저 적용한다. A4 는 칸을 무너뜨리므로 폴백으로 쓰지 않는 기존 결정 유지.
- 취소된 패턴·쉼은 세지 않는다(끝까지 끝난 패턴만 `NotifyPatternFinished`).

## 열린 질문 (Open questions)
- 없음(2026-10-21 사용자 결정: A4 보장 규칙, N 임시값 후 테스트하며 조정).

## 해결 기록 (Resolution) — 해결 후 작성
- 진행(2026-10-21): 구현·리컴파일·Play 수치 확인 완료, 사용자 육안 확인 대기 → [기록](../../HANDOVER/BF_Leers/2026-10-21/boss-a-grab-frequency-hold.md)
- 최종 결정: A4 가 아닌 패턴이 3번 끝까지 이어지면 상황 규칙보다 먼저 A4(`BossController._grabGuaranteeCount`). 사용자 육안 확인 통과(2026-10-21), 3 유지.
- 관련 커밋/PR: 커밋 전 → [기록](../../HANDOVER/BF_Leers/2026-10-21/boss-a-grab-frequency-hold.md)
