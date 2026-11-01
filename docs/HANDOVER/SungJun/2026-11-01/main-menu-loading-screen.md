# 메인 메뉴 복귀 로딩 화면 + 오프닝 메인 버튼·검은 배경 (intent-115 후속, intent-116)

## 1. 작업 요약 및 방법
- 오프닝(메인 메뉴 → 폰 주인 영상 → 튜토리얼) 후속 (intent-115 기록에도 반영):
  - 설정창에 "메인 화면으로"도 보이게 함. 미리 불러온 튜토리얼은 취소가 안 돼(메인 메뉴를 열면 유니티가 튜토리얼부터 켬 → StageManager가 커서 잠금·플레이어 스폰) 오프닝도 영상이 끝난 뒤 튜토리얼을 불러오도록 변경. `_mainMenuButton` 필드 제거.
  - 오프닝 흐름에서는 로딩 배경(`T_LoadingBackground`, 파란색)을 검게 칠함: `LoadingScreen._background`(`LoadingScreen/Background` 연결, `Loading.unity`).
- 메인 메뉴 복귀 로딩 화면 (intent-116):
  - `SceneLoader.TryLoadMainMenu`: 저장·세션 초기화 뒤 `_isLoading`·`_isMainMenuPending`을 켜고 로딩씬을 Additive로 연다. 로딩씬이 이미 열려 있으면(로딩 컷씬 설정창) 메인 씬을 바로 연다.
  - `SceneLoader.LoadPendingSceneAsync`: 메인 메뉴가 기다리면 `MainScene`을 불러오고, 끝나면 `_isMainMenuPending`도 푼다.
  - `LoadingScreen.CoLoad`: 메인 메뉴면 컷씬 없이 `LoadingStageView.ShowMainMenu()` → 기본 화면(달리는 플레이어·펫, 진행 바) 문구 "메인 메뉴로 가는 중..."(`_mainMenuMoveText`).
  - 설정창 메인 화면·사망 팝업 메인 메뉴·엔딩 크레딧 끝 모두 `GameManager.ReturnToMainMenu` → `TryLoadMainMenu`라 함께 적용.

## 2. 결정 사항
- 메인 메뉴는 ESceneType에 넣지 않고 `IsMainMenuPending`으로 따로 표시한다.
- 메인 메뉴 문구는 SO 이동 문구 목록(ESceneType 키) 대신 `LoadingStageView._mainMenuMoveText`에 둔다.

## 3. 현재 상태 및 이슈
- 리컴파일 통과, 새 콘솔 에러 없음. TestScene Play(PlayerPrefs 백업·복원):
  - `ReturnToMainMenu` → 로딩 화면(달리는 플레이어·펫, "메인 메뉴로 가는 중...", 진행 바) 캡처 확인 → MainScene, 커서 풀림, 로딩 상태 정리, AudioManager는 MainScene 것.
  - 오프닝 중 설정창 메인 화면 → 로딩씬을 또 열지 않고 MainScene으로 바로 감, 튜토리얼 안 켜짐.
  - 오프닝 배경 검정, 영상 끝 → 검은 화면 대기 → 튜토리얼 진입.
- `GameManager.ReturnToMainMenu`는 바로 게임 시간을 되돌리므로, 로딩 화면이 덮이는 0.3초 페이드 동안 이전 씬이 잠깐 움직일 수 있다 (화면은 로딩 화면이 덮는 중).
- 실제 게임 흐름(사망 팝업·엔딩 크레딧 끝·인게임 설정창 버튼 클릭)으로는 확인 안 함. 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 실제 게임에서 각 메인 메뉴 복귀 경로와 오프닝 대기 시간(빌드) 확인.
