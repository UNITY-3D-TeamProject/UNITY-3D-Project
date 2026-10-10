# Core 기준 낙하 복귀·라운드 사망 복구 설계

작성일: 2026-10-08. 설계 제안이며 런타임 코드는 변경하지 않았다.

## 결론

StageManager가 복귀 위치와 시점을 결정하고 기존 플레이어를 이동시킨다. PlayerSpawner는 전달받은 위치에 플레이어를 생성한다. GameManager가 소유한 PlayerState에는 씬 전환용 저장값과 라운드 최초 진입 스냅샷을 분리해서 보관한다.

사용자 확인 사항: 완전 사망하면 일반 체크포인트 진행도도 해당 라운드 시작 체크포인트까지 되돌린다. 라운드 최초 진입 스냅샷은 재시도 시 덮어쓰지 않는다.

## 현재 Core 코드에서 확인한 사실

| 현재 코드 | 확인 내용 | 설계상 변경 |
|---|---|---|
| StageManager.SCheckpoint | Area와 RespawnPoint가 있으며 _checkpoints 배열이 존재 | 배열을 공통 체크포인트 목록으로 사용 |
| StageManager | 낙하 구역·데미지·플레이어 참조 필드는 있으나 감지·데미지·이동 처리는 아직 없음 | 낙하와 복구 처리 구현 필요 |
| StageManager.HandlePlayerDeath | EndStage로 현재 StageBase 참조를 비우고 OnStageFailed 발생 | 라운드 복구로 연결하고 최종 스테이지 실패와 구분 |
| PlayerSpawner.SpawnPlayer | 로비는 GetLobbySpawnPoint, 스테이지는 스포너 자신의 transform 사용 | 스테이지 위치를 StageManager에서 전달받기 |
| PlayerSpawner | NewGame과 Respawn에 기본 Effect 적용 | 동일 객체 이동 시 스포너를 호출하지 않음. 재생성이 필요하면 라운드 복원 정책 추가 |
| GameManager.CompletePlayerSpawn | SceneTransition만 저장값 복원, 그 외 저장값 삭제 | 씬 전환 저장과 라운드 스냅샷의 수명·삭제 API를 분리 |
| GameManager.GameState | Title / Playing / Pause enum | HP·배터리 데이터 저장소로 사용하지 않음 |
| PlayerState | 현재 AttributeSet 참조와 씬 전환용 Dictionary 보유 | 라운드 스냅샷 보관·복원 추가 |
| StageBase / StagePhaseBase | StageProgress와 Enter/Exit 기반 틀은 있으나 라운드 재시작 계약 없음 | 게임 진행 초기화까지 필요할 때 별도 계약 확정 |

Core 밖의 전투·이동 구현은 검토하지 않았다. 기존 CombatMediator.OnDeath 연결은 확인했지만 모든 HP 감소 경로의 사망 통지, 부활 가능 여부, 이동 상태 초기화 API는 검증하지 않았다.

## 동작 규칙

| 상황 | 목적지 | HP | 배터리 | 일반 체크포인트 진행도 |
|---|---|---|---|---|
| 스테이지 최초 입장 | StageManager의 시작 지점 | 기존 초기화 또는 씬 전환 정책 | 기존 초기화 또는 씬 전환 정책 | 시작점 |
| 낙하 데미지 후 생존 | 실제 활성화한 체크포인트 중 가장 높은 인덱스 | 데미지 적용 후 현재값 | 현재값 유지 | 유지 |
| 낙하 데미지로 사망 | 현재 라운드의 지정 체크포인트 | 해당 체크포인트 최초 도달 시 값 | 해당 체크포인트 최초 도달 시 값 | 라운드 체크포인트로 되돌림 |
| 전투 등 다른 원인으로 사망 | 위와 동일 | 위와 동일 | 위와 동일 | 위와 동일 |

인덱스가 높더라도 방문하지 않은 지점은 낙하 목적지가 아니다. 이전 체크포인트 재접촉으로 진행도를 낮추지 않는다. 라운드 체크포인트도 공통 목록에 포함되므로 생존 낙하의 복귀 대상이다.

## 책임 분리

### StageManager

