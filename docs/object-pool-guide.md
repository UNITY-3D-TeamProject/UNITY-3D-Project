# 오브젝트 풀 적용 가이드

`Instantiate`/`Destroy`로 만들고 지우던 오브젝트(총알·몬스터)를 `ObjectPoolManager`의 `Get`/`Release`로 바꾸는 방법을 정리한다.
스크립트는 `Assets/_Project/Scripts/Core/ObjectPool/`(`Core.ObjectPool`), 매니저 프리팹은 `Assets/_Project/Prefabs/System/Manager/ObjectPoolManager.prefab`에 있다.

| 파트 | 고칠 곳 | 할 일 |
| --- | --- | --- |
| 플레이어 총알 | `BulletFactory`, `BulletController` | `Instantiate` → `Get`, `Destroy` → `Release`, `OnEnable` 초기화 |
| 몬스터 | `EnemyRangedAttack`, 사망 처리(`CharacterMediator`), `AttributeSet` 등 | 적 총알 `Get`, 사망 시 `Release`, 재사용 초기화 |
| 몬스터 스포너 | `MonsterSpawner` | `Instantiate` → `Get` |
| NPC(Crowd) | 없음 | `CrowdLane`이 자체 풀을 쓰므로 적용하지 않는다 |

> **몬스터와 몬스터 스포너는 반드시 같이 적용한다.** 스포너만 `Get`으로 바꾸고 몬스터 쪽 초기화가 없으면, 죽었던 몬스터가 체력 0 상태로 다시 나온다.

---

## 0. 등록된 키

키는 매니저 프리팹 인스펙터에 등록돼 있다. 꺼낼 때는 카테고리를 몰라도 키만 넘기면 된다.

| 카테고리 | 키 | 쓰는 곳 |
| --- | --- | --- |
| Enemies | `Melee`, `Ranged` | `MonsterSpawner` |
| Player Bullets | `Bullet` | `BulletFactory` (비차지) |
| Player Charged Bullets | `ChargedBullet` | `BulletFactory` (차지) |
| Enemy Bullets | `EnemyBullet` | `EnemyRangedAttack` |
| Crowd | `Car`, `Pedestrian` | 등록만 돼 있음 (`CrowdLane`은 자체 풀 사용) |

카테고리마다 인스펙터 값 3개가 **그 카테고리의 키 하나하나에 각각** 적용된다.

| 항목 | 기본값 | 설명 |
| --- | --- | --- |
| Default Capacity | 10 | 내부 목록 자리만 미리 잡는다. 오브젝트를 만들지는 **않는다**. 거의 신경 쓸 필요 없음 |
| Prewarm Count | 10 | 씬 시작 시 꺼진 상태로 **미리 만들어 둘 개수**. 다 쓰면 그 뒤로는 `Get`할 때 하나씩 새로 만든다 |
| Max Size | 50 | 반납된 오브젝트를 **보관할 최대 개수**. 생성 상한이 아니다. 넘치는 반납분은 `Destroy`된다 |

예: Player Bullets의 Prewarm Count가 10이면 씬 시작 시 `Bullet` 10개가 미리 만들어진다. 동시에 15개가 필요하면 나머지 5개는 그 순간 새로 만들고, 반납되면 15개를 보관해 다음부터 재사용한다.
Prewarm Count는 **한 화면에 동시에 나올 최대 개수** 정도로 잡는다. 미리 만든 개수를 넘는 순간에는 `Instantiate` 끊김이 생길 수 있다.

---

## 1. Get은 언제 하나

**기존에 `Instantiate`하던 자리에서 바로 `Get`한다.**

```csharp
using Core.ObjectPool;

// GameObject로 받기
GameObject monster = ObjectPoolManager.Instance.Get("Melee", position, rotation);

// 컴포넌트로 바로 받기 (Get + GetComponent)
BulletController bullet = ObjectPoolManager.Instance.Get<BulletController>("Bullet", position, rotation);
bullet.Initialize(direction, cursor);

// 프리팹 칸으로 꺼내기 (키 문자열 없이)
GameObject monster = ObjectPoolManager.Instance.Get(_monsterPrefab, position, rotation);
```

