---
id: intent-052
title: UI View들의 루트 켜기·끄기 코드를 BaseView로 합치기
part: UI
status: resolved   # open | resolved
created: 2026-10-20
resolved: 2026-10-20 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
`InfoPanelView`·`PlayerHudView`·`PetGuideView`·`SettingsView` 4개가 `_root` 필드와 `Show`·`Hide`·`SetVisible`·`IsVisible`을 각자 복사해 들고 있다.
InfoPanelView는 PlayerHudView 주석("HUD 보이게 하는 함수")까지 그대로 붙어 있고, 파일마다 `SetVisible`의 공개 범위(public/private/없음)와 `IsVisible` 유무가 제각각이다.

## 기대 결과 (Proposed outcome)
- `UI.BaseView`(abstract, MonoBehaviour)가 `_root`, `IsVisible`, `Show()`, `Hide()`, private `SetVisible(bool)`을 가진다.
- 4개 View는 `BaseView`를 상속하고, 겹치던 코드를 지운다. 각 View 고유 기능(PetGuide `Show(string)` 등)은 그대로 둔다.
- 밖에서 쓰는 것은 `Show()`·`Hide()`·`IsVisible`뿐이다. (`SetVisible`은 View 밖 호출 0곳을 확인함)
- 프리팹·씬에 연결된 Root 참조는 그대로 유지된다 (필드 이름 `_root` 유지).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 각 Presenter, UIManager, DialoguePresenter(`_petGuideView.Hide()`)
- 어떤 시스템/모듈이 건드려지는가: `Scripts/UI/BaseView.cs`(새 파일), InfoPanelView·PlayerHudView·PetGuideView·SettingsView

## 제약 (Constraints)
- 예산/기한: 없음
- 지켜야 할 정책: 외부에서 부르는 public 메서드 이름·동작은 바꾸지 않는다.
- 쓰지 말아야 할 것: 버튼 리스너 등록·해제 공통화, MainMenuView 상속 (루트가 splash/menu 2개이고 전체 숨김 호출이 없음)

## 열린 질문 (Open questions)
- (없음) MainMenu 제외, `IsVisible` 포함, `SetVisible` private → 사용자 확정.
- 참고(이번 범위 아님): 나중에 대화가 시작되면 대화창 외에 떠 있는 UI를 모두 숨기는 기능을 넣을 수 있음. 그때 `IsVisible`로 원래 떠 있던 UI를 기억해 복구하는 데 쓸 수 있다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 기대 결과대로 구현. `Scripts/UI/BaseView.cs`(namespace `UI`, abstract) 추가, InfoPanel·PlayerHud·PetGuide·Settings View가 상속하고 겹치던 `_root`·`IsVisible`·`Show`·`Hide`·`SetVisible` 삭제. PetGuide `Show(string)`은 `Show()`를 부르도록 변경. 필드 이름 `_root` 유지로 프리팹 Root 연결 그대로(Canvas_InGame·Canvas_Legacy·Canvas_MainMenu 확인, 프리팹 파일 변경 없음). 컴파일 0에러, TestScene Play에서 Show·Hide·IsVisible·PetGuide `Show(string)` 확인.
- 관련 커밋/PR: (커밋 전)
