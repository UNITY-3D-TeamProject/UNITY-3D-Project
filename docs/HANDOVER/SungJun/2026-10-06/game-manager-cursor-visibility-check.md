# GameManager 커서 표시와 중앙 고정 수정

## 작업 요약 및 방법
- 현재 GameManager, UIManager, 입력 액션을 읽고 Playing/ESC/Pause/Resume 흐름을 확인했다.
- 작업 트리의 GameManager 변경과 HEAD를 비교하고, ESC 설정창 흐름에 맞춰 커서 표시 값을 수정했다.
- 후속 요청으로 Playing 중 중앙 고정을 위해 `Confined`를 `Locked`로 변경했다.

## 변경된 파일
- `Assets/_Project/Scripts/Core/GameManager.cs`: `SetCursorLocked`가 `Locked/None`과 `Cursor.visible`을 함께 전환하도록 변경.

## 결정 사항
- Playing은 `Locked + visible false`, Pause는 `None + visible true`로 정했다.

## 현재 상태 및 이슈
- `Awake`와 `ResumeGame`은 `SetCursorLocked(true)`를 호출해 커서를 중앙에 고정하고 숨긴다.
- ESC는 `OpenSettings`/`CloseSettings`에 연결되어 있으며 `PauseGame`은 `SetCursorLocked(false)`를 호출해 커서를 표시한다.
- Unity 공식 문서에서 `Locked`의 Game 뷰 중앙 고정 동작을 확인했다.
- `dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet` 성공: 오류 0개, 기존 경고 7개.
- `git diff --check` 통과.
- Unity Play 모드에서 실제 동작은 확인하지 않았다.

## 다음 할 일
- Unity Play 모드에서 ESC 두 번을 눌러 커서 표시 및 중앙 고정 전환을 확인한다.
