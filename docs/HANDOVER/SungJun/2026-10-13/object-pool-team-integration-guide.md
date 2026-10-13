# 오브젝트 풀 연결 가이드 — Spawner / Player / Enemy 담당별

작성일: 2026-10-13

**Spawner가 씬의 풀 매니저를 생성된 Player·Enemy의 발사 컴포넌트에 전달한다. 발사 컴포넌트는 그 매니저로 탄환을 꺼내고, 탄환은 자기 PooledObject를 통해 반납한다.**

이 문서는 현재 코드와 대화 내용을 정리한 구현 가이드다. 아래의 추가 함수와 연결 예시는 아직 런타임 코드에 반영하지 않았다. 코드 블록은 기존 클래스에 적용할 부분 예시이며, 클래스 전체를 교체하는 코드가 아니다.

- 사용자가 말한 EnemySpawner의 현재 클래스 이름은 `MonsterSpawner`다.
- Player의 발사 담당은 `Projectile.BulletFactory`, Enemy의 원거리 발사 담당은 `Skill.Skills.EnemyRangedAttack`이다.
- 두 탄환 모두 `Projectile.BulletController`를 사용한다.
- 현재 풀은 `ObjectPoolManager`가 키마다 별도 풀을 관리한다. 별도 `PrefabPool` 컴포넌트를 추가할 필요는 없다.

## 1. 담당별 작업 범위

| 담당 | 준비할 것 | 구현할 것 |
|---|---|---|
| Spawner 담당(사용자) | 씬의 ObjectPoolManager, 키·프리팹 등록, 씬 스포너의 매니저 참조 | Player 생성 후 매니저 전달, Enemy Get 후 매니저 전달 및 스폰 초기화 연결 |
| Player 담당 | Player 내부 BulletFactory, 일반·차지 탄환 키 | 매니저를 받는 함수, 탄환 Instantiate를 Get으로 교체, 탄환 Initialize 호출 |
| Enemy 담당 | Enemy 내부 EnemyRangedAttack, 적 탄환 키, Enemy 재사용 초기화 진입점 | 매니저를 받는 함수, 탄환 Get·Initialize, Enemy 상태 복원 및 사망 시 반납 |
| 공통 탄환 담당 | BulletController가 붙은 탄환 프리팹 | 재사용 초기화, 수명 종료·충돌 시 반납, 중복 피해 방지 |

`BulletController`는 Player와 Enemy가 공유하므로 수정 담당 한 명을 정해 공통으로 반영한다. Player 본체는 이번 풀 전환 대상이 아니며 기존 생성 흐름을 유지한다.

```text
씬의 ObjectPoolManager
  ├─ PlayerSpawner가 참조
  │    → Player 생성
  │    → Player의 BulletFactory에 매니저 전달
  │         → 발사 시 Get("PlayerBullet")
  │         → 탄환.Initialize(방향, 발사자)
  │
  └─ MonsterSpawner가 참조
       → Get("RangedEnemy")
       → Enemy의 EnemyRangedAttack에 매니저 전달
       → Enemy 스폰 초기화 완료
            → 발사 시 Get("EnemyBullet")
            → 탄환.Initialize(방향, Effect, 발사자)

탄환 사용 종료 → 탄환의 PooledObject.Release() → 원래 풀로 반납
```

**탄환에 ObjectPoolManager를 다시 전달하지 않는다.** 풀 매니저가 생성 시 루트에 `PooledObject`를 자동으로 붙이고 소속 매니저와 키를 기록한다.

## 2. Spawner 담당이 할 일

### 2-1. 씬과 Inspector 설정

1. 사용할 씬마다 `ObjectPoolManager`를 하나 배치한다. 현재 매니저는 싱글톤이나 DontDestroyOnLoad 객체가 아니다.
2. 매니저의 카테고리(`Enemies`, `Player Bullets`, `Enemy Bullets`)마다 `Entries`에 키와 프리팹을 등록한다. `Capacity`·`Max Size`·`Prewarm Count`(키 하나당 씬 시작 시 미리 만들 개수, 기본 10, Max Size까지만)도 카테고리마다 따로 설정한다.
3. 두 스포너에 아래 매니저 필드를 추가한다.
4. Hierarchy에서 **씬에 배치된 PlayerSpawner·MonsterSpawner 인스턴스**를 각각 선택한다.
5. Inspector의 매니저 필드에 같은 씬의 ObjectPoolManager를 드래그해 연결한다.

