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

- (2026-10-14) 후퇴 중에도 사격한다(뒷걸음질 사격). 기대 결과의 "이동 중에는 쏘지 않는다"를 대체한다
  - 그래프에서 후퇴와 원거리 공격을 `Run In Parallel`(Mode: Until Any Complete)로 묶는다. 새 노드는 만들지 않는다
  - 후퇴는 `Repeat`로 감싸고, 병렬 쪽 원거리 공격의 Range는 RetreateRange로 둔다. 후퇴 한 구간이 약 0.4초라서, 감싸지 않으면 구간마다 병렬 노드가 다시 시작되며 사격 대기가 리셋된다
  - `AIRangedAttackAction.KeepMoving`(기본 false): true면 시작할 때 `StopMove`를 부르지 않아 후퇴 이동이 유지된다
  - `AIController.ShouldFaceMoveDirection`(기본 true): 원거리 공격이 실행되는 동안 false로 둔다. 이동 방향 회전 요청과 대상 방향 회전 요청이 프레임마다 서로 덮어써 떨리는 것을 막는다
- (2026-10-14) 후퇴 시작 거리와 끝 거리를 나눈다. 병렬 쪽 원거리 공격의 Range를 블랙보드 `RetreatExitRange`(6)로 둔다(위의 "Range는 RetreateRange"를 대체). 진입 Guard는 `RetreateRange`(4) 그대로다
  - 두 값이 모두 4였을 때는 쫓기는 동안 거리가 3.9~4.08m를 오가며 병렬 노드가 1초에 여러 번 다시 시작했다. 그래서 사격 대기가 계속 리셋되어 이동 중 사격이 0회였다(Play 측정)
- (2026-10-14) 후퇴하는 동안에만 이동 속도를 낮춘다. `AIRetreatAction.SpeedRatio`(기본 0.4)
  - 적 MoveSpeed(10, 이 맵은 스폰 효과로 8)가 플레이어(6)보다 빨라서 약 0.5초 만에 6m를 벗어났다. 그래서 병렬 노드가 사격 대기(1~3초)보다 먼저 끝났다
  - 방식: `AIController.ScaleMoveSpeed(ratio)`가 `CharacterMediator.Get/SetAttribute`로 MoveSpeed 어트리뷰트에 곱하고, 끝날 때 1/ratio로 되돌린다. 덮어쓰지 않고 곱하므로 감속 지대(×0.5/÷0.5)와 겹쳐도 맞는다. 애니메이션 재생 속도도 어트리뷰트를 따라가므로 함께 느려진다
  - `MoveMediator.SetMoveSpeedRatio` 경로는 쓰지 않는다. 이를 위해서는 중재자에 인터페이스를 추가해야 하고, 애니메이션 속도도 맞지 않는다

## 열린 질문 (Open questions)
- ~~후퇴하는 동안 몸이 이동 방향을 보므로 등을 보인다~~ → 2026-10-14 결정 사항으로 해결
- 후퇴 도중 적이 비활성화되면 `OnEnd`가 불리지 않아 MoveSpeed가 낮은 채로 남는다. 다시 활성화하면 그래프가 이어서 돌며 `OnEnd`에서 복구되는 것은 확인했다. 사망·풀 재사용 시 그래프를 새로 시작하는 경로가 있다면 복구되지 않을 수 있다(미확인)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
