# UI 생성·소멸 흐름 재확인

## 작업 요약 및 방법
- UI 3종과 GameManager, PlayerSpawner, PlayerState, Singleton, AttributeSet을 재검토했다.
- 사용자가 Bind의 기존 구독 해제에서 isActiveAndEnabled 조건을 제거한 최신 변경을 확인했다. 앞서 지적한 UIManager/Presenter 비활성화 순서에 따른 해제 누락은 이 변경으로 해소된다.
- Unity 공식 생명주기 및 Instantiate 설명을 참고했다. 정적 검토이며 Play Mode 검증은 하지 않았다.

## 결정 사항
- 코드 수정 없이 현재 상태와 전제를 설명한다.
- 활성 GameManager가 씬에 미리 존재하고 필수 SO/UI 참조가 연결되며 활성 플레이어를 생성하는 흐름에서는 추가 실행 순서 지정이 필요하지 않다.
- UIManager가 GameManager.Awake보다 먼저 실행돼도 기존 인스턴스 검색과 CurrentPlayerState의 null 안전 조회로 대기 가능하다. 씬의 초기 Awake/OnEnable 이후 PlayerSpawner.Start에서 플레이어를 생성하고 등록한다.

## 현재 상태 및 이슈
- UIManager/Presenter의 통상적인 활성화, 비활성화, UI 전체 파괴 시 구독 정리는 최신 Bind 수정으로 순서 의존성이 제거됐다.
- GameManager 자체가 UI보다 늦게 생성되면 UIManager의 연결 재시도가 없다.
- 플레이어만 제거되고 HUD가 남는 경우 등록 해제 및 제거 알림이 없어 마지막 표시가 남는다. 새 플레이어 등록 시 재연결은 가능하다.
- Bind 새 구독 조건의 _view 검사는 아직 추가되지 않았다.
- GameManager.HandleBeforeSceneChange는 정의만 있고 호출/구독 연결이 없다. 씬 전환 저장은 별도 미완성이다.

## 다음 할 일
- Bind의 View 검사를 보완한다.
- 플레이어 단독 제거를 지원할 때 현재 플레이어 등록 해제, UI 알림, HUD 숨김/초기화를 함께 구현한다.
- 씬 전환 저장 이벤트를 연결하고 실제 씬 구성에서 생성·재활성화·파괴를 검증한다.
