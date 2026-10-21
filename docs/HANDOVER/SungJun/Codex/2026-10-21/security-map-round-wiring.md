# Map_4_Security 라운드 구역·문·발판 연결

intent: [intent-057](../../../../intent/intent-057-security-app-rounds.md)

## 작업 요약 및 방법
- 사용자 요청에 따라 `Map_4_Security.unity`의 기존 SecurityApp에 1라운드 방 4개, 2라운드 특수몹 발판 5개, 3라운드 보스 스포너·탈출문을 연결했다.
- Unity 6000.4.8f1 및 Unity CLI 1.0.0-beta.13(공식 업데이트 확인 결과 최신)을 사용했다. Unity MCP의 에디터 API로 오브젝트·SerializedProperty를 설정하고 씬을 저장했다.
- 스크립트, Canvas_InGame 및 기타 프리팹 원본, 다른 씬은 수정하지 않았다. 다른 씬을 열거나 실제 씬 전환을 실행하지 않았다. 커밋·push하지 않았다.

## 사용자 결정
- 요청서의 방 순서와 실제 씬 구조가 달라 **실제 동선인 굽은 통로 → 큰 방 → 작은 방 → 작은 방** 순서로 Room_1~4를 설정했다.
- 통로는 BoxCollider 하나로 덮으면 큰 방과 겹치므로, 사용자 확인 후 **CombatZone 하나에 트리거 BoxCollider 2개**를 붙였다. 나머지 방은 각각 1개다.
- Platform_1~5에 특수몹 1마리씩 배치하고 각 구역을 깨면 다음 발판(2~6)이 나타나게 했다. Platform_1은 시작부터 보인다.
- 실제 2라운드 체크포인트 트리거·리스폰 위치는 Platform_1 위, 3라운드는 **Platform_6 위**다. 요청서에 적힌 Platform_5와 다르며, 루트 Transform 위치와 실제 자식 트리거 위치도 다르다. 체크포인트 위치·기존 연결은 유지했다.
- 보스 아레나 뒤쪽에 로비행 탈출문을 배치했다.
- 루트 MonsterSpawner 5개는 삭제 대신 비활성화했다.

## 배치 및 연결

### 1라운드
위치: `Stage_BossRun/03_Round1_Battle/CombatZones/Room_1~4`

| 구역 | 형태 | 적 수(임시) | Spawn Points | 출구 문 루트 월드 위치 X/Z | 문짝 / 잠긴 패널 |
|---|---|---|---|---|---|
| Room_1 | 굽은 통로 | 근접 3 + 원거리 1 | 4개 | 100 / 80 | 2 / 2 |
| Room_2 | 큰 방 | 근접 4 + 원거리 2 | 4개 | 130 / 50 | 2 / 2 |
| Room_3 | 작은 방 | 근접 3 | 3개 | 180 / 10 | 1 / 2 |
| Room_4 | 작은 방 | 근접 3 | 3개 | 230 / 10 | 1 / 2 |

- 적 수는 요청서의 **방 형태별 임시값**을 실제 동선에 맞춰 적용했다. 합계 16마리다.
- 모든 트리거 높이는 월드 6m, 바닥에서 위로 덮는다. 다른 방의 트리거와 Bounds가 겹치지 않음을 검사했다.
- Spawn Points 14개는 기존 1라운드 NavMesh를 `SamplePosition`으로 확인하고 그 위에 배치했다.
- Doors는 해당 문의 기존 InteractButton 이벤트 대상 StateMover를 사용하고, 같은 문을 여는 양쪽 패널 2개를 Locked Panels에 연결했다.
- 입구 문은 기존 E 상호작용을 유지한다. Room_3 입구(X=150), Room_4 입구(X=200) 패널도 그대로다.
- 비활성 상태였던 `03_Round1_Battle/NavMeshSurface`를 활성화했다. 기존 NavMesh 데이터는 다시 굽지 않았다.

