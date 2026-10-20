# UIManager 선언 순서 및 대화 설정창 입력

## 작업 요약
- `UIManager.cs`의 필드와 메서드를 역할 및 접근 수준에 따라 재배치했다.
- 대화 중 ESC로 설정창을 열 수 있도록 `Dialogue/OpenSettings` 연결을 복원하고 입력 에셋에 액션과 ESC 바인딩을 추가했다.

## 방법/접근
- 직렬화 필드는 인스펙터 표시 순서를 유지했다.
- 일반 필드는 GameManager, 플레이어 입력, 설정창, 대화 콜백 순으로 모았다.
- 메서드는 Unity 수명주기, 공개 UI API, 내부 처리 순으로 모았다.
- 선언 순서 변경 당시 기존 메서드 16개의 본문이 동일함을 확인했다.
- 입력 에셋 JSON과 Dialogue 맵의 ESC 바인딩을 확인하고 `git diff --check`를 통과했다.
- Player 프리팹의 PlayerInput이 수정한 `FID_InputSystem.inputactions`를 참조함을 확인했다.

## 변경된 파일
- `Assets/_Project/Scripts/UI/UIManager.cs`
- `Assets/FID_InputSystem.inputactions`
- 이 작업 기록과 `HANDOFF_POINTER.md`

## 결정 사항
- UIManager의 기존 동작과 직렬화된 필드 순서를 유지한다.
- 일반 플레이 중 설정창 입력은 유지한다.
- Dialogue 맵의 `OpenSettings`는 ESC로 설정창을 열고, 닫을 때 대화가 진행 중이면 Dialogue 맵으로 돌아간다.

## 현재 상태 및 이슈
- 작업 도중 같은 파일에 컷씬 관련 코드가 별도로 추가되어, 그 코드는 보존했다.
- Unity 에디터에서 직접 키 입력 테스트는 실행하지 않았다.

## 다음 할 일
- Unity 에디터에서 컷씬 중 ESC로 설정창 열기, 다시 ESC로 닫기, 대사 넘기기를 확인한다.
