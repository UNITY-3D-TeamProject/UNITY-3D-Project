# 원거리 적 후퇴 중 사격 (2026-10-14)

관련 intent: [intent-013](../../../intent/intent-013-ranged-enemy-behavior.md) (`open`)
브랜치: `feature/AI/Enemy`

## 1. 작업 요약 및 방법
- 문제: `EnemyAI_Ranged` 그래프는 Try In Order 1번이 후퇴, 2번이 공격이다. 그래서 플레이어가 4m 안에 들어오면 공격이 끊긴다. 이동 중에는 `AIController.Update`가 몸을 이동 방향으로 돌려서 등도 보인다.
- 방법: 새 노드 없이 후퇴와 원거리 공격을 `Run In Parallel`(Until Any Complete)로 동시에 돌린다. 그러면 생기는 충돌 두 가지를 코드로 막았다.
  - `AIController.ShouldFaceMoveDirection` (bool 속성, 기본 true): false면 이동 중에도 이동 방향 회전 요청(`RequestRotate(desired2D)`)을 보내지 않는다. 이동 요청은 그대로 보낸다.
  - `AIRangedAttackAction.KeepMoving` (블랙보드 bool, 기본 false): true면 `OnStart`에서 `StopMove`를 부르지 않는다.
  - `AIRangedAttackAction`은 `OnStart`에서 `ShouldFaceMoveDirection = false`로 바꾸고 `OnEnd`에서 true로 되돌린다. 그래서 회전은 공격 노드의 `FaceDirection`만 따른다.
- `AIRetreatAction`은 summary 주석만 고쳤다.

## 2. 변경된 파일
- `Assets/_Project/Scripts/AI/AIController.cs`
- `Assets/_Project/Scripts/AI/BT/AIRangedAttackAction.cs`
- `Assets/_Project/Scripts/AI/BT/AIRetreatAction.cs` (주석)
- `docs/intent/intent-013-ranged-enemy-behavior.md` (결정 사항 추가, 열린 질문 해결)

## 3. 결정 사항
- 원거리 공격이 실행되는 동안에는 항상 대상을 바라본다(이동 방향 회전 끔). 제자리 사격에는 이동이 없으므로 영향이 없다.
- 기본값이 기존 동작과 같다(`KeepMoving=false`, `ShouldFaceMoveDirection=true`). 그래서 근거리 적과 기존 공격 가지는 바뀌지 않는다.

## 4. 현재 상태 및 이슈
- 컴파일 에러 0건(MCP `recompile`).
- **[USER] 그래프 배선 수정 필요** (`EnemyAI_Ranged.asset`, 에디터에서):
  ```
  Try In Order
   1. Guard: 대상이 [RetreateRange] 안 (기존)
        → Run In Parallel (Mode: Until Any Complete)
             ├ Repeat → 후퇴 (Range=RetreateRange, MoveDistance=6)
             └ 원거리 공격 (Range=RetreateRange, KeepMoving=true, 간격·조준 시간은 2번 가지와 같게)
   2. 공격 가지 (기존, KeepMoving=false)
  ```
  - 처음 배선(후퇴를 Repeat 없이, 공격 Range=AttackRange)으로 Play eval을 돌려 보니 문제가 있었다.
    - 후퇴 한 구간이 약 0.4초(약 2m) 만에 끝난다. 그때마다 병렬 노드가 끝났다가 다시 시작해 사격 대기(1~3초)가 리셋되고, 그래서 거의 쏘지 못했다(후퇴 중에도 몸은 플레이어를 바라봄은 확인).
    - 후퇴를 Repeat로 감싸면 병렬 노드가 계속 유지된다. 공격 Range를 RetreateRange로 두면 대상이 4m 밖으로 나갈 때 공격이 Success로 끝나며 병렬 노드도 함께 빠져나온다.
  - 공격 노드에서 남은 대기를 이어 쓰는 방식도 시도했다. 하지만 `_phaseEndTime`이 절대 시간이라 노드가 쉬는 동안에도 만료되어 효과가 없었고, 되돌렸다.
- **Play 검증 미완.**
- 알려진 트레이드오프:
  - 후퇴 지점에 도착하는 순간 조준 중이었다면 그 사격은 취소된다. 그 뒤 2번 가지가 새 랜덤 대기부터 다시 시작한다.
  - 후퇴 중 공격에는 사선 조건이 없다.
  - 원거리 적 Animator에 뒷걸음 모션이 있는지 확인해야 한다.

## 5. 다음 할 일
1. [USER] 위 그래프를 배선하고 Play로 확인한다.
   - 다가가면 플레이어를 바라본 채 물러나는지
   - 후퇴 중에도 조준선 → 발사가 나오는지
   - 후퇴가 끝나면 제자리 사격으로 이어지는지
   - 추격·순찰 중 회전이 예전과 같은지(회귀)
2. 검증이 끝나면 intent-013을 resolved 처리한다.
