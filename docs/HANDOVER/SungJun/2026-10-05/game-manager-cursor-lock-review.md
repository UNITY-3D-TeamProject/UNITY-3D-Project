# GameManager 커서 잠금 확인

## 작업 요약 및 방법
- GameManager의 커서 설정과 호출 시점을 코드에서 확인했다.
- `CursorLockMode.Locked`와 `CursorLockMode.None`의 동작 차이를 사용자에게 설명했다.
- 후속 질문에서 게임 창 안에서 자유롭게 움직이는 요구를 확인하고 Unity 공식 문서의 `Confined` 동작 및 플랫폼 지원 범위를 검토했다.

## 변경된 파일
- 게임 코드 변경 없음.

## 결정 사항
- 없음. 현재 동작만 확인했다.

## 현재 상태 및 이슈
- `Awake`와 `ResumeGame`은 커서를 화면 중앙에 고정하고 숨긴다.
- `PauseGame`은 잠금을 풀고 커서를 표시한다.
- 게임 창 내부에서 자유롭게 움직이게 하는 `CursorLockMode.Confined`는 사용하지 않는다.
- Unity Play 모드에서 실제 입력 동작은 검증하지 않았다.
- `Confined`는 Windows/Linux standalone 빌드에서 지원된다. Game 뷰와 실제 빌드의 동작이 다를 수 있다.
- 카메라는 Look 입력을 회전에 전달하므로, 커서가 창 경계에 도달한 뒤 계속 회전해야 하는 조작인지 확인할 필요가 있다.

## 다음 할 일
- 창 안에서 커서가 보이면서 자유롭게 움직여야 한다면 Playing 상태에 `Confined`와 `Cursor.visible = true`를 적용하고 Windows 빌드에서 확인한다.
- 카메라를 연속 회전해야 한다면 `Locked` 방식과 사용 맥락을 다시 검토한다.
