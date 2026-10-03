# 독백 및 안내 패널 UI 스크립트 틀 생성

## 작업 요약
- `Assets/_Project/Scripts/UI/`에 독백과 범용 안내 패널용 View/Presenter 스크립트 네 개를 만들었다.

## 방법/접근
- 기존 UI 스크립트와 같은 `UI` 네임스페이스, `MonoBehaviour` 형식으로 빈 클래스를 만들었다.
- Unity 참조가 안정적으로 유지되도록 각 스크립트의 `.meta` 파일을 함께 만들었다.

## 변경된 파일
- `MonologueView.cs`, `MonologuePresenter.cs`, `InfoPanelView.cs`, `InfoPanelPresenter.cs` 및 각 `.meta` 파일.
- 이 작업 기록과 `HANDOFF_POINTER.md`.

## 결정 사항
- 패널이 튜토리얼 외에도 쓰일 수 있어 `InfoPanel` 이름을 사용했다.
- 요청에 따라 필드, 메서드, 표시 로직은 추가하지 않았다.

## 현재 상태 및 이슈
- 네 클래스는 비어 있으며 씬이나 프리팹에 연결되지 않았다.
- 파일 내용과 `.meta` GUID 중복 여부는 확인했다. Unity 에디터 컴파일은 실행하지 않았다.

## 다음 할 일
- 필요할 때 각 View/Presenter의 실제 표시 동작과 씬 연결을 구현한다.
