# UIManager 설계 검토 (지울 수 있는 것 위주)

## 작업 요약 및 방법
- 사용자 요청은 현재 `UIManager` 설계 검토와 삭제 가능한 부분 찾기로 해석했다. 코드 수정·Play 검증은 하지 않았다.
- `UIManager.cs`(작업 트리의 `_isSettingsOpen` 변경 포함), `DialogueDirector`, `StageIntroDialogue`, `DialoguePresenter`, `PlayerFacade`, `GameManager`를 읽었다.
- 코드 호출(`UIManager.Instance.*`)과 씬·프리팹의 UnityEvent(`m_TargetAssemblyTypeName: UI.UIManager`)를 grep으로 확인했다.

## 사용처 확인 결과
| 멤버 | 코드 호출 | UnityEvent |
|---|---|---|
| `PlayCutscene` | StageIntroDialogue | 없음 |
| `PlayDialogue`, `IsDialoguePlaying` | DialogueDirector | 없음 |
| `ShowPetGuide` | 없음 | TestScene TriggerPlate 9곳 |
| `ShowPlayerHud`, `HidePlayerHud` | 없음 | 없음 |
| `HidePetGuide` | 없음 | 없음 |
| `StopDialogue` | UIManager.OnDisable(내부)만 | 없음 |
| `OpenSettings`, `CloseSettings` | 내부 이벤트 구독만 | 없음 |

## 검토 의견 (아직 사용자 확정 아님)
1. 삭제 후보: `ShowPlayerHud`/`HidePlayerHud`/`HidePetGuide`는 사용처 0. `StopDialogue`는 외부 호출이 없어 private 또는 OnDisable 인라인 가능.
2. `DialogueDirector.Play`의 `onFinished` 인자·`_onFinished` 필드는 DialogueCutscene 삭제 후 호출자가 null만 넘긴다. 반환값 bool도 아무도 안 읽는다.
3. `PlayDialogue` 실패 경로: `StartDialogue` 실패 시 `HandleDialogueEnded`가 onFinished를 호출하고 false도 반환해서, Director가 `EndStaging`을 두 번 한다(무해하지만 계약이 헷갈림).
4. `OnEnable`에서 HUD가 없으면 GameManager 구독까지 통째로 건너뛴다. 다른 Presenter는 null 허용인데 HUD만 필수라 일관성이 없다.
5. 입력 경로가 둘(설정: PlayerFacade 이벤트, 대화: PlayerInput 직접). intent-044에서 PlayerFacade 미변경으로 정한 결과라 유지 가능.
6. `#region` 미사용(컨벤션 5-5), 주석 오타 `HandlePlayerSpwned`.
7. 작업 트리의 `_activeSettingsPresenter` → `_isSettingsOpen` 변경은 적절한 단순화(미커밋).

## 반영 (사용자 요청: 5·6번만)
- 5번: `OnEnable`에서 HUD가 없으면 LogWarning만 남기고 계속 진행하게 했다. `ShowPlayerHud`·`HidePlayerHud`·`HandlePlayerSpawned`에 `?.`를 넣었다.
- 6번: 멤버 순서는 그대로 두고 `#region` 7개(Constants ~ Private Methods)로 감쌌다. `HandlePlayerSpwned` 오타를 고쳤다.
- 확인: 컴파일 에러 0. TestScene Play에서 시작 대화와 HUD가 표시되고, 대화 중 ESC로 설정창 열기·닫기 후 대화로 돌아오며, Space로 끝까지 넘기면 조작이 돌아오고, 평소 ESC로 설정창이 열리는 것까지 확인했다. 새 콘솔 에러는 없었다.
- 1~4번은 아직 반영하지 않았다.

