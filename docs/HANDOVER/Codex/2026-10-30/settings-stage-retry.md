# 인게임 설정창 스테이지 다시하기

## 작업 요약
- 설정창 하단의 고정 버튼 줄에 `RETRY`를 추가했다.
- 버튼을 누르면 현재 스테이지 씬을 다시 불러오는 기능을 연결했다.

## 방법/접근
- 기존 `MAIN MENU` 버튼 모양을 복제하고, 기존 Row_Exit 안에 첫 번째 버튼으로 배치했다.
- SettingsView → SettingsPresenter → UIManager 이벤트로 버튼 입력을 전달한다. 설정창을 닫아 입력 맵과 일시정지를 해제한 뒤 `StageManager.RestartStage()`를 호출한다.
- 첫 라운드 사망 복구와 같은 `StageRestart` 스폰 이유를 사용한다. SceneLoader의 캠페인 순번은 변경하지 않는다.

## 변경된 파일
- `Assets/_Project/Prefabs/UI/Canvas_InGame.prefab`
- `Assets/_Project/Scripts/UI/Settings/SettingsView.cs`
- `Assets/_Project/Scripts/UI/Settings/SettingsPresenter.cs`
- `Assets/_Project/Scripts/UI/UIManager.cs`
- `Assets/_Project/Scripts/Core/Stage/StageManager.cs`
- `docs/intent/intent-092-settings-stage-retry.md`, `docs/intent/INTENT_POINTER.md`

## 결정 사항
- 씬 재시작은 현재 스테이지 진행과 체크포인트를 초기화하고, 기존 `StageRestart` 능력치 복원 규칙을 따른다.
- `RETRY`, `MAIN MENU`, `QUIT GAME`은 모두 설정창에서 처음부터 보인다.

## 현재 상태 및 이슈
- 프리팹 계층·참조와 코드 연결을 정적으로 점검했다. Unity Editor Play에서 실제 클릭과 씬 재진입은 확인하지 못했다.
- `intent-092`는 Play 검증이 남아 open 상태다.

## 다음 할 일
- 인게임 씬 Play에서 ESC → RETRY 클릭 후 같은 씬이 초기 상태로 열리고 플레이어가 첫 체크포인트에 스폰되는지 확인한다.
