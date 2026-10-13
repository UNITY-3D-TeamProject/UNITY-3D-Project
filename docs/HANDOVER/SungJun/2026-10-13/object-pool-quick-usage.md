# 오브젝트 풀 간단 사용법

> 코드: `Assets/_Project/Scripts/Core/ObjectPool/` (`using Core.ObjectPool;`)
> 자세한 담당별 연결: [담당별 연결 가이드](./object-pool-team-integration-guide.md)

**`ObjectPoolManager.Instance` 하나로 꺼내고(Get) 반납한다(Release).**

```csharp
GameObject obj = ObjectPoolManager.Instance.Get("키", 위치, 회전); // Instantiate 대신
ObjectPoolManager.Instance.Release(gameObject);                    // Destroy 대신
```

---

## 1. 씬 준비 (씬 담당)

1. 풀이 필요한 씬에 빈 GameObject를 만들고 `ObjectPoolManager`를 붙인다. **한 씬에 하나만** 둔다.
2. 카테고리(`Enemies`, `Player Bullets`, `Enemy Bullets`)의 `Entries`에 **키와 프리팹**을 등록한다.
3. 풀이 필요 없는 씬(로비·피드 등)에는 두지 않는다. 씬이 바뀌면 매니저와 풀도 함께 사라진다.

| 카테고리 | 키 예시 |
|---|---|
| Enemies | `MeleeEnemy`, `RangedEnemy`, `SpecialEnemy` |
| Player Bullets | `PlayerBullet`, `PlayerChargedBullet` |
| Enemy Bullets | `EnemyBullet` |

키는 모든 카테고리를 통틀어 겹치면 안 되고, 코드에서 부르는 문자열과 정확히 같아야 한다.

## 2. 꺼내기 — Get

```csharp
// Instantiate(prefab, position, rotation) 자리
GameObject enemy = ObjectPoolManager.Instance.Get("MeleeEnemy", position, rotation);
if (enemy == null) return; // 키 오타·미등록이면 콘솔에 에러 + null

// 컴포넌트가 바로 필요하면 Get<T>
BulletController bullet = ObjectPoolManager.Instance.Get<BulletController>("PlayerBullet", position, rotation);
if (bullet == null) return;
bullet.Initialize(direction, cursor); // 이번 발사 정보는 Get 직후에 넣는다
```

## 3. 반납 — Release

```csharp
// Destroy(gameObject) 자리 (총알 수명 종료·충돌, 적 사망 등)
ObjectPoolManager.Instance.Release(gameObject);
```

- 키는 넘기지 않는다. 오브젝트가 어느 풀에서 왔는지는 풀이 알아서 기억한다.
- 풀에서 나온 게 아니면(씬에 직접 배치, Instantiate, Player) Destroy된다.
- 같은 오브젝트를 두 번 Release해도 두 번째는 무시된다.

## 4. 꼭 지킬 것

| 규칙 | 이유 |
|---|---|
| `Instance`는 **Start 이후**에 사용한다 | 매니저 Awake에서 준비되므로 다른 컴포넌트의 Awake에서는 null일 수 있다 |
| 매니저가 없는 씬에서도 도는 코드는 **null 확인** | 예: Player와 공용인 사망 처리. 아래 예시 참고 |
| 재사용 초기화는 **OnEnable**, 정리는 **OnDisable** | Awake는 처음 생성될 때 한 번만 불린다 (체력·타이머 리셋은 OnEnable) |
| `Destroy(gameObject, 시간)` 금지 | 타이머가 끝나면 `Release`를 호출한다 |
| `SetActive(false)`로 반납하지 않는다 | 끄기만 하면 풀로 돌아가지 않는다. 반드시 `Release` |
| Release에는 **Get으로 받은 루트**를 넘긴다 | 자식 컴포넌트에서 호출한다면 루트 GameObject를 넘긴다 |

```csharp
// 매니저가 없는 씬에서도 실행될 수 있는 공용 코드
if (ObjectPoolManager.Instance != null)
    ObjectPoolManager.Instance.Release(gameObject);
else
    Destroy(gameObject);
```

## 5. 파트별 한 줄 요약

| 파트 | 꺼내기 | 반납 |
|---|---|---|
| Spawner (MonsterSpawner) | `Instance.Get("MeleeEnemy", ...)` | — |
| Player (BulletFactory) | `Instance.Get("PlayerBullet", ...)` → `Initialize` | — |
| Enemy (EnemyRangedAttack) | `Instance.Get("EnemyBullet", ...)` → `Initialize` | 사망 시 `Instance.Release(gameObject)` (공용 코드면 null 확인) |
| 탄환 (BulletController) | — | 수명 종료·충돌 시 `Instance.Release(gameObject)` |
