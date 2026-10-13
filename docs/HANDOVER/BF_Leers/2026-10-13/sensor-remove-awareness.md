# Sensor 발견 게이지 제거 (2026-10-13)

## 1. 작업 요약 및 방법
- 사용자 판단: 발견 게이지 때문에 적이 플레이어를 인식해도 추격·공격 가지로 바로 넘어가지 않는다. 그래서 게이지를 없앴다.
- `Assets/_Project/Scripts/AI/Sensor.cs`만 수정했다.
  - SerializeField `_detectTimeNear`, `_detectTimeFar`, `_awarenessDecayTime`와 private `_awareness`를 삭제했다.
  - `Detect()`: 대상이 보이면 그 감지 주기에 바로 `_currentTarget`, `_graceTimer`, `_lastKnownPosition`을 설정한다. 놓쳤을 때의 게이지 감소 줄은 삭제했다.
  - 클래스와 `Detect()` summary 주석을 동작에 맞게 고쳤다.
- 공개 API가 바뀌지 않아 AIController와 BT 노드는 수정하지 않았다.
- intent-011과 INTENT_POINTER에 제거 결정을 기록했다.

## 2. 결정 사항
- 놓친 뒤 유예(`_loseGraceTime`)는 **유지**한다. 유예는 놓친 뒤에만 작동해 공격 진입과 무관하다. 또 "등 뒤로 돌면 바로 놓쳐 엉뚱한 곳을 수색함" 문제를 계속 막는다.
- Melee/Ranged/GC 프리팹 YAML에 남은 옛 필드 값은 직접 지우지 않았다. Unity는 이 값을 무시하고, 프리팹을 다시 저장할 때 자동으로 정리한다.

## 3. 현재 상태 및 이슈
- `recompile` 결과 컴파일 에러 0. **Play 검증은 아직 안 했다.**
- 첫 감지에는 여전히 최대 `_detectInterval`(기본 0.2초)만큼 지연이 있다. 감지 주기 분산용이며, 필요하면 프리팹 값을 낮추면 된다.
- 콘솔에 이번 작업과 무관한 기존 에러가 있다. `EnemyRangedAttack` / `EnemyAimLine`의 "발사 지점이 연결되지 않았습니다" Assert(MonsterSpawner로 생성될 때)와 Map/Feed 프리팹의 누락된 중첩 프리팹이다. 원거리 적이 공격하지 않는다면 발사 지점 배선부터 확인해야 한다.

## 4. 다음 할 일 (Next Steps)
- Play 모드에서 플레이어가 시야에 들어오면 곧바로 추격·공격하는지, 등 뒤로 돌아도 유예 동안 계속 추격하는지 확인한다. 확인되면 intent-011을 `resolved`로 바꾸고 `clear/`로 옮긴다.
- 원거리 적 프리팹의 발사 지점(`_firePoint`) 연결 여부를 확인한다.
