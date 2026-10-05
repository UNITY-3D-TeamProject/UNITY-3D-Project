---
id: intent-013
title: 설정창 입력 전환과 초기 Playing 상태 연결
part: UI / Game State
status: resolved
created: 2026-10-05
resolved: 2026-10-05
---

## 문제 (Problem)
- 타이틀 씬과 StartGame 진입 경로가 없어 초기 Title 상태에서 설정창이 열리지 않는다.
- UIManager에 커서 제어가 중복되어 있고 UI/CloseSettings 액션이 연결되지 않았다.

## 기대 결과 (Proposed outcome)
- GameManager가 기본 Playing 상태로 시작하며 초기 커서를 숨긴다.
- UIManager가 Player/OpenSettings와 UI/CloseSettings를 구독한다. ESC로 열고 닫으며 X 버튼도 기존 CloseSettings 경로를 사용한다.
- GameManager가 Pause/Playing, 시간 정지/복구, 커서 표시/숨김을 담당한다.

## 영향 범위 (Affected users and systems)
- GameManager.cs, UIManager.cs 및 작업 기록.

## 제약 (Constraints)
- StartGame 진입 경로를 추가하지 않는다.
- 플레이어 스크립트와 사용자가 추가한 FID_InputSystem 액션은 수정하지 않는다.
- Canvas와 Inspector 연결은 사용자가 후속 작업으로 수행한다. 이번 완료 범위는 스크립트 수정과 가능한 컴파일 검증이다.

## 열린 질문 (Open questions)
- 없음. 사용자 지시로 범위가 확정되었다.

## 해결 기록 (Resolution)
- GameManager.CurrentState의 기본값을 Playing으로 바꾸고 Awake에서 초기 커서를 숨겼다. StartGame 진입 경로는 추가하지 않았다.
- UIManager의 커서 메서드와 호출을 제거했다. UI/CloseSettings의 performed를 구독하고 교체·비활성화 시 구독 해제한다. X 버튼과 CloseSettings 액션은 같은 닫기 메서드로 연결된다.
- MSBuild로 Assembly-CSharp 컴파일 성공(오류 0, 기존 경고 6). git diff --check 통과.
- Canvas 연결 및 Unity Play 모드 입력 검증은 사용자 후속 작업이다.
- 커밋/PR 없음.
