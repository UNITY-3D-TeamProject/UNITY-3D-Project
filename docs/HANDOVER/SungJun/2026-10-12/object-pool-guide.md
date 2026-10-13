# 오브젝트 풀 사용 가이드 (Player / AI 파트용)

> 코드 위치: `Assets/_Project/Scripts/Core/ObjectPool/` (네임스페이스 `Core.ObjectPool`)
> 관련 intent: `docs/HANDOVER/SungJun/intent/intent-012-object-pool-system.md`
> 2026-10-13 변경: 매니저 참조를 전달하는 방식 대신 `ObjectPoolManager.Instance`로 접근한다. 반납도 `ObjectPoolManager.Instance.Release(gameObject)`로 바뀌었다(`PooledObject.ReleaseOrDestroy` 삭제). 최신 담당별 연결 방법은 [담당별 연결 가이드](../2026-10-13/object-pool-team-integration-guide.md)를 따른다.

## 한눈에 보기

| 질문 | 답 |
|---|---|
| 풀은 어디에 있나? | 풀이 **필요한 씬에만** `ObjectPoolManager` 하나. `DontDestroyOnLoad` 아님 (씬과 함께 사라짐) |
| 꺼내려면 무엇을 알아야 하나? | **키 문자열**만. 프리팹은 몰라도 된다 |
| 꺼내는 방법은? | `ObjectPoolManager.Instance.Get("Monster1", position, rotation)` |
| 매니저 참조는 어떻게 얻나? | `ObjectPoolManager.Instance`. 매니저가 없는 씬이거나 매니저 Awake 전이면 null |
| 반납은? | `ObjectPoolManager.Instance.Release(gameObject)` 한 줄. 키는 넘기지 않는다 |
| 씬이 바뀌면? | 매니저·풀·풀에서 나온 오브젝트가 씬과 함께 정리된다 |

카테고리: `Monster`, `Npc`, `Effect`, `PlayerBullet`, `EnemyBullet`, `BossSkill`
카테고리는 **인스펙터 정리와 개수 설정용**이다. 꺼낼 때는 키만 넘기면 매니저가 알아서 해당 카테고리에서 꺼낸다.
(플레이어 본체는 씬에 1개뿐이고 재스폰도 드물어 풀 대상이 아니다.)

## 구성 요소

| 파일 | 역할 |
|---|---|
| `ObjectPoolManager` | 씬에 배치. 카테고리별 `PrefabPool` 6개를 들고, `Get(키)` / `Get<T>(키)`로 꺼내 준다 |
| `PrefabPool` | 카테고리 하나. 인스펙터에 **키-프리팹** 목록, 기본 용량, 최대 개수를 설정 |
| `PooledObject` | (풀 내부용) 풀이 만든 인스턴스에 **자동으로 붙어** 돌아갈 풀을 기억한다. 사용하는 쪽은 몰라도 된다 |
| `IPoolable` | (선택) 재사용 시 상태 초기화/정리용. `OnGet()`, `OnRelease()` |

## 1. 씬 배치 (씬 담당)

1. 씬에 빈 GameObject `ObjectPoolManager`를 만들고 컴포넌트를 붙인다.
2. 카테고리의 `Entries`에 **키와 프리팹**을 등록한다. 예: Monster 카테고리에 `Monster1` → `Monster1.prefab`
3. 키는 씬 안의 모든 카테고리에서 겹치면 안 된다. 겹치면 Awake에서 에러 로그가 뜨고 뒤쪽 항목은 무시된다.
4. 같은 키를 쓰는 오브젝트가 여러 씬에서 나온다면 **씬마다 같은 키로 등록**해야 한다.
5. `Max Size`: 프리팹 하나당 풀에 보관할 최대 개수. 넘치는 반납분은 Destroy된다.

## 2. 매니저 참조 얻기

인스펙터 연결이나 전달 없이 `ObjectPoolManager.Instance`를 쓴다. 씬 배치 오브젝트와 런타임 생성 오브젝트 모두 같다.

- `Instance`는 매니저 Awake에서 채워지고, 씬이 내려가 매니저가 파괴되면 null이 된다.
- 매니저를 배치하지 않은 씬(로비·피드 등)에서는 null이다.

## 3. 꺼내기

키를 넘기면 오브젝트를 돌려준다. 컴포넌트가 필요하면 `Get<T>`를 쓴다.

```csharp
// MonsterSpawner
[Header("Pool")]
[SerializeField] private string _monsterKey = "Monster1";

GameObject monster = ObjectPoolManager.Instance.Get(_monsterKey, transform.position, transform.rotation);
```

