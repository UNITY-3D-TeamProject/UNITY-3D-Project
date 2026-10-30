---
id: intent-100
title: 스테이지로 갈 때 로딩 화면에 앱 아이콘·접속 문구·팁·준비 단계 표시
part: SceneTransition / Loading / UI
status: open   # open | resolved
created: 2026-10-31
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
어느 씬으로 가든 로딩 화면은 똑같이 "Loading..." 문구, 진행 바, 달리는 플레이어·펫만 보여 준다. 지금 어느 앱(스테이지)으로 들어가는지, 그 스테이지에서 무엇을 알아 두면 좋은지 알 수 없다.

## 기대 결과 (Proposed outcome)
로비 입구 4곳(피드·파일·보안·라이브)으로 갈 때만 로딩 화면이 바뀐다.
1. 화면 가운데에 목적지 앱의 네온 로고(`Art/Map/Lobby/AppLogos/Neon/T_Neon_*.png`)가 나온다. 달리는 플레이어·펫은 왼쪽으로 옮긴다.
2. 진행 바 위 문구가 "Loading..." 대신 "피드 앱에 접속 중..."처럼 바뀐다.
3. 진행 바 아래에 `TIP` 표시와 그 스테이지 팁 중 하나가 랜덤으로 나온다.
4. 오른쪽에 준비 단계 4줄(데이터 분석 중 / 앱에 연결 중 / 스테이지 준비 중 / 완료 중...)이 있고, 진행 바가 찰수록 위에서부터 체크된다.

튜토리얼·로비로 갈 때는 지금 화면 그대로다.

### 설계
- 데이터: `SOLoadingStageData`(SO 하나) 안에 씬별 `SLoadingStageInfo { Scene, AppName, Icon(Texture2D), Tips[] }`. 목록에 없는 씬은 기본 화면.
- 화면: `LoadingStageView`(`ProgressGroup/StageInfo`, 저장 시 꺼 둠). `TryShow(목적지)`로 아이콘·문구·랜덤 팁을 넣고 켜며, 기본 "Loading..." 문구를 숨기고 러너를 왼쪽으로 옮긴다. `SetProgress(값)`로 준비 단계 체크.
- `LoadingScreen`: `_stageView` 필드 하나. `CoLoad`에서 목적지를 받은 직후 `TryShow`, `SetProgress`에서 같은 값을 넘긴다. `ProgressGroup` 안이라 컷씬 영상 중에는 함께 숨겨진다.
- 아이콘은 RawImage로 띄워 네온 텍스처의 임포트 설정(로비 머티리얼이 씀)은 바꾸지 않는다.
- TIP 상자·체크 표시용 스프라이트는 `Art/Loading/UI/`에 새로 만든다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 로비에서 스테이지로 들어가는 플레이어, 팁 문구를 고칠 팀원
- 어떤 시스템/모듈이 건드려지는가:
  - `Scripts/SceneTransition/LoadingScreen.cs` (필드 1개, 호출 2곳)
  - 신규 `Scripts/SceneTransition/SOLoadingStageData.cs`, `LoadingStageView.cs`
  - 신규 `ScriptableObjects/SceneTransition/SOLoadingStageData.asset`, `Art/Loading/UI/*.png`
  - `Scenes/Loading.unity` (`ProgressGroup/StageInfo` 추가·연결)

## 제약 (Constraints)
- 지켜야 할 정책: 튜토리얼·로비 이동, 로딩씬에서 바로 Play한 경우의 화면은 바뀌지 않는다.
- 쓰지 말아야 할 것: `ThirdParty` 스프라이트(Git에 안 올라감), 네온 텍스처 임포트 설정 변경.

## 열린 질문 (Open questions)
- 팁 문구는 기획보드·스킬 문서 기준 초안. 피드·파일·보안·라이브 실제 플레이와 맞는지 사용자 확인 필요.
- 씬별 배경 이미지는 이번 범위에서 뺌(배경 그림이 아직 없음).

