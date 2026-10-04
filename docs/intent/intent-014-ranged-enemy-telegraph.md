---
id: intent-014
title: 원거리 적 사격 예고 (랜덤 간격 + 고정 예비 동작 + 조준 고정)
part: AI
status: open   # open | resolved
created: 2026-10-04
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
원거리 적은 사거리 안에서 매 틱 대상을 향해 돌며 공격을 요청하고, 1초 쿨타임마다 정면으로 총알을 쏜다.
사격 순간까지 몸이 대상을 따라 돌기 때문에 사실상 항상 정조준이 되어, 플레이어가 피할 방법이 없다.

## 기대 결과 (Proposed outcome)
"언제 쏠지는 예측할 수 없지만, 신호를 보고 움직이면 반드시 피할 수 있다."
- 사격 간격만 랜덤(MinInterval~MaxInterval)이다.
- 발사 직전 예비 동작(windup)은 길이가 고정이고, 시작 순간 조준 방향을 고정한다. 예비 동작 중에는 몸이 돌지 않는다.
- 예비 동작 동안 레이저 조준선이 보이며, 총알은 정확히 그 선을 따라 날아간다.
- 예비 동작 중 사선이 막히거나 사거리를 벗어나면 취소되고 총알이 나가지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 원거리 적 (EnemyAI_Ranged 그래프)
- 어떤 시스템/모듈이 건드려지는가:
  - `AI/AIController.cs`: 범용 API `FaceDirection` / `ExecuteSkill` / `StopSkill` 추가, `Attack()`은 이를 조합하도록 내부만 변경
  - `AI/BT/AIRangedAttackAction.cs` (신규): 대기(랜덤) → 예비 동작(고정, 조준 고정) → 발사
  - `Skill/Skills/EnemyAimLine.cs` (신규): `AttackWindup` 스킬. LineRenderer 조준선
  - [USER] `Ranged.prefab`에 `AttackWindup` 스킬 오브젝트 추가, `EnemyAI_Ranged` 그래프의 공격 노드 교체

## 제약 (Constraints)
- 지켜야 할 정책: 중재자 구조. 조준선은 기존 스킬 요청 경로로 켜고 끈다(새 중재자 경로 없음). 행동 파라미터는 BT 노드가 갖는다(intent-013).
- 쓰지 말아야 할 것: 플레이어 이동 예측 조준(리드샷) — "신호를 보고 움직이면 피한다"를 깨뜨린다.
- 근접 적(`AIAttackTargetAction`)의 동작은 바꾸지 않는다.
- `RangedAttack`의 `CooldownCost`는 `MinInterval + WindupDuration`보다 짧아야 한다(아니면 조준선 후 총알이 안 나갈 수 있음).

## 열린 질문 (Open questions)
- 피격 시 예비 동작 취소: 피격 이벤트를 중재자 → AIController 로 전달하는 경로가 없다. 피격·경직 설계 때 함께 정한다.
- 여러 적의 동시 예비 동작 제한(공격 토큰 등): 랜덤 간격이 겹치면 사실상 피할 수 없는 순간이 생길 수 있다.
- 회피 가능성 수치 검증: `WindupDuration + 비행 시간 ≥ 반응 시간(약 0.25s) + (총알 반지름 + 플레이어 반지름) ÷ 플레이어 이동속도`. 최근접 거리 기준으로 플레이 테스트 후 조정.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