프리팹 칸을 가진 쪽(스포너 등)은 `Get(프리팹)`을 쓴다. 매니저에 **같은 프리팹이 등록돼 있어야** 하며, 이름이 아니라 참조로 찾으므로 프리팹 이름을 바꿔도 동작한다. 같은 프리팹을 여러 키로 등록했다면 먼저 등록된 키를 쓴다.

규칙

1. **`Start` 이후에 호출한다.** 매니저는 `Awake`에서 준비되므로, 다른 컴포넌트의 `Awake`에서 `Get`하면 실패한다(에러 로그 + null).
2. **꺼낸 직후 `Initialize`로 값을 넘긴다.** 실행 순서는 `Get` → (위치 설정) → `OnEnable` → `Initialize`다. 그래서 `OnEnable`에서는 `Initialize`로 받는 값(방향, 효과 등)을 쓸 수 없다.
3. **위치·회전은 `Get`에 넘긴다.** `OnEnable` 전에 적용되므로 `OnEnable`에서 새 위치를 볼 수 있다.
4. 키를 잘못 쓰면(오타·미등록) 에러 로그가 나오고 null이 돌아온다.
5. 꺼낸 오브젝트의 부모는 매니저다. 다른 부모가 필요하면 꺼낸 뒤 직접 `SetParent`한다(반납 시 매니저 아래로 돌아온다).

---

## 2. Release는 언제 하나

**기존에 `Destroy(gameObject)`하던 자리에서 `Release`한다.** "이 오브젝트를 다 썼다"는 시점이다.

```csharp
ObjectPoolManager.Instance.Release(gameObject);
```

| 대상 | Release 시점 |
| --- | --- |
| 총알 | 수명이 끝났을 때, 무언가에 맞았을 때 |
| 몬스터 | 사망 애니메이션이 끝났을 때 (`OnDeathEnded` 시점) |

규칙

1. **반납할 오브젝트의 루트(`Get`으로 받은 오브젝트)를 넘긴다.** 자식을 넘기면 풀 출신이 아니라고 보고 파괴한다.
2. **어느 풀로 돌아갈지 몰라도 된다.** 풀이 만들 때 자동으로 붙인 `PooledObject`가 기억한다.
3. **두 번 반납해도 괜찮다.** 이미 반납된 오브젝트면 무시한다. 예를 들어 같은 프레임에 수명 종료와 충돌이 겹쳐도 문제없다.
4. **반납한 뒤에는 그 오브젝트를 건드리지 않는다.** 이미 꺼져 있고 곧 다른 곳에서 재사용된다.
5. **풀에서 나온 게 아닌 오브젝트를 넘기면 그냥 `Destroy`한다.** 그래서 씬에 직접 배치하거나 `Instantiate`한 총알이 섞여 있어도 안전하다.
6. **`Destroy(gameObject, 시간)` 같은 지연 반납은 없다.** 시간이 필요하면 타이머(`Update` 누적이나 코루틴)로 잰 뒤 `Release`한다.

### 매니저가 없는 씬

매니저가 없는 씬에서는 `ObjectPoolManager.Instance`가 null이다. 여러 씬에서 쓰이는 코드(예: `BulletController`는 맵 기믹 `SentryTurret`, `ProjectileLauncher`에서도 `Instantiate`로 쓴다)는 아래처럼 반납한다.

```csharp
if (ObjectPoolManager.Instance != null)
    ObjectPoolManager.Instance.Release(gameObject);
else
    Destroy(gameObject);
```

---

## 3. 재사용 초기화 (가장 중요)

풀에서 나온 오브젝트는 **새로 만든 게 아니라 쓰던 것**이다. `Awake`는 처음 만들 때 한 번만 실행되므로, 재사용할 때 초기화가 필요한 값은 **`OnEnable`에서 다시 설정**한다.

| 값 | 초기화하지 않으면 |
| --- | --- |
| 경과 시간·타이머 | 나오자마자 수명이 끝나 바로 반납된다 |
| 체력 등 Attribute | 죽을 때의 체력(0)으로 다시 나온다 |
| 스폰 시 건 효과 | 재사용할 때마다 효과가 계속 겹친다 |
| 사망 때 끈 컴포넌트 | 이동·스킬·회전이 꺼진 채로 나온다 |
| AI 상태·애니메이션 | 사망 상태나 이전 행동으로 시작한다 |
| 이벤트 구독 | 재사용할 때마다 구독이 겹쳐 콜백이 여러 번 불린다 → 반납(`OnDisable`) 때 해제 |
| `Initialize`로 덮어쓴 값 | 다음 사용 때 이전 값이 남는다 (아래 예시) |