- Inspector의 체크포인트 목록, 시작 위치, 라운드별 체크포인트 매핑 소유.
- 현재 라운드와 최신 일반 체크포인트 인덱스 관리.
- 낙하 감지 후 기존 Attribute/Effect 경로를 통한 데미지 적용 요청.
- 사망 이벤트 수신, 복구 우선순위·중복 실행 제어.
- PlayerState에 스냅샷 저장/복원 요청.
- 기존 플레이어 이동과 이동 전후 입력·속도·전투 상태 정리 순서 조율.
- 라운드 재시도에서 EndStage를 호출하지 않음. 최종 실패/포기/퇴장은 별도 처리.

### PlayerSpawner / SpawnerBase

- PlayerSpawner.Start는 기존 로비/스테이지 분기를 유지해도 된다.
- 로비는 기존 GetLobbySpawnPoint 사용.
- 스테이지는 StageManager.TryGetInitialSpawnPoint로 위치를 받은 뒤 공통 SpawnPlayer(spawnPoint) 호출.
- Transform을 전달하면 현재 SpawnerBase.TrySpawnWithAttributes 시그니처를 그대로 활용할 수 있다.
- 최초 생성, 공통 초기화, GameManager.CompletePlayerSpawn 통지만 담당한다.
- 스포너는 체크포인트 목록, 현재 라운드, 사망 복귀 규칙을 판단하지 않는다.
- StageManager 참조와 시작점이 누락되면 명확한 오류로 생성 중단. 스포너 위치로 조용히 대체하지 않는다.

### GameManager / PlayerState

- GameManager는 PlayerState의 수명, 생성 완료와 씬 전환 연결을 계속 담당한다.
- PlayerState는 데이터 복사·복원만 담당한다. 체크포인트 선택·텔레포트·스테이지 종료를 결정하지 않는다.
- GameState enum은 현재 역할을 유지한다. 별도 GameState 데이터 클래스를 추가할 필요는 없다.
- PlayerState 저장은 런타임 메모리 보관이다. 게임 종료 후 이어하기는 별도 요구사항이다.

## 데이터 모델 제안

체크포인트 배열과 라운드 매핑을 분리한다. 배열 위치를 진행 인덱스로 사용하는 현재 틀을 유지하면 별도 Index 필드를 중복 저장할 필요가 없다.

```text
StageManager
  _initialSpawnPoint: Transform
  _checkpoints: SCheckpoint[]                 // 기존 Area + RespawnPoint
  _roundCheckpointIndices: int[]              // 라운드 → 공통 체크포인트 인덱스
  _currentRoundIndex: int
  _currentCheckpointIndex: int                // 활성화한 일반 복귀 진행도
  _recoveryState: ERecoveryState              // Playing / ReturningFromFall / RecoveringFromDeath
  _hasPendingDeath: bool                     // 낙하 처리 중 사망 통지 보존

PlayerState
  _playerAttributesForSaving                 // 기존 씬 전환용 저장
  _roundSnapshots: Dictionary<int, SRoundCheckpointSnapshot>

SRoundCheckpointSnapshot (변경 불가능한 값 데이터)
  CheckpointIndex: int
  Health: float
  Battery: float
```

라운드 매핑이 사망 목적지의 기준이다. 체크포인트에 IsRoundCheckpoint를 별도로 추가해 두 설정을 동시에 유지하지 않는다. 예: 공통 목록 C0~C6, 라운드 매핑 [0, 3, 6]. 일반 체크포인트 C2까지 갔다고 라운드가 바뀌지는 않는다.

단순 순차 맵에서는 지정된 다음 라운드 체크포인트의 활성화 시점에 라운드를 전환할 수 있다. 별도 목표 달성으로 라운드가 바뀌는 게임이라면 그 진행 로직이 StageManager의 동일 진입 함수를 호출하게 한다. 현재 StageProgress나 CurrentClearProgress를 라운드 번호라고 가정하지 않는다. 아직 방문하지 않아 스냅샷이 없는 지점을 사망 목적지로 활성화하지 않는다.

스냅샷은 현재 스테이지 시도에 속한다. 새 스테이지 시도 시작 시 비우고, 같은 시도의 사망·라운드 재시도에는 유지하는 것을 기본안으로 제안한다. 같은 씬을 복원용으로 재로드하는 경우는 새 시도와 구별해야 한다. 장기 저장에는 씬 Transform을 넣지 않고 안정적인 ID가 필요하다.

