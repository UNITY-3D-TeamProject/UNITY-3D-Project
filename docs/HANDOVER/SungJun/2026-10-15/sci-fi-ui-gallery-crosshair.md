# UI 팩 미리보기 갤러리 + MainScene HUD 교체 (2026-10-15)

## 작업 요약
- 프리팹 파일만으로는 생김새를 알 수 없는 UI 팩 두 개를 한 씬에 전부 꺼내 놓고 비교할 수 있게 갤러리 씬을 만들었다.
  - `SCI-FI UI Pack Pro/SCI-FI UI Components/Prefabs` 629개 (폴더 10개별 Canvas)
  - `Shift - Complete Sci-Fi UI/Prefabs` 31개 (폴더 12개별 Canvas, SCI-FI 갤러리 왼쪽)
- MainScene(`Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Scene/MainScene.unity`) Canvas에 Shift 프리팹을 원본에서 새로 넣었다.
  - Shift `Slider.prefab` → 이후 사용자가 `HpBar`로 이름을 바꾸고 기존 HpBar(Linearprogress005Blue)를 대체, Handle Slide Area 삭제, 위치 (0, -400)·Scale 3으로 조정(사용자 작업).
  - 크로스헤어: Shift `Horizontal Selector/Indicator Item.prefab`을 Canvas 정중앙 (0, 0)에 추가, 기존 `CrossHair`는 비활성화(삭제 안 함).

## 방법/접근
- Unity MCP `eval`로 에디터에서 직접 생성. 갤러리는 MainScene이 저장 안 된 상태라 닫지 않고 Additive 새 씬으로 만들었다.
- 칸마다 프리팹 인스턴스(프리팹 연결 유지)를 400×400 칸에 맞게 축소하고 `번호. 프리팹이름` 라벨을 붙였다. 스트레치 앵커 프리팹은 1920×1080 기준으로 축소.
- 갤러리 위치는 x=100000 근처(다른 씬 오브젝트와 안 겹치게), World Space Canvas.
- Game 뷰 캡처는 Unity가 포커스를 잃으면 갱신되지 않아서(CrossHair를 껐는데도 그대로 찍힘으로 확인) 씬 뷰를 Canvas에 맞춰 확인했다.

## 변경된 파일
- `Assets/Test/UIGallery/SciFiComponentsGallery.unity` (신규, Git 제외 폴더)
- MainScene: Canvas에 `Indicator Item` 추가, `CrossHair` 비활성. **씬 미저장** (원래 사용자의 미저장 변경이 있어 AI가 저장하지 않음)
- 코드 변경 없음

## 결정 사항
- 갤러리에서 쓴 인스턴스를 옮기지 않고 항상 원본 프리팹으로 새로 배치한다(사용자 지시).
- 기존 CrossHair는 코드·씬 참조가 없음을 확인하고 비활성화만 했다.

## 현재 상태 및 이슈
- Shift Modal Window 4개는 `Custom/UI/Blur Fast` 셰이더가 URP에서 깨져 분홍색으로 나온다(`_HBlur` 콘솔 에러도 같은 원인으로 추정).
- Shift 프리팹은 UI Manager가 런타임에 색·폰트를 입히는 구조라 편집 모드에서는 수수하게 보일 수 있다(추정).
- HpBar가 흐려 보이는 원인 3가지: Game 뷰 창이 444px 폭에 1920×1080을 축소 표시 + Low Resolution Aspect Ratios 켜짐, HpBar Scale 3배(128px 스프라이트 확대), 스프라이트 Compressed.
- HpBar의 Animator(`Slider.controller`) Normal/Highlighted/Pressed 클립이 Background·Fill Area의 `sizeDelta.y`를 -24 등 고정값으로 덮어쓴다. Scale 대신 크기를 키우면 Play 때 바 두께가 어긋난다.
- ThirdParty 프리팹·텍스처 설정은 Git에 안 올라가므로 팀원도 같은 팩이 설치돼 있어야 보인다.

