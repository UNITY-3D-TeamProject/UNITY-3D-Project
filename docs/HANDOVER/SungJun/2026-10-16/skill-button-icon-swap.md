# MainScene 스킬 버튼 IconButton05Cyan 교체 (2026-10-16)

## 작업 요약 및 방법
- MainScene(`Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Scene/MainScene.unity`) Canvas의 `SkillButtons` 아래 3칸(`SkillSlotBg`, `(1)`, `(2)`)에 있던 `SkillButton01Blue` 3개를 삭제하고, 원본 `SCI-FI UI Pack Pro/SCI-FI UI Components/Prefabs/Prefabs_IconButtons/IconButton05Cyan.prefab` 인스턴스를 칸마다 넣었다.
  - 이름 `IconButton05Cyan`, `IconButton05Cyan (1)`, `IconButton05Cyan (2)`, 칸 정중앙 (0, 0), Scale 0.5 (256px → 128px). 이전 버튼과 같은 크기·위치.
  - Unity MCP `eval`로 Undo 그룹("Replace skill buttons with IconButton05Cyan")을 걸어 작업했다.
- 교체 전 씬 전체에서 기존 버튼(자식·컴포넌트 포함)을 참조하는 직렬화 필드를 찾았는데, 부모 RectTransform의 자식 목록 말고는 없었다.
- 씬 뷰 캡처로 HP바 위 3칸에 아이콘 버튼이 들어간 것 확인 후 MainScene 저장(작업 전 미저장 변경 없음).

## 결정 사항
- 박스(`SkillSlotBg`, 150×150)와 부모 `SkillButtons`((0, -240), 620×200)는 그대로 유지.
- 갤러리 인스턴스를 옮기지 않고 원본 프리팹으로 새로 배치(기존 규칙 유지).

## 현재 상태 및 이슈
- 10-15 작업 기록에는 이미 IconButton05Cyan으로 교체·저장했다고 되어 있었지만, 이번 세션 시작 시 씬에는 SkillButton01Blue가 남아 있었다(되돌려졌거나 저장이 반영되지 않은 것으로 추정, 원인 미확인).
- MainScene과 SCI-FI UI Pack Pro 모두 Git 제외 폴더(`Assets/Test/`, `Assets/ThirdParty/`)라 팀원에게는 공유되지 않는다.
- 아이콘은 세 칸 모두 프리팹 기본(달리는 사람) 그대로.

## 추가 작업: 스킬 칸 아래 Quick Match Button 배치
- 원본 `Shift - Complete Sci-Fi UI/Prefabs/Button/Quick Match Button.prefab`(488×150, "QUICK PLAY")을 MainScene Canvas에 `SkillButtons` 바로 다음 순서로 넣었다. 위치 (0, -400), Scale 1.
- 스킬 칸 아래(-315)와 HP바 위(-364) 사이 빈 공간이 약 50px뿐이라 원래 크기로는 HP바와 겹친다. 사용자가 "원래 크기로 겹쳐 놓기"를 골랐고, 위치는 사용자가 직접 조정하기로 함. 순서상 HP바 위에 그려진다.
- 씬 뷰 캡처로 확인 후 MainScene 저장(작업 전 미저장 변경 없음).

## 추가 작업: 스킬 칸 검은 배경 제거
- 사용자가 아이콘 버튼 3개를 칸 밖으로 옮겨 둔 상태(여전히 각 `SkillSlotBg`의 자식)에서, 검은 배경만 없애 달라는 요청을 받았다.
- 오브젝트를 지우면 자식 아이콘도 같이 사라지므로 `SkillSlotBg`, `(1)`, `(2)`의 **Image 컴포넌트만 제거**했다. 오브젝트·계층·아이콘 위치는 그대로(CanvasRenderer는 남아 있음). Undo 그룹 "Remove SkillSlotBg images".
- 씬 뷰 캡처로 검은 배경이 없어지고 아이콘 3개가 남은 것 확인.
- 작업 전에 사용자의 미저장 변경(Quick Match Button 위치·크기, SkillButtons 위치, 아이콘 위치 등)이 있어 바로 저장하지 않았고, 이후 사용자 요청으로 MainScene 저장함(사용자 변경 포함).

