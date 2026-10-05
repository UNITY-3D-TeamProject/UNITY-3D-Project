# 커서 ON/OFF 로그만 유지

## 작업 요약 및 방법
- 사용자의 요청에 따라 GameManager.SetCursorVisible의 로그를 마우스 커서 켜짐/꺼짐으로 단순화했다.
- 앞서 추가한 UIManager, SettingsPresenter, SettingsView의 설정창 진단 로그와 그에 딸린 분기만 제거했다. 기존 오류 로그는 유지했다.
- UIManager의 비 UTF-8 파일 인코딩을 보존했다.

## 변경된 파일
- Assets/_Project/Scripts/Core/GameManager.cs
- Assets/_Project/Scripts/UI/UIManager.cs
- Assets/_Project/Scripts/UI/SettingsPresenter.cs
- Assets/_Project/Scripts/UI/SettingsView.cs

## 결정 사항
- 추가 디버그 로그는 GameManager 커서 전환 시점의 켜짐/꺼짐만 출력한다.

## 현재 상태 및 검증
- MSBuild Assembly-CSharp 빌드 성공(오류 0, 기존 경고 6).
- git diff --check 통과.
- Canvas 참조 연결과 Unity Play 모드 입력 확인은 별도 후속 작업이다.

## 다음 할 일
- Play 모드 Console에서 ESC 열기/닫기마다 커서 로그가 출력되는지 확인한다.
