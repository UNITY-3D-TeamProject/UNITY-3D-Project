# 관리자 초상화 추가 + 화자 이름 한글화 (intent-107)

## 작업 요약 및 방법
- 미로 2라운드에 `FileMazeCutsceneStaging._administratorPrefab`으로 생성되는 관리자 모델 `ThirdParty/Spook Ghost Cute Series/Prefabs/Spook.prefab`을 정면(+Z)에서 Idle 0프레임 자세로 512×512 투명 배경 촬영 → `Assets/_Project/Art/UI/AdminPortraits/Admin_00_Default.png`. 임포트 설정은 `Player_00_Default.png`와 같게 복사(Sprite, Single).
  - `PreviewRenderUtility`로 찍으면 URP에서 조명이 안 들어가 파란 Emission만 찍힘(진한 파랑). `EditorSceneManager.NewPreviewScene` + 직접 만든 Directional Light(씬과 같은 색 1, 0.957, 0.839 / 세기 1) + `camera.scene` 지정으로 다시 찍어 게임처럼 하늘색 몸통·흰 해골로 나오게 함. 환경광은 그때 열린 씬 값이 들어감.
- `Canvas_InGame.prefab`의 `CutscenePresenter._speakerDefaultPortraits`에 `관리자 = Admin_00_Default` 항목 추가(독백 `MonologuePresenter`에는 넣지 않음).
- 화자 이름 일괄 변경(YAML 직접 치환, `\u` 이스케이프): `ADMIN → 관리자`(6줄), `PET → 비트`(155줄 + `CutsceneDirector._petSpeakerName`, `PetGuidePresenter._petName`), `PLAYER → 코리`(94줄 + `CutsceneDirector._playerSpeakerName`). 대상: `ScriptableObjects/UIandCinematic/` Cutscene·Dialogue·Monologue SO 67개 + `Canvas_InGame.prefab`.
- 스크립트 기본값·툴팁 예시를 새 이름으로 갱신: `CutsceneDirector`, `PetGuidePresenter`, `CutscenePresenter`, `MonologuePresenter`, `SOCutscene`.

## 결정 사항
- 화자 이름은 한글 `코리`(플레이어)·`비트`(펫)·`관리자`로 쓴다. `CutsceneDirector`는 이 문자열로 말하는 사람 카메라를 고르므로, 새 대사를 쓸 때도 정확히 같은 글자로 적어야 한다.
- 초상화는 실시간 RT가 아니라 정적 스프라이트(플레이어·펫·보스와 동일 방식).

## 현재 상태 및 이슈
- 리컴파일 통과. 에셋을 다시 읽어 SO 화자 이름(`비트 관리자 …`), 프리팹 초상화 목록(`관리자=Admin_00_Default`), `CutsceneDirector`/`PetGuidePresenter` 값 확인. 남은 `SpeakerName: PET/PLAYER/ADMIN` 0건.
- 실제 미로 2·3라운드 컷씬 Play 화면은 확인하지 않음(`Map_3_File` 열지 않음). 씬에 `Canvas_InGame` 오버라이드가 없어 프리팹 값이 그대로 쓰인다.
- 엔딩 크레딧(영문) 등 대사 화자가 아닌 곳의 `PLAYER` 표기는 바꾸지 않음.
- 커밋하지 않음. 작업 전부터 있던 `Pretendard-Bold SDF.asset`, `Map_3_File.unity`, `SOMazeChainSettings_File.asset`, `ShaderGraphSettings.asset` 변경은 건드리지 않음.

## 다음 할 일
- 미로 2라운드 컷씬을 Play로 돌려 관리자 줄에 초상화·이름이 뜨는지, 하늘색 톤이 실제 모델과 맞는지 확인.
- 다른 표정이 필요하면 같은 방식으로 `Admin_01_…` 추가 후 SO 줄의 `Portrait`에 직접 지정.
