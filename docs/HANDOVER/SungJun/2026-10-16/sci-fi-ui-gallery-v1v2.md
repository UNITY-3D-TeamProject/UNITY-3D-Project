# UI 갤러리에 SCI-FI V1(Blue)·V2(Cyan)·Common 추가 (2026-10-16)

## 작업 요약 및 방법
- 10-15에 만든 갤러리 씬(`Assets/Test/UIGallery/SciFiComponentsGallery.unity`)에는 SCI-FI `SCI-FI UI Components` 629개 + Shift 31개만 있었다. 빠져 있던 SCI-FI UI Pack Pro의 나머지 프리팹 325개를 같은 칸 스타일로 추가했다.
  - `SciFi_V1Blue_Gallery` (x = 104800): `SCI-FI UI Pack_V1 Pro(Blue)/Prefabs` 160개, 폴더 5개
  - `SciFi_V2Cyan_Gallery` (x = 109600): `SCI-FI UI Pack_V2 Pro(Cyan)/Prefabs` 161개, 폴더 5개
  - `SciFi_Common_Gallery` (x = 114400): `Common/White Icons/Prefabs` 아이콘 시트 4장
- 이제 갤러리 칸 총 985개 = 두 팩의 프리팹 전부. 왼쪽부터 Shift → Components → V1 → V2 → Common 순.
- Unity MCP `eval`로 생성. 기존 칸 구조(Cell / CellBg / Label / Frame / 프리팹 인스턴스, 400×400 칸, 380px에 맞춰 축소, 10열)와 색·폰트를 기존 오브젝트에서 그대로 복사했다.
- V1·V2는 섹션 순서(Button → Icon → Option → PanelAndPopup → Widget)와 높이가 같아서 좌우로 같은 줄끼리 비교할 수 있다.
- 갤러리 씬만 저장. MainScene은 열려 있는 미저장 상태 그대로 두었다.

## 결정 사항
- Common 아이콘 시트는 1920×1080 크기라 칸을 2000×1160(2열)로 크게 잡았다.
- Common 프리팹은 Screen Space Overlay Canvas가 루트라 저장된 Scale이 0, 앵커가 (0,0)이다. 갤러리 인스턴스만 Scale 1, 앵커·피벗 0.5, 크기 1920×1080으로 오버라이드했다(원본 프리팹은 그대로).

## 현재 상태 및 이슈
- 씬 뷰 캡처로 V1·V2·Common 모두 보이는 것 확인. 새로 생긴 콘솔 에러 없음.
- 갤러리 씬은 `Assets/Test/`에 있어 Git에 안 올라간다(팀원 공유 안 됨).
- 기존 이슈(Shift Modal Window 분홍색 등)는 그대로.

## 다음 할 일
- 필요한 UI를 고르면 원본 프리팹으로 MainScene 등에 새로 배치(갤러리 인스턴스는 옮기지 않는다).

## 추가 작업: Test 저장소 커밋 정정 + MainScene에 목표 목록·실드 바 추가
- MainScene은 Test 저장소(`Assets/Test/UNITY-3D-Project-for-KSJ-Test`, 별도 Git)에서 관리한다. 디스크의 MainScene이 HUD가 빠진 옛 버전으로 저장돼 있었는데 확인 없이 커밋(`47d6a0c`)했다가, 사용자 요청으로 커밋을 취소하고 MainScene을 `5a74078`(HUD 있는 버전)으로 되돌렸다. push는 하지 않았다.
- Shift `Simple HUD (Desktop)`(URP & HDRP)에서 `Active Mission`(목표 목록, 왼쪽 위 (22.5, -25))과 `Armor`(파란 52% 바, 오른쪽 아래 (-14.9, 85))를 복사해 MainScene Canvas에 넣었다. 도시 배경 이미지는 제외. MainScene 저장.
- 상세는 Test 저장소 `Docs/WorkLog/2026-10-16.md` 작업 1. Test 저장소에 `fb547ba`로 커밋(push 안 함).
- 팀 저장소에는 작업 기록 커밋 `b95e67e`(push 안 함).

## 추가 작업: HpBar 10칸 색 고정
- `HpBar`~`(9)`의 `Background`·`Glow`·`Fill` `UIManagerImage` 30개 비활성화, 참고 이미지 색으로 Fill (25, 249, 251), Glow (55, 212, 232), Background는 Fill RGB(알파 유지). MainScene 저장, Test 저장소 `2dc8a94` 커밋(push 안 함). 상세는 Test `Docs/WorkLog/2026-10-16.md` 작업 2.

