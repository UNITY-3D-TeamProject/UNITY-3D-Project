# UI 스크립트 폴더 기능별 정리

## 작업 요약 및 방법
- `Assets/_Project/Scripts/UI/`에 평평하게 있던 스크립트 16개를 기능별 하위 폴더로 나눴다.
- Unity `AssetDatabase.MoveAsset`으로 옮겨 `.meta` GUID를 그대로 유지했다 (씬·프리팹·SO 연결 유지).
- 코딩 컨벤션(네임스페이스 = `Scripts/` 아래 폴더 경로)에 맞춰 네임스페이스도 바꿨다 (사용자 선택).

| 폴더 | 파일 | 네임스페이스 |
|---|---|---|
| `UI/` | UIManager | `UI` |
| `UI/PlayerHud/` | PlayerHudPresenter, PlayerHudView | `UI.PlayerHud` |
| `UI/Settings/` | SettingsPresenter, SettingsView | `UI.Settings` |
| `UI/MainMenu/` | MainMenuPresenter, MainMenuView | `UI.MainMenu` |
| `UI/PetGuide/` | PetGuidePresenter, PetGuideView, SOPetGuideTable | `UI.PetGuide` |
| `UI/Dialogue/` | DialoguePresenter, SODialogueSequence | `UI.Dialogue` |
| `UI/InfoPanel/` | InfoPanelPresenter, InfoPanelView | `UI.InfoPanel` |
| `UI/Monologue/` | MonologuePresenter, MonologueView | `UI.Monologue` |

- 추가한 `using`: UIManager(`UI.Dialogue`, `UI.PetGuide`, `UI.PlayerHud`, `UI.Settings`), DialoguePresenter(`UI.PetGuide`), MainMenuPresenter(`UI.Settings`), Cutscene/DialogueDirector(`UI.Dialogue`), Cutscene/DialogueCutscene(`UI` → `UI.Dialogue`).
- UI 폴더 밖에서 UI 타입을 쓰는 코드는 Cutscene 스크립트 2개뿐이었다 (Test 저장소 스크립트 포함 검색).

## 결정 사항
- UIManager는 여러 기능을 묶는 진입점이라 `UI/` 바로 아래, 네임스페이스 `UI`에 둔다.
- 기능 하나 = 폴더 하나 = 네임스페이스 하나. View·Presenter·전용 SO를 같은 폴더에 둔다.

## 검증 결과
- 컴파일 에러 0.
- Canvas_InGame·Canvas_Legacy·Canvas_MainMenu 프리팹 누락 스크립트 0, 각 컴포넌트가 새 네임스페이스 타입으로 로드됨.
- SOPetGuideTable·SODialogue_Tutorial·SODialogue_TestPlate가 새 타입으로 로드됨. TestScene 누락 스크립트 0.
- TestScene Play: UIManager·PlayerHudPresenter 정상, 시작 대화 재생·Dialogue 맵 전환 정상, 콘솔 에러 없음.
- git이 이동을 rename으로 인식함 (`UI/{ => Settings}/SettingsView.cs.meta` 등).

## 현재 상태 및 이슈
- 커밋 안 함.
- 다른 브랜치에서 `Scripts/UI/` 파일(예: PlayerHudPresenter, 다른 팀원도 수정 이력 있음)을 고치고 있으면 병합 시 경로·네임스페이스 충돌이 날 수 있다. git rename 감지로 대부분 따라가지만, 그 브랜치 코드에서 `UI.PlayerHudPresenter`처럼 옛 네임스페이스로 참조하면 using 추가가 필요하다.
- MainScene·튜토리얼 등 다른 씬은 열어서 확인하지 않았다 (GUID가 그대로라 연결은 유지된다).

## 다음 할 일 (Next Steps)
- 팀원에게 UI 폴더·네임스페이스 변경 공유
- 커밋 시 이동(rename)과 using 변경을 한 커밋으로 묶는다 (나누면 중간 커밋이 컴파일되지 않음)
