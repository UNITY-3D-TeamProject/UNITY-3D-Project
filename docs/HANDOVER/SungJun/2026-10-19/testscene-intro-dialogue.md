# TestScene 첫 스폰 대화 연출 배치

## 작업 요약 및 방법
- 사용자 요청으로 개인 테스트 씬 `Assets/Test/UnityAssets/Scene/TestScene.unity`에 처음 스폰하면 펫과 대화하는 연출을 넣었다. (intent-044, [이전 기록](../2026-10-18/stage-intro-dialogue.md))
- TestScene에는 이미 StageManager(FeedApp), PlayerSpawner, Canvas_InGame(UIManager에 DialoguePresenter 연결됨), CinemachineBrain가 있어 코드 변경 없이 배치만 했다.
- 추가한 것:
  - `Assets/Test/UnityAssets/Dialogue/SODialogue_TestScene.asset`: Id `TestScene.Intro`, 임시 대사 4줄 (PET Happy / PLAYER Player_00_Default / PET Neutral / PET Default)
  - 씬 루트 `StageIntroDialogue` 오브젝트: StageManager·SO·카메라 2대 연결
  - `DialogueCam_0_Wide`, `DialogueCam_1_PlayerClose`: 스폰 위치 (0, 0.59, 0) 기준, 튜토리얼과 같은 오프셋·Priority 20·FOV 40
- 튜토리얼과 Id가 달라 본 기록(`DialogueSeen.TestScene.Intro`)이 따로 저장된다.

## 결정 사항
- 테스트 씬 전용 데이터라 대사 SO도 `Assets/Test`(팀 Git 미포함, 개인 Test 저장소)에 두었다.
- TestScene의 Canvas_InGame 인스턴스에는 PetGuidePanel을 켜 둔 기존 오버라이드가 있다. 그대로 두었다 (대화가 시작되면 대화 내용으로 덮고, 끝나면 숨긴다).

## 검증 결과
- 컴파일 에러 없음, 새 콘솔 에러 없음.
- Play: 스폰 직후 Dialogue 맵, 카메라 0, PET(Happy) 첫 줄 → Space마다 PLAYER(Player 초상화, 카메라 1) → PET(Neutral, 카메라 0) → PET(Default, 카메라 모두 꺼짐) → 대화 종료 후 Player 맵, `seen=1`.
- 재시작 시 대화 안 나옴. 검증 후 `DialogueSeen.TestScene.Intro`를 지워 두었다.
- 카메라 1 구도는 임시 카메라로 렌더링해 확인했다 (플레이어 정면 + 펫이 화면에 같이 보임, 플레이어가 화면 아래쪽).
- 검증 중 에디터 포커스를 다시 잡을 때 키 이벤트가 하나 더 들어가 한 줄이 넘어간 적이 있었다. 깨끗한 Play에서 다시 돌려 4줄이 순서대로 나오는 것을 확인했다 (테스트 환경 문제).

## 현재 상태 및 이슈
- TestScene과 SO는 `Assets/Test`라 팀 저장소에는 올라가지 않는다. 개인 Test 저장소 커밋은 안 함.
- 대사·카메라 구도는 임시다.

## 다음 할 일 (Next Steps)
- 에디터에서 직접 Play해 보고 대사·카메라 위치 조정
- 팀 공유 씬에 넣을 때는 SO를 `Assets/_Project/ScriptableObjects/UI/`에 만들고 같은 방식으로 배치

## 추가 작업 (같은 날)
- **TestScene에서 두 번째부터 대화가 안 나온 원인:** 본 기록 `DialogueSeen.TestScene.Intro`가 1로 저장돼 "처음 한 번만" 규칙대로 건너뛴 것(버그 아님).
  - `StageIntroDialogue`에 `_shouldAlwaysPlay`(테스트용, 기본 false) 추가. 켜면 본 기록과 상관없이 시작할 때마다 재생한다. TestScene에서만 켰다.
