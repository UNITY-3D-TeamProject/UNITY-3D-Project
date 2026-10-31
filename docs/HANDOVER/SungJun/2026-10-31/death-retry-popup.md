# 사망 팝업 (RETRY / MAIN MENU) — intent-103

## 작업 요약 및 방법
- 보안맵 1라운드 문제 진단
  - 죽으면 문이 안 열림: 방에 들어가면 입구 문을 닫고 입구 패널을 잠그는데, 깬 방은 사망 리셋(`ResetUnclearedRooms`) 대상이 아니라 잠긴 채 남음. 리스폰은 1라운드 시작점이라 방 2~4에서 죽으면 방 1 입구(`100205829`)에 막힘.
  - 쫄몹 미초기화: "못 깬 방만 리셋" 설계 그대로. 깬 방 몹은 다시 나오지 않음.
  - 결론: 1라운드는 이미 있는 `StageManager._restartSceneOnFirstRoundDeath`(씬 재시작)를 쓰기로 함. 입장 컷씬 26번에는 `HideOnStageRestart`가 이미 있음, `ObjectPoolManager`는 씬 단위라 재시작 시 옛 적이 안 남음.
- 사망 팝업 구현 (모든 스테이지)
  - `StageManager`: `OnDeathEnded` 때 바로 복구하지 않고 `OnRetryChoiceRequested` 발행(구독자 없으면 예전처럼 즉시 복구). 기존 복구 본문은 `RecoverFromDeath()`로 분리, 팝업에서 부르는 `RetryAfterDeath()` 추가. `_isWaitingRetry`로 사망 통지 중복 방지.
  - `GameManager.SetCursorLocked`를 public으로.
  - `UI/Death/DeathPopupView`(버튼 두 개, `BaseView`) + `DeathPopupPresenter`(View 이벤트 → `RetryRequested`/`MainMenuRequested`, `Open`/`Close`).
  - `UIManager`: 플레이어 스폰 때 `GameManager.CurrentStageManager`를 구독. 팝업 열 때 UI 입력 맵 요청 + 커서 해제(게임 시간은 안 멈춤), RETRY는 닫고 커서 잠금 후 `RetryAfterDeath`, MAIN MENU는 닫고 `ReturnToMainMenu`.
  - `Canvas_InGame.prefab`: 설정창 `SettingsPanel`을 복제한 `DeathPopup/Panel`(창 640×210, 제목 `YOU DIED`, `RETRY`·`MAIN MENU` 버튼). `UIManager._deathPopupPresenter` 연결.

## 결정 사항
- 적용 범위: 모든 스테이지 (사용자 선택)
- 팝업 중 게임은 계속 진행, 커서만 풀고 UI 입력 맵(ESC 설정창 안 열림) (사용자 선택)
- 문구: 영어, 설정창과 통일 (사용자 선택)
- RETRY = 기존 사망 복구 그대로. 첫 라운드 + 재시작 옵션이면 씬 재시작, 아니면 현재 라운드 체크포인트 리스폰(보안맵 2라운드는 발판 전체 리셋)
- 열고 닫는 시점은 UIManager가 정하고(입력 맵·커서 조율), 표시는 Presenter → View (MVP)

## 현재 상태 및 이슈
- 리컴파일 에러 0. TestScene Play에서 확인: 사망 → 팝업 표시·커서 해제·UI 맵·`Playing` 유지, RETRY → 리스폰(HP 100, Player 맵, 커서 잠금), 두 번째 사망에도 팝업 한 번, MAIN MENU → MainScene. 새 에러 없음.
  - 에디터가 백그라운드라 프레임이 거의 멈춰서, 사망 애니메이션 종료 이벤트는 `HandlePlayerDeath`를 직접 호출해 대신함. 버튼은 `onClick.Invoke`로 누름.
  - 팝업 화면은 미리보기 씬 렌더로만 확인. 실제 게임 화면에서 마우스 클릭은 미확인.
- **씬 재시작 경로는 미확인**: TestScene이 빌드 목록에 없어 `LoadScene(buildIndex)`를 못 탐.
- **`Map_4_Security` StageManager의 `Restart Scene On First Round Death`는 아직 꺼져 있음** (사용자가 직접 켜기로 함). 켜기 전에는 보안맵 1라운드 RETRY도 기존 리스폰이라 문 막힘 문제가 그대로임.
- `RestartStage`는 로딩 화면 없이 동기 로드라 보안맵에서 잠깐 멈출 수 있음.
- 커밋 안 함.

## 다음 할 일
- `Map_4_Security` → StageManager → Restart Scene On First Round Death 체크.
- 보안맵 1라운드 방 2 이후에서 죽고 RETRY → 씬 재시작·문·쫄몹 초기화 확인, 2라운드에서 죽고 RETRY → 발판 리셋 리스폰 확인.
- 실제 화면에서 팝업 마우스 클릭, 팝업 중 주변 적·낙하물 동작 확인.
- 문제 없으면 intent-103 resolved 처리.
