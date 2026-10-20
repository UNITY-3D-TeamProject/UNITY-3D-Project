# 2026-10-21 — 피드 앱 라운드 진행도를 Active Mission(InfoPanel)에 표시

## 1. 작업 요약 및 방법
- intent-053 작성. 사용자 결정: 집계는 Core 스크립트만 수정하고, 2라운드 표기는 보낸 수/전체, 두 줄은 제목+내용, 스테이지 클리어 보고는 이번 범위에서 제외.
- `Scripts/Core/Stage/FeedApp.cs` (CP949 인코딩 유지, 추가만 함)
  - `StartStage`에서 `FeedRound2`를 찾아 `OnStarted`(UnityEvent)에 리스너를 붙인다. 2라운드 시작 때 `RemainingCount`를 전체 수(`Round2TotalCount`)로 기록한다.
  - 프로퍼티: `CurrentRound`(1/2), `CollectedItemCount`, `RequiredItemCount`(`FeedRound2.RequiredItems` 수, 없으면 아이템 종류 수), `Round2TotalCount`, `Round2SentCount`
  - `event Action OnProgressChanged`: 아이템 획득, 2라운드 시작, 2라운드 남은 수 변화 때 호출. `FeedRound2`는 남은 수 변화 이벤트가 없어서 `FeedApp.Update`에서 int 비교로 감지한다.
- `Scripts/UI/InfoPanel/InfoPanelView.cs`: `Text` → `TMP_Text`, `SetTexts(title, body)` 추가
- `Scripts/UI/InfoPanel/InfoPanelPresenter.cs`: `Start`에서 `FeedApp`을 찾아 구독. 없으면 패널을 숨긴다. 1라운드 `1라운드 / 피드 재료 n/5`, 2라운드 `2라운드 / 피드 분류 n/전체`.
- `Prefabs/UI/Canvas_InGame.prefab`의 `Active Mission`
  - `InfoPanelView`(Root = Active Mission, 제목 = `Mission Example/Text`, 내용 = `Mission Example (1)/Text`)와 `InfoPanelPresenter` 추가
  - 두 Text에서 `UIManagerText`를 떼고 폰트를 `Pretendard-SemiBold SDF`로 변경. 테마 폰트 Rajdhani에 한글이 없어 □로 나왔기 때문이다. 색은 기존 값(0.388, 0.776, 1.0) 그대로 유지했다.

## 2. 결정 사항
- Map/Feed 스크립트(`FeedRound2` 등)는 수정하지 않는다. Core가 Map을 읽는 방향은 `StageManager`의 `using Map.Gimmicks`에 이미 전례가 있다.
- `FeedRound2`는 맵 생성기 `MapGeneratorBase.Awake`에서 만들어지므로 `FeedApp.StartStage`(플레이어 스폰 뒤 호출) 시점에 찾을 수 있다.
- 한글 텍스트는 기존 펫 안내와 같이 Pretendard + UIManagerText 없음으로 맞춘다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러.
- TestScene Play(스테이지 FeedApp, `FeedRound2` 없음)에서 확인:
  - 시작 시 `1라운드 / 피드 재료 0/5`
  - `CollectItem` 4번(중복 1번 포함) → `피드 재료 3/5`
  - 런타임 임시 `FeedRound2` + 가짜 피드 12개로 2라운드 시작 → `2라운드 / 피드 분류 0/12`, 하나 제거 후 Update → `1/12`. 가짜 피드는 설정이 없어 Assert 에러가 났지만 테스트용 오브젝트에서만 난 것이다.
  - 폰트 교체 후 한글 글리프 경고 사라짐, 새 경고·에러 없음
- 실제 `Map_2_Feed` 씬의 Play 확인은 하지 않았다(TestScene만 다룬다는 규칙). 실제 맵에서 아이템 획득 → 2라운드 진입 → 피드 분류까지 숫자가 바뀌는지 직접 확인이 필요하다.
- TestScene 왼쪽 위의 Object Pool Test 화면(테스트용 IMGUI)이 Active Mission과 겹친다. 테스트 씬에만 있는 표시다.
- 폰트 `Pretendard-SemiBold SDF`는 `Assets/ThirdParty`에 있어 Git에 올라가지 않는다. 같은 프리팹의 펫 안내도 이미 같은 폰트를 쓰고 있다.
- 작업 트리에 `StagePhaseBase.cs`(.meta) 삭제와 `EditorBuildSettings.asset` 변경이 보이는데, 이번 작업에서 만든 변경이 아니다. 건드리지 않았다.
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- `Map_2_Feed`에서 실제 플레이로 진행도 갱신 확인 → 확인되면 intent-053 resolved 처리
- 스테이지 클리어 보고(`StageManager`에 완료 처리 추가, 클리어 시점 결정)는 별도 작업
- 다른 스테이지(File·Security 등)에서 Active Mission을 쓸 때 Presenter가 스테이지별로 문구를 고르도록 확장할지 검토(지금은 FeedApp이 없으면 숨김)