HP와 배터리만 복원하므로 씬 전환용 전체 Attribute Dictionary를 그대로 재사용하지 않는다. 데미지·속도·버프까지 원치 않게 되돌릴 수 있다. 실제 Attribute 키는 구현 전 데이터 정의와 대조한다.

## 제안 API와 실행 순서

아래 이름은 새로 추가할 계약의 예시이며 현재 존재하는 API라고 가정하지 않는다.

```text
StageManager
  TryGetInitialSpawnPoint(out Transform spawnPoint)
  ActivateCheckpoint(int checkpointIndex)
  HandlePlayerFall()
  HandlePlayerDeath()
  ReturnToLatestCheckpoint()
  RecoverAtRoundCheckpoint()
  MovePlayerTo(Transform destination)

PlayerSpawner
  SpawnPlayer(Transform spawnPoint)

PlayerState
  BeginStageSession()                         // 새 시도일 때만 라운드 저장 초기화
  TryCaptureRoundSnapshot(roundIndex, checkpointIndex)
  TryRestoreRoundSnapshot(roundIndex)
  ClearRoundSnapshots()
```

### 최초 입장

1. StageManager의 씬 참조·위치 설정을 준비하고 새 시도의 진행도/스냅샷을 초기화한다.
2. PlayerSpawner가 StageManager에서 시작 위치를 받아 한 번만 생성한다.
3. 기존 Effect 또는 씬 전환 저장값을 적용한 뒤 CompletePlayerSpawn으로 현재 플레이어 참조를 등록한다.
4. StageManager는 플레이어 참조, 사망 이벤트, 필요한 이동/충돌 참조를 연결한다.
5. 첫 라운드 체크포인트를 시작 위치와 연결하고 최종 초기 능력치로 최초 스냅샷을 저장한다. 시작점은 트리거 진입을 기다리지 않고 명시적으로 활성화한다.
6. 준비가 끝난 후 실제 스테이지 진행과 낙하/체크포인트 판정을 허용한다.

현재 GameManager.HandleSceneLoaded는 StartStage를 바로 호출한다. 이 호출만으로 플레이어와 스냅샷까지 준비됐다고 가정하면 안 된다. 설정 준비와 플레이 시작을 나누고, 생성 완료 이후 StartStage를 한 번만 실행하도록 조정한다. StageManager와 PlayerSpawner 양쪽이 각각 초기 생성을 호출하지 않게 한다.

### 체크포인트 활성화

1. 정상 플레이 중 살아 있는 플레이어만 체크포인트를 활성화할 수 있다.
2. 높은 인덱스의 유효한 도달 지점만 _currentCheckpointIndex에 반영한다.
3. 해당 지점이 지정된 라운드 진입점이라면 HP·배터리 스냅샷을 저장한다.
4. 같은 라운드의 스냅샷이 이미 있으면 보존한다. 재접촉·텔레포트 도착·사망 복귀로 덮어쓰지 않는다.

최초 스냅샷은 현재 AttributeSet의 참조를 보관하는 것이 아니라 float 값을 복사한 데이터다. HP가 0 이하인 상태를 유효한 부활 스냅샷으로 저장하지 않는다.

### 생존 낙하와 낙하 사망

1. 낙하 구역 진입을 한 번만 처리하도록 잠그고 데미지를 적용한다. 구역 내 체류나 복수 Collider 때문에 매 프레임 중복 데미지가 발생하면 안 된다.
2. 데미지 적용 중 사망 이벤트가 발생하면 _hasPendingDeath에 기록한다. 이 콜백 안에서 즉시 HP를 복원하고 처리를 끝내지 않는다.
3. 데미지 처리가 끝난 뒤 사망 통지 여부 또는 최종 HP <= 0을 확인해 한 경로만 실행한다.
4. 생존이면 최신 체크포인트로 이동한다. HP·배터리는 복원하지 않는다.
5. 사망이면 공통 사망 복구를 실행한다. _currentCheckpointIndex를 라운드 체크포인트 인덱스로 되돌리고, 그 이후 체크포인트의 활성화 표시/캐시도 되돌린다.
6. 이동·복구가 끝난 뒤 입력과 판정을 다시 허용한다. 텔레포트 도착을 새 체크포인트 진입으로 중복 처리하지 않는다.

