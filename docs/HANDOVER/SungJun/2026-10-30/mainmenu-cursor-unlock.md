# 2026-10-30 — 메인씬 시작 시 커서가 사라지는 문제 수정

## 1. 작업 요약 및 방법
- 증상: 에디터에서 MainScene으로 Play를 시작하면 가끔 커서가 사라져 Start 버튼을 누를 수 없다.
- 원인: 메인씬 시작 시 커서를 푸는 코드가 없었다. `GameManager.Awake`의 잠금은 지웠지만 해제 코드는 없었고, `HandleSceneLoaded`는 `StageManager`가 없는 씬에서 바로 return한다. 에디터에서는 `Cursor.lockState`·`Cursor.visible`이 Play가 끝나도 남아 있어서(Domain Reload로도 초기화 안 됨), 맵 씬에서 커서가 잠긴 채 Play를 멈추면 다음 Play의 메인씬이 그 상태를 그대로 물려받았다.
- 수정: `GameManager.Awake`에서 `SetCursorLocked(false)`를 호출한다.
- 확인: 컴파일 0에러. 에디트 모드에서 `Locked`·`visible=false`로 만든 뒤(값이 그대로 남는 것도 확인) MainScene을 Play하자 `Awake`에서 `None`·`visible=true`로 풀렸다. 에러 로그 0.

## 2. 결정 사항
- `HandleSceneLoaded`에서 "StageManager 없으면 커서 해제"로 처리하지 않는다. `Map_1_Lobby`에는 StageManager가 없어서, 그렇게 하면 맵 → 로비 이동 때 커서가 풀린다.
- `GameManager`는 Play당 한 번만 `Awake`가 실행되므로(싱글톤) Play 시작 때만 커서를 푼다. 맵 씬에서 바로 Play하면 `Awake`에서 풀었다가 `HandleSceneLoaded`가 다시 잠근다.

## 3. 현재 상태 및 이슈
- 마우스로 Start 버튼을 직접 눌러 보지는 않았다.
- (기존 동작, 이번에 바꾸지 않음) 로비에는 StageManager가 없어서, 메인 메뉴의 "이어하기"로 로비에 바로 들어가면 커서가 잠기지 않을 수 있다. 확인하지 않았다.

## 4. 다음 할 일
- 맵 씬에서 커서가 잠긴 채 Play를 멈춘 뒤 MainScene으로 다시 Play해서 Start 버튼이 눌리는지 확인.
- "이어하기 → 로비" 경로에서 커서가 잠기는지 확인.