- **말하는 사람별 기본 얼굴:** 대사의 Portrait를 비우면 기본 얼굴을 쓴다 (전에는 초상화를 숨겼다).
  - `DialoguePresenter._defaultPortraits`(SpeakerName → 기본 Sprite). Canvas_InGame 프리팹에 PET → Pet_00_Default, PLAYER → Player_00_Default 지정. SpeakerName은 대사와 글자까지 같아야 한다. 목록에 없는 이름이면 초상화를 숨긴다.
  - `SODialogue_Tutorial`·`SODialogue_TestScene`에서 기본 얼굴과 같은 초상화는 비웠다. 표정이 다른 줄(Surprised, Happy, Neutral)만 초상화가 들어 있다.
- 검증: 본 기록이 1인 상태에서 TestScene Play → 대화 재생, 재시작해도 다시 재생. PLAYER 줄 = Player_00_Default, 마지막 PET 줄 = Pet_00_Default(둘 다 비운 칸에서 기본값). 컴파일·콘솔 에러 없음.
- 튜토리얼은 `_shouldAlwaysPlay`가 꺼져 있어 기존대로 한 번만 나온다. 현재 `DialogueSeen.Tutorial.Intro`=1 상태라 다시 보려면 Edit > Clear All PlayerPrefs.

## 추가 작업 2: 펫 안내 표정 칸
- `SOPetGuideTable`의 항목(`SPetGuideEntry`)에 `Portrait` 칸 추가. 비워 두면 `PetGuidePresenter._petPortrait`(Pet_00_Default), 넣으면 그 얼굴.
- `TryGetMessage(int id, out string message, out Sprite portrait)`로 초상화도 함께 돌려준다. 호출하는 곳은 `PetGuidePresenter` 하나뿐이다.
- 대화 SO와 펫 안내 SO는 합치지 않는다 (안내 = 번호로 한 줄, 조작 안 막음 / 대화 = 여러 줄 순서, 카메라·조작 차단·본 기록).
- 기존 `SOPetGuideTable.asset`은 바꾸지 않았다 (9개 항목 모두 표정 칸 비어 있음 = 기본 얼굴).
- 검증: Play에서 `ShowPetGuide(1)`·`(3)` → Pet_00_Default, 메모리 복사본에서 2번에 Pet_01_Happy를 넣고 `ShowPetGuide(2)` → Pet_01_Happy. 에셋 변경 없음, 컴파일·콘솔 에러 없음.
- 플레이어 혼자 하는 독백도 대화 SO로 만들 수 있다 (모든 줄 SpeakerName = PLAYER). 다만 지금 재생 계기는 스테이지 시작(`StageIntroDialogue`)뿐이라, 스테이지 중간 특정 위치에서 독백을 띄우려면 트리거 컴포넌트가 따로 필요하다.

## 추가 작업 3: 펫이 앞에 나와 플레이어를 보고, 말하는 사람을 비추는 카메라
- **펫 배치 (펫 스크립트 수정 없음):** 펫(`PetFollower`)은 플레이어 안의 `PetPivot`을 따라가고, 멈추면 그 pivot의 앞쪽을 본다. 대화 시작 시 `StageIntroDialogue`가 `PetSpawner.Pivot`을 플레이어 앞 `_petStandOffset`(오른쪽 0, 위 1.2, 앞 1.8 m)으로 옮기고 플레이어 쪽으로 돌려 두며, 대화가 끝나면 원래 local 위치·회전으로 되돌린다. 평소 pivot 앞쪽 = 플레이어가 보는 방향이라 이를 기준 방향으로 쓴다.
- **말하는 사람 카메라:** `StageIntroDialogue` 아래 `SpeakerCam_Player`, `SpeakerCam_Pet`(CinemachineCamera, Priority 20, FOV 35, 평소 꺼짐)을 두고, 대화 시작 때 실제 플레이어·펫 위치로 배치한다.
  - 플레이어가 말할 때: 펫 뒤(`_playerShotOffset` 오른쪽 0.5, 위 0.1, 앞 0.9)에서 플레이어 얼굴(발 + 1.2 m)을 본다.
  - 펫이 말할 때: 플레이어 어깨 뒤(`_petShotOffset` 오른쪽 0.7, 위 1.6, 뒤 1.0)에서 펫을 본다.
