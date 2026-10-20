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

## 현재 상태 및 이슈
- TestScene(Assets/Test, Git 미포함) 디스크 파일에 삭제된 `Cutscene.DialogueCutscene.Play`를 부르는 TriggerPlate가 남아 있다(Missing 호출). 에디터에서 저장 안 된 변경일 수 있음.

## 다음 할 일 (Next Steps)
- 사용자가 고른 항목만 삭제·정리 후 컴파일 0에러와 TestScene Play(스테이지 시작 대화, 발판 대화, 대화 중 ESC) 확인.