```csharp
// using Core.ObjectPool; 필요
[SerializeField] private ObjectPoolManager _objectPoolManager;
```

등록 예시:

| 매니저 목록 | 키 예시 | 프리팹 |
|---|---|---|
| Enemies | `MeleeEnemy` | 근접 적 프리팹 |
| Enemies | `RangedEnemy` | 원거리 적 프리팹 |
| Enemies | `SpecialEnemy` | 특수 적 프리팹 |
| Player Bullets | `PlayerBullet` | Bullet.prefab |
| Player Bullets | `PlayerChargedBullet` | ChargedBullet.prefab |
| Enemy Bullets | `EnemyBullet` | EnemyBullet.prefab |

키는 팀에서 맞춰 사용하는 예시 이름이다. 모든 카테고리에서 중복되면 안 되며, 호출하는 키와 정확하게 같아야 한다. 사용하는 씬마다 등록한다.

**Project 창의 프리팹 에셋에는 씬 매니저를 연결하지 않는다.** 스포너가 프리팹이어도 씬에 배치된 인스턴스에는 연결할 수 있다. 이 참조는 씬 인스턴스의 설정으로 둔다. 스포너도 런타임에 생성한다면, 그 스포너를 생성하는 씬 쪽 코드에서 매니저를 전달한다.

### 2-2. PlayerSpawner에서 전달

기존 `TrySpawnWithAttributes(...)`로 Player를 생성한 뒤, `CompletePlayerSpawn(...)`으로 스폰 완료를 알리기 전에 매니저를 전달한다. 최초 스폰과 새 Player를 만드는 재스폰 모두 이 경로를 거친다.

```csharp
// using Projectile; 필요
// player는 방금 생성한 Player GameObject다.
BulletFactory bulletFactory = player.GetComponentInChildren<BulletFactory>(true);

if (bulletFactory == null)
{
    Debug.LogError("Player에 BulletFactory가 없습니다.", player);
    Destroy(player); // Player 본체는 기존 일반 생성 객체다.
    return;
}

bulletFactory.SetObjectPoolManager(_objectPoolManager);
```

`SetObjectPoolManager(...)`는 **Player 담당이 추가할 함수**다. 매니저가 연결되지 않았다면 스폰 시작 전에 오류를 알리고 중단한다. 현재 예시는 BulletFactory가 하나인 구성이다.

### 2-3. MonsterSpawner에서 Get 및 전달

Enemy 생성은 기존 Instantiate 경로 대신 풀의 Get을 사용한다. `SpawnerBase.TrySpawnWithAttributes()`는 Player도 사용하므로 공용 함수 전체를 무조건 풀 방식으로 바꾸지 않는다.

```csharp
// using Skill.Skills; 필요
GameObject enemy = _objectPoolManager.Get(
    "RangedEnemy",
    transform.position,
    transform.rotation);

if (enemy == null) return;

EnemyRangedAttack rangedAttack =
    enemy.GetComponentInChildren<EnemyRangedAttack>(true);

if (rangedAttack == null)
{
    Debug.LogError("원거리 적에 EnemyRangedAttack이 없습니다.", enemy);
    PooledObject.ReleaseOrDestroy(enemy);
    return;
}

rangedAttack.SetObjectPoolManager(_objectPoolManager);
// 이어서 아래의 Enemy 스폰 초기화 절차를 실행한다.
```

이 예시는 원거리 적용이다. 근접 적에는 EnemyRangedAttack을 요구하지 않는다. 적 안에 탄환 생성 컴포넌트가 여러 개라면 각 컴포넌트에 전달해야 한다.

Enemy 초기화 책임은 다음처럼 나눈다.

| 순서 | 담당과 처리 |
|---|---|
| 1 | Spawner: Enemy를 Get하고 필요한 매니저 참조 전달 |
| 2 | Enemy: 재사용 초기화 진입점에서 기본 능력치와 AI·이동·공격 상태 복원 |
| 3 | Spawner: 이번 스폰의 SOAttributeEffect 적용 |
| 4 | Enemy: 최종 능력치를 전투·이동 컴포넌트에 동기화하고 행동 시작 |