사망 콜백이 즉시 HP를 복원하면 낙하 함수가 복원된 HP를 보고 생존 복귀도 실행할 수 있다. 따라서 사망을 한 번 확정한 요청은 HP가 복구되더라도 생존 경로로 바뀌지 않아야 한다. 전투와 낙하가 같은 처리 구간에 겹쳐도 사망 복구가 우선이며 한 번만 실행한다.

### 모든 원인의 사망 복구

1. 공통 사망 진입점에서 중복 요청을 차단하고 입력·추가 피격·체크포인트 판정을 잠근다.
2. 현재 라운드의 지정 위치와 최초 스냅샷이 유효한지 확인한다. 정상 흐름에서 항상 존재하도록 초기화한다. 누락 시 임의의 0 HP나 만피 값을 적용하지 말고 오류를 드러낸다.
3. 일반 체크포인트 진행도를 라운드 시작점으로 되돌린다. 스냅샷 자체는 보존한다.
4. StageManager가 기존 플레이어를 지정 위치로 이동시키고 이동 시스템의 속도·외력·낙하 상태를 정리한다.
5. PlayerState를 통해 최초 HP·배터리를 복원한다.
6. 전투 사망 플래그·애니메이션·Collider 등 실제로 사망 처리된 상태를 부활 계약에 따라 정리한다. 모든 상태가 일관된 후 플레이를 재개한다.

현재 Core는 CombatMediator.OnDeath를 구독한다. 우선 이 통지 경로를 활용하되, 구현 시 모든 HP 변경 원인이 HP <= 0 전이를 이 이벤트로 알리는지 확인해야 한다. 이 조건을 만족하지 못하면 HP 변화의 공통 경로에서 사망 통지를 통합해야 하며, 원인별로 별개의 복구 코드를 늘리지 않는다.

## 생성과 이동에 대한 선택

권장 기본안은 같은 씬에서 생존 낙하와 사망 모두 기존 플레이어를 이동·복구하는 것이다. 이 경우 PlayerSpawner는 최초 입장/실제 새 인스턴스가 필요할 때만 호출한다. 사망도 반드시 Instantiate를 해야 한다는 뜻은 아니다.

단, HP 복원만으로 전투 시스템의 사망 상태가 풀린다고 가정할 수 없다. 현재 객체가 사망 시 파괴되거나 재사용 불가능한 구조라면 재생성이 필요하다. 이 경우에도 StageManager가 위치를 정하고 PlayerSpawner만 생성한다. 오래된 인스턴스와 이벤트 구독을 정리한 뒤 새 인스턴스 하나만 남겨야 한다.

재생성 대안에서는 EPlayerSpawnReason에 RoundRecovery와 같은 명시적 정책을 추가하거나 Respawn의 의미를 명확히 바꾼다. 필요한 기본 Attribute 초기화 후 라운드 HP·배터리 스냅샷을 적용하고, 그 후에 OnPlayerSpawned를 알린다. 기존 Respawn 경로의 기본 Effect와 ClearSavedAttributes만으로는 요구사항을 만족하지 않는다. 같은 객체 이동에는 CompletePlayerSpawn을 다시 호출하지 않고 기존 Attribute 변화 알림으로 UI를 갱신한다.

이동 대상은 AttributeSet이 붙은 자식이 아니라 실제 플레이어 인스턴스/이동 시스템이 요구하는 대상이어야 한다. 생성 시 받은 GameObject를 명시적으로 등록하는 방안이 transform.root 추정에 의존하는 것보다 명확하다. 물리/이동 시스템의 워프·초기화 계약은 Core 밖 코드를 확인한 뒤 연결한다.

## 적용 패턴

| 패턴/원칙 | 적용 위치 | 이유 |
|---|---|---|
| Memento의 스냅샷 방식 | PlayerState + 변경 불가능한 SRoundCheckpointSnapshot | 최초 HP·배터리를 복사하고 필요할 때 복원 |
| Observer | 기존 OnPlayerSpawned, CombatMediator.OnDeath | 생성/사망 사실을 알리고 StageManager가 대응 |
| 작은 상태 머신 | StageManager의 ERecoveryState와 사망 요청 보존 | 정상 진행·낙하 복귀·사망 복구의 중복 실행 방지 |
| 책임 분리 | StageManager 결정/이동, PlayerSpawner 생성 | 스폰 규칙을 여러 클래스에서 중복 판단하지 않음 |