## 추가 작업: Squad Member Button 3개 배치
- 원본 `Shift - Complete Sci-Fi UI/Prefabs/Button/Squad Member Button.prefab`(60×60, + 아이콘)을 3개 넣었다.
  - 빈 부모 `SquadMemberButtons`(Canvas에서 `SkillButtons` 바로 다음, (0, -150), 220×60) 아래에 `Squad Member Button`, `(1)`, `(2)`, x = -80 / 0 / 80, Scale 1.
  - 위치 지정이 없어 사용자가 옮겨 둔 아이콘 3개 아래(-96)와 Quick Match Button 위(-200) 사이 빈 공간에 놓음(사용자가 옮길 수 있음).
- 씬 뷰 캡처로 확인 후 MainScene 저장(작업 전 미저장 변경 없음).

## 추가 작업: Btn04Cyan 3개 배치
- 원본 `SCI-FI UI Pack Pro/SCI-FI UI Components/Prefabs/Prefabs_Buttons/Btn04Cyan.prefab`(334×134, "Button 04")을 3개 넣었다.
  - 빈 부모 `Btn04Buttons`(Canvas에서 `SquadMemberButtons` 바로 다음, (0, -495), 570×67) 아래에 `Btn04Cyan`, `(1)`, `(2)`, x = -200 / 0 / 200, Scale 0.5(167×67).
  - 원래 크기로는 기존 버튼 사이에 자리가 없어 HP바 아래(-450 ~ -540) 빈 공간에 줄여서 놓음(사용자가 옮길 수 있음).
- 씬 뷰 캡처로 확인 후 MainScene 저장(작업 전 미저장 변경 없음).

## 추가 작업: HP바 + 스킬 3개 구도를 참고 이미지처럼 재배치
- 사용자가 준 참고 이미지(HP가 위, 그 아래 SCAN(Q)·ROLL(SHIFT)·BLUETOOTH(F)가 가운데만 살짝 내려간 호 모양)처럼 **위치만** 옮겼다. 크기·글자·계층(부모)은 그대로.
- 사용자가 미리 해 둔 짝: 키 라벨 `Btn04Cyan`='Q', `(1)`='F', `(2)`='Shift', 각각 바로 위 아이콘 `IconButton05Cyan (1)` / `IconButton05Cyan` / `IconButton05Cyan (2)`.
- 화면 중심 기준 RectTransform 중심 좌표:
  - `HpBar` (0, -200)
  - Q: 아이콘 `IconButton05Cyan (1)` (-200, -310), 라벨 `Btn04Cyan` (-200, -410)
  - Shift: 아이콘 `IconButton05Cyan (2)` (0, -340), 라벨 `Btn04Cyan (2)` (0, -440)
  - F: 아이콘 `IconButton05Cyan` (200, -310), 라벨 `Btn04Cyan (1)` (200, -410)
  - 참고 이미지 순서에 맞춰 F와 Shift의 좌우 자리를 바꿨다(기존 Q·F·Shift → Q·Shift·F).
- 부모(`SkillButtons`, `SkillSlotBg*`, `Btn04Buttons`)는 그대로 두고 자식 위치만 옮겼다. 그래서 아이콘의 anchoredPosition이 부모 기준으로 큰 값(-518 등)이다. Undo 그룹 "Arrange HUD like reference".
- 씬 뷰 캡처로 확인. 작업 전에 사용자의 미저장 변경(Quick Match Button·SquadMemberButtons를 왼쪽 위로 이동, 라벨 글자 등)이 있어 바로 저장하지 않았고, 이후 사용자 요청으로 MainScene 저장함(사용자 변경 포함).