### 2라운드
- `Platform_1~5/CombatZone`을 각 발판의 자식으로 추가했다. 발판이 숨겨지면 해당 구역도 숨겨진다.
- 각 구역은 `SOMonsterData_Special` 1마리와 SpawnPoint_1을 사용한다. 다음 발판은 같은 단계의 `NextPlatform`에 연결했다.
- Platform_1의 구역 시작 X는 231.65로, 중앙 진행 시 2라운드 체크포인트 트리거(시작 X≈231.37)에 먼저 진입하도록 했다. 나머지 구역은 각 발판 바닥의 X/Z 범위를 덮는다.
- `04_Platforms`에 NavMeshSurface를 추가했다. 보스 구간처럼 Children / PhysicsColliders / 전체 레이어를 수집하되, 에이전트 타입은 특수몹의 타입 **0**을 사용했다. 보스 전용 타입을 복사하지 않았다.
- 모든 발판이 켜진 에디터 상태에서 `NavMesh-04_Platforms.asset`을 베이크했다. 발판 6개 중심과 실제 생성 위치의 NavMesh를 확인했다.

### 3라운드 및 기타
- SecurityApp의 Boss Fight Spawner를 `Stage_BossRun/05_Boss/BossFight`에 연결했다.
- 기존 `ScenePortal_ToLobby` 프리팹을 씬 인스턴스로 배치했다. 이름은 `05_Boss/ExitDoor_ToLobby`, 월드 위치 **(378.5, 0, 10)**, Y 회전 90도다. 기존 아레나의 뒤쪽 끝에서 약 4m 안쪽이다.
- Destination=Lobby, Is Locked=false, Is Return Point=false. 보스 라운드의 Exit Door로 연결했다. 시작 시 숨김, 보스 A 격파 시 활성화된다.
- 씬에 원래 있던 `Security_CoverGenerator/GimmickSwitch._indicator`의 깨진 선택용 참조 1건을 씬 인스턴스에서 None으로 정리했다. 표시등은 선택 필드이고 코드의 null 처리 경로가 있다. 프리팹 원본은 수정하지 않았다.

## 검증 결과
검증은 **이 씬의 Play 모드**에서 수행했다. 위치 이동으로 실제 트리거에 들어가고, HP 값을 0으로 설정해 기존 사망 이벤트 경로를 실행했다. 게임 코드나 프리팹에 검증 코드를 추가하지 않았다.

- 컴파일 상태: `failed=false`, `errors=[]`, `compilationFailed=false`.
- 최종 편집 모드 검사: Missing Script 0, 끊어진 ObjectReference 0, 방 간 트리거 겹침 0. Rooms=4, Steps=5, 보스·탈출 참조 정상.
- 시작 상태: 네 방의 출구 패널 콜라이더 전부 비활성, Platform_1 활성, Platform_2~6 비활성, 탈출문 비활성, 보스 체력바 숨김.
- 1라운드: 진입 시 4 / 6 / 3 / 3마리 생성 확인. 전멸 후 해당 문 StateMover 활성 및 패널 콜라이더 복구, 최종 문 이동 progress=1, `IsRoundCleared(0)=true` 확인.
- 사망 복구: Room_1 클리어 후 Room_2에서 6마리 중 1마리 처치 → 플레이어 HP 0 → 사망 애니메이션 종료 후 1라운드 시작점에 새 플레이어 생성. Room_2는 살아 있는 적 제거·대기 상태로 복구되고 재진입 시 6마리 생성. Room_1의 클리어·열린 문은 유지.
- 2라운드: 체크포인트에서 라운드 번호 1로 변경. 각 발판 진입 시 특수몹 1마리 생성 및 처치 후 다음 발판 활성, 마지막에 `IsRoundCleared(1)=true` 확인. 다섯 특수몹 모두 `NavMeshAgent.isOnNavMesh=true`, 첫 특수몹의 실제 위치 이동 확인.
- 2라운드 사망 복구: Platform_1 클리어 후 Platform_2에서 사망. 사망 애니메이션이 끝난 뒤 Platform_1 체크포인트로 복구, Platform_1 클리어 및 Platform_2 활성 유지, Platform_2 적 제거·재진입 1마리 재생성 확인. 최초 자동 검증은 사망 애니메이션 종료 전에 다음 조작을 보내 유효하지 않았으므로 결과에서 제외하고, 새 플레이어 복구 확인 후 다시 검사했다.
- 3라운드: Platform_6의 체크포인트에서 라운드 번호 2로 변경하고 실제 보스 A/B 생성 및 BossEncounter 시작 확인.
- 체력바: 1-1 필드 보스 위 → B HP 0으로 그로기 시작·거대 보스 위 → 그로기 타이머를 검증 중 11초 진행시켜 1-2·필드 보스 위 → B HP 0으로 최종·거대 보스 위 확인. 위쪽 바 Y=-30, 아래쪽 바 Y=-100. 최초 보스 HUD는 화면 캡처로도 확인했다.
- 보스 A HP 0 → Defeated, `IsRoundCleared(2)=true`, 체력바 숨김, 탈출문 활성 확인.
- 로비 목적지 enum과 활성 Build Settings 인덱스 1 일치, `SceneLoader.CanEnter(Lobby)=true` 확인. 실제 씬 전환은 실행하지 않았다.
- Play 종료, timeScale=1 / runInBackground=false 복원, 씬 저장 완료·dirty=false 확인.
- Git diff 검사: Unity 직렬화가 추가하는 빈 필드 뒤 공백을 제외한 검사는 통과했다. 스크립트·프리팹 원본 변경 없음.

