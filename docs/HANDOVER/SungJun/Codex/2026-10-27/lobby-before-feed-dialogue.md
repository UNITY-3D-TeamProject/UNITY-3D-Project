# 피드 앱 입장 전 대화 수정

## 작업 요약 및 방법
- 피드 앱 입구에서 재생하는 `SOCutscene_Lobby_BeforeFeed.asset`의 11개 대사를 사용자 제공 문안으로 교체했다.
- 첫 PET 대사의 `Lobby_FeedPortal` 카메라 키를 유지하고 나머지는 화자 카메라를 쓰도록 했다.
- 로비 씬의 `ScenePortal_Feed`가 E 상호작용 시 컷씬 12번을 먼저 재생하고 입장하는 연결을 확인했다.

## 변경된 파일
- `Assets/_Project/ScriptableObjects/UIandCinematic/Cutscene/SOCutscene_Lobby_BeforeFeed.asset`

## 결정 사항
- 기존 컷씬 테이블 12번, 포탈 E 이벤트, 씬 전환 구조는 수정하지 않았다.

## 현재 상태 및 이슈
- 11개 대사의 화자·문구·카메라 키와 포탈 컷씬 번호를 정적 검증했다. `git diff --check`도 통과했다.
- Unity 플레이 화면에서 대사 표시와 씬 전환은 확인하지 않았다.

## 다음 할 일
- 필요하면 로비의 피드 앱 입구에서 E 상호작용해 대사와 전환을 플레이 확인한다.