## 추가 작업: HP바 디자인 이미지 미리보기
- 사용자가 준 HP바 디자인 이미지(webp, 2000×667, 배경 투명)를 느낌 확인용으로 Canvas에 띄웠다.
  - Pillow로 PNG 변환 → `Assets/Test/UIPreview/HpBarPreview.png`(Git 제외 폴더), Sprite·Alpha Is Transparency·밉맵 끔으로 임포트.
  - MainScene Canvas에 `HpBarPreview`(Image, Preserve Aspect, Raycast Target 끔)를 `HpBar` 바로 다음 순서로 추가. 위치 (0, -200), 600×200(실제 표시 600×200 안에서 비율 유지). 기존 HpBar는 그대로 두어 뒤에 겹쳐 보인다.
- 씬 뷰 캡처로 확인. **MainScene 미저장**: 미리보기용이고, 작업 전에 사용자의 미저장 변경도 있었다. Undo 그룹 "Add HpBarPreview".

## 추가 작업: HP 칸 5개 아치 배치
- 사용자가 HpBar(Shift Slider)를 50×100으로 줄여 5개(`HpBar`, `(1)`~`(4)`, Canvas 직속)로 복제해 둔 것을 디자인 이미지처럼 아치 모양으로 배치했다. **위치·회전만** 변경(크기·값·색 그대로).
  - 반지름 R ≈ 554인 원호 위에 중심 간격 58, 칸마다 6°씩 기울임. 가운데 `HpBar (2)`가 (0, -200)으로 가장 높고, 양끝으로 갈수록 내려가며 기운다.
  - 좌→우: `HpBar` (-115.2, -212.1) 12° / `(1)` (-57.9, -203.0) 6° / `(2)` (0, -200) 0° / `(3)` (57.9, -203.0) -6° / `(4)` (115.2, -212.1) -12°.
  - 디자인 이미지(7칸, 칸마다 약 4°, 양끝 12°)의 양끝 기울기를 5칸에 맞춘 값. 5칸으로도 모양이 나와 7칸으로 늘리지 않음.
- 씬 뷰 확대 캡처로 확인. 각 칸이 슬라이더 값 때문에 절반만 채워져 보인다. Undo 그룹 "Arrange HP segments in arc".
- **MainScene 미저장**: 작업 전에 사용자의 미저장 변경(HpBar 복제 등)이 있었다.

## 추가 작업: HP 칸 10개로 늘려 아치 재배치
- 최대 체력 100 ÷ 칸 수가 정수로 떨어지도록 10칸(한 칸 = 10)으로 결정(사용자).
- `HpBar (4)`를 에디터 Duplicate 방식(프리팹 연결·오버라이드 유지)으로 5개 복제해 `HpBar (5)`~`(9)` 추가, Hierarchy에서 `HpBar`~`(9)`가 연속되게 정리(`HpBarPreview` 앞). Undo 그룹 "Add HP segments to 10".
- 아치: 반지름 R ≈ 1108, 중심 간격 58, 칸마다 3°. 가운데 두 칸 `(4)`·`(5)`가 (∓29, -200.4) ±1.5°, 양끝 `HpBar`·`(9)`가 (∓258.6, -230.6) ±13.5°. 전체 폭 약 566. Undo 그룹 "Arrange 10 HP segments in arc".
- 씬 뷰 캡처로 확인. 사용자가 스킬 아이콘을 키워 둔 상태(200×200, 위 꼭짓점 y -210)라 `(1)`·`(8)` 칸(아래쪽 -236)이 Q·F 아이콘 위 꼭짓점과 약 26px 겹친다. 사용자가 아치 위치는 그대로 두기로 함.
- **MainScene 미저장**.