## 추가 작업: HpBar value 0일 때 남는 게이지 제거
- 원인은 `Fill Area` 안쪽 여백(왼 25·오른 10)과 `Fill` 폭 +15. 10칸 모두 `Fill Area`를 Background와 같은 크기(Left/Right 0, Top/Bottom 12), `Fill` offset 0으로 통일. value 1 → 칸 전체, 0 → 안 보임. 7칸 1, 3칸 0. MainScene 저장, Test 저장소 `a57a8a4` 커밋(push 안 함). 상세는 Test `Docs/WorkLog/2026-10-16.md` 작업 3.

## 추가 작업: UI 아치 휘기 컴포넌트(UIArcBend), HpFrame 적용
- Test 저장소 `UnityAssets/Script/UI/UIArcBend.cs`(`Project.UI`, BaseMeshEffect) 신규. Image 메시를 가로 10px 조각으로 나눠 반지름 `_radius`(Rect 가운데 가로선 기준) 원호를 따라 휜다. Raycast는 원래 사각형.
- MainScene `HpFrame`에 R 1108로 적용, HP 칸과 같은 곡률 확인, MainScene 저장. Test 저장소 미커밋. Intent는 Test `Docs/Intent/ui-arc-bend.md`(resolved).
- HpFrame 가운데가 HP 칸 줄보다 약 26px 아래라 칸이 프레임 위쪽에 붙어 보임(위치 미조정).

## 추가 작업: 체력바 아래 스킬 HUD(SkillHud)
- `Canvas/SkillHud` (0, -400)에 스킬 3개: SCI-FI `Btn_LargePlayCyan` 링(Scale 0.25) + 아이콘 `Icon_Scan`/`Icon_Run2`/`ICON_Wifi`(블루투스 대체) + `Btn04Cyan` 키(Q/SHIFT/F) + Rajdhani 이름(SCAN/ROLL/BLUETOOTH). 쿨타임 바 없음.
- 사용자의 HpFrame 미저장 변경이 함께 있어 **MainScene 미저장**. 상세는 Test `Docs/WorkLog/2026-10-16.md` 작업 5.

## 추가 작업: HP 바 뒤 패널, 하트 + HP, 체력 % 칸
- `HpPanel`(Btn06Cyan_n 760×190, (0, -180), UIArcBend R 1108)을 HpFrame 뒤에, `HpTitle`(Icon_Heart + "HP", (0, -128)), `HpPercent`(Btn04Cyan_n + "70%", (430, -250)) 추가. **MainScene 미저장**. 상세는 Test `Docs/WorkLog/2026-10-16.md` 작업 6.

- 사용자 요청으로 MainScene 저장(HpFrame 변경, SkillHud, HpPanel·HpTitle·HpPercent 포함). Test 저장소 미커밋.
- Test 저장소 커밋: `417f16d`(UIArcBend 스크립트 + Intent), `3f89270`(MainScene + WorkLog 작업 4~6). push 안 함.

## 추가 작업: 상태 HUD 시안 Canvas
- MainScene에 `Canvas_StatusHudConcept`(Overlay, Sort Order 1) 추가. 왼쪽 아래에 스킬 3칸(Shift Cut Frame + Scan/Run/Wifi 아이콘 + 키 + 이름)과 INTEGRITY 72 / BATTERY 64 / HEAT 28 바 패널(Btn06Cyan_n). 팀원 공유용 기본 형태, 기능 연결 없음. MainScene 저장, Test 저장소 미커밋.
- 기존 Canvas의 왼쪽 아래 마름모 스킬 아이콘과 겹침. 상세는 Test `Docs/WorkLog/2026-10-16.md` 작업 7.
- 이후 `StatusHud`를 가운데 아래 (0, 40), Scale 1.25로 조정. 기존 Canvas를 다시 켜고 MainScene 저장(사용자 요청). Test 저장소 미커밋.

## 추가 작업: 상태 HUD 시안 B Canvas
- 시안 A·기존 HUD 상태를 Test 저장소 `2493ef6`로 커밋(push 안 함).
- MainScene에 `Canvas_StatusHudConceptB`(Sort Order 2) 추가: 원형 링 스킬 3개 + 칸 12개짜리 HP 72 / BATTERY 86 / HEAT 35 바. 시안 A와 같은 자리(가운데 아래, Scale 1.25). MainScene 저장, Test 저장소 `e663dfd` 커밋(push 안 함). 상세는 Test `Docs/WorkLog/2026-10-16.md` 작업 8.

