# UI 캔버스 프리팹 이름 정리 (2026-10-17)

## 작업 요약 및 방법
- 인게임 UI로 쓸 것은 `RealCanvas`인데 이름이 `Canvas`와 헷갈려서, Unity `AssetDatabase.RenameAsset`으로 두 프리팹 이름을 바꿨다.
  - `Assets/_Project/Prefabs/UI/Canvas.prefab` → `Canvas_Legacy.prefab` (GUID `3eb1f113c117f8f4c9eda3194e7d6bcf` 유지)
  - `Assets/_Project/Prefabs/UI/RealCanvas.prefab` → `Canvas_InGame.prefab` (GUID `514dc01ad07d9fb4fa0b522b2368c9b3` 유지)
- 이름은 같은 폴더의 `Canvas_MainMenu`에 맞춰 `Canvas_` 접두사로 통일했다(사용자 선택).
- 프리팹 내용은 루트 오브젝트 이름(`m_Name`) 한 줄만 바뀌었다. `.meta`는 동일하다.

## 결정 사항
- 이번에는 이름만 바꾼다. 맵 씬 6개(`Map_0`~`Map_5`)는 여전히 `Canvas_Legacy`를 쓴다. 각 씬 인스턴스에 `m_Name: Canvas` 오버라이드가 있어 Hierarchy에는 계속 `Canvas`로 보이고, 씬 파일은 바뀌지 않았다.
- 맵 씬을 `Canvas_InGame`으로 교체하는 것은 다음 작업으로 미뤘다(사용자 선택).

## 현재 상태 및 이슈
- Test `UITestScene`은 GUID로 `Canvas_InGame`을 참조하므로 그대로 동작한다.
- 이전 작업 기록에 나오는 `Canvas.prefab`·`RealCanvas.prefab` 이름은 기록이므로 고치지 않았다.
- 이 이름 변경만 따로 커밋했다(push 안 함). 작업 트리에 있던 무관한 `audio-manager-design.md`와 그 HANDOFF_POINTER 항목은 커밋하지 않았다.

## 다음 할 일
- 맵 씬 6개의 `Canvas_Legacy`를 `Canvas_InGame`으로 교체한다. `PlayerHudPresenter` 등 HUD 연결이 `Canvas_InGame`에 있는지 먼저 확인한다.
- 교체가 끝나면 `Canvas_Legacy` 삭제 여부를 정한다.
- 팀원 작업을 모두 합친 뒤 예전 1줄 게이지바 로직을 지운다(사용자 결정). 대상은 다음과 같다. `SetGaugeValue`는 HP 칸이 쓰므로 남긴다.
  - `PlayerHudView`의 `_hpSlider`·`_batterySlider`·`_heatSlider`와 쿨타임 슬라이더 `_rollCooldownSlider`·`_bluetoothCooldownSlider`·`_scanCooldownSlider` 필드, 그리고 그 `SetGaugeValue` 호출
  - `Canvas_InGame`의 `PlayerHudRoot/PlayerHUD/HpSlider`·`BatterySlider`·`HeatSlider`(꺼져 있음)