- **CameraIndex 의미 변경:** -1 = 말하는 사람 카메라(자동), 0 이상 = `Fixed Cameras` 배열의 고정 카메라. 두 대사 SO의 CameraIndex는 모두 -1로 바꿨다. 기존 고정 카메라(`DialogueCam_0_Wide`, `_1_PlayerClose`)는 씬에 남겨 두었다 (특정 줄을 고정 구도로 찍을 때 사용).
- 말하는 사람 판별은 SpeakerName 문자열(`_playerSpeakerName`=PLAYER, `_petSpeakerName`=PET)로 한다.
- `DialoguePresenter.OnLineChanged`와 `UIManager.PlayDialogue`의 줄 콜백이 카메라 번호 대신 `SDialogueLine` 전체를 넘긴다.
- 적용 씬: TestScene, Map_0_Tutorial (둘 다 저장).
- 검증 (Play): 대화 시작 시 펫이 플레이어 앞 1.8 m에서 플레이어를 정면으로 봄(각도 0°). PET 줄 → SpeakerCam_Pet(플레이어 머리 너머로 펫 얼굴), PLAYER 줄 → SpeakerCam_Player(펫 등 너머로 플레이어 얼굴), 두 씬 모두 화면으로 확인. 대화 종료 후 pivot local (-0.56, 0.80, 1.20)/(90, 0, 0)으로 복구, 카메라 꺼짐, Player 맵. 컴파일·콘솔 에러 없음. 튜토리얼 본 기록은 지워 두었다.
- 주의: 연출 카메라 배치는 대화 시작 순간의 위치 기준이다. 대화 중 플레이어는 못 움직이므로 문제없지만, 펫 앞쪽에 벽이 있으면 펫이 벽에 묻힐 수 있다 (펫 추종에는 벽 검사가 없음).
- 플레이어 HUD(크로스헤어 등)는 대화 중에도 보인다. 숨길지는 미정.

## 추가 작업 4: 원하는 위치(발판)에서 대화 컷씬 재생
- **구조 분리:** `StageIntroDialogue` 안에 있던 연출을 나눴다.
  - `Cutscene.DialogueDirector` (씬에 1개): 펫 앞세우기·복구, 말하는 사람 카메라 2대, `UIManager.PlayDialogue` 호출. `Play(대화, 고정 카메라, 종료 콜백)`. 대화는 Play를 부른 순간의 플레이어 위치·몸 방향 기준으로 연출한다.
  - `Cutscene.DialogueCutscene` (컷씬마다): 대사 SO, 그 컷씬 전용 고정 카메라, 항상 재생 옵션, 본 기록(`DialogueSeen.{Id}`), `public void Play()`. 이미 대화 중이면 무시한다.
  - `StageIntroDialogue`: 스테이지 시작 때 연결한 `DialogueCutscene.Play()`만 부른다.
