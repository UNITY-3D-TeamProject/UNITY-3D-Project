# 플레이어 독백 구조 검토

## 작업 요약
- 플레이어 독백의 표시 타이밍과 대사 저장 위치에 대한 구조를 검토했다.

## 방법/접근
- `UIManager`, `PlayerHudPresenter`, `PlayerHudView`, `StageManager`, `GameManager`의 현재 이벤트 연결을 읽었다.
- 기존 HUD의 Manager/Presenter/View 역할 분리를 기준으로 독백 표시 구조를 제안했다.

## 변경된 파일
- 이 작업 기록과 `HANDOFF_POINTER.md`만 변경했다. Unity 코드와 에셋은 변경하지 않았다.

## 결정 사항
- 확정된 구현 결정은 없다. 타이밍을 감지하는 게임 로직이 독백 ID를 전달하고, 별도 독백 Presenter/View가 해당 문구를 표시하는 방식을 제안했다.
- 대사가 여러 개라면 프로젝트 내부 ScriptableObject 에셋에 ID와 문구를 저장하는 방식을 제안했다.

## 현재 상태 및 이슈
- `StageManager.OnStageStarted`는 선언과 호출 코드가 있지만 `StartStage()`를 호출하는 연결은 현재 확인되지 않았다. 나머지 스테이지 이벤트는 선언만 되어 있다.
- 어떤 상황에 어떤 독백을 띄울지, 중복 발생 시 처리 방식은 아직 정해지지 않았다.

## 다음 할 일
- 독백 발생 시점과 대사 목록을 정한 뒤 표시 방식과 저장 단위를 확정한다.
