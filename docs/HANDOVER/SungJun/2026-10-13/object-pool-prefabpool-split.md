# 오브젝트 풀: Manager → PrefabPool 2단 분리 + IPoolable 삭제 (2026-10-13)

## 작업 요약 및 방법
- `ObjectPoolManager` 한 곳이 인스펙터 목록·키별 풀 생성·인스턴스 생성/반납/파괴·Get 라우팅을 모두 맡고 있어서, 처음 설계처럼 카테고리 단위 `PrefabPool`로 나눴다.

| 파일 | 역할 |
|---|---|
| `ObjectPoolManager.cs` | 씬 배치. `PrefabPool` 3칸(Enemies, PlayerBullets, EnemyBullets). Awake에서 각 풀을 초기화하고 키 → 카테고리 표(`_poolByKey`)를 만듦(카테고리 간 중복 키는 에러, 먼저 등록한 쪽 우선). `Get` / `Get<T>`는 표에서 찾아 넘김 |
| `PrefabPool.cs` (신규, `[Serializable]`) | 카테고리 하나. `Entries`(키-프리팹), `Capacity`, `MaxSize`. 키별 `ObjectPool<GameObject>` 생성, Instantiate + PooledObject 부착·Bind, 반납(끄고 매니저 아래로)·파괴 콜백 |
| `PooledObject.cs` | 돌아갈 곳을 매니저 대신 `PrefabPool + 키`로 기억. 매니저가 파괴됐는지는 `PrefabPool.IsAlive`(루트 Transform 생존)로 판단 |
| `IPoolable.cs` | **삭제** |

- 재사용 초기화는 풀이 하지 않는다. 프리팹 컴포넌트가 `OnEnable`(리셋)과 `OnDisable`(정리)에서 직접 한다.
- Get 순서: 위치 설정 → `PrepareForGet`(사용 중 표시) → `SetActive(true)`. 반납 순서: `PrepareForRelease`(반납 표시) → `SetActive(false)` → 매니저 아래로 이동.
  그래서 OnEnable에서는 `IsReleased == false`, 반납으로 인한 OnDisable에서는 `IsReleased == true`로 구분할 수 있다.
- `Assets/Test`의 `PoolTestMonster`를 IPoolable에서 OnEnable/OnDisable 방식으로 바꿨다.
- 담당별 연결 가이드의 등록 위치, BulletController 예시, 호출 순서를 OnEnable 방식으로 고치고 intent-012에 반영했다.

## 결정 사항
- PrefabPool은 MonoBehaviour가 아니라 `[Serializable]` 일반 클래스다. 설정이 매니저 인스펙터 한 곳에 모인다.
- Capacity·MaxSize는 카테고리마다 따로 둔다.
- IPoolable은 쓰지 않는다. Awake는 생성할 때 한 번뿐이므로 재사용 초기화는 반드시 OnEnable에 둔다.

## 현재 상태 및 이슈
- Unity 재컴파일 에러 0.
- Play 모드 eval 확인:
  - Get 10 → 서로 다른 10개, 모두 활성, GetCount 1, IsReleased false
  - 전체 Release + 이중 Release → ReleaseCount 1(이중 반납 무시), 풀 `CountInactive` 5 / `CountAll` 5(MaxSize 5 초과분 파괴)
  - 다시 Get 3 → 3개 모두 재사용, GetCount 2, 새 위치 반영
  - 미등록 키 → 에러 로그 + null
  - 같은 키를 Enemies·EnemyBullets에 등록 → 에러 로그, Enemies가 우선
- 직렬화 경로가 바뀌어(`_enemies` → `_enemies._entries`) 기존 씬 등록값이 끊겼다. TempScene의 매니저 2개(`/ObjectPoolManager`, `/PoolTest/ObjectPoolManager`)는 eval로 이전 값을 다시 넣었다. **TempScene은 이 작업 전부터 저장 안 된 변경이 있던 상태라 저장하지 않았다.** 에디터에서 확인 후 직접 저장해야 한다.
- 게임 루프가 진행되지 않아 지연 Destroy 반영과 J/K/L 키 입력은 눈으로 확인하지 못했다.

## 다음 할 일 (Next Steps)
- TempScene을 확인하고 저장한 뒤 Play에서 J/K/L로 재사용·파괴를 눈으로 확인한다.
- Player·Enemy 파트에 "재사용 초기화는 OnEnable" 규칙을 공유한다(담당별 연결 가이드 5-1).

## 추가 변경: internal 제거 + 미리 만들어 두기(Prewarm)
- `PrefabPool`·`PooledObject`의 `internal`을 모두 `public`으로 바꿨다. asmdef가 없어 모든 코드가 `Assembly-CSharp` 하나라서 원래 public과 동작 차이가 없었고, 사용자 요청으로 뺐다. (Internal Methods region은 Public Methods로 합침)
- `PrefabPool`에 `Prewarm Count`(키 하나당, 기본 10)를 추가했다. `Initialize`에서 키마다 풀을 만든 직후 유니티 풀에서 그 수만큼 꺼냈다가 바로 반납해 꺼진 상태로 보관한다. Max Size보다 크면 Max Size까지만 만든다. 다 쓰면 그 뒤는 Get할 때 추가로 만든다.
  - 매니저 Get을 거치지 않아 오브젝트를 켜지 않으므로 OnEnable이 추가로 불리지 않고, `CountAll`/`CountInactive` 집계도 정상이다.
  - Capacity는 내부 목록 칸 수라 그대로 두고, 미리 만들 개수는 별도 칸으로 뒀다(사용자 선택).
- Play eval 확인: `/ObjectPoolManager`의 Melee·Ranged·Bullet·ChargedBullet·EnemyBullet 각 10개, `/PoolTest`의 TestMonster는 Max Size 5로 5개 미리 생성. 이어서 Get 7 → 5개 재사용 + 2개 신규, GetCount 1(미리 만들기는 사용 횟수로 안 셈), 반납 후 5개 보관.
- 주의: TempScene에는 NavMesh가 없어 Melee/Ranged를 미리 만들 때 `Failed to create agent because there is no valid NavMesh` 경고가 개체마다 뜬다. 실제 스테이지에서는 NavMesh가 있으면 사라지지만, 켜진 상태로 저장된 적 프리팹은 씬 로드 때 Awake/OnEnable이 매니저 위치에서 한 번 돈다.