## 추가 작업: HP 칸 뒤 프레임 배치
- 디자인 이미지의 HP 패널(어두운 남색 바탕 + 시안 발광 테두리 + 양옆 흰 브래킷, 위아래가 휜 아치형)과 가장 비슷한 에셋을 찾았다.
  - 두 팩(SCI-FI UI Pack Pro, Shift)과 `ThirdParty/AIImage`(Gemini 이미지 7장)를 다 봤지만 **아치형으로 휜 패널은 없다**.
  - 후보: `Btn06Cyan_n`, `Windows013_CyanTitle`, `Btn_PauseCyan1_n`, `DialogBoxTitle`, `Panel_Time02`. 이 중 어두운 바탕 + 시안 테두리 + 양옆 흰 꺾쇠가 가장 닮은 `SCI-FI UI Components/Textures/Button/Btn06Cyan_n.png`(338×138, 9-slice 90/48)를 골랐다.
- MainScene Canvas에 `HpFrame`(Image, Sliced, Raycast Target 끔)을 `HpBar` 바로 앞 순서(칸들 뒤)에 추가. 위치 (0, -195), 680×140. 버튼 기능이 필요 없어 프리팹 대신 스프라이트만 썼다.
- 사용자가 임시로 깔아 둔 `Quick Match Button`((-11, -209.5), HP 칸 뒤)은 삭제하지 않고 **비활성화**만 했다.
- 씬 뷰 캡처로 확인. 같은 Undo 그룹 "Add HpFrame"(Ctrl+Z 한 번에 프레임 제거 + Quick Match Button 다시 켜짐). **MainScene 미저장**.

## 추가 작업: HpFrame 스프라이트를 Panel_Time02로 교체
- 사용자 선택으로 `HpFrame` Image 스프라이트를 `SCI-FI UI Pack_V2 Pro(Cyan)/Textures/DemoScene_Cyan/PopupsAndPanels/Panel_Time02.png`(286×109, 위가 넓은 사다리꼴, 9-slice 좌우 70)로 교체, Sliced 유지. 위치·크기는 사용자가 바꿔 둔 (0, -226.6), 680×203.2 그대로. **MainScene 미저장**.
- 참고(사용자 질문): Slider value를 0으로 해도 칸이 남는 이유는 `Fill`의 RectTransform이 앵커와 상관없이 Width 15(sizeDelta.x 15, Pos X -25)를 고정으로 갖고 있어서다. value 0이면 앵커가 0으로 모여도 15px 폭이 남는다. 사용자가 직접 고치기로 함.
- 사용자가 `HpBar (9)`의 Fill Width를 0으로 바꾼 뒤 요청해 나머지 9개(`HpBar`~`(8)`)도 `Fill` sizeDelta.x = 0으로 맞췄다(Pos X -25는 그대로). Undo 그룹 "HP Fill width 0". 씬 뷰에서 value 0 칸이 비어 보이는 것 확인. value 1 칸은 Fill이 칸 오른쪽 끝까지 다 차지 않는다(Fill Area Left 25 / Right 10 + Pos X -25 때문). **MainScene 미저장**.
- 이어서 10칸 모두 `Fill Area` Left/Right 0(offsetMin.x·offsetMax.x 0, Top/Bottom 12/-12 유지), `Fill` Pos X 0(Width 0 유지)으로 바꿨다. Fill 폭이 value 1 → 50(배경과 같음), value 0 → 0으로 측정됨. Slider 애니메이터는 Fill Area의 y값(anchoredPosition.y, sizeDelta.y)만 쓰므로 Play에서도 유지된다. Undo 그룹 "HP cell fill full width". **MainScene 미저장**.

## 추가 작업: HP 칸을 아치 → 일자형으로 변경
- 사용자 요청으로 10칸을 회전 0, 간격 58 그대로 가로 일직선 배치. x = -261 ~ 261(58 간격), y는 `HpFrame` 세로 중앙인 -226.6.
- 이때 `HpFrame` 스프라이트는 다시 `Btn06Cyan_n`이었다(사용자가 되돌린 것으로 보임), 위치 (0, -226.6), 680×203.2.
- 씬 뷰 캡처로 확인. Undo 그룹 "HP cells straight line". **MainScene 미저장**.