```csharp
// BulletFactory
[SerializeField] private string _bulletKey        = "PlayerBullet";
[SerializeField] private string _chargedBulletKey = "PlayerChargedBullet";

string key = isCharged ? _chargedBulletKey : _bulletKey;
BulletController bullet = ObjectPoolManager.Instance.Get<BulletController>(key, position, rotation);
if (bullet == null) return;

bullet.Initialize(direction, cursor);
```

- 키를 코드에 직접 쓰기보다 위처럼 `[SerializeField] string`으로 두면 인스펙터에서 바꿀 수 있다.
- 등록되지 않은 키면 에러 로그를 남기고 null을 돌려준다. **오타는 실행해야 드러나므로** 처음 연결할 때 콘솔을 확인한다.
- `Get`은 **위치/회전을 먼저 설정한 뒤 활성화**한다. 그래서 `OnEnable`·`IPoolable.OnGet`에서 새 위치를 볼 수 있다.
- `Initialize(...)` 같은 데이터 설정은 `Get` 직후 호출하면 된다(기존 Instantiate 직후 호출과 동일).
- 매니저 Awake 이전(다른 컴포넌트의 Awake)에는 `Instance`가 null이다. Start 이후에 사용한다.

## 4. 반납 — 풀에서 나온 오브젝트 쪽 (BulletController, MeleeHitController 등)

`Destroy(gameObject)`를 아래로 바꾼다. 풀 출신이면 자기 풀로 돌아가고, 아니면(씬 배치·Instantiate 생성) Destroy된다.

```csharp
// 변경 전
Destroy(gameObject);

// 변경 후
ObjectPoolManager.Instance.Release(gameObject);
```

- 이미 반납된 오브젝트에 다시 호출해도 무시된다(이중 반납 방지).
- `Destroy(gameObject, 시간)`처럼 **지연 Destroy는 쓰지 않는다.** 코루틴이나 Update 타이머로 시간이 지나면 `Release`를 호출한다.
- 매니저가 없는 씬에서도 실행될 수 있는 코드(Player와 공용인 사망 처리 등)는 `Instance`가 null인지 먼저 확인하고, null이면 `Destroy(gameObject)`한다.

## 5. 재사용 초기화 — IPoolable (필요한 경우만)

풀 오브젝트는 Awake가 **처음 생성 때 한 번만** 실행된다. 매번 초기화해야 하는 값은 `IPoolable.OnGet`에서 리셋한다.

```csharp
public class BulletController : MonoBehaviour, IPoolable
{
    public void OnGet()
    {
        _elapsedTime = 0.0f;   // 수명 타이머 리셋
    }

    public void OnRelease()
    {
        _cursor = null;        // 이전 발사자 참조 정리
    }
}
```

- 오브젝트 자신과 자식에 있는 모든 `IPoolable` 구현이 호출된다.
- 호출 시점: `OnGet` = 위치 설정·활성화 이후, `OnRelease` = 비활성화 직전.

현재 코드 기준 재사용 시 주의할 곳:

| 파일 | 주의점 |
|---|---|
| `Projectile/BulletController.cs` | `_elapsedTime`이 리셋되지 않음 → OnGet에서 0으로. `Destroy(gameObject)` 2곳 → `Instance.Release` |
| `Hit/MeleeHitController.cs` | `Destroy(gameObject, _lifeSpan)` 지연 Destroy → 타이머 후 `Instance.Release` |
| `Skill/Skills/EnemyRangedAttack.cs`, `EnemyMeleeAttack.cs` | 프리팹 `Instantiate` → 키로 `Get` |

## 6. 이펙트 (ParticleSystem) 자동 반납

이펙트 프리팹 설정:
1. **루트** GameObject의 ParticleSystem → `Stop Action` = **Callback**
2. `Play On Awake` 켜기 (활성화될 때마다 처음부터 재생)

재생이 끝나면 풀 내부 컴포넌트(`PooledObject`)가 `OnParticleSystemStopped`를 받아 자동 반납한다. 중간에 끊고 싶으면 `ObjectPoolManager.Instance.Release(이펙트)`를 직접 호출하면 된다.

```csharp
ObjectPoolManager.Instance.Get("HitSpark", hitPoint, Quaternion.identity);
```

## 담당별 할 일 요약

| 파트 | 할 일 |
|---|---|
| 씬 담당 | 풀이 필요한 씬에만 `ObjectPoolManager` 배치, 카테고리별 키-프리팹 등록 |
| Core (Spawner) | `MonsterSpawner`: `ObjectPoolManager.Instance`로 키로 Get |
| Player | `BulletFactory`: `ObjectPoolManager.Instance`로 키로 Get. `BulletController` 반납·`IPoolable` 초기화 |
| AI | `EnemyRangedAttack`/`EnemyMeleeAttack`: `ObjectPoolManager.Instance`로 키로 Get. `MeleeHitController` 지연 Destroy 교체 |
