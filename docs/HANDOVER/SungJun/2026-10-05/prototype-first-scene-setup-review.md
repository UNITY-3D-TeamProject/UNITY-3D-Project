# 프로토타입 첫 씬 배치 검토

## 작업 요약 및 방법
- GameManager, PlayerSpawner, MonsterSpawner, UIManager/Canvas 코드와 로컬 프리팹, 씬 YAML, 빌드 씬 목록을 정적으로 대조했다.
- Unity 공식 문서에서 프리팹 인스턴스 오버라이드와 비활성 프리팹 복제 동작을 확인했다.

## 변경된 파일
- 당시에는 이 문서와 인수인계 목차만 변경했다. 현재 두 문서는 `docs/HANDOVER/SungJun/`에 정리되어 있다. 게임 코드와 에셋은 변경하지 않았다.

## 결정 사항
- 공통 프리팹 및 SO 참조는 프리팹 원본에 설정하고, 씬별 위치와 로비 복귀 Transform은 씬 인스턴스에서 설정한다.
- 현재 PlayerSpawner는 비활성 플레이어를 다시 활성화하지 않으므로 플레이어 프리팹 루트를 활성 상태로 둔다.
- GameManager는 첫 씬에서 하나를 활성 배치해 씬 이동 간 유지한다. PlayerSpawner와 Canvas는 필요한 각 씬에 배치한다. MonsterSpawner는 적이 필요한 위치에만 배치한다.

## 현재 상태 및 이슈
- 현재 Build Settings 첫 씬은 `Assets/_Project/Scenes/Test/MapTest_0_Tutorial.unity`이며, 저장된 파일에는 네 구성 요소가 없다. 테스트 씬에는 직접 배치한 플레이어가 있다.
- `Assets/Test/UnityAssets/Prefabs`에 GameManager, PlayerSpawner, MonsterSpawner, Canvas가 있으나 `Assets/Test`는 Git 공유 대상이 아니다.
- PlayerSpawner 프리팹의 Player Effects는 HP 50, JumpPower 5, Battery 80, MoveSpeed 5의 네 개다. KSJ_Lobby의 스포너는 세 개만 설정됐고 Is Lobby Spawner는 꺼져 있다.
- Canvas 프리팹은 UIManager의 Presenter 참조와 HUD Slider 참조가 연결돼 있다. SettingsPresenter 자식의 View와 SettingsView의 Root도 연결돼 있지만 Close Button은 비어 있고 SettingsPanel에 자식이 없다. Canvas 루트와 SettingsPanel에도 미연결 Presenter/View 컴포넌트가 중복돼 있다.
- MonsterSpawner 프리팹은 Melee와 HP 100, MoveSpeed 100 Effect를 연결한다. 속도 값은 의도 확인이 필요하다.
- 저장된 씬/코드의 정적 확인만 했으며 Unity Play 모드 검증은 하지 않았다.

## 다음 할 일
- 실제 통합 첫 씬을 정한 뒤 Build Settings 첫 항목을 맞추고, 프리팹과 필요한 의존 에셋을 `Assets/_Project`로 옮겨 공유한다.
- 각 씬에 GameManager/Spawner/Canvas/EventSystem을 목적에 맞게 배치하고, Canvas 설정창 버튼 및 씬별 참조를 연결한 뒤 Play 모드에서 스폰·HUD·설정창·씬 전환을 확인한다.