현재 두 복귀 규칙은 StageManager의 두 메서드로 충분하다. Strategy는 복귀 규칙이 여러 개로 늘고 런타임 교체가 필요해질 때 검토한다. enum 상태 몇 개를 위해 상태 클래스 계층을 만들 필요도 없다. PlayerSpawner는 생성 책임을 가진 객체라는 설명이면 충분하며 기존 구조를 억지로 GoF Factory Method라고 부를 필요는 없다.

## 예시

라운드 1 시작점 C0에서 HP 100 / 배터리 80을 저장했다고 하자.

1. C1 → C2까지 진행해 HP 70 / 배터리 45가 됨.
2. 낙하 데미지 10: HP 60 / 배터리 45로 C2 복귀.
3. 이후 전투로 사망: C0으로 이동하고 HP 100 / 배터리 80 복원. 최신 체크포인트도 C0으로 되돌림.
4. C1을 다시 찍기 전에 낙하: C0으로 복귀.
5. 라운드 2 시작점 C3에 최초 도달 시 HP 55 / 배터리 30이면 새 라운드 스냅샷은 55 / 30.
6. 이후 라운드 2에서 반복 사망해도 C3과 55 / 30을 사용. 재시도로 스냅샷을 덮어쓰지 않음.

## 검증 기준과 구현 전 확인 사항

설계 검토만 수행했으며 Unity 실행·컴파일·플레이 테스트는 하지 않았다. 구현 후에는 다음을 확인한다.

- 생존 낙하가 데미지 적용 후 값으로 활성화된 가장 높은 체크포인트에 한 번만 복귀하는가.
- 치명적인 낙하가 사망 복귀 한 번만 실행되고 일반 낙하 복귀가 뒤따르지 않는가.
- 전투 등 모든 원인의 HP <= 0이 같은 사망 복구로 연결되는가.
- 사망 후 일반 진행도는 라운드 시작점으로 되돌아가고 스냅샷은 반복 사망/재접촉에도 유지되는가.
- 첫 체크포인트를 건드리기 전 사망에도 초기 라운드 위치와 스냅샷이 준비돼 있는가.
- 씬 전환용 저장/삭제가 라운드 스냅샷을 덮어쓰거나 지우지 않는가.
- 이동 뒤 잔여 낙하 속도, 사망 플래그, 입력 잠금이 남지 않는가.
- 라운드 매핑의 범위·순서, null 영역/위치, 시작점과 첫 라운드 지점 연결을 검증하는가.
- Pause/복구/종료 중 체크포인트와 낙하 판정이 진행되지 않는가.
- 씬의 낙하 구역은 탈출 가능한 하단을 빠짐없이 덮는가. 점 단위 겹침 감지로 고속 통과를 놓칠 수 있어 판정 방식과 구역 두께를 실제 이동 속도로 검증해야 한다.

구현 전 확인이 필요한 것은 전투·이동의 실제 부활/워프 API, HP·배터리 키, 라운드 재시도 시 적·기믹·목표까지 초기화할지 여부다. 이번 요청은 플레이어 위치와 두 능력치 복구를 확정하며 월드 전체 롤백은 확정하지 않는다.

## 작업 기록 및 다음 단계

- 방법: CLAUDE.md, 응답/컨벤션/intent/handover 규칙, SungJun 이전 설계 기록과 Core 스크립트를 읽고 대조했다.
- 결정: 두 복귀 위치를 분리하고 최초 라운드 스냅샷을 별도 보관한다. 사용자 답변에 따라 사망 시 일반 체크포인트 진행도도 되돌린다.
- 변경 파일: 이 설계 문서, 설계 intent와 관련 목차. 게임 스크립트·프리팹·씬은 변경하지 않았다.
- 현재 상태: 설계 완료, 구현 전. 부활/이동 계약과 월드 초기화 범위는 확인 대상이다.
- 다음 단계: 구현 요청 시 외부 시스템 연결 계약을 확인하고 데이터 → 초기 스폰 위치 → 체크포인트 활성화 → 낙하/사망 공통 복구 순으로 적용한다.
