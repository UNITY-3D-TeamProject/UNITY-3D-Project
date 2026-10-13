# 오브젝트 풀: 반납 창구를 `ObjectPoolManager.Release`로 통일 (2026-10-13)

## 작업 요약 및 방법
- 사용하는 쪽이 꺼낼 때는 `ObjectPoolManager`, 반납할 때는 `PooledObject`를 알아야 하던 구조를 `ObjectPoolManager` 하나로 모았다.

```csharp
GameObject enemy = ObjectPoolManager.Instance.Get("MeleeEnemy", position, rotation);
ObjectPoolManager.Instance.Release(gameObject);
```

| 파일 | 변경 |
|---|---|
| `ObjectPoolManager.cs` | `Release(GameObject)` 추가: null이면 무시, PooledObject가 있고 풀이 만든 것(`IsBound`)이면 `pooled.Release()`, 아니면 Destroy. 사용 흐름 주석을 Get / Release 기준으로 갱신 |
| `PooledObject.cs` | static `ReleaseOrDestroy` 삭제(매니저로 이동). `IsBound`(소속 풀이 기록됐는지) 추가. `[AddComponentMenu("")]`로 Add Component 메뉴에서 숨김. 주석을 "풀 내부용, 사용하는 쪽은 몰라도 됨"으로 변경 |
| `PrefabPool.cs` | `ReleaseOrDestroy`를 가리키던 주석 1줄만 수정 |

- 문서: 담당별 연결 가이드, 10-12 사용 가이드, intent-012의 반납 예시를 `ObjectPoolManager.Instance.Release(gameObject)`로 바꿨다. 공용 사망 처리에는 null 확인 예시를 추가했다.

## 결정 사항
- 사용하는 쪽은 `ObjectPoolManager`만 안다. 어느 풀에서 왔는지는 풀이 생성할 때 자동으로 붙이는 `PooledObject`가 기억한다(`PrefabPool.CreateObject`의 AddComponent + Bind). 그래서 `Release`에는 키를 넘기지 않는다.
- 반납은 호출한 매니저가 아니라 오브젝트가 기억하는 PrefabPool로 간다.
- `Release`는 인스턴스 메서드다(사용자 선택: Get과 같은 모양). 매니저가 없는 씬에서도 실행될 수 있는 코드(Player와 공용인 `CharacterMediator` 사망 처리)는 호출하는 쪽에서 `Instance` null을 확인하고, null이면 Destroy한다.
- Get은 지금처럼 위치·회전을 필수로 받는다(사용자 선택).
- `PooledObject.Release()`·`IsReleased`·`Bind`·`PrepareFor*`는 public으로 유지한다. 매니저·PrefabPool·파티클 콜백, `Assets/Test` 시험 코드가 쓴다.

## 현재 상태 및 이슈
- Unity 재컴파일 에러 0.
- Play 모드 eval (TempScene, 키 Melee):
  - 처음 보관 10개 → Get 후 9개 → Release 후 10개 → 같은 오브젝트를 다시 Release해도 10개(이중 반납 무시). 반납 후 비활성 상태이고, 다시 Get하면 같은 인스턴스가 나온다.
  - 풀에서 나오지 않은 GameObject, PooledObject만 손으로 붙인 GameObject → Release하면 다음 프레임에 파괴된다.
  - `Release(null)` → 에러 없음.
- TempScene의 매니저 2개 중복 에러는 그대로다([Instance 작업 기록](./object-pool-scene-instance.md) 참고).
- 과거 작업 기록(10-12 refactoring, 10-13 lifecycle·team-guide·test-scene)에는 `ReleaseOrDestroy`가 남아 있다. 당시 기록이므로 수정하지 않았다.
- `CharacterMediator`(Core 밖)에는 아직 연결하지 않았다. Enemy 파트 작업 시 가이드 4-2의 null 확인 예시를 따른다.

## 다음 할 일 (Next Steps)
- Instance + Release 작업을 커밋한다(코드·문서).
- Player·Enemy 파트에 갱신된 담당별 연결 가이드를 공유한다.
