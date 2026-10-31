# 튜토리얼 시작 영상 뒤 바로 진입

## 작업 요약
- 새 게임 진입 시 시작 영상을 재생하고, 영상 종료 또는 스킵 후 튜토리얼 씬을 활성화하도록 변경했다. 진행 바는 표시하지 않는다.

## 방법/접근
- `LoadingScreen.CoLoad`에서 튜토리얼 씬을 영상 재생 중 비동기로 준비한다. 영상 완료 후 씬이 준비되면 활성화한다.
- 피드·파일 종료 영상은 기존처럼 영상 뒤에 목적지 로딩을 시작한다.

## 변경된 파일
- `Assets/_Project/Scripts/SceneTransition/LoadingScreen.cs`
- `Assets/_Project/Scripts/SceneTransition/SOLoadingCutsceneData.cs`
- `docs/intent/intent-090-loading-transition-videos.md`

## 결정 사항
- 튜토리얼 목적지의 `allowSceneActivation`은 시작 영상 완료 또는 스킵 전까지 false로 둔다. 영상 재생에는 기존 로딩 씬의 VideoPlayer를 사용하며 튜토리얼 진입 시 로딩 화면 페이드아웃은 건너뛴다.

## 현재 상태 및 이슈
- 로딩 씬의 컷씬 SO와 튜토리얼 시작 MP4 참조가 연결된 것을 정적으로 확인했다.
- `git diff --check` 통과. Unity Play에서 실제 영상 재생과 씬 전환은 미확인이다.

## 다음 할 일
- `MainScene`의 새 게임 버튼으로 `영상 → 튜토리얼` 순서와 진행 바 비표시를 실제 Play에서 확인한다.