Enemy 담당은 Spawner가 호출할 재사용 초기화 함수를 제공해야 한다. **현재 코드에는 위 절차를 한 번에 수행하는 함수가 없으므로, 함수 이름과 인자를 맞춘 뒤 연결한다.** 내부 필드를 Spawner가 일일이 수정하지 않는다.

SO 에셋 자체를 수정하지 않고 인스턴스의 런타임 능력치를 설정한다. 기본값을 복원하지 않은 채 Add/Multiply Effect만 재적용하면 값이 누적될 수 있다. 초기화 중 임시 체력 0이 사망 이벤트로 이어지지 않도록 Enemy 쪽에서 초기화 상태를 처리해야 한다.

## 3. Player 담당에게 전달할 작업

### 3-1. BulletFactory에 매니저를 받는 함수 추가

```csharp
// using Core.ObjectPool; 필요
private ObjectPoolManager _objectPoolManager;

public void SetObjectPoolManager(ObjectPoolManager objectPoolManager)
{
    _objectPoolManager = objectPoolManager;
}
```

Player 프리팹 안의 BulletFactory에는 씬 매니저를 Inspector로 연결하지 않는다. 위 함수를 PlayerSpawner가 생성 직후 호출한다.

### 3-2. 발사 시 Get 후 Initialize

일반·차지 발사 흐름은 유지하고, `BulletFactory.Create()`의 Instantiate 부분을 다음 방식으로 교체한다. 키를 Inspector에서 설정하려면 SerializeField 문자열 필드로 만들어도 된다.

```csharp
if (_objectPoolManager == null)
{
    Debug.LogError("BulletFactory에 풀 매니저가 전달되지 않았습니다.", this);
    return;
}

string key = isCharged ? "PlayerChargedBullet" : "PlayerBullet";
GameObject bulletObject = _objectPoolManager.Get(key, position, rotation);
if (bulletObject == null) return;

if (!bulletObject.TryGetComponent(out BulletController bullet))
{
    Debug.LogError("탄환 루트에 BulletController가 없습니다.", bulletObject);
    PooledObject.ReleaseOrDestroy(bulletObject);
    return;
}

bullet.Initialize(direction, cursor);
```

`position`, `direction`, `cursor`는 기존 Create의 입력값이고, `rotation`은 기존 코드처럼 방향으로 계산한다. 프리팹 선택을 키 선택으로 바꾸면 이전 프리팹 필드에 대한 null 검사도 함께 정리한다.

발사 담당이 하는 일은 **매니저로 탄환을 꺼내고 이번 발사 정보를 넣는 것**이다. 탄환에 매니저를 넘기거나 반납까지 직접 관리하지 않는다.

## 4. Enemy 담당에게 전달할 작업

### 4-1. EnemyRangedAttack에 매니저를 받는 함수 추가

3-1과 동일한 필드와 `SetObjectPoolManager(...)`를 EnemyRangedAttack에도 추가한다. MonsterSpawner가 Enemy를 꺼낸 뒤 호출한다.

매니저가 전달되기 전에는 발사하지 않는다. 이번 초기화가 끝났다는 상태는 매니저가 null인지와 별도로 관리한다. 재사용 객체에는 이전 매니저 참조가 남아 있을 수 있기 때문이다.

### 4-2. 원거리 발사 코드 교체

`EnemyRangedAttack.Execute()` 안의 Instantiate 부분을 다음 방식으로 바꾼다.

```csharp
if (_objectPoolManager == null || _firePoint == null)
{
    Debug.LogError("적 발사에 필요한 참조가 없습니다.", this);
    return;
}

GameObject bulletObject = _objectPoolManager.Get(
    "EnemyBullet",
    _firePoint.position,
    _firePoint.rotation);

if (bulletObject == null) return;

if (!bulletObject.TryGetComponent(out BulletController bullet))
{
    Debug.LogError("탄환 루트에 BulletController가 없습니다.", bulletObject);
    PooledObject.ReleaseOrDestroy(bulletObject);
    return;
}

bullet.Initialize(_firePoint.forward, _hitEffect, _cursor);
```

