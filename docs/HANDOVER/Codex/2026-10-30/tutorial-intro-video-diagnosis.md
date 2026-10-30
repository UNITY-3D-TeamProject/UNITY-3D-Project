# 튜토리얼 전 MP4 미재생 경로 조사

## 작업 요약
- 튜토리얼 시작 전 `1_BeforeTutorial.mp4` 재생 경로를 정적으로 조사했다.

## 방법/접근
- `MainMenuPresenter`의 새 게임 버튼부터 `SceneLoader`, `LoadingScreen`, `SOLoadingCutsceneData`까지 호출 흐름과 씬 참조를 확인했다.
- 영상 파일과 메타 GUID가 SO 참조와 일치하고, MP4 내 영상·음성 코덱 표기가 각각 `avc1`·`mp4a`임을 확인했다.
- `SOLoadingCutsceneData`에서 로블록스 영상의 기존 튜토리얼→로비 등록은 제거됐지만 `Map_5_Live`의 별도 VideoPlayer는 여전히 같은 파일을 참조함을 확인했다.
- `unity pipeline list`와 `unity status`에서 연결 가능한 Unity 에디터가 없음을 확인했다.

## 변경된 파일
- 이 작업 기록 및 `HANDOFF_POINTER.md`만 변경했다.

## 결정 사항
- 재생 경로의 정적 증거만으로 원인을 단정하거나 코드를 변경하지 않는다.

## 현재 상태 및 이슈
- 튜토리얼 씬을 직접 Play하면 `SceneLoader.TryStartNewGame()`과 로딩 씬을 거치지 않아 MP4가 재생되지 않는다.
- 메인 메뉴의 새 게임 경로에서는 코드상 영상 선택이 가능하다. 실제 재생 여부와 Unity 콘솔 오류는 에디터 연결이 없어 검증하지 못했다.
- 사용자가 메인 메뉴의 새 게임 경로에서도 영상이 나오지 않는다고 확인했다. 화면이 넘어가는지 멈추는지와 콘솔 경고는 아직 확인되지 않았다.
- 작업 시작 전부터 존재한 로딩 영상 관련 미커밋 변경은 보존했다.

## 다음 할 일
- 사용자가 `MainScene`에서 새 게임 버튼을 눌렀는지, `Map_0_Tutorial`을 직접 Play했는지 확인한다.
- 메인 메뉴 경로에서도 실패한다면 Unity 콘솔의 `[LoadingScreen] 컷씬을 재생하지 못했습니다` 경고와 실제 화면 증상을 확인한다.
