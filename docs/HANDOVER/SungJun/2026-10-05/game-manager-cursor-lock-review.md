# GameManager 커서 잠금 확인

## 작업 요약 및 방법
- GameManager의 커서 설정과 호출 시점을 코드에서 확인했다.
- `CursorLockMode.Locked`와 `CursorLockMode.None`의 동작 차이를 사용자에게 설명했다.

## 변경된 파일
- 게임 코드 변경 없음.

## 결정 사항
- 없음. 현재 동작만 확인했다.

## 현재 상태 및 이슈
- `Awake`와 `ResumeGame`은 커서를 화면 중앙에 고정하고 숨긴다.
- `PauseGame`은 잠금을 풀고 커서를 표시한다.
- 게임 창 내부에서 자유롭게 움직이게 하는 `CursorLockMode.Confined`는 사용하지 않는다.
- Unity Play 모드에서 실제 입력 동작은 검증하지 않았다.

## 다음 할 일
- 화면 중앙 고정 대신 게임 창 내부 이동 제한이 필요한지 요구사항을 확인한다.
