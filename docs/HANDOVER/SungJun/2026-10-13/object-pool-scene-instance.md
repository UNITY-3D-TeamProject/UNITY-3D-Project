# 오브젝트 풀: 씬 한정 `ObjectPoolManager.Instance` 추가 (2026-10-13)

## 작업 요약 및 방법
- 매니저 참조를 인스펙터로 연결하거나 Spawner가 Player·Enemy에 전달(`SetObjectPoolManager`)하던 방식을 없애려고, `ObjectPoolManager`에 static `Instance`를 추가했다.
- 구조 자체는 그대로다. 풀이 필요한 씬에만 매니저를 배치하고, 씬이 내려가면 매니저·풀·풀에서 나온 오브젝트가 함께 정리된다.

| 위치 | 변경 |
|---|---|
| `ObjectPoolManager.Instance` | `public static ... { get; private set; }`. Awake에서 `Instance = this`, OnDestroy에서 `Instance == this`일 때만 null |
| `ObjectPoolManager.Awake` | 이미 `Instance`가 있고 **같은 씬**이면 LogError(배치 실수). 이전 씬 매니저가 아직 남아 있는 경우는 정상으로 본다 |
| 상단 주석 | 사용 흐름을 `ObjectPoolManager.Instance.Get(...)` 기준으로 갱신 |

- 문서: intent-012 기대 결과, 10-12 사용 가이드, 10-13 담당별 연결 가이드를 `Instance` 사용으로 고쳤다. 담당별 가이드에서 매니저 필드·`SetObjectPoolManager`·PlayerSpawner 전달 절차를 삭제했다.

## 결정 사항
- 공용 `Core.Singleton<T>`는 상속하지 않는다. 이 클래스는 Awake에서 무조건 DontDestroyOnLoad를 걸고 GameManager와 공유한다. 사용자가 원한 것은 "싱글톤식 접근 + 씬과 함께 소멸"이다.
- 풀이 필요 없는 씬(로비·피드 등)에는 매니저를 두지 않는다. 그 씬에서는 `Instance == null`이다.
- `Instance`는 Awake에서 채워지므로 Start 이후에 사용한다. 기존 Get 제약과 같다.

## 현재 상태 및 이슈
- Unity 재컴파일 에러 0.
- Play 모드 eval 확인 (TempScene):
  - `Instance`가 null이 아니고, Get → Release → Get에서 같은 인스턴스를 재사용하며, 반납 후 비활성 상태다.
  - TempScene을 다시 로드하면 `Instance`가 새 씬의 매니저를 가리킨다(파괴된 참조 아님).
  - Map_1_Lobby를 로드하면(매니저 없음) `Instance`가 C# null이다. OnDestroy에서 비워진 것이 확인됐다.
  - 같은 씬 중복 LogError가 정상적으로 출력된다.
- **TempScene에는 매니저가 2개 있다**(`/ObjectPoolManager`, `/PoolTest/ObjectPoolManager`). 그래서 Play할 때마다 중복 에러가 1회 나고, 나중에 Awake된 쪽이 Instance가 된다. 시험 구역(`PoolTestSpawner`)은 인스펙터 참조를 쓰므로 동작에는 지장이 없다.
- TempScene은 이전 작업부터 저장 안 된 변경이 있는 상태이며, 이번에도 저장하지 않았다. Play 종료 후 편집 상태(등록값)가 유지되는 것은 확인했다.
- 아직 실제 프로젝트 씬(Map_*)에는 ObjectPoolManager가 배치되어 있지 않다.

## 다음 할 일 (Next Steps)
- TempScene의 매니저를 하나로 합칠지 결정한다(중복 에러 제거).
- Spawner·Player·Enemy 파트에 변경된 담당별 연결 가이드(Instance 사용)를 다시 공유한다.
- 로딩씬(Additive) 작업은 별도로 진행한다. 로비를 언로드한 뒤 스테이지 매니저가 Awake되므로 Instance는 자연스럽게 교체된다.