`base.Execute()` 등 기존 스킬 실행 마무리는 정상 경로에서 유지한다. `_bulletPrefab` 검사와 Awake의 프리팹 Assert는 키 기반 생성에 맞게 바꾼다. 매니저는 Awake 이후 전달될 수 있으므로 Awake에서 매니저 연결 완료를 요구하지 않는다.

### 4-3. Enemy 재사용 초기화 및 반납

Enemy는 풀에서 매번 새로 생성되는 것이 아니다. 다음 상태를 다음 스폰에 맞게 복원하는 진입점을 제공한다.

- 체력과 기본 능력치, 스폰별 Effect를 적용하기 전 기준값.
- AI 순찰 시작점, 이전 이동 목적지·경로.
- Sensor의 이전 타깃, 마지막 위치 기억, 탐지 진행도·타이머.
- 공격 쿨타임, 실행 중 스킬, 이동·낙하 상태 및 변경된 자식 활성 상태.
- 초기화가 끝나기 전 행동·피격 처리를 막는 상태와 완료 시 해제 처리.

현재 AttributeSet의 기본 SO 적용은 Awake에 있고, AIController의 순찰 시작점 설정은 Start에 있다. 이것만으로 재사용 초기화가 되지 않는다. `SkillBase.ResetState()`도 현재 비어 있어 호출만으로 쿨타임이 초기화된다고 가정하면 안 된다.

사망 또는 사망 연출 완료 시점에는 **풀에서 받은 Enemy 루트**를 반납한다.

```csharp
// 이 gameObject가 풀에서 꺼낸 Enemy 루트일 때
GetComponent<PooledObject>().Release();
```

현재 사망 제거 코드는 `CharacterMediator.OnDeathCallback()`의 Destroy다. 현재 Melee/Ranged 프리팹에서는 이 컴포넌트가 루트에 있다. 다만 Player와 공용인 클래스이므로 일반 생성 Player의 사망 처리까지 풀 전용 Release로 바꾸면 안 된다. 공용 제거 지점을 유지하려면 `PooledObject.ReleaseOrDestroy(gameObject)`로 출처에 맞게 처리할 수 있다.

기존 CharacterMediator.OnDisable의 구독 해제는 활용하고 중복 작성하지 않는다. 진행 중인 외부 작업·예약 콜백이 있다면 반납 시 종료한다.

## 5. 공통 BulletController 초기화와 반납

### 5-1. 이 가이드의 권장 분담

**기본값 리셋은 OnEnable, 이번 발사 상태는 Initialize, 반납 정리는 OnDisable에 둔다.** 풀은 IPoolable 같은 콜백을 부르지 않고 `SetActive(true/false)`만 한다(IPoolable은 2026-10-13에 삭제됨).

| 함수 | 호출하는 쪽 | 역할 |
|---|---|---|
| `Initialize(...)` | BulletFactory 또는 EnemyRangedAttack | 이번 발사 상태 설정 |
| `OnEnable()` | 유니티 (풀이 켤 때) | 재사용 기본값 리셋. 몇 번 불려도 안전한 리셋만 |
| `PooledObject.Release()` | 사용이 끝난 객체 | 실제 풀 반납 요청 |
| `OnDisable()` | 유니티 (풀이 끌 때) | 반납 과정에서 필요한 정리 |

Initialize에서는 매번 다음을 확정한다.

- `_elapsedTime = 0.0f`.
- 이번 방향과 발사자. null을 전달한 경우에도 이전 발사자가 남지 않도록 덮어쓴다.
- 이번 Effect. 기본 효과용 오버로드는 프리팹 기본 Effect를 복원한다.
- 중복 피격 방지 상태를 새 발사 상태로 복원한다.

현재 두 Initialize 오버로드에는 타이머 초기화가 없다. 기본 효과용 오버로드는 `_hitEffect`도 복원하지 않으므로 보완해야 한다. 프리팹 기본 Effect는 최초 Awake에서 별도 필드에 보관하고, 명시적으로 Effect를 받은 발사와 구분한다.

부분 예시:

