# 설정창 입력 전환 검토

## 작업 요약 및 방법
- `FID_InputSystem.inputactions`, Player 프리팹, `PlayerInputComponent`, `UIManager`를 읽고 설정창 입력 흐름을 검토했다.
- 후속 질문에서 `PlayerHudPresenter`·`PlayerHudView`와 빈 `InfoPanelPresenter`·`MonologuePresenter`를 확인하고 설정창의 MVP 책임 분리를 검토했다.
- 코드나 에셋은 변경하지 않았다.

## 결정 사항
- 최신 사용자 지시: `SettingsPresenter`는 PlayerInput 액션을 직접 구독하지 않는다. `UIManager`가 구독하고 현재 설정 UI의 활성화, 액션맵·커서 전환을 담당하며 `GameManager.PauseGame`/`ResumeGame`을 호출한다. Presenter는 활성화된 설정창의 View 이벤트와 설정값 처리를 맡는다. 아래의 이전 제안보다 이 결정이 우선한다.
- 아직 사용자와 구현 방식을 확정하지 않았다.
- 제안: 설정창을 관리하는 쪽에서 패널 표시, Player/UI 액션맵 전환, 커서 잠금·표시를 한 번에 제어한다.
- 제안: 열기/닫기와 입력 모드 전환만 필요하면 SettingsMenuController 하나가 맡고, 실제 설정값을 Model과 View 사이에 표시·수정할 때 Presenter를 둔다.
- 후속 설계 제안: 사용자가 택한 MVP 구조에서는 `SettingsView`가 X 클릭을 알리고, `SettingsPresenter`가 View 호출과 닫기 요청을 맡는다. `UIManager`가 플레이어 입력 이벤트 구독, Presenter 호출, 액션맵·커서 전환을 한 방향으로 조정한다. 앞선 단일 Controller 제안은 채택하지 않는다.

## 현재 상태 및 이슈
- Player 프리팹의 기본 액션맵은 `Player`, `UI Input Module` 참조는 비어 있다.
- 후속 작업 트리에서 `FID_InputSystem`의 Player 맵에 Escape 바인딩 `OpenSettings` 액션이 추가된 것을 확인했다.
- 프로젝트 스크립트에서 커서 잠금·표시 제어와 설정창 동작 구현은 확인되지 않았다.
- 최신 파일 확인: 사용자가 `SettingsView`에 `Show`/`Hide`와 `CloseClicked`를 구현했고, `SettingsPresenter`는 View 이벤트를 `CloseRequested`로 전달한다. 이전에 확인한 빈 View 스텁 문제는 해소되었다.

## 다음 할 일
- 사용자가 실제 파일 구현을 요청하면 설정창 범위와 ESC 재입력 시 닫기 동작을 확정한다.
- UI 입력 모듈 연결, 입력맵·커서 전환, 설정창 닫기 흐름을 구현하고 Unity Play 모드에서 검증한다.
- 플레이어 코드 수정 없이 `UIManager`의 기존 스폰 연결에서 현재 플레이어의 `PlayerInput`을 찾아 `Player/OpenSettings` 액션을 구독한다. 스폰 교체와 비활성화 시 구독을 해제한다. SettingsPresenter 직접 입력 구독 제안은 사용자 지시에 따라 폐기했다.