## 추가 반영: DialogueDirector → DialogueManager
- 결정: `Instance`로 가져오는 클래스는 Manager 이름만 쓴다(사용자 방침). 그래서 씬당 하나인 `DialogueDirector`를 `DialogueManager`로 이름을 바꿨다. CutsceneManager도 후보였지만 사용자가 DialogueManager를 골랐다.
- `.cs`와 `.meta`를 같이 `git mv` 해서 GUID(36f4da30…)를 유지했다. 그래서 Map_0_Tutorial과 TestScene의 컴포넌트 연결이 끊기지 않는다. 코드 참조와 주석은 UIManager·StageIntroDialogue·DialoguePresenter에서 고쳤다.
- 확인: 컴파일 에러 0. TestScene Play에서 시작 컷씬(1번)이 재생되고 끝까지 넘어가며, "DialogueManager가 없어" 경고가 없었다. Map_0_Tutorial은 Play로 확인하지 않았다.
- 씬 오브젝트 이름과 씬 YAML의 `m_EditorClassIdentifier`는 그대로 뒀다. 동작에는 영향이 없다.

## 추가 반영: SO 이름 정리
- 이름 규칙: "번호로 찾는 목록 = Table, 순서대로 재생하는 대화 하나 = SODialogue". `SOPetGuideTable`은 번호마다 한 줄이라 Table로 그대로 둔다.
- `SOCutsceneTable` → `SODialogueTable`: 스크립트, 메뉴(`UI/Dialogue Table`), 에셋(`ScriptableObjects/UI/SODialogueTable.asset`, Unity rename으로 GUID df949ebf… 유지)을 바꿨다. 내부 struct는 `SDialogueEntry`로 바꿨다.
- `SODialogueSequence` → `SODialogue`: 스크립트와 메뉴(`UI/Dialogue`)를 바꿨다. CameraIndex Tooltip에 남아 있던 `DialogueCutscene`을 `DialogueManager`로 고쳤다.
- 일부러 그대로 둔 것: `_cutsceneTable` 필드, `PlayCutscene`, `TryGetSequence`, 항목 필드 `Sequence`(직렬화 이름), 변수명 `sequence`, Test 폴더의 `SOCutsceneTable_Test.asset` 파일명.
- 확인: 컴파일 에러 0. 에셋 데이터(1번 → SODialogue_Tutorial)와 Canvas_InGame 연결이 유지됐다. TestScene Play에서 시작 컷씬이 재생됐다.
- 남은 것: `SODialogue._id` Tooltip "본 기록 저장 키"는 본 기록을 없앤 뒤라 맞지 않는다(미수정).

## 추가 반영: PlayCutscene → PlayDialogue(int)
- 결정(사용자): 원하는 곳에서 대화를 부르는 함수 이름은 `UIManager.PlayDialogue(int id)`. 기존 `PlayDialogue(SODialogue, onLineChanged, onFinished)`는 `StartDialogue`로 바꿨다(DialogueManager만 쓰는 내부용, 안에서 `DialoguePresenter.StartDialogue`를 부름).
- 고친 곳: `UIManager`(두 함수, `_cutsceneTable` Tooltip), `DialogueManager`(호출·주석), `SODialogueTable`·`StageIntroDialogue`(호출·주석), TestScene `Plate_Cutscene` UnityEvent의 `m_MethodName`(YAML 직접 수정, Git 미포함 파일).
- Map_0_Tutorial은 건드리지 않았다(이 브랜치 방침). 튜토리얼에는 `PlayCutscene` UnityEvent 연결이 없어 영향 없음.
- 확인: 컴파일 에러 0. TestScene Play에서 시작 대화(1번) 재생, `Plate_Cutscene` 발판 UnityEvent → `PlayDialogue(2)` 재생, 경고·에러 없음.