`Initialize`로 덮어쓴 값 예시: `BulletController.Initialize(direction, hitEffect, cursor)`는 `_hitEffect`를 덮어쓰지만 `Initialize(direction, cursor)`는 덮어쓰지 않는다. 한 키에서 두 오버로드를 섞어 쓰면 이전 효과가 남는다.

`OnEnable`에서 `PooledObject.IsReleased`는 false, `OnDisable`(반납 중)에서는 true다. 반납 때만 할 처리를 구분할 때 쓴다.

---

## 4. 파트별 적용 방법

### 4-1. 플레이어 총알

`BulletFactory.Create` (`Projectile/BulletFactory.cs`)

```csharp
string key = isCharged ? "ChargedBullet" : "Bullet";
BulletController bullet = ObjectPoolManager.Instance.Get<BulletController>(key, position, rotation);
bullet.Initialize(direction, cursor);
```

`BulletController` (`Projectile/BulletController.cs`, 적 총알과 공용이므로 먼저 하는 사람이 한 번만 고친다)

- `OnEnable`에서 `_elapsedTime = 0`으로 초기화한다.
- `Update`의 수명 종료, `OnTriggerEnter`의 충돌에서 쓰는 `Destroy(gameObject)` 두 곳을 2장의 "매니저가 없는 씬" 형태로 반납한다.

### 4-2. 몬스터

- **적 총알** (`Skill/Skills/EnemyRangedAttack.cs`): `Instantiate(_bulletPrefab, ...)`를 `Get<BulletController>("EnemyBullet", ...)`로 바꾼다. `_bulletPrefab` 필드는 키를 쓰면 필요 없어진다.
- **사망 반납**: 지금 `CharacterMediator.EndDeath`의 `Destroy`가 주석 처리돼 있어 적이 죽어도 사라지지 않는다. 사망 애니메이션이 끝나는 시점(`OnDeathEnded`)에 `Release`한다. `CharacterMediator`는 플레이어도 쓰므로 **몬스터일 때만** 반납해야 한다.
- **재사용 초기화**: 3장 표를 모두 확인한다. 특히 `AttributeSet`은 `Awake`에서만 기본 SO 값을 채우므로, 기본값으로 되돌리는 방법이 필요하다(Attribute 담당자와 협의).

### 4-3. 몬스터 스포너

- **적용 완료.** `MonsterSpawner`는 프리팹 칸을 그대로 두고 `Get(_monsterPrefab, ...)`으로 꺼낸다. `SpawnerBase.TrySpawnWithAttributes`는 `PlayerSpawner`도 쓰므로 고치지 않았다.
- 스포너를 놓는 씬에는 `ObjectPoolManager`가 있어야 하고, 스포너의 몬스터 프리팹이 매니저에 등록돼 있어야 한다. 없으면 에러 로그를 남기고 몬스터를 만들지 않는다.
- `ApplySpawnEffects`는 꺼낼 때마다 다시 적용되므로, 몬스터의 Attribute가 먼저 기본값으로 초기화돼 있어야 효과가 겹치지 않는다(4-2와 같이 적용).

---

## 5. 자주 하는 실수

| 증상 | 원인 |
| --- | --- |
| `Get`이 null, "초기화 전에 Get" 에러 | 다른 컴포넌트의 `Awake`에서 호출함 → `Start` 이후로 옮긴다 |
| "등록되지 않은 키" 에러 | 키 오타 또는 매니저 프리팹에 미등록 |
| 총알이 나오자마자 사라짐 | `OnEnable`에서 타이머를 초기화하지 않음 |
| 몬스터가 바로 죽거나 점점 강해짐 | Attribute 초기화 없이 재사용 → 체력 0 유지, 스폰 효과 누적 |
| 콜백이 여러 번 불림 | 이벤트 구독을 반납 때 해제하지 않음 |
| `NullReferenceException` (Instance) | 매니저가 없는 씬에서 `Instance.Release` 호출 → 2장 형태로 반납 |
| 씬 시작 시 꺼진 적이 많이 생김 | 그 씬에서 쓰지 않는 키도 Prewarm됨 → 씬에서 프리팹 오버라이드로 키를 빼거나 Prewarm을 줄인다 |
