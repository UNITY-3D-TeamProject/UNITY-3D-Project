# 몬스터 스포너 기본 생성

## 작업 요약
- `MonsterSpawner`를 추가해 지정한 몬스터 프리팹을 스포너 위치에 한 번 생성하고 `SOAttributeEffect` 배열을 생성 인스턴스에 적용한다.
- `PlayerSpawner`와 겹치는 프리팹 생성, 자식 `AttributeSet` 조회, Effect 적용을 `SpawnerBase`로 추출했다.
- 두 스포너에 향후 비활성 생성과 재활성화 위치를 주석으로 표시했다.

## 방법/접근
- 사용자가 선택한 방식대로 프리팹의 `AttributeSet.Awake`가 기본 `SOAttributeData`를 읽고, 스포너가 개체별 Effect 값을 덮어쓴다.
- 기존 `PlayerSpawner`의 자식 `AttributeSet` 조회 변경과 플레이어 저장값 복원 분기는 유지했다.
- 활성 프리팹 생성 후 `SetActive(false)`는 최초 `Awake`/`OnEnable`을 늦출 수 없으므로 주석에 선행 조건을 적었다.

## 변경된 파일
- `Assets/_Project/Scripts/Core/SpawnerBase.cs` 및 `.meta`
- `Assets/_Project/Scripts/Core/MonsterSpawner.cs` 및 `.meta`
- `Assets/_Project/Scripts/Core/PlayerSpawner.cs`
- `docs/HANDOVER/SungJun/intent/intent-012-monster-spawner-foundation.md`, `docs/intent/INTENT_POINTER.md`

## 결정 사항
- 이번 단계는 단일 프리팹, 스포너 자신의 Transform 위치, 시작 시 한 번 생성으로 한정했다.
- 위치별 몬스터 대응과 랜덤 선택은 후속 단계로 둔다.

## 현재 상태 및 이슈
- 씬의 `MonsterSpawner` 컴포넌트에는 프리팹과 Effect 에셋을 인스펙터에서 연결해야 한다.
- 현재 적 기본 SO의 `CurrentHp`는 0이므로 시작 체력이 필요하면 해당 값을 설정하는 Effect를 연결해야 한다.
- Unity Play Mode는 실행하지 않았다.

## 다음 할 일
- 위치 Transform과 프리팹/선택 방식의 1:1 대응 구조를 설계하고 구현한다.
- 지연 활성화가 필요할 때 `AttributeSet`의 Awake 초기화를 명시적 초기화로 옮기고 활성화 순서를 검증한다.
