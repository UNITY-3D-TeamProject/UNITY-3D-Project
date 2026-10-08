# 3단계 최초 스폰 위치 및 첫 라운드 스냅샷 연결 안내

- 작업 요약 및 방법: 현재 StageManager, PlayerSpawner, GameManager, SpawnerBase, PlayerState를 대조하고 사용자 적용용 코드를 대화로 안내했다. 게임 파일은 수정하지 않았다.
- 결정 사항: StageManager.GetInitialSpawnPoint가 첫 라운드에 매핑된 체크포인트의 RespawnPoint를 반환한다. GameManager.CurrentStageManager 조회 프로퍼티를 통해 PlayerSpawner가 위치를 받아 기존 생성 경로를 사용한다. 로비 위치 선택은 유지한다.
- 초기화 순서: GameManager.HandleSceneLoaded에서는 TakeCurrentStage만 수행하고 기존 StartStage 호출은 제거한다. CompletePlayerSpawn에서 능력치 초기화/복원 완료 후 StageManager.InitializeFirstRound(playerState), OnPlayerSpawned 통지, StartStage 순서로 연결한다.
- 첫 라운드 준비: StageManager 인스턴스당 최초 준비 시 기존 라운드 스냅샷을 비우고 0번 라운드를 저장한다. 저장 성공 확인 후 현재 라운드/체크포인트 인덱스와 준비 플래그를 설정한다. 중복 준비는 건너뛰고 StartStage는 준비 플래그를 확인한다. 같은 씬의 사망 재시도에서는 이 초기 스폰 경로를 호출하지 않는다.
- 현재 상태 및 이슈: 제안 코드이며 적용·Unity 컴파일·실행 검증을 하지 않았다. 사망 시 기존 EndStage 처리는 아직 남으며 라운드 복구 구현은 7단계다. 스테이지 진입은 현재의 씬 로드 후 PlayerSpawner.Start 흐름을 기준으로 한다.
- 확인 기준: 첫 라운드 시작 Transform에 한 번 생성, 첫 스냅샷은 Effect/씬 전환 복원 완료 값, 준비 전에 Stage 시작 방지, 로비 스폰 유지. 체크포인트/라운드 인덱스 및 RespawnPoint의 Inspector 연결 필요.
- 다음 할 일: 사용자 코드 적용 확인 후 4단계 체크포인트 도달과 다음 라운드 최초 스냅샷 저장 연결.

## 후속 확인 — 단일 커밋 메시지 안내

- 사용자가 코드와 문서를 묶는 커밋 메시지 하나를 요청했다. 실제 커밋은 수행하지 않는다.
- 실제 diff: StageManager의 최초 스폰 위치 조회·첫 라운드 초기화 함수·시작 준비 검사, PlayerSpawner의 위치 조회 연결, GameManager의 CurrentStageManager 프로퍼티가 추가됐다.
- 미반영 사항: GameManager.CompletePlayerSpawn의 InitializeFirstRound 호출 및 스폰 완료 후 StartStage 호출은 아직 추가되지 않았다. HandleSceneLoaded의 기존 StartStage 호출도 남아 있다. 따라서 현재 상태에서는 준비 플래그가 설정되지 않아 스테이지 시작이 차단된다.
- 커밋 메시지는 완성된 시작 흐름으로 표현하지 않고 최초 스폰 위치 연결과 첫 라운드 초기화 기반 추가로 작성한다. Unity 실행 검증은 미실시다.
- 다음 할 일: GameManager의 남은 초기화·시작 호출 순서 변경 후 Unity 확인.

## 후속 안내 — GameManager 누락 호출 연결

- 현재 파일을 다시 확인하고 CompletePlayerSpawn 끝부분에서 InitializeFirstRound → OnPlayerSpawned → StartStage 순서로 연결하는 교체 코드를 안내했다.
- HandleSceneLoaded에서는 TakeCurrentStage를 유지하고 StartStage 호출 한 줄을 제거하도록 안내했다.
- 게임 파일은 수정하지 않았으며 적용·Unity 검증은 사용자 적용 이후 확인해야 한다. 같은 씬 사망/낙하에서는 이 스폰 완료 경로를 재호출하지 않는다.

## 최종 적용 확인 및 단일 커밋