- **발판 연결:** 팀 공용 `TriggerPlate`의 `On Player Entered`에 `DialogueCutscene.Play`를 연결하면 된다 (코드 없이 인스펙터 연결).
- **씬 정리:** 튜토리얼·TestScene 모두 기존 `StageIntroDialogue` 오브젝트를 `DialogueDirector`로 이름 바꾸고(말하는 사람 카메라 유지), `Cutscene_StageIntro`(DialogueCutscene + StageIntroDialogue + 고정 카메라 2대)를 새로 만들었다. 튜토리얼은 항상 재생 꺼짐, TestScene은 켜짐.
- **TestScene 시험 공간 `CutsceneTest`:** 펫 안내 구역 북쪽 (-22, 0, 44), 바닥 24x26(펫 안내 바닥과 이어짐), 입구 팻말 "컷씬 시험", 발판 `Plate_Cutscene`(TriggerPlate, 한 번만 끔) → `Cutscene_Plate.Play`, 팻말 "대화 컷씬"은 연출 화면을 가리지 않게 발판 왼쪽 뒤(-25.5, 42.6)에 둠. 대사 `Assets/Test/UnityAssets/Dialogue/SODialogue_TestPlate.asset`(Id `TestScene.Plate`, 4줄, 항상 재생 켬).
- **검증 (Play):** 시작 대화 정상 종료 후 플레이어를 발판 위에 두고 발판 `OnTriggerEnter`를 플레이어 콜라이더로 호출 → Dialogue 맵, 첫 줄 PET, 펫이 플레이어 앞 1.8 m에서 플레이어를 봄(0°), PET 줄 = SpeakerCam_Pet, PLAYER 줄 = SpeakerCam_Player 화면 확인, 끝나면 pivot 복구·Player 맵·본 기록 저장, 항상 재생이라 다시 밟으면 재생. 튜토리얼도 분리 후 시작 대화 정상 재생.
  - 에디터가 백그라운드라 Play 프레임이 멈춰서, 실제로 걸어가 밟는 대신 발판 트리거 함수를 직접 불렀고 프레임은 일시정지 + Step으로 진행했다. 실제 걸어서 밟는 확인은 사용자 에디터에서 필요.
  - 대화 중 화면이 말하는 사람 카메라로 바뀌어도 플레이어 몸은 돌지 않음(90프레임 동안 펫 pivot 위치 변화 없음).
- 주의: 펫 위치는 몸(SK_ZMike) 기준 자식이라 몸이 돌면 따라 돈다. 루트 회전을 강제로 바꾸면 회전 시스템이 몸을 원래 방향으로 돌려 카메라 배치와 어긋날 수 있다 (일반 플레이에서는 해당 없음).

## 추가 작업 5: DialoguePresenter 이름 정리와 넘기기 안내
- **이름 정리:** `IsPlaying`→`IsDialoguePlaying`, `OnLineChanged`→`OnDialogueLineChanged`, `OnFinished`→`OnDialogueEnded`, `Play`→`StartDialogue`, `Next`→`ShowNextLine`, `Stop`→`StopDialogue`, `Finish`→`EndDialogue`, `_sequence`/`_lineIndex`→`_currentDialogue`/`_currentLineIndex`, `_view`→`_petGuideView`, `_defaultPortraits`→`_speakerDefaultPortraits`. 직렬화 필드 2개는 `[FormerlySerializedAs]`로 프리팹 값 유지 후 Canvas_InGame 재저장. UIManager 호출부와 `HandleDialogueFinished`→`HandleDialogueEnded` 함께 변경. `//` 주석 추가(summary 없음).
- **넘기기 안내:** PetGuidePanel 오른쪽 아래 `AdvanceHint`(TMP, Pretendard-SemiBold 16, 이름 라벨 시안 85%, 평소 꺼짐). 대화 중 줄마다 `Space ▶ 다음`, 마지막 줄은 `Space ▶ 닫기`(문구는 DialoguePresenter `_nextLineHint`/`_lastLineHint`). 펫 안내(`ShowPetGuide`)에서는 숨김.
  - `PetGuideView`: `_advanceHintText`, `ShowAdvanceHint(문구)`, `HideAdvanceHint()`.
- 검증 (TestScene Play): 1~3줄 "다음", 4줄 "닫기", 종료 후 패널 숨김, `ShowPetGuide(1)` 안내 숨김. 게임 뷰 캡처로 위치 확인. 콘솔 에러 없음.
- 문구의 "Space"는 표시용 글자다. Dialogue 맵의 Advance 키를 바꾸면 문구도 같이 바꿔야 한다.
