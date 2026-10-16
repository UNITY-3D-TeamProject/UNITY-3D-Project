# 원거리 적 후퇴 중 사격 — 시작/끝 거리 분리 + 후퇴 감속 (2026-10-14)

관련 intent: [intent-013](../../../intent/intent-013-ranged-enemy-behavior.md) (`open`)
이전 기록: [원거리 적 후퇴 중 사격](./ranged-retreat-fire.md)
브랜치: `feature/AI/Enemy`

## 1. 작업 요약 및 방법
- 증상: 후퇴를 Repeat로 감싼 뒤에도 "후퇴만 하고 안 쏜다".
- Play 진단: Map_4_Security_T에서 원거리 적 노드 상태를 0.25초마다 기록하고, 스킬 호출을 감시했다.
  1. 원인 1: 진입 Guard(`RetreateRange` 4)와 병렬 공격 종료 Range(4)가 같은 경계였다. 쫓기는 동안 거리가 3.9~4.08m를 오가며 병렬 노드가 계속 다시 시작했고, 사격 대기(1~3초)가 매번 새로 정해졌다. 그래서 이동 중 사격이 0회였다.
     - [USER] 그래프 수정: 블랙보드 `RetreatExitRange`=6을 추가하고, 병렬 공격 Range를 여기에 연결했다.
  2. 원인 2: 적 `MoveSpeed`(SO 10, 이 맵은 스폰 효과 `Monster_Effect 1`로 8)가 플레이어(6)보다 빠르다. 그래서 약 0.5초 만에 6m를 넘고 병렬 노드가 대기보다 먼저 끝났다.
     - 코드 수정: 후퇴하는 동안에만 MoveSpeed 어트리뷰트에 배율을 곱한다.
- 코드
  - `AIController`: `[Header("Speed")] _mediator`(CharacterMediator, 비우면 GetComponent)와 `_speedAttributeKey`("MoveSpeed")를 추가했다. `ScaleMoveSpeed(float ratio)`는 `GetAttribute × ratio → SetAttribute`를 한다.
  - `AIRetreatAction`: 블랙보드 `SpeedRatio`(기본 0.4)를 추가했다. `OnStart`에서 `MoveTo`에 성공하면 곱하고, 실제 곱한 값을 `_appliedSpeedRatio`에 기억한다. `OnEnd`에서 `1/_appliedSpeedRatio`를 곱해 되돌린다.

## 2. 변경된 파일
- `Assets/_Project/Scripts/AI/AIController.cs`
- `Assets/_Project/Scripts/AI/BT/AIRetreatAction.cs`
- `Assets/_Project/BehaviorTree/EnemyAI_Ranged.asset` ([USER] 에디터에서 `RetreatExitRange` 추가·연결)
- `docs/intent/intent-013-ranged-enemy-behavior.md` (결정 사항, 열린 질문 추가)

## 3. 결정 사항
- 속도 변경은 `CharacterMediator.Get/SetAttribute` 경로로 한다. `SpeedOverTime`, `CrowdLane`과 같은 경로다. AI는 AttributeSet이나 EffectTarget을 직접 참조하지 않는다.
- 덮어쓰지 않고 곱하고 나눈다. 감속 지대(×0.5/÷0.5)와 겹쳐도 맞고, 애니메이션 속도도 어트리뷰트를 따라 함께 느려진다.
- `MoveMediator.SetMoveSpeedRatio`(탈것 버프용 비율)는 쓰지 않는다.

## 4. 현재 상태 및 이슈
- 컴파일 에러 0(MCP `recompile`). Play 검증 중 콘솔 에러 0.
- Play 결과 (Map_4_Security_T, 기준 MoveSpeed 8)
  - A. 플레이어가 초속 6m로 쫓을 때: 후퇴 중 MoveSpeed는 3.2였다. 병렬 노드가 약 11초 동안 끊기지 않았고, 움직이면서 사격 4회(조준 → 발사, 약 2~3초 간격)를 했다. 그 뒤 지형에 막혀 멈춘 상태에서 1회를 더 쐈다.
  - B. 플레이어가 가만히 있을 때: 약 1.5초 만에 6.03m에 도달했다. 그 뒤 2번 가지에서 약 2초 간격으로 사격했고, MoveSpeed는 8로 복구됐다.
  - 여러 번 후퇴한 뒤에도 MoveSpeed가 8로 유지됐다(누적 오차 없음).
- 알려진 문제: 후퇴 도중 적을 비활성화하면 `OnEnd`가 불리지 않아 MoveSpeed가 3.2로 남는다. 다시 활성화하면 그래프가 이어서 돌며 `OnEnd`에서 8로 복구되는 것은 확인했다. 사망·풀 재사용 때 그래프를 새로 시작하는 경로가 있으면 복구되지 않을 수 있다(미확인).
- 참고: 후퇴 노드 `MoveDistance`가 `RetreatExitRange` 변수에 연결되어 있다. 값(6)은 맞지만, 끝 거리를 바꾸면 이 값도 같이 바뀐다.
- 진단용 eval 감시는 모두 해제했다. 씬·프리팹은 저장하지 않았다.

## 5. 다음 할 일
1. [USER] 실제 조작으로 체감을 확인하고 `SpeedRatio`(0.4)와 `RetreatExitRange`(6)를 조정한다.
2. 적 사망·풀 재사용 경로에서 MoveSpeed가 복구되는지 확인한다. 안 되면 리셋 지점을 정한다.
3. 원거리 적 Animator에 뒷걸음 모션이 있는지 확인한다(이전 기록에서 넘어온 항목).
4. 검증이 끝나면 intent-013을 resolved 처리한다.
