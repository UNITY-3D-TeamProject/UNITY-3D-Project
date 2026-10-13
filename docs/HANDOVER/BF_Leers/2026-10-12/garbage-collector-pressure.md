# GC 압박 가지 (플레이어 근처 웨이포인트로 주기적 이동)

## 1. 작업 요약 및 방법
- 테스트해 보니 무작위 순찰만으로는 플레이어가 2라운드 미로에서 GC를 한 번도 만나지 못할 수 있었다.
- Alien: Isolation의 Director 방식을 참고했다. 정확한 위치가 아니라 근처 구역만 알려 주는 방식이다.
- 규칙: **N초 동안 플레이어를 못 보면 플레이어에게 가장 가까운 웨이포인트로 간다.** 이 동작은 BT 가지로 구현했다.
- 코드 변경
  - `AIController`
    - `TimeSinceTargetSeen`: `Update` 맨 앞에서 `HasVisibleTarget`이면 시각을 갱신한다. `Start`에서 초기화한다.
    - `MoveToPatrolPointNear(Vector3)`: Route에서 받은 최근접 지점으로 `MoveTo`한다.
  - `WaypointPatrolRoute.TryGetNearestWaypoint`: 기존 `FindNearestIndex`를 재사용하고 `_lastIndex`를 갱신한다.
  - 새 범용 BT 노드
    - `AITimeSinceTargetSeenCondition` (`Seconds`)
    - `AIMoveNearTargetAction` (`Target`, GameObject). 도착하면 Success, 정체되면 Failure로 끝나며 `AIPatrolAction`과 같은 방식이다.
  - `GarbageCollectorSpawner`
    - 생성 시 `BehaviorGraphAgent.SetVariableValue("Player", 플레이어 몸체)`로 플레이어를 넣는다. 몸체는 `CharacterController`가 붙은 오브젝트이며, `StageManager.BindPlayer`와 같은 방식으로 찾는다.
    - 사망 후 재스폰하면 플레이어 오브젝트가 새로 만들어지므로 `GameManager.OnPlayerSpawned` 시점에 다시 넣는다.
- 컴파일 0에러(MCP recompile). Play는 검증하지 못했다. 그래프 배선이 아직 없다.

## 2. 결정 사항
- **판단은 BT가 한다.** 처음 계획은 Route 안에 타이머를 숨기는 방식이었다. 사용자 지적으로 바꿔서, 조건과 쿨다운을 그래프에 두고 코드는 조회·이동 API만 제공한다.
- 쿨다운은 Unity Behavior 기본 `Cooldown` 노드를 쓴다. 자식이 끝난 뒤 Duration 동안 Failure를 반환한다(`CooldownModifier.cs` 확인).
- 추격이 끝나면 "못 본 시간"이 0부터 다시 쌓이므로 추격 직후에는 압박하지 않는다. 따로 만든 쿨다운이 없어도 쉬는 시간이 생긴다.
- 스포너는 `SpawnerBase`를 상속하지 않는 지금 구조로 유지한다. 시스템 팀원 확인이 필요하며, intent-021 열린 질문에 적었다.
- 존재 알림(사운드·연출)은 후속 작업이다.

## 3. 현재 상태 및 이슈
- [USER] `GarbageCollectorAI` 그래프가 아직 조립되지 않았다.
  - 블랙보드에 `Player`(GameObject)를 추가해야 한다. 없으면 스포너가 에러 로그를 1회 남긴다.
  - 수색 가지와 순찰 가지 사이에 다음 가지를 넣어야 한다.
    ```
    Sequence
    ├─ [대상을 Seconds 초 동안 못 봤다]  (Seconds = 20)
    └─ Cooldown (Duration = 20)
        └─ [Target 근처 순찰 지점으로 이동]  (Target = Player)
    ```
  - abort 설정은 기존 순찰 가지와 같게 맞춘다.
- 최근접 판단은 직선거리 기준이다. 벽 너머 지점이 고를 수 있지만 "근처로 압박"하는 의도에는 충분하다고 판단했다.
- 이전 세션의 Console 기록에 `[SpawnerGC] 프리팹 루트에 WaypointPatrolRoute 가 없어...` 에러가 있다. 프리팹 배선을 확인해야 한다.

## 4. 다음 할 일 (Next Steps)
1. [USER] 위 그래프를 조립한다. 처음에는 Seconds와 Duration을 5로 낮춰 테스트한다.
2. Play 검증
   - 5초 동안 못 보면 근처 웨이포인트로 가는지 확인한다.
   - 도착 후 Cooldown 동안 순찰하는지 확인한다.
   - 추격하다 놓친 직후에는 압박하지 않는지 확인한다.
   - 사망 후 재스폰해도 새 플레이어를 기준으로 압박하는지 확인한다.
3. PR 본문에 GC 스포너가 `SpawnerBase`를 쓰지 않은 이유를 적고, 시스템 팀원에게 확인받는다.
4. 후속 작업으로 존재 알림을 intent로 분리할지 결정한다.
