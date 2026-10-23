# 낙하 복귀 방향 설정 되돌림 (intent-059, 진행 중)

## 1. 작업 요약 및 방법
- `StageManager.MovePlayerTo` 끝에 넣었던 `_playerFacade.SetLookDirection(destination.forward)`와 주석을 지웠다(`251f381c`에서 추가한 3줄).
- 이유: `SetLookDirection`이 몸 방향을 고정하고 생성 연출 끝에만 풀어서, 낙하 복귀 뒤 시점을 돌려도 몸(Look)이 돌지 않았다. 사용자 요청으로 복귀 방향보다 몸 회전을 우선한다.
- 화살표 Gizmo(`SpawnFacingGizmo`, `CheckpointTrigger`, `PlayerSpawner`)는 그대로 둔다.

## 2. 결정 사항
- 낙하 복귀 방향은 당분간 떨어지기 전 방향 그대로 둔다. 플레이어 쪽에 고정하지 않는 방향 설정 API가 생기면 다시 연결한다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. Play로 낙하 복귀 후 몸 회전은 직접 확인하지 않음(되돌린 코드는 원래 동작으로 돌아감).
- 낙하 복귀 시 체크포인트 방향을 보지 않는 원래 문제는 그대로 남아 있다.

## 4. 다음 할 일 (Next Steps)
- 플레이어 담당(이세훈)에게 고정하지 않는 방향 설정(또는 생성 연출 중이 아니면 바로 고정 해제) 요청 → 생기면 `MovePlayerTo`에 다시 연결하고 TestScene에서 확인.
