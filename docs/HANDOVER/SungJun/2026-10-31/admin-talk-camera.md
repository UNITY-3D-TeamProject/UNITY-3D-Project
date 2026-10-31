# 미로 관리자 대화 카메라 (intent-108)

## 작업 요약 및 방법
- 문제: 미로 2·3라운드 컷씬에서 대사가 시작되면 `CutsceneDirector.BeginStaging()`이 펫을 플레이어 앞 1.8m에 세워 관리자(앞 3m)를 가리고, 모든 줄이 카메라 키 `FileMazeStaging`(플레이어 등 뒤 고정) 하나라 플레이어-펫 대화처럼 보였다.
- `SOCutscene.SCutsceneStep.IsPartnerTalk`(줄 단위) 추가 → `CinematicManager` → `ICutsceneController.ShowCutsceneLine(..., isPartnerTalk)` → `UIManager` 로 전달.
- `UIManager.ShowCutsceneLine`: 상대 대화 줄이면 `BeginPartnerTalk()`, 아니면 처음 한 번만 `BeginStaging()`(펫 세우기, 새 플래그 `_isCutscenePetStaged`). 끝날 때 `EndStaging()` 한 번.
- `CutsceneDirector`:
  - `BeginPartnerTalk()`: 펫은 건드리지 않고, 플레이어 정면 기준 `_partnerLookOffset`(0, 2.1, 3) 을 관리자 몸 가운데로 보고 `_partnerShotCamera`(관리자 카메라, 플레이어 어깨 너머 `_partnerShotOffset` 0.8, 1, -1.6)와 `_playerShotCamera`(관리자 등 너머 역샷 `_partnerPlayerShotOffset` 1.4, -0.1, 3.2 / 바라볼 높이 `_partnerPlayerLookHeight` 1.1)를 놓는다. 두 위치 모두 플레이어 가슴에서 SphereCast로 벽 앞까지 당김(플레이어·펫 무시).
  - `ShowPartnerTalkLine(speaker)`: 코리·비트 → 플레이어 역샷, 그 밖(관리자) → 관리자 카메라.
  - 공통 준비를 `PrepareStaging()`으로 빼고, 플레이어 Animator 원래 모드를 두 번 저장하지 않도록 막음.
- `Canvas_InGame.prefab`: `CutsceneDirector/SpeakerCam_Partner`(펫 카메라 복제, Priority 20, 꺼진 상태) 추가·연결, 위 오프셋 값 저장.
- SO: `SOCutscene_File_Round2Start` 7줄 전부, `SOCutscene_File_Round3Start` 0·1번(관리자) 줄에 `IsPartnerTalk` 켜고 `CameraKey` 비움. 3라운드 2번 이후(파일 전달 샷·비트 대사·도둑)는 그대로.
- 이름 칸 `PetGuideView/NameLabel` 폰트가 영문 전용 `Rajdhani-Bold SDF`라 한글 화자 이름(비트·코리·관리자)이 `□□`로 나오던 것을 `_Project/Art/Fonts/Pretendard-Bold SDF`로 교체(지난 커밋 14abaf7e 의 이름 변경으로 드러난 문제).

## 결정 사항
- 관리자 생성 코드(`Map/Maze/FileMazeCutsceneStaging.cs`, 양준범)는 수정하지 않는다. 관리자 위치는 그 생성 규칙(플레이어 정면 3m·위 1.4m, Idle 자세에서 몸 가운데 약 +0.7m)에 맞춘 오프셋이므로, 생성 규칙이 바뀌면 `_partnerLookOffset` 을 같이 바꿔야 한다.
- 상대 대화 여부는 컷씬이 아니라 줄 단위로 지정한다(3라운드처럼 관리자가 중간에 떠나는 컷씬 때문).

## 현재 상태 및 이슈
- 리컴파일 통과. TestScene Play 에서 Spook 을 같은 규칙으로 직접 놓고 2라운드 SO 재생: 비트 줄 → 역샷(관리자 등이 오른쪽 위에 걸리고 플레이어 정면), 관리자 줄 → 관리자 카메라(플레이어 머리 왼쪽 아래, 관리자 얼굴 정면) 화면 캡처 확인, 펫 위치 그대로, 이름 `비트`/`관리자` 정상 표시. 3라운드 SO: 관리자 2줄 뒤 비트 줄에서 펫이 앞으로 나오고, 끝나면 펫 위치·Animator 모드·카메라 모두 원상 복구 확인.
- 실제 `Map_3_File` 미로 안에서는 확인하지 않음(씬 미오픈). 좁은 통로·막다른 길에서는 역샷 카메라가 관리자 뒤 6m 쪽 벽에 막혀 당겨지면 관리자 몸에 가릴 수 있다.
- 에디터는 확인을 위해 TestScene 으로 바꿔 둔 상태(사용자가 열어 둔 `Map_3_File` 은 저장 안 된 변경 없음 확인 후 전환).
- 커밋 안 함. 작업 전부터 있던 `Pretendard-Bold SDF.asset`, `Map_3_File.unity`, `SOMazeChainSettings_File.asset`, `ShaderGraphSettings.asset` 변경은 건드리지 않음.

## 다음 할 일
- 미로 2라운드에서 실제로 컷씬을 보고 두 구도와 벽 근처 동작 확인, 필요하면 `CutsceneDirector` 인스펙터의 Partner Talk 값 조정.
