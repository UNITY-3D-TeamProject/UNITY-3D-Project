# 튜토리얼 안내 패널 MVP 구조 검토

## 작업 요약
- 튜토리얼 안내 패널에 MVP를 적용하는 범위와 독백 UI와의 역할 차이를 검토했다.

## 방법/접근
- `Tutorial`, `StageBase`, `StagePhaseBase`와 기존 HUD의 Manager/Presenter/View 구조를 확인했다.

## 변경된 파일
- 이 작업 기록과 `HANDOFF_POINTER.md`만 변경했다. Unity 코드와 에셋은 변경하지 않았다.

## 결정 사항
- 확정된 구현 결정은 없다. 튜토리얼 단계가 표시 시점과 완료 조건을 소유하고, Presenter가 단계 데이터를 View에 전달하며, View는 패널과 버튼을 표시하는 방식을 제안했다.
- 패널 닫기나 버튼 입력만으로 스테이지 진행도를 UI에서 직접 변경하지 않는 방식을 제안했다.

## 현재 상태 및 이슈
- `Tutorial.StartStage()`와 `EndStage()`는 비어 있다. `StagePhaseBase.OnCompleted`는 존재하지만 튜토리얼 흐름과 연결되어 있지 않다.
- 단계 수, 안내 내용, 확인 버튼과 플레이어 행동 중 무엇으로 단계를 완료할지 아직 정해지지 않았다.

## 다음 할 일
- 튜토리얼 단계별 안내 내용과 완료 조건을 정한 뒤 View/Presenter와 단계 흐름을 구현한다.
