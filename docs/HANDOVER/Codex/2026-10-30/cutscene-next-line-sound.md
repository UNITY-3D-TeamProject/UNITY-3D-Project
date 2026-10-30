# 컷신 E 대사 넘김 효과음

## 작업 요약 및 방법
- `UIManager.HandleNextLine`에서 컷신 대사 또는 일반 대화가 실제로 E 입력을 받을 때 기존 `SOAudioData_UiClick`의 `Click_18`을 UI 채널로 재생한다.
- `Canvas_InGame` 프리팹의 UIManager에 해당 오디오 SO를 연결하고, 대사 반복에 맞게 재생 음량을 기본값의 45%로 낮췄다.
- 펫 안내 닫기와 대사가 없는 상태의 E 입력에는 이 소리를 재생하지 않는다.

## 결정 사항
- 이미 프로젝트에서 사용 중인 UI 클릭 음원을 재사용한다. 컷신 전용 새 음원 파일은 추가하지 않는다.
- 소리는 게임 시간이 멈춘 컷신에서도 동작하는 `AudioManager.PlayUi`로 재생한다.

## 현재 상태 및 이슈
- 변경 파일의 diff와 오디오 SO·클립 GUID 참조를 확인했다. `git diff --check` 통과.
- Unity Pipeline 에디터 인스턴스에 연결할 수 없어 실제 청취와 Unity 컴파일은 확인하지 못했다. `dotnet build --no-restore`도 오류 메시지 없이 실패해 컴파일 검증으로 인정할 수 없다.

## 다음 할 일
- Unity 에디터에서 컷신을 재생해 E를 누를 때 클릭음의 크기와 타이밍을 청취한다.
