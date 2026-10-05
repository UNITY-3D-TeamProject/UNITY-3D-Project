# 설정창 흐름 연결 누락 검토

## 작업 요약 및 방법
- 사용자 요청의 ESC → 설정창 → Pause/UI 입력 → X → Playing/Player 입력 경로를 현재 스크립트와 저장된 씬·프리팹에서 추적했다.
- 소스와 에셋은 수정하지 않았으며 Unity 실행 검증은 수행하지 않았다.

## 결정 사항
- 입력 구독과 UI 활성화는 UIManager, 게임 상태·시간·커서는 GameManager, UI 내부 이벤트는 SettingsPresenter/View가 담당한다.

## 현재 상태 및 이슈
- UIManager의 OpenSettings/CloseSettings에서 PauseGame/ResumeGame과 액션맵 전환 호출은 구현되어 있다. X의 View → Presenter → UIManager 이벤트 경로도 구현되어 있다.
- GameManager.CurrentState는 기본값 Title이며, 저장된 Assets의 코드와 UnityEvent에서 StartGame 호출은 찾지 못했다. UIManager.OpenSettings는 Playing이 아니면 반환하므로 게임 시작 상태 연결이 필요하다.
- 저장된 Assets/Test/UnityAssets/Prefabs/Canvas.prefab의 UIManager에는 _settingsPresenter 필드 연결이 없으며, SettingsPresenter/View 스크립트 GUID를 참조하는 저장된 씬·프리팹은 찾지 못했다. 에디터의 미저장 변경은 확인하지 않았다.
- 커서 제어가 GameManager와 UIManager 양쪽에 남아 있다. GameManager.StartGame에는 커서 숨김이 아직 없다.
- KSJ_Home 씬에 EventSystem과 InputSystemUIInputModule은 존재한다. 모듈의 액션 에셋 GUID는 FID_InputSystem과 다르고 Player 프리팹의 m_UIInputModule은 비어 있다. 이 사실만으로 버튼 클릭 불가라고 단정할 수는 없다.

## 다음 할 일
- 실제 게임 진입 경로에서 Playing 상태로 전환하고, 설정 UI 오브젝트와 Inspector 참조를 배치·저장한다.
- 커서 정책을 GameManager에 일원화하고, 실행 중 UI 입력 모듈 연결과 X 클릭 후 복귀를 확인한다.
