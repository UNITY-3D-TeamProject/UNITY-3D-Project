# 컷씬 영상 4개 새 버전으로 교체

## 작업 요약 및 방법
- 사용자가 `Assets/ThirdParty/Audio/CutScene/`을 `PreviousCutScene/`(기존 1~4번)과 `NewCutScene/`(New_1~4)으로 나눠 넣었고, 기존 참조를 새 영상으로 옮겼다.
- 새 영상 임포트 설정을 기존 영상과 같게 맞췄다: Transcode 켜기, Codec VP8, Original Size, Bitrate High, Spatial Quality High. 적용 후 `.meta`의 설정 부분이 기존 영상 `.meta`와 GUID 빼고 같다.
  - 트랜스코드 시간: 1번 57초, 2번 64초, 3번 39초, 4번 113초.
- 참조 교체(파일에서 GUID 한 줄씩만 바꿈, 맵 씬은 에디터로 열지 않음):
  - `SOLoadingCutsceneData`: 튜토리얼 전 → `New_1_BeforeTutorial`, Feed→Lobby → `New_2_AfterFeedApp`, File→Lobby → `New_3_AfterFileApp`.
  - `Map_5_Live` 스크린 VideoPlayer(`m_VideoClip`) → `New_4_Ending`. 다른 값(`ScreenVideoZoom` 5초 진입·45초 퇴장, 오디오 Direct, 크레딧 연결 등)은 그대로.

| 영상 | 기존 길이 | 새 길이 | 새 GUID |
|---|---|---|---|
| 1 BeforeTutorial | 44.3초 | 45.3초 | `5e3a4d81356070d499dc078a997e44a1` |
| 2 AfterFeedApp | 48.8초 | 53.8초 | `c65600e417d93b44ba8355cb52ecb419` |
| 3 AfterFileApp | 28.5초 | 30.5초 | `7a0aa5888166a4c4184399a04ab437d5` |
| 4 Ending | 68.0초 | 68.0초 | `48506bbe2164a2941be782a7ba99a8e0` |

모두 1920×1080, 30fps, 소리 1트랙(원본도 같음).

## 결정 사항
- 엔딩 영상 길이가 기존과 같아(68초) 연출 시점 값은 바꾸지 않았다.
- 로딩 컷씬은 영상 끝까지 재생하는 구조라 길이가 늘어도 코드 변경이 필요 없다.

## 현재 상태 및 이슈
- 에디터에서 SO 참조가 새 영상 경로로 읽히는 것까지 확인했다. **Play 재생·청취는 하지 않았다.**
- 엔딩 새 영상의 장면 구성이 기존과 다르면 5초 진입·45초 퇴장 시점이 어색할 수 있다(직접 보고 판단 필요).
- `ThirdParty`는 Git 제외라 팀원에게 `NewCutScene/` 폴더의 mp4와 `.meta`를 함께 공유해야 참조가 유지된다.
- 커밋 안 함.

## 다음 할 일
- 메인 메뉴 새 게임 → 1번, Feed·File 클리어 후 로비 → 2·3번, 라이브 엔딩 → 4번을 실제로 재생해 확인.
