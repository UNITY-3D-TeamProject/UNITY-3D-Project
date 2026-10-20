# 2026-10-20 — UI View 공통 부모 BaseView 추가 (intent-052)

## 1. 작업 요약 및 방법
- `Assets/_Project/Scripts/UI/BaseView.cs` 추가 (namespace `UI`, `abstract class BaseView : MonoBehaviour`)
  - `[Header("Root")] _root`, `IsVisible`, public `Show()`·`Hide()`, private `SetVisible(bool)`
- `InfoPanelView`·`PlayerHudView`·`PetGuideView`·`SettingsView`가 `BaseView`를 상속하고, 각자 들고 있던 `_root`·`IsVisible`·`Show`·`Hide`·`SetVisible`을 지움
  - `PetGuideView.Show(string)`은 문구를 넣은 뒤 `Show()`를 부르도록 변경 (오버로드 유지)
  - `SettingsView`는 `IsVisible`이 새로 생김 (기능 추가만, 동작 변화 없음)
- `MainMenuView`는 그대로 둠

## 2. 결정 사항
- BaseView에는 루트 켜기·끄기만 둔다. 버튼 리스너 등록·해제는 View마다 달라 공통화하지 않는다.
- `SetVisible`은 private. 밖에서는 `Show()`·`Hide()`·`IsVisible`만 쓴다 (작업 전 View 밖 `SetVisible`·`IsVisible` 호출 0곳 확인).
- `IsVisible`은 지금 쓰는 곳은 없지만 사용자 결정으로 BaseView에 둔다. 나중에 "대화가 시작되면 대화창 외 떠 있는 UI 전부 숨기기"를 넣을 때 원래 떠 있던 UI를 기억해 복구하는 데 쓸 수 있다.
- MainMenu는 루트가 splash/menu 2개이고 전체 숨김 호출이 없어 상속하지 않는다. 필요해지면 그때 상속.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. 필드 이름 `_root`를 유지해서 프리팹 Root 연결이 그대로 남음 (Canvas_InGame·Canvas_Legacy·Canvas_MainMenu, 프리팹 파일 변경 없음).
- TestScene Play: 모든 BaseView 자식에서 Hide → `IsVisible` false, Show → true, PetGuide `Show(string)` → true 확인. 씬 dirty 없음.
- 기존 문제(이번 작업과 무관): 세 Canvas 프리팹 모두 `SettingsPanel` 오브젝트에 `_root`가 비어 있는 `SettingsView`가 하나 더 붙어 있음 (실제 사용은 `SettingsView` 오브젝트 쪽). 손대지 않음.
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 필요하면 `SettingsPanel`에 중복으로 붙은 빈 `SettingsView` 정리 여부 결정
- 대화 중 다른 UI 전부 숨기기 기능을 할 때 `BaseView.IsVisible` 활용
