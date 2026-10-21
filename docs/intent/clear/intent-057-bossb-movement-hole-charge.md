---
id: intent-057
title: 보스 B 대각선 돌진이 A 예고에 막혀 멈추고, 1-1 에서 구멍 앞에 막힘
part: AI / Boss / BossB
status: resolved
created: 2026-10-21
resolved: 2026-10-21
---

## 문제 (Problem)
AI_TODO [보스-3] "작은 보스가 멍청하게 움직이는 문제". 사용자 확인(2026-10-21) 결과 1-1 구간에서 두 가지가 보인다.

1. **대각선 돌진이 멈춤**: B 가 돌진 예고를 시작하면 A 가 "돌진 경로 바로 옆 칸"을 내려친다(`BossAPatternSelector` 규칙 1).
   그런데 돌진 방향을 가로·세로 중 큰 축 하나로만 판단해(`BossEncounter.HandleChargePrepared`) 대각선 돌진이면 고른 "옆 칸"이 실제 돌진 직선 위에 놓일 수 있다.
   B 돌진은 앞 칸이 예고·구멍이면 급정거하고 2초 휘청이므로(`BossBCharge.IsBlocked`), A 가 B 의 길을 막는 꼴이 된다.
2. **1-1 에서 구멍 앞에 막힘**: B 와 플레이어 사이에 구멍이 있으면 돌진하지 않고(`BossBAttackRule`), 1-1 은 도약이 잠겨 있어 NavMesh 추격만 남는다. 빙 돌아가거나 길이 없으면 구멍 앞에 서 있다.

(A 손·팔 콜라이더에 B 가 몸으로 걸리는 것도 제보됐으나, 1번이 고쳐지면 손이 경로에 내려오지 않으므로 이번에는 다루지 않는다 — 사용자 결정)

## 기대 결과 (Proposed outcome)
1. A 규칙 1 은 B 돌진 시작 위치에서 돌진 방향 직선이 지나는 칸을 후보에서 뺀다. 두 옆 칸이 모두 빠지면 규칙 1 을 건너뛴다(기존 "멀쩡한 옆 칸이 없을 때"와 같음). 돌진 방향은 지금처럼 자유 방향.
2. B 가 구멍 때문에 공격을 못 고르는 상황(사이에 구멍 + 도약 불가 + 돌진 준비됨)이면, 플레이어까지 직선에 구멍·예고가 없는 가장 가까운 멀쩡한 칸으로 먼저 걸어간 뒤, 직선이 깨끗해지는 즉시 공격을 다시 골라(보통 돌진) 쓴다. 도약은 지금처럼 1-2 부터.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스전 플레이어
- 어떤 시스템/모듈이 건드려지는가:
  - `AI.Boss.Logic.ArenaGrid` — 직선이 지나는 칸 목록, 돌진 자리 칸 찾기 추가
  - `AI.Boss.Logic.SBossSituation` / `BossAPatternSelector` — 돌진 시작점·방향(칸 단위) 추가, 규칙 1 후보에서 직선 위 칸 제외
  - `AI.Boss.Fight.BossEncounter` — 돌진 시작점·방향을 스냅샷에 채움
  - `AI.Boss.Arena.IArenaQuery` / `BossArena` — 돌진 자리 칸 질의 추가
  - `AI.Boss.BT.BossBChooseAttackAction` — 돌진 자리로 걸어가는 동안 Running

## 제약 (Constraints)
- BT 그래프(`SmallBoss.asset`)는 바꾸지 않는다(고르기 노드가 걸어가는 동안 Running 을 돌려 Switch 를 늦춘다).
- 범용 `AIChaseTargetAction` 은 잡몹과 공용이라 바꾸지 않는다.
- 이 PC 에 Codex CLI 가 없어 Claude 가 직접 구현한다.
- `Assets/Test/Boss` 단위 테스트는 주석 처리·asmdef 비활성 상태라 검증은 리컴파일 + Unity eval 로직 호출로 한다.

## 열린 질문 (Open questions)
- 없음 (2026-10-21 사용자 결정: A 가 경로 칸을 피해서 침 / A 손 충돌은 보류 / 1-1 구멍은 막히지 않는 방향으로 돌진).
- 조건을 "1-1" 이 아니라 "구멍 + 도약 불가 + 돌진 준비" 로 잡았다. 1-2·최종에서 도약 쿨타임 중일 때도 같은 동작이 된다.

## 진행 기록
- 2026-10-21: 구현·리컴파일 통과(에러 0). eval 로 로직 확인, Play(`Map_4_Security_Boss_Phase_T`)로 B 자리 옮김·돌진과 A 규칙 1 예고 확인. 육안 확인 대기 → [기록](../../HANDOVER/BF_Leers/2026-10-21/boss-b-hole-charge-lane.md)

- 2026-10-21: 추가 제보(구멍이 먼저 있어도 짧게 예고하고 돌진) — `HasHoleBetween` 0.25칸 표본이 대각선 모서리 구멍을 놓치던 것(8.1%)을 칸 경계 추적으로 수정. 놓침 0건, Play 확인.
- 2026-10-21: 추가 제보(돌진 예고 중 A 가 찍어 범위가 줄어듦) — 사용자 결정에 따라 돌진 예고~돌진 중엔 A 의 모든 패턴이 돌진 직선 위 칸을 빼고 침(A1 은 경로 밖 가장 가까운 칸으로 대체). Play 확인.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: A 는 B 돌진 예고~돌진 중 돌진 직선 위 칸을 빼고 친다(`BossAPatternSelector.ExcludeBlocked`, 규칙 1 옆 칸 후보도 직선 위 칸 제외). B 는 사이에 구멍이 있어 공격을 못 고르고 돌진이 준비됐으면 직선이 깨끗한 칸으로 옮긴 뒤 돌진한다(`BossBChooseAttackAction`). `HasHoleBetween` 은 칸 경계 추적으로 판정. A 손 충돌은 보류. 사용자 육안 확인 완료(2026-10-21).
- 관련 커밋/PR: 커밋 전
