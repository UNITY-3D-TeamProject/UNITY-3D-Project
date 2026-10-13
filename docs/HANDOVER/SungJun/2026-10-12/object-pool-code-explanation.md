# 오브젝트 풀 코드 구조 및 사용 흐름 확인

## 작업 요약 및 방법
- 현재 작업 트리의 `Core/ObjectPool` 4개 스크립트와 `docs/HANDOVER/SungJun/2026-10-12/object-pool-guide.md`를 읽고 구조와 사용 흐름을 정리했다.
- `Assets/_Project/Scripts`의 호출부 및 씬·프리팹의 매니저 GUID 참조를 검색해 연결 상태를 확인했다.

## 변경된 파일
- 런타임 코드 변경 없음. 이 작업 기록과 `HANDOFF_POINTER.md`만 추가·갱신했다.

## 결정 사항
- 구조 변경이나 새 규칙은 정하지 않았다. 현재 구현을 설명하는 읽기 전용 검토였다.

## 현재 상태 및 이슈
- 현재 구조는 씬별 `ObjectPoolManager` → 카테고리별 `PrefabPool` → 인스턴스의 `PooledObject`이며, `IPoolable`은 선택적 상태 초기화 인터페이스다.
- `Assets/_Project`의 씬·프리팹에는 `ObjectPoolManager` 스크립트 GUID 참조가 없고, 검색한 스포너·투사체·근접 공격 사용처도 아직 `Instantiate`/`Destroy` 경로다.
- `Get<T>`에서 T가 없으면 null을 반환하지만 이미 꺼낸 오브젝트를 반납하지 않는다. 연결 시 프리팹 컴포넌트를 확인해야 한다.

## 다음 할 일
- 실제 도입 시 씬에 매니저를 배치하고 키·프리팹을 등록한 뒤, 각 생성·반납 코드를 연결하고 Play 모드에서 재사용·상태 초기화를 검증한다.