## 구현 기록 (2026-10-31)
- 코드: `SOLoadingStageData`·`LoadingStageView` 신규, `LoadingScreen`에 `_stageView` 필드와 호출 2곳(`CoLoad` 목적지 받은 직후 `TryShow`, `SetProgress`에서 같은 값 전달).
- 에셋: `SOLoadingStageData.asset`(피드 5·파일 4·보안 5·라이브 2줄 팁, 네온 로고 연결), `Art/Loading/UI/` 흰색 스프라이트 4개(둥근 사각 채움·테두리 9-slice, 체크 박스, 빈 원).
- `Loading.unity`: `ProgressGroup/StageInfo`(저장 시 꺼 둠) — `AppIcon`(RawImage 460, 가운데 (120, 70)), `ConnectText`(Pretendard 44, 기존 `LoadingText` 자리), `TipGroup`(1200×56, 진행 바 아래), `StepList`(오른쪽, 4줄). 스테이지 화면이면 러너를 (-560, -59)로 옮김.
- 확인: 리컴파일 통과. Play에서 `TryShow`를 직접 불러 4개 스테이지 화면 캡처 확인, 로비·튜토리얼은 `false`(기본 화면 유지). 실제 로비 → 스테이지 이동 중 화면은 미확인.
- 참고: 동적 폰트(Pretendard)에 처음 쓰는 글자는 첫 프레임에 빈칸으로 보일 수 있음. 실제 로딩은 0.3초 페이드인 동안 지나감.

## 후속 (2026-10-31): 앱 순서 줄·남색 배경
- 사용자 요청: 가운데 큰 아이콘 대신 앱 순서(피드 → 파일 → 보안 → 라이브)를 보여 주고 지금 들어가는 앱을 강조. 배경을 검정 대신 남색 그라데이션으로.
- `SOLoadingStageData`: 목록 순서 = 줄 순서. `Stages` 속성 추가, `TryGetStage`가 순서 번호도 돌려줌.
- `LoadingStageView`: `_appIcon` → `_rowIcons`(4칸) + `_currentFrame`(강조 테두리). 지금 앱은 1.5배·불투명, 나머지는 반투명, 테두리를 지금 칸으로 옮김.
- `Loading.unity`: `StageInfo/AppIcon` 삭제, `AppRow`(가운데 (150, 70), 칸 x ±90·±270, 사이 화살표 3개, `CurrentFrame` 220) 추가, `StepList` (-200, 30)으로 이동. `Background`에 `T_LoadingBackground`(1920×1080 남색 그라데이션, 바닥 푸른 빛) — 튜토리얼·로비 이동에도 적용.
- 새 이미지: `Art/Loading/UI/UI_LoadingArrow.png`, `UI_LoadingCurrentFrame.png`(9-slice), `T_LoadingBackground.png`.
- 확인: 리컴파일 통과, Play에서 기본 화면과 4개 스테이지 화면 캡처.
- 사용자 요청으로 `LoadingScreen._fillDuration` 1 → 3초(진행 바·준비 단계가 최소 3초에 걸쳐 참). 최소 로딩 시간 약 1.8초 → 3.8초. 모든 로딩에 적용.
- 사용자 요청으로 `LoadingRunners/PetRunner` 로컬 x -0.55 → -0.15(플레이어 머리 뒤로 0.4m 당김, 높이·깊이 그대로). Play 캡처 확인.
- 사용자 요청으로 진행 바를 HUD(HP 패널) 스타일로: `ProgressBar`에 `Glow`(Shift `Cut Frame Glow - 3px`, 하늘색)·`Frame`(Shift `Cut Frame Filled`, 어두운 청록) 추가, `Background`·`Fill`을 새 칸 이미지 `UI_LoadingBarCell.png`(30×24, 모서리 잘린 칸) Tiled로 교체(바탕 어두운 청록·채움 하늘색). 오른쪽 `PercentText`(Rajdhani-SemiBold 30) + `LoadingScreen._percentText`(SetProgress에서 반올림 %). 스테이지 화면 러너 위치 (-560 → -600, -59), `TipGroup` y 44 → 36. 모든 로딩 공통(러너 위치·TIP 제외). Play 캡처 확인.
- 사용자 요청 배치 조정: 아래 묶음(`LoadingText`·`ConnectText` y 210, `ProgressBar`·`PercentText` y 150, `TipGroup` y 86)을 50 올림, 스테이지 화면 러너 (-720, -59), `AppRow` 가운데 (0, 70), `StepList` 오른쪽 위(앵커 (1,1), (-200, -170)). Play 캡처 확인.
- 사용자 요청: `_fillDuration` 3 → 5초(최소 로딩 약 5.8초, 모든 로딩 공통), `AppRow` x 0 → 60 → 140.
- 사용자 요청: 튜토리얼·로비로 갈 때 "Loading..." 대신 "튜토리얼로 가는 중..." / "로비로 가는 중...". `SOLoadingStageData._moveTexts`(`SLoadingMoveText { Scene, Text }`) + `TryGetMoveText`, `LoadingStageView._defaultLoadingText`를 `TMP_Text`로 바꿔 스테이지가 아니면 문구 교체. `LoadingText` 폰트 LiberationSans → Pretendard-Bold 44(한글). Play 확인.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