```csharp
// 재사용마다 기본값으로 되돌린다. Initialize보다 먼저 불린다.
private void OnEnable()
{
    _elapsedTime = 0.0f;
}

// 반납·씬 종료·게임 로직이 끌 때 모두 불린다. 여러 번 불려도 안전하게 둔다.
private void OnDisable()
{
    _cursor = null;
}
```

OnEnable 초기화 규칙:

- **Awake는 Instantiate할 때 한 번만 불린다.** 재사용마다 초기화할 값은 반드시 OnEnable에 둔다. Awake에는 GetComponent 캐싱 같은 1회성 작업만 둔다.
- 풀은 위치를 먼저 잡고 켜므로 OnEnable에서 새 위치를 읽을 수 있다. 발사 방향·발사자처럼 호출자가 넘기는 값은 OnEnable 다음의 Initialize에서 설정한다.
- 켜진 상태로 저장된 프리팹은 처음 생성할 때 Awake → OnEnable → OnDisable이 한 번 먼저 돌고, 첫 Get에서 OnEnable이 다시 불린다. OnEnable에는 몇 번 불려도 문제없는 리셋만 둔다(이벤트 등록·사운드 재생 같은 부수효과 금지).
- 사용 중 꺼 둔 자식은 부모를 켜도 꺼진 채로 남는다. 루트 OnEnable에서 자식 활성 상태도 되돌린다.
- 실제 꺼냄/반납인지 구분해야 하면 `PooledObject.IsReleased`를 본다. OnEnable에서는 false, 반납으로 인한 OnDisable에서는 true다. (최초 생성 시에는 PooledObject가 아직 없다.)

### 5-2. Destroy 두 곳을 반납으로 교체

현재 BulletController의 수명 종료와 충돌 처리에 있는 Destroy를 모두 교체한다.

```csharp
// 반드시 풀에서 가져온 탄환이고, BulletController가 루트에 있을 때
GetComponent<PooledObject>().Release();
```

일반 Instantiate 탄환도 지원해야 하는 동안에는 다음 함수를 사용한다.

```csharp
PooledObject.ReleaseOrDestroy(gameObject);
```

- `Release()`는 풀 출신 객체의 반납 요청이다. 컴포넌트를 수동으로 붙이기만 한 객체는 풀에 등록된 것이 아니다.
- `ReleaseOrDestroy()`는 풀 출신이면 반납하고, 일반 생성 객체이면 Destroy하는 편의 함수다.
- `SetActive(false)`만 호출해도 풀에 반납되는 것은 아니다.
- 대상은 Get으로 받은 프리팹 루트다. 자식 컴포넌트에서 처리한다면 해당 루트의 PooledObject를 참조한다. `transform.root`는 풀 매니저일 수 있으므로 무조건 사용하지 않는다.
- PooledObject는 Instantiate 이후 자동 추가될 수 있으므로, 탄환 Awake에서 반드시 존재한다고 가정하지 않는다. 캐싱한다면 Get 이후 Initialize에서 확인할 수 있다.
- 수명 종료 후에는 return하여 이동 등 나머지 처리를 중단한다. 충돌에서는 Effect 적용 전에 처리 완료 여부를 검사·표시해 중복 피해를 막는다.
- 지연 Destroy를 예약하지 않는다. 타이머로 수명이 끝나면 반납하며, 이전 사용의 예약 작업이 재사용 객체를 건드리지 않게 종료한다.

## 6. 현재 풀의 호출 순서와 연결 시 주의점

```text
Get 요청
→ 풀에서 GameObject 획득
→ 위치·회전 설정
→ PooledObject가 사용 중으로 표시
→ 활성화 / OnEnable
→ 호출자에게 반환
→ 호출자가 Initialize 실행

Release 요청
→ 반납 상태로 표시
→ 비활성화 / OnDisable
→ 매니저 아래로 이동
→ 풀에 보관 (보관 한도 초과 시 파괴)
```