## 추가 작업: 채워진 HP 칸도 테두리가 보이게 Fill 안쪽으로 줄임
- 원인: `Fill Area`가 `Background`와 같은 크기라 value 1이면 `Fill`이 테두리(`Background > Glow`, 그리는 순서상 Fill보다 먼저)를 덮었다. Glow를 Fill 위로 옮기는 건 프리팹 인스턴스라 계층 이동 불가.
- 10칸 모두 `Fill Area` offsetMin (4, 16) / offsetMax (-4, -16)으로 사방 4px 안쪽으로 줄였다(Inspector: Left 4, Right 4, Top 16, Bottom 16). Fill 42×18, 배경 50×26. Undo 그룹 "HP fill inset".
- 확인 방법: 씬 뷰 창이 닫혀 있었고 Game 뷰는 포커스 문제로 갱신되지 않아(같은 바이트 수의 캡처), eval에서 Canvas를 잠깐 Screen Space - Camera + 임시 카메라로 렌더링해 확인한 뒤 바로 Overlay로 되돌렸다. 채워진 칸도 바깥 테두리와 얇은 틈이 보임. **MainScene 미저장**.

## 추가 작업: HP 칸을 Shift Switch로 교체
- 사용자 요청으로 원본 `Shift - Complete Sci-Fi UI/Prefabs/Switch/Switch.prefab`(110×55, Button + SwitchManager + Animator, 테두리 안 네모 손잡이가 좌우로 움직이는 토글) 10개를 HP 칸 자리에 놓았다.
  - 이름 `HpSwitch`, `(1)`~`(9)`, Canvas 직속(`HpBar (9)` 바로 다음 순서), Scale 0.5(55×27.5), 간격 62(x -279 ~ 279), y -226.6.
  - 기존 `HpBar`~`(9)`(Fill 조정까지 해 둔 Slider)는 **삭제하지 않고 비활성화**.
- 오프스크린 렌더로 확인: 10개 모두 기본 상태(isOn=false, 손잡이 왼쪽 + 채워진 시안 네모). Undo 그룹 "Replace HP cells with Switch". **MainScene 미저장**.
- 주의: Switch는 클릭하면 켜짐/꺼짐이 바뀌는 버튼이다. HP 표시용이면 Button `Interactable`을 끄거나 Raycast를 막아야 하고, HP 반영도 Slider value가 아니라 `SwitchManager`(isOn·애니메이션)로 해야 한다.

## 추가 작업: HpBar·HpSwitch 두 줄 비교 배치
- 사용자 요청으로 꺼 두었던 `HpBar`~`(9)`를 다시 켜고, 프레임(`HpFrame`, 사용자가 높이 150으로 바꿈, y -226.6) 안에서 위아래로 나눴다: HpBar 줄 y -191.6(위), HpSwitch 줄 y -261.6(아래). x는 각자 그대로. Undo 그룹 "Show HpBar and HpSwitch rows". 오프스크린 렌더로 확인. **MainScene 미저장**.
- 참고(사용자 질문): Shift Modal Window 4종(Exit, Key Binding, Play Chapter, Selected Server)이 분홍색인 이유는 `Blur` 오브젝트의 `Modal Window Blur.mat` 셰이더 `Custom/UI/Blur Fast`(`Shaders/Fast UI Blur.shader`)가 `GrabPass`를 쓰기 때문이다. 프로젝트가 URP라 GrabPass를 지원하지 않는다. 해결: `Blur` Image의 Material을 비우거나(None) `Blur` 오브젝트를 끄기.

