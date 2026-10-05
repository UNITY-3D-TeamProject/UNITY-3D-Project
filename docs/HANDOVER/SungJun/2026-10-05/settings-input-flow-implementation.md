# 설정창 입력 흐름 구현

## 작업 요약 및 방법
- 사용자 지시대로 타이틀 씬이 없는 현재 단계의 초기 상태를 Playing으로 지정했다.
- GameManager Awake에서 초기 커서를 숨기고 UIManager의 커서 처리 메서드와 호출을 제거했다.
- UIManager가 사용자가 추가한 Player/OpenSettings와 UI/CloseSettings 액션을 직접 구독하도록 연결했다. 양쪽 ESC 바인딩을 확인했고 X 클릭은 기존 CloseSettings 경로를 유지했다.
- UIManager의 기존 비 UTF-8 인코딩을 보존하며 필요한 ASCII 코드만 수정했다.

## 변경된 파일
- Assets/_Project/Scripts/Core/GameManager.cs
- Assets/_Project/Scripts/UI/UIManager.cs
- intent 및 인수인계 문서

## 결정 사항
- GameManager는 상태, 시간, 커서를 관리한다. UIManager는 입력 구독, 설정 UI 활성화와 액션맵 전환을 관리한다.
- StartGame 진입 로직과 플레이어 스크립트는 변경하지 않는다. Canvas 연결은 사용자가 후속 작업으로 진행한다.

## 현재 상태 및 검증
- MSBuild Assembly-CSharp 빌드 성공: 오류 0, 기존 경고 6.
- 최초 빌드는 샌드박스의 Windows SDK 경로 접근 제한으로 실패했고, 권한 확장 후 성공했다.
- git diff --check 통과.
- 사용자에게 맡긴 Canvas 연결 전이므로 Unity Play 모드에서 실제 ESC/X 동작은 검증하지 않았다.

## 다음 할 일
- Canvas의 SettingsPresenter/View/패널/X 버튼 참조를 연결한다.
- ESC로 Pause/UI 전환, ESC 재입력 및 X 클릭으로 Playing/Player 복귀를 Play 모드에서 확인한다.
