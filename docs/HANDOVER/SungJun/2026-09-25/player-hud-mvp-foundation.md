# 플레이어 HUD MVP 기반

## 작업 요약
- `Scripts/UI` 폴더에 `UIManager`, `PlayerHudPresenter`, `PlayerHudView`를 추가했다.
- 플레이어의 HP, 배터리, 열 Attribute 변경을 HUD Slider에 반영하도록 구성했다.

## 방법/접근
- `UIManager`는 씬 단위 비싱글톤으로 두고 HUD 표시와 숨김만 조정한다.
- `PlayerHudPresenter`는 기존 `AttributeSet`의 변경 이벤트를 구독한다.
- `PlayerHudView`는 전달받은 값을 정규화하여 Slider에 표시한다.

## 변경된 파일
- `Assets/_Project/Scripts/UI/UIManager.cs`
- `Assets/_Project/Scripts/UI/PlayerHudPresenter.cs`
- `Assets/_Project/Scripts/UI/PlayerHudView.cs`
- `docs/intent/clear/intent-006-player-hud-mvp-foundation.md`

## 결정 사항
- 네임스페이스는 `UI`를 사용한다.
- UI 데이터의 원본은 기존 플레이어 `AttributeSet`으로 유지한다.
- 씬과 프리팹 연결은 이번 작업 범위에서 제외한다.

## 현재 상태 및 이슈
- `dotnet build UNITY-3D-Project.sln --no-restore` 성공, 추가 코드 오류는 없다.
- 기존 `StageManager`의 미사용 이벤트 경고 3개가 있으며 이번 변경과 무관하다.
- Inspector 참조와 Slider 연결은 아직 하지 않았다.

## 다음 할 일
- Canvas 및 HUD 오브젝트에 View와 Presenter를 부착한다.
- Presenter에 플레이어 `AttributeSet`과 View를 연결한다.
- View에 HUD Root와 HP, 배터리, 열 Slider를 연결한다.