## 현재 상태 및 이슈
- 씬 연결은 완료했다. 실제 걷기·사격·발판 간 점프로 처음부터 끝까지 통과하는 플레이는 **미확인**이다. 패널을 직접 조준해 `[E]` 문구를 확인하는 것도 미확인이다(콜라이더 잠금/복구는 확인).
- 보스 단계는 실제 보스 객체의 HP 변경과 타이머 진행으로 검사했다. 실제 공격·보스 패턴·장시간 전투와의 조합은 미확인이다.
- 로비 로딩·도착까지는 **미확인**이다. 다른 씬을 열지 않는 요청 범위를 유지했다.
- Play 시작 시 기존 ObjectPoolManager의 PrefabPool.Prewarm → CreateObject 경로에서 NavMesh 밖 생성 경고 10건이 발생했다. 기존 풀은 Instantiate 후 SetActive(false) 순서로 생성한다. 실제 CombatZone 생성·특수몹 이동에서는 추가 NavMesh 경고가 없었다. 경고를 없애기 위한 코드 변경은 하지 않았다.
- 기존 보스 프리팹에서 AttributeClamper / AttributeRegenerator 및 BossA의 MoveSpeedReducer 미지정 경고 5건이 발생했다. 개인 Test 폴더의 TPS Setup 편집기 스크립트 경고 1건도 있었다. 컴파일·런타임 오류는 0건이었다.
- 위 수동 검증과 기존 경고가 남아 intent-057은 open 상태를 유지한다.
- 시작 전부터 변경돼 있던 EditorBuildSettings.asset / ShaderGraphSettings.asset 및 미추적 요청서 문서는 보존했다. 이번 에디터 실행 중 생긴 Pretendard-Bold SDF의 동적 글꼴 캐시 변경은 원복했다.

## 변경된 파일
- `Assets/_Project/Scenes/Map_4_Security.unity`
- `Assets/_Project/Scenes/Map_4_Security/NavMesh-04_Platforms.asset` 및 `.meta`
- 이 작업 기록, Codex/HANDOFF_POINTER.md
- `docs/intent/intent-057-security-app-rounds.md`, `docs/intent/INTENT_POINTER.md`

## 다음 할 일
- 실제 입력으로 시작점부터 탈출까지 플레이하고 발판 간 점프, 적 수·위치, 출구문 위치를 확인한다.
- 다른 씬을 여는 검증이 허용되면 탈출 포탈의 로비 도착을 확인한다.
- 풀 사전 생성·보스 선택 컴포넌트 경고를 없애려면 별도 코드/프리팹 작업으로 검토한다.
- 최종 플레이 검증 후 intent-057 해결 여부를 판단한다. 커밋·push는 요청자가 별도 지시할 때 수행한다.
