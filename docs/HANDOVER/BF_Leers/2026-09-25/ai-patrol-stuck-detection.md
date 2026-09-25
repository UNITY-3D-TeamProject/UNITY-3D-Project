# AIController 정체(Stuck) 감지 도입 — 순찰 무한 Running 버그 수정

## 1. 작업 요약 및 방법

**증상**: Enemy가 순찰 중 벽 등 지형에 낑기면 순찰 액션이 무한 `Running` 상태에 빠져 아무 행동도 하지 않음.

**원인**: `AIController.HasArrived`는 "도착했는가"만 판정하고 "정체됐는가"는 판정하지 않았다.
`Update()`에서 실제 이동은 `CharacterMotor`가 물리적으로 수행하는데, Body가 벽에 막혀 전진하지 못하면
`_agent.remainingDistance`가 줄지 않아 `HasArrived`가 영원히 `false`를 반환한다.
`AIPatrolAction.OnUpdate()`는 `HasArrived == false`이면 무조건 `Status.Running`만 반환해 정체와 정상 이동 중을
구분하지 못했다.

**수정**: `AIController`에 정체 판정을 추가하고, `AIPatrolAction` / `AISearchLastKnownPositionAction`이
정체 시 `Status.Failure`로 빠져나오도록 분기를 추가했다.

- `AIController` (`Assets/_Project/Scripts/AI/AIController.cs`)
  - `IsStuck` 프로퍼티 신설. 다음 세 조건 중 하나면 `true`:
    1. `_agent.pathStatus == NavMeshPathStatus.PathInvalid` (경로 자체가 불가능)
    2. 목적지 설정 후 `DESTINATION_TIMEOUT`(10초) 안에 도착 못함
    3. `STUCK_CHECK_INTERVAL`(2초) 동안 이동 거리가 `STUCK_DISTANCE_THRESHOLD`(0.1m) 미만
  - `Update()`에서 매 프레임 `UpdateStuckState()` 호출, `MoveTo()`/`StopMove()`에서 `ResetStuckState()` 호출.
  - 수치(2초/0.1m/10초)는 사용자 승인하에 기본값으로 잡음 — **세부 조정은 사용자가 인스펙터로 진행** (private const라 노출 안 함, 조정 필요 시 상수만 바꾸면 됨).
- `AIPatrolAction` / `AISearchLastKnownPositionAction` (`Assets/_Project/Scripts/AI/BT/`)
  - `OnUpdate()`에 `Controller.IsStuck` 분기 추가 → 정체 시 `Status.Failure`.
  - `AIPatrolAction`에 `OnEnd() { Controller.StopMove(); }` 신설 — 노드가 끝난 뒤에도
    `AIController.Update()`가 벽으로 계속 이동을 시도하는 잔여 문제까지 함께 막음
    (`AISearchLastKnownPositionAction`은 이미 같은 패턴을 갖고 있었음).

## 2. 결정 사항

- 정체 판정 기준값은 사용자가 "기본값으로 잡고 세부조정은 내가 하는 식으로" 하기로 결정 → 2초/0.1m/10초로 확정, 프로젝트 CLAUDE.md의 "코딩 전 생각하기" 원칙에 따라 임의로 세밀 조정하지 않음.
- 경로 불가능(`PathInvalid`)은 타이머를 기다리지 않고 즉시 정체로 판정 (실패를 최대한 빨리 알림).
- 검증(Play 모드 실측)은 이번 세션에서 진행하지 않음 — 코드 변경만 완료, Unity 컴파일도 미확인.

## 3. 현재 상태 및 이슈

- **Unity 컴파일 미확인** — 다음 세션(또는 사용자) 최우선으로 컴파일 에러 0건 확인 필요.
- **Play 모드 실측 미완** — 실제로 벽에 낑겼을 때 10초 후 정체 판정이 걸려 순찰이 재시도되는지 확인 안 됨.
- `AIChaseTargetAction`, `AIStopMoveAction`은 이번 수정 범위에 포함하지 않음 — Chase는 목표가 계속 움직이므로 같은 정체 패턴이 그대로 맞을지 별도 검토 필요.

## 4. 다음 할 일 (Next Steps)

1. Unity 컴파일 확인 (0 에러).
2. Play 모드에서 Enemy를 의도적으로 벽에 낑게 만들어 정체 판정 → 실패 → 순찰 재시도가 실제로 동작하는지 확인.
3. 필요 시 `STUCK_CHECK_INTERVAL` / `STUCK_DISTANCE_THRESHOLD` / `DESTINATION_TIMEOUT` 수치를 실측 기반으로 재조정.
4. `AIChaseTargetAction`도 같은 정체 문제가 있는지 검토(추격 중 벽에 막히는 경우).

---

## 관련 문서
- [intent-008: 적 AI 시스템](../../../intent/intent-008-enemy-ai-system.md)