## 추가 반영: 스테이지 시작 대화를 씬 오브젝트 없이 (intent-047)
- 배경: 16:09 develop 머지(PR #97, `feature/Map/Tutorial`)로 튜토리얼 씬이 팀원 버전으로 덮어써져 `DialogueDirector`·`Cutscene_StageIntro`가 사라졌다. 튜토리얼 씬은 이 브랜치에서 수정하지 않는다.
- 결정(사용자): 스테이지 시작 대화는 `StageManager`가 스테이지 종류(`EStageType`)로 번호를 골라 `UIManager.PlayDialogue(번호)`를 부른다(`PlayIntroDialogue`, switch). Core가 UI를 참조하게 되는 것은 사용자 승인.
- 사용자 요청으로 지금은 모든 스테이지에서 시작 대화를 꺼 뒀다(switch에 `_ => 0`만). 켤 때는 `EStageType.Tutorial => 1,`처럼 한 줄 추가.
- `DialogueManager` + `SpeakerCam_Player`·`SpeakerCam_Pet`을 `Canvas_InGame` 프리팹 안으로 옮겼다(TestScene의 Director를 복사, 고정 카메라 목록 비움, 카메라 컴포넌트는 꺼서 저장). Canvas_InGame이 있는 맵 씬(튜토리얼·로비·Feed·File·Security)은 씬 수정 없이 `PlayDialogue`가 연출까지 된다. Map_5_Live에는 Canvas_InGame이 없다.
- 고정 카메라(CameraIndex 0 이상)는 없어졌다. 그런 줄은 말하는 사람 카메라로 나온다.
- `StageIntroDialogue.cs` 삭제. TestScene에서 `DialogueDirector`, `Cutscene_StageIntro`(고정 카메라 2대 포함) 삭제 후 저장.
- 확인: 컴파일 에러 0. TestScene Play(스테이지 FeedApp, IsRunning)에서 시작 대화 없음, `Plate_Cutscene` 발판 → `PlayDialogue(2)` 재생, DialogueManager 부모 = Canvas_InGame, 첫 줄 PET에서 SpeakerCam_Pet 켜짐. 튜토리얼은 Play로 확인하지 않았다(씬 미수정).
- 커밋: 2824eb23(이름 변경), b99ca76c(시작 대화 구조). intent-047은 resolved로 clear/에 이동.

## 추가 반영: Advance → NextLine
- 결정(사용자): 대화 넘기기 입력 이름을 `NextLine`으로. `FID_InputSystem.inputactions` Dialogue 맵 액션 `Advance` → `NextLine`(Space 바인딩·id 유지, 래퍼 코드 생성 안 함).
- UIManager: `ACTION_NEXT_LINE = "Dialogue/NextLine"`, `_nextLineAction`, `HandleNextLine`.
- 안내 문구 쪽: `PetGuideView.ShowNextLineHint`·`HideNextLineHint`, 직렬화 필드 `_advanceHintText` → `_nextLineHintText`(Canvas_InGame 프리팹 YAML의 필드명도 같이 수정, 연결 유지 확인), `DialoguePresenter` Header "Next Line Hint". 프리팹 오브젝트 이름 `AdvanceHint`는 그대로 둠.
- 확인: 컴파일 0에러, 프리팹 `_nextLineHintText` → AdvanceHint 연결 유지, TestScene Play에서 `Dialogue/NextLine` 액션 enabled·구독자 `HandleNextLine`, 호출 시 다음 줄로 넘어감. 에디터가 백그라운드라 Game 창 포커스 규칙(PointersAndKeyboardsRespectGameViewFocus) 때문에 simulate_key Space는 전달되지 않아 실제 키 입력은 확인 못 함.

## 현재 상태 및 이슈
- TestScene(Assets/Test, Git 미포함) 디스크 파일에 삭제된 `Cutscene.DialogueCutscene.Play`를 부르는 TriggerPlate가 남아 있다(Missing 호출). 에디터에서 저장 안 된 변경일 수 있음.

## 다음 할 일 (Next Steps)
- 사용자가 고른 항목만 삭제·정리 후 컴파일 0에러와 TestScene Play(스테이지 시작 대화, 발판 대화, 대화 중 ESC) 확인.
- 사용자 방침: 씬은 TestScene만 열고 수정한다. Map_0_Tutorial 등 다른 씬은 확인용으로도 열지 않는다.
- (완료) Dialogue 입력 맵 `Advance` → `NextLine` 이름 변경. 아래 "추가 반영" 참고. 커밋 안 함.
