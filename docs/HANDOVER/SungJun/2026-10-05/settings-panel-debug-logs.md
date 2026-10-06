# 설정창 패널·커서 진단 로그

## 작업 요약 및 방법
- GameManager의 SetCursorVisible에 ON/OFF 및 적용 직후 visible/lockState 로그를 추가했다.
- UIManager에 Player/OpenSettings 입력 수신, SettingsPresenter/PlayerInput/상태 누락, 설정 UI 활성화 결과 로그를 추가했다.
- SettingsPresenter와 SettingsView에 View/Root 참조 누락 및 Panel 활성화 결과 로그를 추가했다.

## 변경된 파일
- Assets/_Project/Scripts/Core/GameManager.cs
- Assets/_Project/Scripts/UI/UIManager.cs
- Assets/_Project/Scripts/UI/SettingsPresenter.cs
- Assets/_Project/Scripts/UI/SettingsView.cs

## 결정 사항
- 로그는 입력 경계, 누락된 Inspector 참조, 커서 상태 변경 시점에만 출력한다.

## 현재 상태 및 이슈
- 저장된 Assets/Test/UnityAssets/Prefabs/Canvas.prefab의 UIManager에는 _settingsPresenter 연결이 없다. 현재 씬의 미저장 Inspector 상태는 확인할 수 없다.
- MSBuild Assembly-CSharp 컴파일 성공(오류 0, 기존 경고 6).
- Unity Play 모드에서의 실제 패널 표시 검증은 Canvas 참조 연결 후 필요하다.

## 다음 할 일
- 활성 씬의 Canvas UIManager._settingsPresenter, SettingsPresenter._view, SettingsView._root 참조를 연결하고 Play 모드의 로그 순서를 확인한다.