1. **현재 Get은 초기화 전용 비활성 객체를 반환하는 함수가 아니다.** OnEnable에서는 호출자가 이번에 넘길 발사 정보나 스폰 데이터가 아직 없다. 초기화 완료 전 이동·공격·피격이 실행되지 않게 구성한다.
2. 최초 생성도 주의한다. PrefabPool은 씬 시작(매니저 Awake) 때 Prewarm Count만큼 미리 Instantiate하고 바로 비활성화한다. 그래서 활성 프리팹의 Awake/OnEnable은 씬 로드 시점에, 풀의 소속 기록 전에 한 번 실행된다. NavMeshAgent가 있는 적은 이때 매니저 위치에서 에이전트를 만들려고 한다. OnEnable에서 바로 발사하거나 반납하는 방식은 피한다.
3. OnEnable부터 완성된 데이터가 꼭 필요하다면 Core 담당이 비활성 획득·초기화·활성화 경로를 추가해야 한다. 현재는 없는 기능이다. 비활성 중 초기화한다면 능력치 변경 이벤트를 못 받은 컴포넌트의 최종값 동기화도 필요하다. 현재 MediatorBase의 InitValue 호출은 주석 상태다.
4. Get은 매니저 Awake 초기화 완료 후 호출한다. 현재 스포너의 Start 경로를 활용한다.
5. `Get<BulletController>(...)`는 **GameObject를 꺼낸 뒤 루트의 BulletController를 반환하는 편의 함수**다. 탄환을 Initialize해서 반환하는 의미가 아니다. 현재는 해당 컴포넌트를 못 찾으면 이미 꺼낸 객체가 남으므로, 이 문서 예시는 Get과 컴포넌트 확인을 나누고 실패 시 반납한다.
6. 현재 EnemyBullet의 Effect는 맞는 순간 발사자 참조에서 AttackDamage를 읽는다. Enemy가 먼저 반납·재사용되면 새 사용 회차의 값을 읽을 수 있다. 발사 당시 피해량을 유지하려면 탄환에 피해값을 저장하거나 발사자 사용 회차를 검증하는 처리가 필요하다. 공유 SO의 값을 바꾸는 방식으로 해결하지 않는다.

## 7. 각 파트 완료 확인

- [ ] Spawner: 씬 매니저와 모든 키·프리팹 등록을 완료했다.
- [ ] Spawner: 생성된 Player의 BulletFactory와 획득한 Enemy의 발사 컴포넌트에 매니저를 전달한다.
- [ ] Player: 일반·차지 발사 모두 Get 후 Initialize를 호출한다.
- [ ] Enemy: 재사용 상태 복원 → 스폰 Effect 적용 → 최종값 동기화 → 행동 시작 순서를 연결했다.
- [ ] 공통 탄환: 수명·충돌 두 종료 경로에서 반납하고, 한 탄환이 피해를 중복 적용하지 않는다.
- [ ] 재사용: 같은 인스턴스를 다시 꺼냈을 때 수명·방향·Effect·체력·AI 상태가 정상이다.
- [ ] 순서: 최초 생성과 재사용 모두 초기화 완료 전에 발사·피해 처리가 일어나지 않는다.
- [ ] 발사자 수명: Enemy가 먼저 반납된 뒤 남은 탄환이 맞는 경우도 확인했다.
- [ ] 오류: 매니저·키·컴포넌트 누락을 확인하고, 획득 후 초기화에 실패한 객체가 활성 상태로 남지 않는다.

## 8. 현재 코드 위치

| 영역 | 파일 |
|---|---|
| 풀 공통 | `Assets/_Project/Scripts/Core/ObjectPool/ObjectPoolManager.cs`, `PrefabPool.cs`, `PooledObject.cs` |
| 스포너 | `Assets/_Project/Scripts/Core/PlayerSpawner.cs`, `MonsterSpawner.cs`, `SpawnerBase.cs` |
| Player 발사 | `Assets/_Project/Scripts/Projectile/BulletFactory.cs` |
| Enemy 발사 | `Assets/_Project/Scripts/Skill/Skills/EnemyRangedAttack.cs` |
| 공통 탄환 | `Assets/_Project/Scripts/Projectile/BulletController.cs` |
| 캐릭터 사망 | `Assets/_Project/Scripts/Mediator/CharacterMediator.cs` |

관련 정적 검토: [Get·Release 수명주기 검토](./object-pool-get-release-lifecycle.md).
