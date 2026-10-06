# GameManager 커서 Confined 시범 적용 및 원상 복구

## 작업 요약 및 방법
- 사용자의 요청에 따라 플레이 중 커서를 게임 창 안에서 움직일 수 있도록 잠금 모드를 `Locked`에서 `Confined`로 변경했다.
- 커서가 보이도록 설정하고, 일시정지 시에는 기존처럼 잠금을 해제했다.
- 메서드 이름과 로그를 실제 동작에 맞게 변경했다.
- 사용자가 플레이 중 커서 비표시와 Game 뷰 밖 이동 문제를 확인해, 시범 변경을 되돌렸다.

## 변경된 파일
- `Assets/_Project/Scripts/Core/GameManager.cs`

## 결정 사항
- 최종 상태는 기존 설정과 동일하다. `Awake`와 `ResumeGame`에서는 `Locked`로 중앙 고정 및 숨김, `PauseGame`에서는 `None`으로 잠금 해제 및 표시한다.

## 현재 상태 및 이슈
- Confined 시범 변경 당시 `dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet` 성공: 오류 0개, 기존 경고 7개.
- `git diff --check` 통과.
- `GameManager.cs`는 원본과 동일해 최종 코드 변경이 없다.
- Unity Play 모드에서 실제 커서 이동은 확인하지 않았다.
- Unity 문서상 `Confined`는 Windows/Linux standalone 빌드에서 지원된다. Editor Game 뷰에서는 창 이탈을 막는 용도로 사용할 수 없다.
- GameManager 프리팹은 저장된 씬 중 `Map_0_Tutorial`과 `Map_4_Security`에만 직접 배치되어 있다. 다른 씬을 단독 실행하면 GameManager가 없어 초기 커서 잠금이 실행되지 않을 수 있다.

## 다음 할 일
- 문제가 계속되면 현재 Play 모드를 시작한 씬에 GameManager가 있는지, Game 뷰에 입력 포커스가 있는지 확인한다.
