# 로딩 화면 스테이지 정보 (intent-100)

## 1. 작업 요약 및 방법
- 로비 입구 4곳(피드·파일·보안·라이브)으로 갈 때 로딩 화면에 네온 앱 로고, "○○ 앱에 접속 중...", 진행 바 아래 랜덤 TIP, 오른쪽 준비 단계 4줄 체크를 표시하도록 했다.
- 신규 `Scripts/SceneTransition/SOLoadingStageData.cs`(씬별 앱 이름·아이콘·팁), `LoadingStageView.cs`(`TryShow(목적지)`, `SetProgress(값)`).
- `LoadingScreen.cs`: `_stageView` 필드, `CoLoad`에서 목적지를 받은 직후 `TryShow`, `SetProgress`에서 같은 진행률 전달.
- 에셋: `ScriptableObjects/SceneTransition/SOLoadingStageData.asset`, `Art/Loading/UI/UI_Loading*.png` 4개(Python으로 만든 흰색 스프라이트, Unity에서 색 입힘).
- `Loading.unity`: `ProgressGroup/StageInfo`(저장 시 꺼 둠) 아래 `AppIcon`·`ConnectText`·`TipGroup`·`StepList` 추가, `LoadingScreen._stageView` 연결.

- 후속: 가운데 큰 아이콘을 앱 순서 줄(`AppRow`: 아이콘 4칸 + 화살표 3개 + 강조 테두리)로 바꿈. SO 목록 순서가 줄 순서이고 지금 앱만 1.5배·불투명. `Background`를 남색 그라데이션 `T_LoadingBackground.png`로 교체(모든 로딩 공통).

- 후속: `LoadingScreen._fillDuration` 1 → 3초(사용자 요청). 최소 로딩 시간 약 3.8초, 모든 로딩 공통. (이후 5초로 다시 변경)
- 후속: `LoadingRunners/PetRunner` 로컬 x -0.55 → -0.15(펫을 플레이어 쪽으로 0.4m 당김).
- 후속: 진행 바를 HUD HP 패널 스타일(Shift Cut Frame 패널·빛 테두리 + 모서리 잘린 칸 Tiled 게이지 `UI_LoadingBarCell.png`)로 바꾸고 오른쪽에 퍼센트(`LoadingScreen._percentText`, Rajdhani) 추가. 스테이지 화면 러너 (-600, -59), TIP 상자 y 36.
- 후속 배치: 문구·진행 바·퍼센트·TIP을 50 올림(TIP y 86), 러너 (-720, -59), 앱 순서 줄 화면 가운데, 준비 단계 목록 오른쪽 위.
- 후속: `_fillDuration` 5초(최소 로딩 약 5.8초), 앱 순서 줄 x 140.

## 2. 결정 사항
- SO 하나에 스테이지 목록(`SLoadingStageInfo[]`). 목록에 없는 씬(튜토리얼·로비)은 기본 화면 그대로.
- 아이콘은 RawImage + Texture2D로 띄워 네온 텍스처 임포트 설정(로비 머티리얼이 씀)을 건드리지 않음.
- 기존 `LoadingText`는 한글 없는 LiberationSans라 그대로 두고, 스테이지 화면에서는 숨기고 Pretendard `ConnectText`를 씀.
- 준비 단계 n줄 중 i번째는 진행률 (i+1)/n에서 체크. 문구는 씬에 고정.
- `StageInfo`가 `ProgressGroup` 안이라 컷씬 영상 중 함께 숨겨짐.
- 스테이지 화면일 때 러너를 (-560, -59)로 옮김(아이콘과 겹치지 않게). 로딩씬은 매번 새로 열리므로 되돌리지 않음.

## 3. 현재 상태 및 이슈
- 리컴파일 통과. Play에서 `TryShow`를 직접 불러 4개 화면 캡처 확인. 실제 로비 → 스테이지 이동 중 화면은 미확인.
- 팁 문구는 기획보드·스킬 문서 기준 초안. 실제 플레이와 맞는지 확인 필요(특히 파일 숫자 퍼즐, 보안 보스 실드, 라이브 2줄).
- 씬별 배경 이미지는 범위에서 뺌.
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 튜토리얼·로비로 가는 로딩은 아직 기본 화면("Loading..." + 러너 가운데). 같은 방식으로 꾸밀지 결정 필요(앱 아이콘이 없어 SO 목록 순서 줄과는 따로 다뤄야 함).
- 로비에서 각 입구로 실제 이동해 화면·체크 진행·컷씬 중 숨김 확인.
- 팁 문구 다듬기(SO 에셋에서 바로 수정).
- 필요하면 배경 이미지·퍼센트 표시 추가.
