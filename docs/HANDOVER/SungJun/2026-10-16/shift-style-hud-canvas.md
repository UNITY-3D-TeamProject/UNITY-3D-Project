# Shift 고유 연출 HUD Canvas `Canvas_Shift` (2026-10-16)

## 작업 요약 및 방법
- 사용자 요청: Shift - Complete Sci-Fi UI 위주로 스킬 패널·일반 패널을 만든 버전의 Canvas를 하나 더 만들기(바꿀 수 있는 것만, 이미 쓰는 건 그대로).
- 확인해 보니 켜진 `Canvas`는 이미 거의 전부 Shift 스프라이트였다. Shift가 아닌 것은 아이콘 6종(Shift에 대응 아이콘 없음)과 꺼진 예전 패널뿐이라, 사용자에게 물어 **"Shift 고유 연출 입히기"**로 진행했다. intent: [intent-030](../../../intent/clear/intent-030-shift-style-hud-canvas.md)
- Test MainScene의 `Canvas`를 복제해 `Canvas_Shift`를 만들고(Unity MCP `eval`), 패널마다 Shift 데모 Quick Match 버튼과 같은 겹 구조를 넣었다.
  - 패널 바탕 Image 바로 앞에 형제 `ShiftLayers`(같은 Rect, 레이아웃 그룹 안이면 ignoreLayout)를 만들고 자식 3겹:
    - `Shadow`: `Cut Frame Filled Glow Big`, 검정 0.5, 사방 e만큼 크게, 아래로 e×0.35. ppu 배율 = 36/e (글로우 번짐 폭 36px이 e와 같아지도록 — 배율이 크면 딱딱한 판처럼 보임)
    - `BgGlow`: 같은 스프라이트, 시안 (0.388, 0.776, 1, 0.14), 사방 e×0.7
    - `Content`: `Cut Frame Filled Big`(어두운 남색 0.92) + `Mask` → 자식 `Gradient`(`Gradient Background 3`, 0.42 투명도)
  - 원래 바탕 Image는 끔(`enabled=false`), 원래 테두리(`Glow`/`Frame`)는 `Cut Frame Glow Big - 3px`로 바꾸고 Big 프레임 모서리에 맞게 사방 12/배율만큼 키움.
  - 적용 대상(배율, e): HP 패널(2, 22), 미션 패널(2, 20), 독백 패널(2, 22), 안내 패널 `InfoPanel1`(2, 22, 테두리 새로 추가), 설정창 `Window`(1.6, 30), 스킬 카드 3장 `Bg`(2, 20), 키 배지 `Key/Bg`(4, 8).
- 그 밖에 Shift가 아닌 것 교체: 스킬 카드 `Divider` → `Filled Square`, 꺼진 `PlayerHudRoot` 슬라이더 3개(바탕·채움 `Cut Frame Filled`, 손잡이 `Circle Filled`), 꺼진 예전 `CrossHair` → `Circle Filled`, Legacy `Text` 글꼴 → Rajdhani(Title은 Bold, 나머지 Medium). 안내 패널 글자색을 어두운 회색 → 흰색 계열로.
- 아이콘 6종(불·번개·스캔·달리기·와이파이·하트, SCI-FI UI Pack Pro)은 Shift에 같은 뜻의 아이콘이 없어 그대로 뒀다.
- `Canvas_Shift`를 Test `UnityAssets/Prefabs/UI/MainScene/Canvas_Shift.prefab`으로 저장하고 연결(Canvas별 Test 프리팹 규칙). 원본 `Canvas`는 비교를 위해 끄고 `Canvas_Shift`를 켬.
- 임시 카메라 + RenderTexture로 원본/Shift 버전, 꺼진 패널(설정창·독백·안내)을 켜서 렌더해 확인.

## 결정 사항
- Shift 스타일 패널 = 그림자 + 시안 번짐 + Big 프레임 바탕 안 그라데이션 + Big 글로우 테두리. 배치·크기·아이콘·글자는 원본과 동일하게 유지.

## 현재 상태 및 이슈
- 사용자 요청으로 MainScene 저장(작업 전부터 있던 사용자 미저장 변경 포함, 저장 시점 켜진 Canvas: `Canvas_Shift`). 커밋 안 함.
- 씬의 다른 오브젝트가 원본 `Canvas` 안을 참조하는 곳은 없음을 확인(원본을 꺼도 참조 끊김 없음). 복제본 안의 `SettingsView`·`UIManager` 등 스크립트 참조는 복제본 안으로 따라감.
- 독백 패널(아래 가운데)과 안내 패널(가운데 400×400)은 원래 꺼져 있고, 같이 켜면 서로 겹친다(원본 배치 그대로).

## 다음 할 일
- 사용자가 두 버전 비교 후 채택 결정. 채택하면 원본 `Canvas` 정리 여부 결정.
- Shift에 없는 아이콘 6종을 바꿀지(다른 팩이나 새로 그리기) 결정.
