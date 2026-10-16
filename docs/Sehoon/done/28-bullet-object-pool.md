# 28. 총알 오브젝트 풀 연동

- **우선순위**: 미정
- **상태**: 완료
- **관련 작업**: [29. 총알 메시 + FX](29-bullet-mesh-fx.md)

## 목표
플레이어 총알 생성·소멸을 `ObjectPoolManager` 풀로 바꾼다.

## 현재 상황
- `BulletFactory.Create` 가 `Instantiate`, `BulletController` 가 수명 종료·피격 시 `Destroy(gameObject)` 한다.
- 풀은 이미 있다: `Core/ObjectPool/ObjectPoolManager` (씬 단위 `Instance`, `Get(키|프리팹, 위치, 회전)`, `Release(gameObject)`). 카테고리 `_playerBullets`, `_playerChargedBullets` 가 준비돼 있다. 사용 예: `Core/MonsterSpawner.cs`.
- 매니저 규칙: 재사용 시 초기화는 프리팹 컴포넌트가 `OnEnable` 에서 한다. 현재 `BulletController` 는 `_elapsedTime` 을 초기화하지 않는다.
- `ObjectPoolManager.prefab` 에 `Bullet`·`ChargedBullet`·`EnemyBullet` 키가 등록돼 있다 (각 Prewarm 10, Max 50). 매니저는 `Map_1_Lobby` 씬에만 배치돼 있다.
- 적 총알(`EnemyRangedAttack`)과 맵 기믹(`SentryTurret`, `ProjectileLauncher`)도 `BulletController` 를 `Instantiate` 로 쓴다. `_enemyBullets` 카테고리가 있다.
- 발사 경로: `Fire` → `SkillMediator.OnFireRequested` → `SpawnMediator.CommandSpawnBullet`(조준 방향·효과 주체 계산) → `BulletFactory.Create`.

## 확정 사항
- 구조는 유지한다. 풀 적용은 `BulletFactory` 내부의 생성 부분만 바꾼다 (Fire 에서 직접 생성하지 않는다. 방향·효과 주체가 다른 Mediator 에 있기 때문).

## 작업 범위 (예상)
- `BulletFactory`: `ObjectPoolManager.Get<BulletController>(키)` 사용. `BulletController`: `Release` 사용, `OnEnable` 초기화.

## 열린 질문
- [x] 씬에 매니저가 없을 때: `Instantiate`/`Destroy` 로 대체하는가, 에러로 처리하는가 → 에러로 처리한다 (`BulletFactory`)
- [x] 꺼내는 방식: `Get(프리팹)` / `Get(키)` → `Get(키)` (`Bullet`, `ChargedBullet`). 키는 인스펙터에 노출한다
- [x] 적 총알(`EnemyRangedAttack`)도 함께 바꾸는가 (AI 파트 담당 범위) → 제외한다
- [x] 등록할 씬 목록과 Prewarm Count → 이미 등록돼 있다. 이번 작업에서는 씬·매니저 프리팹을 수정하지 않는다
- [x] 씬 수정 담당 (다른 팀원 씬 충돌 가능성) → 씬을 수정하지 않으므로 해당 없음

## 결과 (2026-10-16)
- `BulletFactory.cs`: `Instantiate` 를 `ObjectPoolManager.Instance.Get<BulletController>(키)` 로 바꿨다. 키는 `[SerializeField] _bulletKey`("Bullet")·`_chargedBulletKey`("ChargedBullet") 로 인스펙터에 노출. 매니저가 없으면 에러 로그 후 생성하지 않는다. 쓰지 않게 된 `_bulletPrefab`·`_chargedBulletPrefab` 필드는 삭제 (`SpawnPrefab.prefab` 에 남은 옛 직렬화 값은 다음 저장 때 정리됨, 동작 영향 없음).
- `BulletController.cs`: `OnEnable` 에서 `_elapsedTime` 초기화. 수명 종료·피격의 `Destroy` 를 `Despawn()` 으로 바꿔 매니저가 있으면 `Release`, 없으면 `Destroy` 한다 (적·맵 기믹 총알은 매니저 없는 씬에서도 `Instantiate` 로 쓰므로).
- 반납 시 `SetActive(false)` 가 되므로, 같은 프레임에 여러 대상과 겹쳐도 효과가 한 번만 적용된다 (기존 `Destroy` 는 지연돼 중복 적용 가능).
- 매니저는 `Map_1_Lobby` 에만 있어 다른 맵에서는 플레이어 총알이 나가지 않는다 (Q1 결정). 다른 맵 배치는 맵 담당과 협의 필요.
- 검증: 컴파일·콘솔 에러 없음. `Map_1_Lobby` 플레이 모드에서 비차지·차지 풀 재사용(재사용 시 `_elapsedTime` 0), 수명 종료 후 반납, 피격 후 반납, 12발 동시 발사 시 2개 추가 생성 후 보관 확인. `Map_0_Tutorial` 에서 에러 로그 출력과 `Instantiate` 한 총알의 `Destroy` 확인. 키 인스펙터 노출 후에는 컴파일·기본값 확인까지.
