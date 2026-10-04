---
id: intent-013
title: 원거리 적 행동 — 사거리 유지 접근 + NavMesh 후보점 기반 후퇴/위치 변경
part: AI
status: open   # open | resolved
created: 2026-10-04
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
원거리 공격 스킬(intent-012)은 생겼지만, BT 노드가 근거리와 같아서(순찰→수색→추격→공격) 플레이어가 붙어도 제자리에서 쏘기만 한다. 원거리 적다운 거리 유지 행동이 없다.

## 기대 결과 (Proposed outcome)
- 보이는데 사거리 밖이면 사거리까지만 접근한다 (기존 추격 + 공격 Guard Abort, 새 코드 없음)
- 사거리 안이면 멈춰서 쏜다 (기존 공격 액션)
- 플레이어가 후퇴 거리 안으로 들어오면 NavMesh 후보점(정후방 0°, ±30°, ±60°, ±90°)을 순서대로 검사해 뒤로 물러나거나 옆으로 자리를 옮긴다. 이동 중에는 쏘지 않는다
- 후보가 전부 막히면 제자리에서 쏘고 1초 뒤에 다시 시도한다

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 원거리 적 (`EnemyAI_Ranged` 그래프)
- 어떤 시스템/모듈이 건드려지는가:
  - `AI/RetreatPointFinder.cs` 신규 (상태 없는 정적 클래스, 후보점 계산)
  - `AI/BT/AIRetreatAction.cs` 신규
  - `AIController`에 `Position` 속성 1줄 추가
  - Sensor, 중재자, 스킬은 무변경

## 제약 (Constraints)
- 예산/기한: 월요일 전투 프로토타입
- 지켜야 할 정책: AIController·Sensor를 더 키우지 않는다. 행동 파라미터는 BT 노드가 갖는다
- 쓰지 말아야 할 것: 점수 기반 후보 평가(프로토타입 범위 밖), `.asset` 직접 편집

## 결정 사항
- 시야가 트였는지 검사는 `NavMesh.Raycast`(후보 → 대상)로 근사한다. 벽은 NavMesh에서 뚫려 있으므로 충분하고, Sensor의 레이어 마스크를 빌리지 않아도 된다
- 경로 길이가 이동 거리의 2배를 넘는 후보는 버린다. 플레이어 쪽으로 돌아가는 우회 경로를 막기 위함이다
- 공격 가지에 사선 조건(`AIHasLineOfFireCondition`, 기대값 `IsClear`)을 둔다. Sensor의 유예 시간(3초) 동안 엄폐물 뒤 대상에게 벽에 대고 쏘던 문제를 막기 위함이다. 사선이 막히면 추격 가지로 넘어가므로, 원거리 적에게 추격은 "사선 확보 + 사거리까지 접근"의 역할을 한다
  - 진입: Conditional Guard(Abort Lower Priority, Requires All)에 "사거리 안" AND "사선 트임=true"
  - 사격 중: Guard 아래 Fail 노드에 "사선 트임=false"를 넣어 공격을 중단한다
  - Conditional Guard는 Lower Priority만 지원하고(Self/Both 불가), Priority Abort는 조건이 거짓이면 부모를 처음부터 다시 돌려 진입 관문으로 쓸 수 없다. 패키지에 조건 반전 기능이 없어서 기대값 변수로 해결했다

## 열린 질문 (Open questions)
- 후퇴하는 동안 몸이 이동 방향을 보므로 등을 보인다. 뒷걸음질 사격이 필요하면 회전 처리를 따로 다룬다

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
