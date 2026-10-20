# 스테이지 첫 입장 대화 연출 (플레이어 ↔ 펫 + 시네머신) 1단계

## 작업 요약 및 방법
- 사용자 요청: 스테이지·튜토리얼에 처음 들어가면 플레이어와 펫이 대화하고, 대화 중 시네머신 카메라가 움직이며, 조작은 막고 Space로만 다음 대사로 넘긴다.
- 설계 질문 4개를 사용자에게 확인한 뒤 구현했다 (intent-044).
  - 카메라: 대사마다 카메라 번호 지정 (Timeline 안 씀)
  - 대화창: 기존 `PetGuidePanel` 재사용 + 이름·초상화 교체
  - 재생: 스테이지별 처음 한 번만 (PlayerPrefs)
  - 시간: timeScale 1 유지, 입력만 차단
- 흐름: `StageManager.OnStageStarted` → `StageIntroDialogue`(씬) → `UIManager.PlayDialogue` → UIManager가 플레이어의 `PlayerInput`을 직접 참조해 `Dialogue` 맵으로 전환 → `Dialogue/Advance`(Space)로 `DialoguePresenter.Next()` → `PetGuideView.ShowLine()`. 줄이 바뀌면 카메라 번호를 콜백으로 돌려줘 씬 컴포넌트가 해당 CinemachineCamera만 켠다.
- 입력 방식 변경 이력: ① Dialogue 맵 + PlayerFacade에 전환 함수·이벤트 추가 → ② PlayerFacade가 다른 파트라 되돌리고 UI 맵 + DialoguePresenter 자체 Space 액션 → ③ 대화 중 ESC로 설정창을 열어야 해서(UI 맵은 ESC가 닫기) 최종적으로 Dialogue 맵 + UIManager가 PlayerInput 직접 참조. PlayerFacade는 최종적으로 변경 없음.

## 결정 사항
- `FID_InputSystem`에 `Dialogue` 맵 추가: `Advance`(Space), `OpenSettings`(ESC). 대화 중에만 이 맵을 켜서 이동·시점·점프·스킬·사격을 막는다.
- PlayerFacade는 바꾸지 않는다. UIManager가 `BindPlayerInput`에서 `playerFacade.TryGetComponent(out PlayerInput)`로 PlayerInput을 받아 두고, Dialogue 맵 전환(`SwitchCurrentActionMap("Dialogue")`)과 두 액션 구독을 직접 한다. Player/UI 맵 전환은 기존대로 PlayerFacade 함수를 쓴다.
- 입력 맵 흐름: 대화 시작 → Dialogue, ESC → 설정창 열기(UI 맵, Pause), 설정창 닫기 → 대화 중이면 Dialogue·아니면 Player, 대화 끝 → Player. 설정창 동안은 Dialogue 맵이 꺼져 있어 Space로 대사가 넘어가지 않는다.
- UI(Canvas 프리팹)는 카메라를 모르고, 씬 컴포넌트(`Cutscene.StageIntroDialogue`)는 UI 내부를 모른다. 카메라는 씬 오브젝트라 씬 컴포넌트가 가진다.
- 대사 데이터는 `UI.SODialogueSequence`(`Id` + `SDialogueLine[]`: SpeakerName, Portrait, CameraIndex, Message). CameraIndex -1 = 플레이어 카메라. Portrait가 비면 초상화 칸을 숨긴다.
- 본 기록 키: `PlayerPrefs "DialogueSeen." + Id`. 대화가 끝나거나(`StopDialogue`로) 중단돼도 본 것으로 저장한다.
- 펫 안내(`ShowPetGuide`)와 대화가 같은 패널을 쓰므로 `PetGuidePresenter`에 `_petName`(PET)·`_petPortrait`(Pet_00_Default)를 두고 안내할 때마다 이름·초상화를 다시 넣는다.
- 대화 중 플레이어가 교체되면 `BindPlayerInput`이 새 PlayerInput의 액션을 다시 구독하고 Dialogue 맵(설정창이 열려 있으면 UI 맵)을 다시 건다.
- 연출 카메라는 `StageIntroDialogue.Awake`에서 모두 끈다. 작업 중 켜 둔 채 저장해도 플레이어 카메라를 덮지 않는다. Priority 20 (플레이어 Base 0, Aim 10).

## 변경된 파일
- `Assets/FID_InputSystem.inputactions`: `Dialogue` 맵(`Advance` Space, `OpenSettings` ESC)
- `Scripts/UI/SODialogueSequence.cs`(새), `Scripts/UI/DialoguePresenter.cs`(새, 입력은 모르고 `Next()`만)
- `Scripts/UI/PetGuideView.cs`: `_nameText`, `_portraitImage`, `ShowLine()`
- `Scripts/UI/PetGuidePresenter.cs`: 펫 이름·초상화를 넣어 `ShowLine` 호출
- `Scripts/UI/UIManager.cs`: `_dialoguePresenter`, `IsDialoguePlaying`, `PlayDialogue()`, `StopDialogue()`, PlayerInput 직접 참조·Dialogue 맵 전환·Advance/OpenSettings 구독, 설정창 닫을 때 대화 중이면 Dialogue 맵 복귀
- `Scripts/Cutscene/StageIntroDialogue.cs`(새, 새 폴더)
- `Prefabs/UI/Canvas_InGame.prefab`: PetGuidePanel에 DialoguePresenter 추가, View의 NameLabel·Portrait 연결, UIManager 연결
- `Art/UI/PlayerPortraits/Player_00_Default.png`(새): Player.prefab(MikeZ) 얼굴을 512x512 투명 배경으로 렌더링. 프리뷰 씬에 프리팹을 넣고 `Idle_Aggro`·`Blinking`(0초, 눈 뜬 상태)을 샘플링한 뒤 정면(+Z)에서 FOV 20, 거리 2.1, head_01 + 0.24 높이를 찍었다. 임포트 설정은 Pet_00_Default와 같다.
- `ScriptableObjects/UI/SODialogue_Tutorial.asset`(Id `Tutorial.Intro`, 임시 대사 4줄, PLAYER 줄에 Player_00_Default 초상화)
- `Scenes/Map_0_Tutorial.unity`: `StageIntroDialogue` 오브젝트 + `DialogueCam_0_Wide`, `DialogueCam_1_PlayerClose`(스폰 지점 (-10.66, 8.23, 4.23) 기준 고정 구도)
- `docs/intent/intent-044-stage-intro-dialogue.md`, `INTENT_POINTER.md`