## 추가 작업: HpSwitch를 HpBar처럼 7칸 켜짐·3칸 꺼짐 + 크기·색 맞춤
- Switch에는 value가 없어 `SwitchManager.isOn`으로 대응: `HpSwitch`~`(6)` = true, `(7)`~`(9)` = false. 편집 모드에서도 보이게 `Switch On.anim`/`Switch Off.anim` 마지막 프레임을 각 인스턴스에 샘플링해 넣었다(손잡이 위치, Background/On/Off 알파). Play에서는 `SwitchManager.OnEnable`이 isOn에 맞는 애니메이션을 다시 재생한다(saveValue false).
- 크기: Scale 50/110 ≈ 0.4545(폭 50, HP 칸 배경과 같음). x는 위 줄 HpBar와 같은 -261 ~ 261(간격 58), y -261.6 그대로.
- 색(사용자 추가 요청): `UIManagerImage.useCustomColor`를 켜고 Border = HpBar 테두리(`Background/Glow`) 색 (0.388, 0.776, 1, 0.588), Background·Handler/Off·Off/Glow = HpBar `Fill` 색 (0.388, 0.941, 1) RGB(알파는 애니메이션 값 유지). 켜짐 손잡이(`Handler/On`, 어두운 회색)는 그대로.
- 오프스크린 렌더로 확인: 켜짐 7칸 = 밝은 시안 바탕 + 오른쪽 어두운 손잡이, 꺼짐 3칸 = 테두리 + 왼쪽 시안 손잡이. Undo 그룹 "HpSwitch 7 on 3 off", "HpSwitch colors like HpBar". **MainScene 미저장**.
- 주의: HpBar `Fill`은 색이 (0.388, 0.941, 1)인데 `UIManagerImage.useCustomColor`가 꺼져 있어, Play나 UI Manager 갱신 때 기본색 (0.388, 0.776, 1)으로 돌아간다. 이 밝은 시안을 유지하려면 10칸 모두 Fill의 Use Custom Color를 켜야 한다. 사용자가 HpBar 색은 그대로 두기로 함.
- 이후 사용자 요청으로 **색을 둘 다 되돌림**: HpSwitch 10개의 Border·Background·Handler/Off·Off/Glow는 Use Custom Color 끔 + 기본 시안 (0.388, 0.776, 1)(Border 알파 1, 나머지 알파는 애니메이션 값 유지), HpBar 10칸 `Fill`도 (0.388, 0.776, 1)로. 켜짐/꺼짐 상태·크기·위치는 유지. Undo 그룹 "Revert HP colors". **MainScene 미저장**.
  - (사용자 의도는 색 되돌리기가 아니라 켜짐 칸의 검은 손잡이를 없애는 것이었음 — 오해.)
- 켜짐 칸의 어두운 회색 손잡이 `Handler/On`(Primary Reversed 색)을 10개 모두 **GameObject 비활성화**. Play 때 Switch On 애니메이션이 On Image 알파를 1로 올리지만 오브젝트가 꺼져 있어 안 보인다. 렌더로 확인: 켜짐 7칸 = 꽉 찬 시안 칸, 꺼짐 3칸 = 테두리 + 왼쪽 작은 시안 손잡이. Undo 그룹 "Hide HpSwitch dark handle". 색은 지금 두 줄 모두 기본 시안(사용자가 이대로 두기로 함). **MainScene 미저장**.
- 켜짐 칸이 꽉 찬 블록이라 두꺼워 보인다는 피드백으로, `HpSwitch`~`(6)`도 isOn=false + `Switch Off.anim` 샘플링해 **10칸 모두 꺼짐 모양**(테두리 + 왼쪽 작은 손잡이)으로 통일. 렌더로 확인. 지금은 Switch 줄에서 체력 있음/없음 구분이 안 보인다(사용자가 10칸 같은 모양으로 두기로 함). Undo 그룹 "All HpSwitch off shape". **MainScene 미저장**.

