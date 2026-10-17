# 스킬 카드형 HUD 시안 Canvas (2026-10-16)

## 작업 요약 및 방법
- 사용자 참고 이미지(카드 3장: 키·아이콘+원형 쿨타임·쿨타임 숫자·이름·번개+소모값)를 보고 Test MainScene에 `Canvas_SkillCards`를 새로 만들었다(Unity MCP `eval`). intent: [intent-028](../../../intent/clear/intent-028-skill-card-hud.md)
- 구성: Screen Space Overlay, Sort Order 5, CanvasScaler 1920×1080(Match 0). `Skills`(아래 가운데, y 40) 아래 카드 3장(190×248, 간격 214).
- 카드 하나(`SkillCard_Scan`/`_Roll`/`_Bluetooth`)의 자식:
  - `Bg`(Shift `Cut Frame Filled` 어두운 반투명) + `Frame`(`Cut Frame - 2px` 시안)
  - `Key`(왼쪽 위, 테두리 박스 + 글자 Q / SHIFT / F)
  - `IconArea`: `Disc` + `CooldownTrack`(흐린 원) + `CooldownBar`(Radial360, 위에서 시계 방향, fill 0.70/0.75/0.80) + `Icon`
  - `CooldownText`(2.8s / 1.5s / 4.0s), `Name`(SCAN / ROLL / BLUETOOTH), `Divider`
  - `Cost`: 번개 아이콘(`Icon_Lightning`) + `Value`(20 / 25 / 30)
- 아이콘은 임시: `Icon_Scan`, `Icon_Run2`, `ICON_Wifi`(블루투스 아이콘이 없어 와이파이). 폰트 Rajdhani Bold/SemiBold SDF. 모든 Image·Text raycastTarget 끔.
- Test `UnityAssets/Prefabs/UI/MainScene/Canvas_SkillCards.prefab`으로 저장하고 씬 인스턴스와 연결했다(Canvas별 Test 프리팹 규칙).
- Game 뷰 캡처가 갱신되지 않아 임시 카메라·RenderTexture로 따로 렌더해 확인했다.

## 결정 사항
- 정적 UI만 만들고 값을 바꾸는 스크립트는 넣지 않았다. 쿨타임 바 = `CooldownBar.fillAmount`(남은 비율), 숫자 = `CooldownText`/`Cost/Value` 글자.

## 현재 상태 및 이슈
- 사용자 요청으로 MainScene 저장(사용자의 미저장 변경 포함, 저장 시점 켜진 Canvas: `Canvas`, `Canvas_SkillCards`). 커밋 안 함.
- 작업 도중 사용자가 Play 모드에 들어가 첫 생성 시도가 실패했다(남은 오브젝트 없음 확인 후 다시 생성).

## 다음 할 일
- 실제 스킬 쿨타임·소모값과 연결하려면 카드용 표시 스크립트가 필요(별도 작업).
- 블루투스 전용 아이콘으로 교체.

## 추가 작업: 키 배지를 카드 위 가운데로, 1.5배
- 사용자 요청으로 카드 3장의 `Key`를 왼쪽 위 → 카드 위 테두리 가운데(anchor 위 가운데, pivot 가운데, y +4)로 옮기고 1.5배로 키웠다: 34×28 → 51×42, SHIFT 70×28 → 105×42, 글자 20 → 30.
- 프리팹 원본(`Canvas_SkillCards.prefab`)을 직접 고쳐 저장했다. 씬 인스턴스의 사용자 오버라이드(카드 Scale 0.8, `Skills` 위치 (630, 22), Canvas RectTransform)는 그대로 유지된다.
- 임시 카메라 렌더로 확인. MainScene은 저장하지 않았다(씬에는 이번 변경이 없고, 사용자의 미저장 변경만 남아 있음). 커밋 안 함.
