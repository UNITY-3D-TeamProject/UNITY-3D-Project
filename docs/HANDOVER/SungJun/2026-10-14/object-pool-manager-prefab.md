# ObjectPoolManager 기본 프리팹 (2026-10-14)

## 작업 요약 및 방법
- `ObjectPoolManager`에 `Crowd (차량·보행자)` 카테고리(`_crowd`) 추가, Awake 등록 순서 맨 뒤.
- 기본 프리팹 `Assets/_Project/Prefabs/System/Manager/ObjectPoolManager.prefab` 신규. 등록 키:
  - Enemies: `Melee`, `Ranged`
  - Player Bullets: `Bullet`
  - Player Charged Bullets: `ChargedBullet` (추가 분리, 아래 결정 사항 참고)
  - Enemy Bullets: `EnemyBullet`
  - Crowd: `Car`, `Pedestrian` (프리팹 파일명은 `Pedstrian`이지만 키는 올바른 철자)
- Prewarm·Max Size는 기본값(키당 10 / 50).

## 결정 사항
- 차량·보행자는 Enemies에 섞지 않고 별도 Crowd 칸.
- `ChargedBullet`은 `Player Charged Bullets` 카테고리(`_playerChargedBullets`)로 분리. 일반 총알만 Prewarm을 크게 올려야 할 때 차지 총알까지 같이 늘지 않게 하기 위함. 값은 분리 전과 같은 10 / 50 그대로이며, 테스트 후 조정한다.
- 스포너처럼 프리팹 칸을 가진 쪽은 키 문자열 대신 `ObjectPoolManager.Get(GameObject prefab, ...)`으로 꺼낸다. 이름이 아닌 참조로 찾는다(매니저가 Awake에서 "프리팹 → 키" 표를 만듦). 데이터 SO 분리는 보류(씬별 목록이 늘어나면 검토).
- `CrowdLane`은 자기 풀을 그대로 쓴다(사용자 지시: 신경 쓰지 않음). 그래서 지금 매니저의 Car·Pedestrian은 등록만 돼 있고 아무도 꺼내지 않는다.

## 검증
- 컴파일 에러 0.
- 개인 TestScene에 `MonsterSpawnerTest` 공간(바닥·팻말·매니저 프리팹·스포너 Melee/Ranged·NavMeshSurface) 구성. Play에서 Instance = 새 매니저, 두 몬스터가 스포너 위치에 풀에서 나와 NavMesh 위에 섬, 에러 0. 기존 테스트용 매니저 2개는 끄고 PoolTestSpawner는 새 매니저를 보게 함(TestMonster 키는 씬 오버라이드).
- Map_1_Lobby Play에서 프리팹 인스턴스화 → Instance 등록, 자식 70개(7키 × 10) 미리 생성, 7키 모두 Get(활성) → Release(비활성) 확인. 에러 0.
- 경고: LilRobot Legacy 애니메이션 경고(기존 이슈), 로비에 NavMesh가 없어 적 Prewarm 시 "no valid NavMesh" 경고 2건.

## 현재 상태 및 이슈
- 실제 생성 코드(스포너·BulletFactory·EnemyRangedAttack 등)는 아직 `Instantiate`/`Destroy` 그대로.
- `BulletController._elapsedTime`을 OnEnable에서 초기화하지 않아 재사용 시 바로 반납됨 — 총알 적용 시 같이 수정 필요.
- `MeleeHitController`의 `Destroy(gameObject, _lifeSpan)`은 Release 지연 기능이 없어 타이머 필요.
- 적 재사용 시 체력·BT·Sensor·약점 위치 초기화는 몬스터 파트 작업.
- `MonsterSpawner`는 `Get(프리팹)`으로 전환 완료. 단 Map_4_Security에는 매니저가 아직 없어 그 씬의 스포너는 몬스터를 만들지 않는다(에러 로그). 씬 담당자와 매니저 배치 협의 필요.
- 몬스터는 아직 Release하지 않으므로(사망 시 Destroy도 주석) 재사용이 일어나지 않는다. 스폰 효과 누적 문제는 몬스터 Release 적용 시점에 Attribute 초기화와 함께 해결해야 한다.
- 씬에 아직 배치하지 않음. 적이 없는 씬(로비)에 이 프리팹을 그대로 두면 적이 꺼진 채 20개 생성되므로, 씬별로 안 쓰는 키는 오버라이드로 빼거나 Prewarm을 줄인다.

## 다음 할 일 (Next Steps)
- 필요한 씬에 프리팹 배치.
- 총알부터 Get/Release 적용(BulletController 초기화 포함), 이후 적은 몬스터 파트와 협의.
- 적용 방법·Get/Release 시점·재사용 초기화 주의사항은 `docs/object-pool-guide.md`에 정리했다. 각 파트(플레이어 총알, 몬스터, 몬스터 스포너)에 전달한다.
