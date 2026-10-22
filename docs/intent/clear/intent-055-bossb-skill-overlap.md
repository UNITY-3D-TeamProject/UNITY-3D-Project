---
id: intent-055
title: 보스 B 스킬이 겹쳐 실행되어 돌진과 도약이 동시에 나옴
part: AI / Boss / BossB
status: resolved
created: 2026-10-21
resolved: 2026-10-21
---

## 문제 (Problem)
- AI_TODO [보스-7]: 1-2 구간에서 B 가 돌진하면서 동시에 도약 점프한다(사용자 확인: "돌진하면서 동시에 점프").
- `Map_4_Security` Play 재현(1-2 강제, 플레이어 순간이동)에서 확인한 흐름:
  1. 회피 도약(Evade)이 체공에 들어가면 `BossBIsOnTelegraphedTileCondition` 이 체공 중이라 거짓을 돌려 피하기 가지가 끊기고, 공격 가지가 도약(Leap)을 시작한다.
  2. 회피 도약이 착지하며 `EndRun` → `AIController.ReleaseHold()` 로 도약이 건 붙잡힘까지 풀어, BT 는 도약이 끝난 줄 알고 돌진(Charge)을 시작한다.
  3. `Charge:Charging + Leap:Flight` 가 같은 프레임에 진행된다.
- 근본 원인: `BossBSkillBase.BeginRun` 이 자기 스킬의 `IsRunning` 만 보고, 같은 B 의 다른 스킬이 진행 중이어도 시작한다. 붙잡힘(Hold)은 AIController 에 하나뿐이라 먼저 끝난 스킬이 남의 붙잡힘을 푼다.

## 기대 결과 (Proposed outcome)
- B 의 스킬(콤보·돌진·도약·회피)은 한 번에 하나만 진행된다. 다른 스킬이 진행 중이면 새 스킬은 시작하지 않는다.
- B 가 스킬 진행 중일 때 공격 고르기는 None(추격)을 써서, 시작하지 못한 공격의 쿨타임이 헛되이 돌지 않게 한다.
- 안전망(발밑 칸 붕괴 시 회피 도약)은 진행 중 스킬을 먼저 끊으므로 그대로 동작한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스전 플레이어
- 어떤 시스템/모듈이 건드려지는가: `BossBController`(진행 중 스킬 조회), `BossBSkillBase.BeginRun`, `BossBChooseAttackAction`

## 제약 (Constraints)
- 공용 `AIController`·`AIUseSkillAndWaitAction` 은 다른 적도 쓰므로 바꾸지 않는다.
- BT 그래프 에셋(`SmallBoss.asset`)은 YAML 로 직접 고치지 않는다(이번 수정에서는 건드리지 않음).

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution) — 해결 후 작성
- 진행(2026-10-21): `BossBController.IsSkillRunning` 추가, `BossBSkillBase.BeginRun` 이 다른 스킬 진행 중이면 시작 거부, `BossBChooseAttackAction` 이 스킬 진행 중이면 None. 리컴파일 통과. Play(1-2 강제, 플레이어 순간이동 150초) 겹침 0건, 발밑 칸 강제 붕괴 → 회피 체공 중 도약·돌진·콤보 시작 87회 시도 모두 거부, 착지 후 돌진·콤보 정상. 수정 전 같은 방법으로 1분 안에 겹침 재현.
- 최종 결정: B 스킬은 한 번에 하나만 시작(`BossBSkillBase.BeginRun` + `BossBController.IsSkillRunning`), 스킬 진행 중이면 공격 고르기 None. 사용자 육안 확인 완료(2026-10-21).
- 관련 커밋/PR: 커밋 전
