# 독백 패널·설정창 HUD 스타일로 교체 (2026-10-16)

## 작업 요약 및 방법
- 사용자 요청: 독백 패널과 설정창을 지금 HUD(HP 패널·퀘스트 패널)와 같은 모양으로, 예전 것을 교체해서 넣기.
- Test MainScene `Canvas`(Test `Canvas.prefab` 인스턴스)의 기존 오브젝트를 그대로 쓰고 모양만 바꿨다(씬 오버라이드). Unity MCP `eval`로 작업.

### 독백 패널 (`MonologueRoot/Monologue Panel`)
- `MonologueRoot` 100×100 → 화면 전체 스트레치.
- `Monologue Panel`: 아래 가운데 앵커, (0, 280), 760×96 (스킬 카드 위). 바탕 Shift `Cut Frame Filled` (0.02, 0.10, 0.15, 0.88), 자식 `Glow` `Cut Frame Glow - 3px` (0.3, 0.85, 1).
- 기존 `Text (Legacy)` 유지(MonologueView가 legacy `Text`를 쓰므로): 폰트 Rajdhani-Medium.ttf 30, 흰색, 가운데 정렬, 좌우 40·위아래 12 여백. 한글은 OS 폰트 대체로 표시됨.

### 설정창 (`SettingsView/SettingsPanel`)
- `SettingsPanel`: 화면 덮는 어두운 배경 (0, 0.02, 0.04, 0.65), 뒤쪽 클릭 막음.
  - 부모 `SettingsView`가 RectTransform이 아니고 Scale 0.42라 스트레치가 안 먹어서, 3840×2160으로 잡고 캔버스 중앙에 위치시킴.
  - 크로스헤어보다 위에 그리도록 `Canvas`(Override Sorting, Order 10) + `GraphicRaycaster` 추가. 프리팹 인스턴스라 형제 순서 변경 대신 이 방법 사용.
- `Window` 760×480: `Cut Frame Filled` + `Glow`, `Header`(Settings 아이콘 + "SETTINGS" Rajdhani-SemiBold 40 + `CloseButton`(Cancel 아이콘)), `Divider`, `Content`(VerticalLayoutGroup).
- `Content` 행 4개: MASTER VOLUME(Audio)·MUSIC(Music)·SFX(SFX)·MOUSE SENSITIVITY(Controls). 각 행에 Unity `Slider`(Cut Frame 트랙·시안 채움·흰 손잡이), 값은 미리보기용 0.8/0.6/0.7/0.5.
- `SettingsView`(SettingsView 오브젝트)의 `_closeButton`에 `CloseButton` 연결.
- 임시 카메라 렌더로 두 패널 확인.

## 결정 사항
- 팝업류 패널은 HP 패널과 같은 Shift Cut Frame + Glow 스타일로 통일한다.

## 현재 상태 및 이슈
- 사용자 요청으로 MainScene 저장, Test 저장소 e254916 커밋(push 안 함). Test `Canvas.prefab`에는 적용 안 함.
- 슬라이더 4개는 모양만 있고 볼륨·감도 로직과 연결 안 됨.
- `SettingsPanel`에 원래부터 붙어 있던 두 번째 `SettingsView`(참조 전부 null)는 손대지 않음.
- 씬에 `MonologueView`/`MonologuePresenter` 컴포넌트가 없어 독백 패널은 아직 코드로 켜지지 않음.

## 다음 할 일
- 설정 슬라이더를 실제 오디오·마우스 감도에 연결.
- MonologueView를 배치해 `_root`=MonologueRoot, `_messageText`=Text (Legacy) 연결.