## 추가 작업: MainScene UI 텍스처 Filter Mode → Point
- MainScene의 모든 Image/RawImage(비활성 포함)가 쓰는 텍스처 24개를 모아, 임포트 설정 Filter Mode를 **Point (no filter)**로 바꿨다(원본 텍스처 임포터 수정).
  - 바꾼 22개: Shift `Textures/Border/Cut/*`(Cut Frame Filled/Glow/Corner 등 13개), `Border/Circle/Circle Filled`, `Glow & Shadow/Circle Filled Glow`, `Background/Gradient Background`, `Icon/Add Glow`·`Glow Mote`·`Placeholder Profile`, SCI-FI `IconButton05Cyan_n`·`Btn04Cyan_n`·`Common/Icons000/Icon_Jump`, `Assets/Test/UIPreview/HpBarPreview.png`.
  - 이미 Point: `Btn06Cyan_n`(사용자가 먼저 바꿈). 못 바꿈: `unity_builtin_extra`(유니티 내장, Background·Fill 등 기본 UI 스프라이트).
- 렌더로 확인(화면 전체·HP 영역). 원본 텍스처 설정이라 **같은 텍스처를 쓰는 다른 씬·프리팹(갤러리 씬 등)도 같이 Point가 된다**. 텍스처가 전부 Git 제외 폴더(`ThirdParty`, `Test`)라 .meta 변경도 팀원에게는 공유되지 않는다.
- Point는 축소 표시(Scale 0.45~0.5 등)에서 경계가 각져 보일 수 있다.

## 추가 작업: 직접 배치한 UI Scale → 1
- 사용자 선택: "Scale만 1로"(Width/Height 유지), "직접 배치한 것만". 대상과 변경: `HpSwitch`~`(9)` 0.4545 → 1(110×55), `Btn04Cyan`·`(1)`·`(2)` 0.5 → 1(150×120), `Indicator Item` 5 → 1(5×5).
- 건드리지 않은 것(프리팹 내부 설계값): `SettingsView`/`SettingsPresenter` 0.42와 `SettingsPanel` 2.4, Quick Match Button `Content/Background` 1.25, Switch `Handler/Off` 0.95(애니메이션 값).
- 렌더 결과: HpSwitch가 110폭이 되어 간격 58보다 넓어 서로 겹쳐 한 줄로 붙어 보인다. Btn04Cyan 키 라벨이 2배로 커짐, 크로스헤어가 5×5로 매우 작아짐. Undo 그룹 "UI scale to 1". **MainScene 미저장**.
- 이어서 HpSwitch 간격을 118(칸 110 + 틈 8)로 넓혀 겹침 해소. x -531 ~ 531, y -261.6 그대로. 줄 전체 폭 약 1172라 `HpFrame`(680×150) 밖으로 양옆이 크게 나가고, 아래쪽도 프레임 경계에 걸친다. 렌더로 확인. Undo 그룹 "HpSwitch spacing". **MainScene 미저장**.
- 이어서 `HpFrame`을 두 줄(HpBar 배경 + HpSwitch)이 다 들어가게 키움: 내용 범위 x -586~586, y -289.1~-178.6에 이전과 같은 여백(좌우 54, 위아래 26)을 더해 680×150 → **1280×162.5**, 위치 (0, -226.6) → **(0, -233.9)**. 렌더로 확인. 프레임 왼쪽 아래가 사용자가 왼쪽 아래로 옮겨 둔 스킬 아이콘(`IconButton05Cyan`, x -596~-396, 위 꼭짓점 y -275) 위쪽과 약 40px 겹친다. Undo 그룹 "Widen HpFrame". **MainScene 미저장**.
- 사용자가 두 버전을 같은 크기로 비교하길 원해 "HpBar(작은 쪽)에 맞춤"을 선택. HpSwitch 10개(Scale 1 유지)를 HpBar 칸 배경과 같은 **50×26**, x는 HpBar와 같은 -261~261(간격 58), y -261.6 그대로. 내부도 비율(26/55 ≈ 0.473)로 줄임: `Handler` 45→21.3, `Handler/On/Glow`·`Off/Glow` 68→32.1, `Highlighted` 50→23.6(Handler 위치·앵커는 애니메이션 값이라 그대로).
- `HpFrame`은 두 줄에 다시 맞춰 680×148, (0, -226.6). 렌더로 확인: 두 줄 폭·칸 크기 동일. Undo 그룹 "Match HpSwitch to HpBar size". **MainScene 미저장**.