- 사용자 수정 후 GameManager.CompletePlayerSpawn의 능력치 복원 → InitializeFirstRound → OnPlayerSpawned → StartStage 순서가 반영된 것을 확인했다.
- HandleSceneLoaded의 기존 StartStage 호출은 주석 처리되어 실행되지 않는다. 앞서 기록된 호출 누락은 해소됐다.
- 사용자 요청에 따라 GameManager, PlayerSpawner, StageManager와 이 안내 문서 및 HANDOFF_POINTER를 하나의 feat 커밋으로 묶는다.
- 검증: 변경 diff와 호출 순서 확인, git diff --check 통과. Unity 컴파일 및 Play Mode 검증은 미실시다.
- 다음 할 일: Inspector 매핑과 최초 스폰/스냅샷 확인 후 4단계 체크포인트 도달 및 다음 라운드 최초 저장 연결. 사망·낙하 복구는 후속 단계에 남아 있다.

## Unity 검증 절차 안내

- 사용자에게 3단계 검증 범위인 최초 스폰 위치, 초기 능력치 스냅샷, 스테이지 시작 순서를 확인하는 절차를 안내했다.
- Inspector: 체크포인트 Area/RespawnPoint, 첫 라운드 인덱스, 스테이지 스포너의 Is Lobby Spawner 해제, Player Prefab/Effects, GameManager 및 구체 StageBase 구현체 존재 확인.
- 관찰: 스포너와 복귀 지점을 떨어뜨려 생성 위치 구분, SaveRoundSnapshot 저장 직후 값 로그와 CompletePlayerSpawn 시작 호출 직후 준비/실행 로그로 확인. 예제 로그는 대화 안내만 제공하고 소스에 삽입하지 않았다.
- 정상 기준: 최초 생성 1개, 지정 지점 사용, 초기화/복원된 HP·배터리 저장, HasRoundSnapshot(0)=true 및 IsRunning=true. 씬 전환 경로에서는 이전 씬의 저장값을 기준으로 비교한다.
- 낙하 복귀·사망 복구·체크포인트 도달은 후속 구현 범위라 이번 단계 성공 기준에 포함하지 않는다. 실제 Unity 검증은 수행하지 않았다.

## 스폰 사유 정리 코드 안내

- 사용자 확인: 최초 소환 위치도 전체 체크포인트 목록에 포함하는 현재 구조를 유지한다.
- 전체 Assets C# 참조 검색 결과 EPlayerSpawnReason.Respawn은 PlayerSpawner 초기 Effect 조건에만 있고 해당 사유를 지정하는 호출은 없다.
- 사용자가 직접 적용할 코드를 요청했으므로 런타임 파일은 수정하지 않았다. GameManager enum에서 Respawn 제거, PlayerSpawner 초기화 조건을 NewGame만으로 변경, PrepareSpawn 설명을 새 게임/씬 전환 용도로 정리하도록 안내한다.
- 씬 전환은 SceneTransition으로 생성하고 기존 저장값을 복원한다. 같은 씬 사망은 StageManager와 PlayerState의 라운드 복구 경로를 사용하며 PrepareSpawn/CompletePlayerSpawn을 재호출하지 않는다. RespawnPoint 이름은 유지한다.
- 코드 적용 및 Unity 검증은 아직 하지 않았다.

## Respawn 사유 제거 적용 완료

- 사용자 직접 수정 요청에 따라 GameManager.EPlayerSpawnReason에서 Respawn을 제거하고 PlayerSpawner의 초기 Effect 조건을 NewGame만으로 변경했다. 관련 주석도 새 게임/씬 전환 기준으로 수정했다.
- 체크포인트의 RespawnPoint, 첫 라운드 시작점과 최초 스폰 위치 매핑은 유지했다. 원본 소스 인코딩을 유지했으며 PlayerSpawner는 CP949다.
- 관련 intent-012와 개인 intent 목차를 최신 합의로 갱신했다. 씬 전환 복원과 후속 StageManager 사망 복구 책임은 유지한다.
- 검증: Assets 전체 C#에서 EPlayerSpawnReason.Respawn 참조 0개, dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet 성공(오류 0, 이번 변경과 무관한 경고 5), git diff --check 확인.
- Unity Play Mode 검증 및 커밋은 수행하지 않았다. 다음 단계는 최초 스폰 동작 확인과 체크포인트 도달 처리다.

## Respawn 정리 커밋·푸시 요청

- 사용자가 현재 변경의 원격 반영을 요청했다. 코드 2개와 관련 문서 4개를 하나의 refactor 커밋으로 묶어 origin/feature/System/DeathSystem에 일반 푸시한다.
- 실행 결과는 Git 이력과 원격 동기화 상태로 확인한다. 기존 빌드 결과는 오류 0개이며 Unity 실행 검증은 남아 있다.
