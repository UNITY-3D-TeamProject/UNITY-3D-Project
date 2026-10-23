# 인게임 설정창 슬라이더·종료 버튼 설계 (intent-073)

## 작업 요약 및 방법
- 사용자 요청: ESC 설정창의 슬라이더 수정 + 오른쪽 스크롤바로 내려서 `게임 종료`·`메인 화면으로` 버튼 추가. 설계만 하고 코드·프리팹은 건드리지 않음.
- 조사: `UI/Settings/SettingsView`·`SettingsPresenter`, `UIManager.OpenSettings/CloseSettings`, `GameManager.PauseGame/ResumeGame`, `MainMenuPresenter`(종료 처리), `SceneLoader`, Build Settings, 에디터 eval로 `Canvas_InGame` 설정창 계층·슬라이더 값.
- 결과를 [intent-073](../../../intent/intent-073-settings-scroll-exit-buttons.md)에 정리하고 INTENT_POINTER에 추가.

## 결정 사항 (제안, 사용자 확인 전)
- `Window/Content`를 세로 ScrollRect로 감싸고 맨 아래 `Row_Exit`에 버튼 두 개.
- 버튼 이벤트: `SettingsView` → `SettingsPresenter`(볼륨 저장 후 전달) → `UIManager` → `GameManager.ReturnToMainMenu()`/`QuitGame()`.
- `MainScene`은 Build Settings 맨 끝(7번)에 넣고 이름으로 불러와 `ESceneType` 번호를 지킴.
- 버튼은 선택 연결이라 메인 메뉴 씬 설정창에는 만들지 않음.

## 현재 상태 및 이슈
- 마우스 감도 슬라이더(`Row_Controls/Slider`)는 코드에 연결되지 않음. 감도는 `CameraControl/PlayerBaseCamera._sensitivity`(private)에만 있어 카메라 담당 API가 필요.
- 슬라이더 `Fill Area`·`Handle Slide Area`에 여백이 없어 끝에서 손잡이가 트랙 밖으로 반쯤 나감.
- `SettingsPanel`에 참조가 빈 `SettingsView` 컴포넌트가 중복으로 붙어 있음(동작 영향 없음).
- `GameManager.StartGame()`은 호출하는 곳이 없음. 메인 화면 → 새 게임으로 다시 들어갈 때 상태가 정상인지 구현 때 확인 필요.

## 다음 할 일 (Next Steps)
- intent-073 열린 질문 5건 사용자 답 받기(슬라이더 수정 범위, 감도 API, 확인 팝업, 로딩씬 경유, 중복 컴포넌트).
- 답에 따라 프리팹 스크롤·버튼 → 코드 → Build Settings 순으로 구현, TestScene Play에서 ESC → 스크롤 → 두 버튼 동작 확인.
