# TempScene 오브젝트 풀 시험 구성 (2026-10-13)

## 작업 요약 및 방법
- `ObjectPoolManager` 동작을 실제 몬스터 없이 검증하기 위해 TempScene에 시험 구역을 만들었다.
- 시험용 코드·프리팹은 모두 `Assets/Test/`에 두었고 `_Project` 코드는 수정하지 않았다. (Git에 올라가지 않음)

| 파일 | 역할 |
|---|---|
| `Assets/Test/UnityAssets/Script/ObjectPool/PoolTestMonster.cs` | `IPoolable` 구현 큐브. 꺼내진 뒤 `_lifeTime`(3초) 지나면 스스로 `Release`. 첫 사용 흰색, 재사용 시 초록색. OnGet/OnRelease 로그 |
| `Assets/Test/UnityAssets/Script/ObjectPool/PoolTestSpawner.cs` | J: 1개, K: 10개 꺼내기, L: 사용 중 전체 반납. 왼쪽 위 OnGUI에 생성·사용 중·보관 중·파괴 개수 표시 |
| `Assets/Test/UnityAssets/Prefabs/PoolTestMonster.prefab` | Cube + `PoolTestMonster` (PooledObject는 매니저가 자동 부착) |

## 씬 배치 (TempScene, 저장 완료)
```
PoolTest (47.25, 0, -5)          ← StartFloor 동쪽 끝(x=41.25)에 이어 붙임
 ├ PoolTestFloor                 크기 (12, 1, 12), StartFloor와 같은 머티리얼
 ├ ObjectPoolManager             Monsters: "TestMonster" → PoolTestMonster.prefab, Monsters Max Size = 5
 └ PoolTestSpawner               키 "TestMonster", SpawnPoint_0~2 (로컬 x -3/0/3, y 1), 흩뿌림 반경 1m
Signs/Sign_오브젝트 풀 (40, 0.5, -11), 회전 y=90   ← Sign_스캔 복제, 서쪽(시작 지점 쪽)을 향함
```
- 키 J/K/L은 InputActions·기존 시험 스크립트(R, T)와 겹치지 않는 키로 골랐다.
- maxSize를 5로 낮춰 K(10개) → L(전체 반납) 시 5개가 파괴되는 것을 볼 수 있게 했다.

## 결정 사항
- 실제 Melee/Ranged 프리팹은 이번 시험에서 제외했다. NavMeshAgent가 있어 NavMesh 굽기가 필요하고, 사망 시 `CharacterMediator.OnDeathCallback`이 `Destroy(gameObject)`를 호출해 풀로 돌아오지 않기 때문이다(Enemy 담당 코드).
- 실제 `MonsterSpawner`는 `AttributeSet`이 필수라 큐브로 시험할 수 없어 수정하지 않았다.

## 현재 상태 및 이슈
- 컴파일 오류 0, 시험 코드 경고 0.
- Play 모드에서 eval로 확인한 결과:
  - 10개 Get → 10개 생성·활성
  - 전체 Release → 풀 내부 `CountInactive` 5, 나머지 5개는 파괴 콜백으로 넘어감 (maxSize 정상)
  - 다시 3개 Get → 새로 만들지 않고 보관 중인 3개 재사용 (`GetCount` 2, 초록색), 위치 갱신, `IsReleased` 정상
- 에디터가 비활성이라 게임 루프가 프레임 2에서 멈춰 있었다. 그래서 **지연 Destroy 반영, 수명 3초 자동 반납(Update), J/K/L 키 입력, OnGUI 표시는 눈으로 확인하지 못했다.**

## 다음 할 일 (Next Steps)
- 에디터에서 Play → 동쪽(+x)으로 이동해 표지판 지나 시험 구역에서 J/K/L을 눌러 확인한다.
  - J 여러 번: 3초 뒤 사라지고, 다시 J 시 "생성된 개체" 수가 늘지 않고 초록 큐브가 나오면 재사용 성공
  - K → L: "파괴됨" 5, "보관 중" 5
- 확인 후 실제 몬스터 연결은 Enemy 담당의 사망 시 `PooledObject.ReleaseOrDestroy` 전환과 HP 초기화(IPoolable) 이후, `MonsterSpawner`를 풀 방식으로 바꿀 때 진행한다.
