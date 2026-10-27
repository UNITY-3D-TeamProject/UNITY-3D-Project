---
id: intent-076
title: 메인 메뉴 버튼에 마우스 올림·클릭 효과음 추가
part: Audio / UI / MainMenu
status: resolved   # open | resolved
created: 2026-10-26
resolved: 2026-10-26
---

## 문제 (Problem)
`MainScene` 메인 메뉴 버튼(Start·Continue·Settings·Exit)에 마우스를 올리거나 눌러도 소리가 나지 않는다.
`AudioManager`(UI 소스 + `PlayUi`)와 `SOAudioData`는 이미 있고 `MainScene`에 배치도 끝났지만, 버튼 쪽에서 `PlayUi`를 부르는 곳이 하나도 없다.
`SOAudioData_UiClick`도 만들어만 두고 아무 데서도 참조하지 않는다.

## 기대 결과 (Proposed outcome)
- 버튼에 마우스를 올리면 `Click_16`, 누르면 `Click_18`이 UI 채널로 난다.
- 버튼 하나에 컴포넌트 하나만 붙이면 되고, 어떤 소리를 낼지는 인스펙터에서 SO로 고른다.
  같은 컴포넌트를 나중에 설정창·인게임 버튼에도 그대로 붙일 수 있다.

### 설계
1. **SO 2개** (`Assets/_Project/ScriptableObjects/Audio/Ui/`)
   - `SOAudioData_UiHover` — 클립 `Click_16` (새로 생성)
   - `SOAudioData_UiClick` — 클립을 `Click_18`로 교체 (기존 에셋 재사용, 현재 참조처 없음)
2. **컴포넌트 `UIButtonSound`** (`Scripts/UI/UIButtonSound.cs`, `namespace UI`)
   - `[RequireComponent(typeof(Button))]`, `IPointerEnterHandler` 구현
   - `[SerializeField] SOAudioData _hoverSound`, `_clickSound`
   - 마우스 올림: `OnPointerEnter`에서 버튼이 `interactable`일 때만 `AudioManager.Instance?.PlayUi(_hoverSound)`
   - 클릭: `OnEnable`/`OnDisable`에서 `Button.onClick`에 구독·해제 → 비활성 버튼(저장 없는 Continue)은 클릭음도 안 남
   - SO가 비어 있으면 그 소리만 건너뜀 (AudioManager가 없는 씬에서도 오류 없음)
3. **씬 연결** — `Canvas_MainMenu/Menu/MenuButtons/` 아래 버튼 4개에 `UIButtonSound`를 붙이고 SO 2개 연결
4. **스플래시**: `MainMenuPresenter`에 `_splashPressSound` 필드를 추가하고, 아무 키 입력으로 메뉴가 뜰 때 `PlayUi`로 낸다 (`SOAudioData_UiClick` 연결).
5. `MainMenuView`·`AudioManager`는 수정하지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 메인 메뉴에서 버튼을 조작하는 플레이어
- 어떤 시스템/모듈이 건드려지는가: `MainScene`(버튼 4개에 컴포넌트 추가), 새 스크립트 1개, UI SO 2개

## 제약 (Constraints)
- `Assets/ThirdParty/`·`Assets/Test/`는 Git에 안 올라가므로, SO가 참조하는 클립 위치를 팀원 모두가 가진 곳으로 정해야 한다.
- 현재 사운드팩이 `Assets/UI Casual Soundpack/`(루트, 미추적)에 들어와 있다.

## 열린 질문 (Open questions)
- ~~사운드팩 위치~~ → 사용자가 `Assets/ThirdParty/UI Casual Soundpack/`으로 옮김 (SVN 공유 필요)
- ~~범위~~ → 메뉴 버튼 4개 + 설정창 `CloseButton`
- ~~스플래시~~ → 아무 키 입력 시 클릭음(`Click_18`)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `UI.UIButtonSound`(마우스 올림 `IPointerEnterHandler` + `Button.onClick` 구독, interactable일 때만) 를 `MainScene`의 `Canvas_MainMenu` 버튼 5개에 씬 오버라이드로 추가(프리팹에는 Apply 안 함). `SOAudioData_UiHover`(Click_16) 생성, `SOAudioData_UiClick` 클립을 Click_18로 교체. 스플래시 소리는 `MainMenuPresenter._splashPressSound`.
- 검증: Play에서 스플래시 Space 입력·Settings 마우스 올림/클릭·CloseButton 클릭 때 `UiSource` 재생 확인, 비활성 Continue 마우스 올림은 무음 확인. 실제 귀로 듣기는 미확인.
- 후속: 인게임 설정창(`Canvas_InGame.prefab`)의 CloseButton·MainMenuButton·QuitButton에도 `UIButtonSound` 추가(프리팹 자체 수정 → Map_0~5·Map Total test 모두 적용). 단, 맵 씬에는 `AudioManager`가 없어 아직 소리가 나지 않음. 배치는 사용자 결정으로 하지 않고 맵 담당이 따로 함(intent-038 남은 일과 같음).
- 관련 커밋/PR: 커밋 전
