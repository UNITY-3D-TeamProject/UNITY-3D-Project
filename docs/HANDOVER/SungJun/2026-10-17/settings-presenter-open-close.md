# 설정창 열기·닫기 규칙을 SettingsPresenter 안으로 이동

## 1. 작업 요약 및 방법
- `SettingsPresenter.Open()`이 `gameObject.SetActive(true)` 후 `_view.Show()`, `Close()`가 `_view.Hide()` 후 `gameObject.SetActive(false)`를 하도록 변경했다.
- `UIManager.OpenSettings`·`CloseSettings`, `MainMenuPresenter.OpenSettings`·`CloseSettings`에서 직접 `SetActive()`하던 줄을 지웠다. 호출자는 `Open()`/`Close()`만 부른다.
- 닫을 때 `CloseRequested` 해제를 먼저 하고 `Close()`를 부르는 순서는 그대로 두었다.

## 2. 결정 사항
- 설정창 Presenter의 활성화 규칙은 Presenter가 책임진다. 호출자는 GameObject를 켜고 끄는 방법을 몰라도 된다.
- 전제: `SettingsPresenter`는 패널 전용 오브젝트에 붙고, `UIManager`·`MainMenuPresenter`와 다른 오브젝트여야 한다 (같으면 `Close()`가 매니저까지 끈다).

## 3. 현재 상태 및 이슈
- 프리팹 계층 확인 결과 전제를 만족한다: `_Project/Prefabs/UI/Canvas.prefab`·Test `Canvas_Shift.prefab`은 `UIManager`와 `SettingsPresenter`가 각자 다른 오브젝트, Test `MainScene`은 `MainMenu`와 `Canvas_Settings` 프리팹 인스턴스의 `SettingsPresenter`가 분리돼 있다.
- Unity 재컴파일을 요청했지만 MCP에서 완료 응답이 오지 않아(`triggered` 상태 유지) 컴파일·Play 확인은 하지 못했다.
- 참고: 각 프리팹에 `_root`가 비어 있는 `SettingsView`가 `SettingsPanel` 오브젝트에도 하나 더 붙어 있다. 이번 작업에서는 건드리지 않았다.

## 4. 다음 할 일
- Unity에서 컴파일 에러가 없는지 확인하고, Play 모드에서 ESC로 설정창 열기·닫기(게임 씬, 메인 메뉴 모두) 동작을 확인한다.