## 검증 결과
- Unity recompile: failed=false, errors 0.
- `Map_0_Tutorial` Play:
  - (최종 방식 재검증) 스폰 직후 대화 시작, 입력 맵 `Dialogue`, 활성 카메라 `DialogueCam_0_Wide`, 대화창에 PET·초상화·첫 대사 표시 (게임 뷰 캡처로 확인)
  - 대화 중 W·Q 누름 → 이동·스킬 요청 0건
  - 대화 중 ESC → 설정창 열림(UI 맵, Pause, timeScale 0), 설정창 동안 Space·W 무시(대사 안 넘어감), ESC로 닫으면 Dialogue 맵·Playing 복귀 후 Space로 이어서 진행
  - 대화가 끝난 뒤 ESC → 설정창 열림, 다시 ESC → Player 맵 복귀 (기존 동작 그대로)
  - Space마다 다음 줄: PLAYER 줄은 Player_00_Default 초상화 + 카메라 1 (게임 뷰 캡처로 확인), PET 줄은 카메라 0, 마지막 줄(-1)은 카메라 모두 꺼짐
  - 마지막 Space → 패널 숨김, Player 맵 복귀, `seen=1`, 대화를 끝낸 Space가 점프로 새지 않음. 끝난 뒤 W 이동·Space 점프 정상
  - W를 누른 채 대화가 시작되면 Move가 (0,0)으로 풀림 → 이동값 잔류 없음 (별도 처리 불필요)
  - Play 재시작 → 대화 안 나옴, 플레이어 BaseCamera
  - 회귀: `ShowPetGuide(1)` → PET·Pet_00_Default·1번 문구, `OpenSettings/CloseSettings` → Pause/UI ↔ Playing/Player 정상
- 검증 중 주의: 에디터가 포커스를 잃으면 Input System이 키보드를 끄고(`ResetAndDisableNonBackgroundDevices`) 게임 뷰 포커스가 없으면 키 입력을 버린다. 그래서 `simulate_key`가 안 먹었고, eval 안에서만 설정을 잠깐 바꾸고 키보드를 켠 뒤 `QueueStateEvent` + `InputSystem.Update()`로 확인했다. 설정은 되돌렸고 파일 변경 없음.
- 플레이어 입력은 `PlayerInputComponent`의 move/jump/skill 콜백을 감싸 기록하는 방식으로 확인했고 끝나면 원래 콜백으로 되돌렸다.
- 검증 끝에 `DialogueSeen.Tutorial.Intro`를 지워 두었다 (다음 Play에서 대화가 다시 나옴).

## 현재 상태 및 이슈
- UIManager가 PlayerFacade를 거치지 않고 PlayerInput을 직접 참조한다. PlayerFacade를 '플레이어와 외부의 단일 창구'로 둔 팀 의도와는 다르므로, 팀원에게 공유가 필요하다.
- 설정창을 대화 중에 열면 timeScale 0이라 카메라 블렌드도 멈췄다가 닫으면 이어진다.
- 커밋 안 함. `ProjectSettings/*`, `UNITY-3D-Project.slnx` 변경은 작업 전부터 있던 것으로 이번 작업과 무관.
- 대사 4줄과 연출 카메라 2대 구도는 **임시**다. 사람이 게임 뷰에서 구도를 직접 본 것은 첫 줄 캡처뿐이다.
- Brain 기본 블렌드가 EaseInOut 0.2s라 카메라 전환이 빠르다. 느리게 하려면 `Main Camera.prefab`에 CustomBlends 에셋이 필요하다 (공용 프리팹이라 손대지 않음).
- 연출 카메라는 고정 위치다. 스폰 지점이 바뀌면 카메라도 옮겨야 한다.
- 펫은 평소처럼 플레이어를 따라다닌다. 대화 중 펫을 특정 위치에 세우는 연출은 없다.
- 시간이 흐르므로 적이 있는 스테이지에서는 대화 중 공격받지 않는 곳에서 시작해야 한다.

## 다음 할 일 (Next Steps)
- 에디터에서 `Map_0_Tutorial` Play로 대화·카메라 구도 확인, 실제 대사로 `SODialogue_Tutorial` 교체, `DialogueCam_*` 위치 조정
- 다른 스테이지: 대사 SO(겹치지 않는 Id) + 씬에 `StageIntroDialogue`와 카메라 배치
- 필요하면 블렌드 시간(CustomBlends), 펫 위치 지정(`PetFollower.MoveAndHold`)
- 확정되면 intent-044를 resolved로 바꾸고 `docs/intent/clear/`로 이동
