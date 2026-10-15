# 28. 총알 오브젝트 풀 연동

- **우선순위**: 미정
- **상태**: 대기
- **관련 작업**: [29. 총알 메시 + FX](29-bullet-mesh-fx.md)

## 목표
플레이어 총알 생성·소멸을 `ObjectPoolManager` 풀로 바꾼다.

## 현재 상황
- `BulletFactory.Create` 가 `Instantiate`, `BulletController` 가 수명 종료·피격 시 `Destroy(gameObject)` 한다.
- 풀은 이미 있다: `Core/ObjectPool/ObjectPoolManager` (씬 단위 `Instance`, `Get(키|프리팹, 위치, 회전)`, `Release(gameObject)`). 카테고리 `_playerBullets`, `_playerChargedBullets` 가 준비돼 있다. 사용 예: `Core/MonsterSpawner.cs`.
- 매니저 규칙: 재사용 시 초기화는 프리팹 컴포넌트가 `OnEnable` 에서 한다. 현재 `BulletController` 는 `_elapsedTime` 을 초기화하지 않는다.
- `Bullet.prefab` 은 어느 씬의 풀에도 등록돼 있지 않다 (씬 파일에서 GUID 미검색).
- 적 총알(`EnemyRangedAttack`)도 `Instantiate` 를 쓴다. `_enemyBullets` 카테고리가 있다.

## 작업 범위 (예상)
- `BulletFactory`: `ObjectPoolManager.Get(프리팹)` 사용. `BulletController`: `Release` 사용, `OnEnable` 초기화.
- 사용하는 씬의 `ObjectPoolManager` 에 `Bullet`·`ChargedBullet` 등록.

## 열린 질문
- [ ] 씬에 매니저가 없을 때: `Instantiate`/`Destroy` 로 대체하는가, 에러로 처리하는가
- [ ] 적 총알(`EnemyRangedAttack`)도 함께 바꾸는가 (AI 파트 담당 범위)
- [ ] 등록할 씬 목록과 Prewarm Count
- [ ] 씬 수정 담당 (다른 팀원 씬 충돌 가능성)
