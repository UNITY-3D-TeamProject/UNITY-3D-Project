# 대화 중 대화창 외 인게임 UI 숨기기 (intent-058)

## 1. 작업 요약 및 방법
- `UIManager`에 `[SerializeField] CanvasGroup _inGameCanvasGroup` 추가.
  - `StartDialogue`: 상호작용 안내 숨김 다음에 alpha 0.
  - `HandleDialogueEnded`: 상호작용 안내 복구 다음에 alpha 1. (끝까지 보기·`StopDialogue`·UIManager `OnDisable` 모두 이 경로)
- `Canvas_InGame.prefab`
  - 루트 `Canvas_InGame`에 CanvasGroup 추가 → UIManager `_inGameCanvasGroup`에 연결.
  - `PetGuidePanel`: CanvasGroup(`Ignore Parent Groups` 켬) + Canvas(Override Sorting 끔) 추가.
  - `SettingsView/SettingsPanel`: CanvasGroup(`Ignore Parent Groups` 켬) 추가. (원래 자체 Canvas 있음)
- 계층 구조 이동·이름 변경 없음. 다른 사람 스크립트 수정 없음.

## 2. 결정 사항
- 숨기는 방식은 Canvas 루트 CanvasGroup 일괄 처리 (사용자 확정). 새 UI를 `Canvas_InGame`에 추가하면 자동으로 대화 중 숨겨진다. 대화 중에도 보여야 하는 UI는 자체 Canvas + `Ignore Parent Groups` CanvasGroup을 붙인다.
- **주의**: Canvas 루트 오브젝트의 CanvasGroup은 같은 Canvas 안의 자식이 `Ignore Parent Groups`를 켜도 무시되지 않는다. 자식에 중첩 Canvas가 있어야 무시된다 (Play에서 확인).
- `BaseView.Hide()` 재사용은 하지 않음: `PlayerHudView._root`가 빈 `PlayerHUD`라 크로스헤어·스킬·HP가 안 숨겨지고, InfoPanel·BossHealth Presenter가 스스로 `Show()`를 다시 부르기 때문.
- 대화 중 ESC 설정창은 설정창만 보이고 HUD는 대화가 끝날 때까지 숨김. 펫 안내 중에는 숨기지 않음 (사용자 확정).

## 3. 현재 상태 및 이슈
- 컴파일 0에러. TestScene Play(창 백그라운드라 Play 중에만 `Application.runInBackground` 켰다 끔)에서 화면 캡처로 확인:
  - `PlayDialogue(1)` → 대화창만 보임, timeScale 0
  - 대화 중 `OpenSettings` → 설정창·대화창만 보임
  - `CloseSettings` 후 끝까지 넘김(`HandleNextLine` 리플렉션) → 미션 패널·크로스헤어·HP·스킬 복구, timeScale 1
  - `StopDialogue` → 복구 / `ShowPetGuide(1)` → HUD 그대로 보임
- 보스 체력바는 TestScene에 보스전이 없어 화면으로는 확인 못 함 (같은 루트 CanvasGroup 아래라 함께 숨겨짐).
- 실제 E 키 입력·맵 씬에서는 확인 안 함. 커밋 안 함.
- 숨긴 동안 UI는 alpha만 0이고 `blocksRaycasts`는 그대로다. 지금 HUD에는 누를 버튼이 없어 문제 없음.

## 4. 다음 할 일
- 보스전 중 대화가 나오는 맵(`Map_4_Security`)에서 보스 체력바도 숨겨지는지 실제 플레이로 확인.
- 대화 중에도 보여야 할 UI(예: 자막·스킵 안내)를 추가하면 위 "주의"대로 중첩 Canvas + `Ignore Parent Groups`를 붙인다.