## 다음 할 일
- HpBar 크기 작업(사용자가 나중에 하기로 함): Scale 1로 되돌리고 HpBar와 자식 RectTransform 위치·크기 3배, Value 폰트 26→78, Animator 비활성 + Slider Transition None(또는 Background·Fill Area 앵커를 0.35~0.65로 바꿔 Animator 유지). HP바라면 Slider `Interactable` 끄기 권장.
- Indicator Item 크기(6×6, 기존 10×10)와 Raycast Target(On·Glow·Off 이미지가 켜져 있음) 필요 시 조정.
- MainScene 저장 여부는 사용자가 확인 후 결정. Canvas 프리팹(`Assets/_Project/Prefabs/UI/Canvas.prefab`)에 Apply할지도 결정 필요.

## 추가 작업: HP바 위 스킬 버튼 3개 배치
- MainScene Canvas에 빈 부모 `SkillButtons`(HpBar 바로 다음 순서, 위치 (0, -255), 크기 460×165)를 만들고, 그 아래에 원본 `SCI-FI UI Pack Pro/.../Prefabs_SkillButtons/SkillButton01Blue.prefab` 인스턴스 3개를 넣었다.
  - 이름 `SkillButton01Blue`, `(1)`, `(2)`, x = -160 / 0 / 160, Scale 0.5(256px → 128px).
  - 사용자가 갤러리에 따로 빼 둔 버튼은 오버라이드가 없어서(이름·TMP 스타일 해시만 다름) 원본 프리팹으로 새로 넣은 것과 같다.
- 씬 뷰 캡처로 HP바 위에 3개가 나란히 있는 것 확인. MainScene 저장함(작업 전 미저장 변경 없었음).
- 갤러리 씬(`SciFiComponentsGallery`)은 사용자의 미저장 변경이 있어 손대지 않음.
- 남은 일: 아이콘·Title(V.01) 텍스트를 스킬별로 교체, 쿨타임 표시 등 스킬 UI 기능 연결.

## 추가 작업: 스킬 버튼 뒤 네모 박스
- 갤러리 칸의 `CellBg`(스프라이트 없는 단색 Image, RGBA 0.14/0.15/0.19/1, 버튼 256 대비 400 = 1.5625배)를 같은 비율로 버튼마다 깔았다.
  - `SkillButtons/SkillSlotBg`, `SkillSlotBg (1)`, `SkillSlotBg (2)` (200×200, x = -210 / 0 / 210), 각 버튼을 그 자식으로 옮기고 (0, 0) 정중앙. 장식용이라 Raycast Target은 끔.
  - 부모 `SkillButtons`는 (0, -240), 620×200으로 바꿔 박스 아래쪽이 HP바(-350)에 닿지 않게 함.
- 씬 뷰 캡처로 확인. 그 사이 세 버튼의 `Your icon`은 사용자가 직접 삭제함(프리팹 오버라이드, 사용자 확인).
- 사용자 확인 후 MainScene 저장함(아이콘 삭제 포함).

## 추가 작업: 스킬 버튼을 IconButton05Cyan으로 교체
- 사용자 요청으로 `SkillSlotBg` 3칸 안의 SkillButton01Blue 3개를 삭제하고, 원본 `SCI-FI UI Pack Pro/.../Prefabs_IconButtons/IconButton05Cyan.prefab`(256×256, 달리는 사람 아이콘 포함) 인스턴스를 칸마다 넣었다.
  - 이름 `IconButton05Cyan`, `(1)`, `(2)`, 칸 정중앙 (0, 0), Scale 0.5. 갤러리 인스턴스는 오버라이드가 없어 원본과 같다.
- 박스(`SkillSlotBg`)는 사용자가 150×150으로 줄여 둔 값을 그대로 유지.
- 씬 뷰 캡처로 확인 후 MainScene 저장.
- 남은 일: 스킬별 아이콘 교체, 쿨타임 등 기능 연결.