## 추가 작업: HP 칸 10개 다시 아치형으로
- 확인 시점에 씬에 `HpSwitch` 오브젝트가 없었다(사용자가 지운 것으로 보임). `HpBar`~`(9)` 10개만 대상.
- 이전 아치와 같은 모양(R ≈ 1108, 간격 58, 칸마다 3°, 양끝 ±13.5°)으로 위치·회전만 변경. 가운데 두 칸 `(4)`·`(5)`를 기존 줄 높이 y -191.6에 두고, 양끝 `HpBar`·`(9)`는 (∓258.6, -221.8). 왼쪽 칸이 +, 오른쪽 칸이 − 회전.
- `HpFrame`(680×148, (0, -226.6))은 그대로 일자형. 오프스크린 렌더로 확인: 아치가 프레임 안에 들어감. Undo 그룹 "Arrange HP segments in arc again". **MainScene 미저장**.

## 추가 작업: MainScene UI 텍스처 Filter Mode → 다시 Bilinear
- 사용자 요청으로 지금 MainScene의 Image/RawImage(비활성 포함)가 쓰는 텍스처 중 Point였던 **21개 전부를 Bilinear**로 되돌림(사용자가 먼저 Point로 바꿨던 `Btn06Cyan_n` 포함). 남은 Point 없음 확인. `unity_builtin_extra`는 대상 아님.
- 지금 MainScene에서 쓰지 않는데 Point인 텍스처 3개는 그대로 둠: Shift `Border/Cut/Cut Frame Filled Big (300ppu)`, `Border/Cut/Cut Frame Filled Glow`, `Border/Square/Filled Square`. 이 중 일부는 지운 `HpSwitch`가 쓰던 것이라 앞선 Point 변경 때 같이 바뀌었을 수 있다.
- 텍스처 임포트 설정이라 Undo 대상이 아니고, ThirdParty·Test 폴더라 Git에 공유되지 않는다.

## 다음 할 일
- Scale 1 이후 크기·간격 재조정(HpSwitch 겹침, 키 라벨 크기, 크로스헤어 크기).
- HpBar(Slider) / HpSwitch 중 어느 쪽을 쓸지 결정 후 나머지 정리.
- HP 칸(Switch)으로 체력 표시 방식 결정: 꺼짐/켜짐 중 어느 쪽을 "체력 있음"으로 볼지, 클릭 막기.
- 아치형 프레임이 꼭 필요하면 이미지를 따로 만들어야 한다(예: 디자인 이미지에서 칸을 지운 프레임만 있는 버전).
- HP 칸 10개를 실제 HP와 연결(칸마다 채움/비움). 지금은 각각 독립 Slider라 값이 절반씩 차 있다.
- `HpBarPreview` 유지 여부 결정(사용자). 쓰기로 하면 이미지를 `Assets/_Project/` 아래로 옮기고 HP 칸 채움 등 기능은 따로 만들어야 한다(지금은 통짜 이미지).
- Quick Match Button 위치·크기 조정(사용자), 버튼 글자("QUICK PLAY")와 용도 결정.
- 스킬별(구르기·블루투스·스캔) 아이콘 교체.
- 쿨타임 표시 연결: `PlayerFacade`에는 쿨타임 조회 함수가 없고 `OnSkillUsed(스킬 이름, 쿨타임)` 이벤트만 있다. 현재 `PlayerHudPresenter`가 이 이벤트로 Slider 3개(`_rollCooldownSlider` 등)를 갱신하므로, 새 버튼에 맞춰 View를 바꿀지(예: Image Fill) 결정 필요.
