# StageManager와 Tutorial 연결 확인

- 작업 요약 및 방법: `StageManager`, `StageBase`, `Tutorial` 코드와 튜토리얼 씬의 컴포넌트 등록을 정적으로 확인했다.
- 결정 사항: 구조 변경 없음. `StageManager`가 씬의 `StageBase` 구현을 찾아 `StartStage()`를 호출하며, 튜토리얼 씬에서는 `Tutorial`이 해당 구현이다.
- 현재 상태 및 이슈: `Tutorial`은 현재 펫 활성화와 대화 종료 후 해금을 처리한다. 런타임 검증과 코드 변경은 하지 않았다.
- 다음 할 일: 추가 요청 시 관리 책임 범위를 기준으로 진행한다.