## 추가 작업: HP·스킬 HUD 왼쪽 아래 이동
- 사용자가 만든 `Canvas (1)`(팀 Canvas 프리팹 인스턴스, 켜진 작업본)에서 HP 아치 요소 + SkillHud를 `HpSkillGroup`(왼쪽 아래 앵커)으로 묶어 화면 왼쪽 아래로 이동. `SkillButtons`·`Btn04Buttons` 비활성화. 꺼진 `Canvas`에 먼저 잘못 적용했다가 되돌림.
- 사용자 요청으로 사용자 변경과 함께 MainScene 저장, Test 저장소 미커밋. 상세는 Test `Docs/WorkLog/2026-10-16.md` 작업 9.

## 추가 작업: 넓고 낮은 HP 아치 시안 Canvas
- MainScene에 `Canvas_HpWide`(Sort Order 3): 기존 Canvas의 HP 요소 + SkillHud 복사본을 R 1800, 칸 62×20(보이는 크기), 가운데 칸 y -300으로 넓고 낮게 재배치, SkillHud Scale 0.8 (0, -452). 사용자가 `Canvas (1)`을 지운 미저장 변경과 함께 **MainScene 미저장**. 상세는 Test `Docs/WorkLog/2026-10-16.md` 작업 10.
- `Canvas_HpWide` HpBar 10칸도 UIManagerImage 30개 끄고 기존 Canvas와 같은 밝은 시안으로 맞춤. **MainScene 미저장**.

## 추가 작업: 크로스헤어 에셋 갤러리 + 참고 이미지 HUD 시안
- 갤러리 씬에 `Crosshair_Gallery`(x = 119200) 추가: `ThirdParty/CrosshairAssembler/CrosshairSamples` Basic 230 / Advanced 70 / Scopes 17 / Empty 1. 전용 셰이더가 월드 공간 Canvas에서 안 그려져 각 프리팹의 `Thumbnail` 스프라이트(미리보기 이미지)로 표시(317개, EmptyCrosshair는 미리보기 없음). 갤러리 씬 저장.
  - 프리팹을 그냥 두면 `CrosshairCanvas` 이름의 `CrosshairController`가 없어서 매 프레임 에러가 쌓이고 에디터가 느려짐. 컨트롤러는 Play 시 자기 자식을 지움.
- MainScene에 `Canvas_HudReference`(Sort Order 4): 왼쪽 아래 HP 칸 바, 가운데 크로스헤어 + 열(빨강)·배터리(파랑) 원호, 아래 가운데 스킬 3개(쿨타임 원호 포함). Shift 원형 스프라이트로 구성. **MainScene 미저장**(사용자 변경 포함). 상세는 Test `Docs/WorkLog/2026-10-16.md` 작업 11.
- `Canvas_HudReference` 크로스헤어를 에셋 기반으로 교체: CrosshairAssembler `Cross_Dot_25`(`CrosshairCanvas` 오브젝트 + 꺼 둔 CrosshairController, GlobalScale 1.6) + SCI-FI `CircularProgress001` 게이지 2개(열 28%, 배터리 61%, 프리팹 글자 사용). 프로젝트가 새 Input System 전용이라 컨트롤러를 켜고 List를 채우면 예외. **MainScene 미저장**.
- 사용자 요청으로 크로스헤어를 다시 직접 만든 버전으로 되돌림. 규칙: 위 반원 = 빨강(열), 아래 반원 = 파랑(배터리), 각 원호는 자기 반원 안에서만 참. **MainScene 미저장**.
- 크로스헤어 원호를 값에 맞춤: fill = 값 × 0.5(100% = 반원). 열 28% → 50.4°, 배터리 61% → 109.8°. **MainScene 미저장**.
- 커밋 전 MainScene 저장(사용자 요청, 사용자 변경 포함). Test 저장소 `90a4ca2`로 커밋(push 안 함). 저장 시점 켜진 Canvas: `Canvas_HpWide`, `Canvas_HudReference`. 갤러리 씬(`Assets/Test/UIGallery`)은 어느 저장소에도 들어가지 않음.
