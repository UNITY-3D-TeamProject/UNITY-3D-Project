# 엔딩 무대 플레이어 양손 총 제거 + 춤출 때 눈 뜨기 + 엔딩 ESC 동작 확인

## 1. 작업 요약 및 방법
- 무대 프리팹 `EndingCastStage`의 플레이어는 원래부터 실제 플레이어 프리팹과 연결이 끊긴 복사본(프리팹 인스턴스 아님)이다. 그 안의 양손 총 메시 `Buster_L/Buster_L/RadicalBuster (4)`, `Buster_R/Buster_R/RadicalBuster (3)`를 삭제. 등의 `BackPack/PowerPod (6)`은 손 총이 아니라 남김.
- 믹사모 춤 클립에 눈꺼풀·눈알·눈썹 뼈 곡선이 없어 Write Defaults로 기본 자세(눈 감김)가 되던 문제: `ThirdParty/Mixamo/EndingCast_Player_Thriller.anim`에 `Anim_ZMIKE_Idle`의 0초 값(눈 뜬 모습)을 고정 곡선으로 추가(eye_*, eyelid_*, Brow_* 27개 뼈, 250곡선, 춤 클립에 이미 있던 곡선은 그대로).

## 2. 결정 사항
- 눈은 깜빡임 없이 뜬 채로 고정 (Idle 0초 값).

### 엔딩 ESC 동작 (코드 확인, 변경 없음)
- 엔딩 크레딧 중에는 `EndingCredits`가 Cutscene 입력 맵을 요청 → Cutscene 맵의 ESC(`Cutscene/OpenSettings`)로 설정창이 열린다 (`UIInputHandler` → `UIManager.OpenSettings`).
- 설정창이 열리면 `GameManager.PauseGame`: 게임 시간 0, 3D 효과음 멈춤, 커서 풀림.
- `EndingCredits`는 `IsGamePaused()`로 페이드·명단 스크롤·대기 시간을 세지 않아 멈춘다.
- (수정 전) 크레딧 노래(`ignoreListenerPause`)는 계속 나오고, 왼쪽 무대(실제 시간·UnscaledTime 애니메이터)는 계속 움직였다.

### 후속: 설정창 열면 노래·왼쪽 무대도 멈춤 (사용자 요청)
- `EndingCredits.Update`: 크레딧 시작 뒤 `IsGamePaused()`가 바뀔 때 노래 `Pause`/`UnPause`.
- `EndingCastDirector`: `OnEnable`에서 `GameManager.Instance`를 받아 두고, `Update`에서 상태가 Playing이 아니면 플레이어·펫 애니메이터 `speed` 0, 다시 Playing이면 1. 장면 시간용 `DeltaTime`(멈춤 중 0) 추가, `CoMove`·`CoTurn`·`CoWait`·인사 달리기가 이 값을 씀. **새 장면 스크립트도 `Time.unscaledDeltaTime` 대신 `stage.DeltaTime`을 써야 같이 멈춘다.**
- 테스트: 꺼 둔 임시 GameManager를 무대에 연결해 상태만 Pause/Playing으로 바꿔 춤 재생 위치가 멈췄다(0.2394 고정, 속도 0) 다시 진행(0.2433)되는 것 확인. 노래 일시정지는 Play로 확인 안 함(코드만).
- GameManager가 없는 씬에서 무대를 켜면 Singleton이 "GameManager이 없습니다" 에러를 한 번 남긴다 (`EndingCredits`와 같은 조건, 라이브 씬에는 있음).
- 설정창 버튼: 닫기 → 이어서 진행 / 메인 화면으로 → `ReturnToMainMenu` / 게임 종료 / 다시하기 → `CurrentStageManager.RestartStage()` (라이브 맵에서 엔딩 중 누르면 어디로 가는지 미확인).

## 3. 현재 상태 및 이슈
- 미리보기 렌더와 에디터 Play(임시 무대)에서 총 없이·눈 뜬 채 춤추는 것 확인. 실제 엔딩 미확인. 커밋 안 함.
- 눈 수정은 Git 제외 폴더(`ThirdParty/Mixamo`)의 클립이라 커밋되지 않는다.

## 4. 다음 할 일 (Next Steps)
- 실제 엔딩에서 ESC로 설정창 열고 명단·노래·왼쪽 무대가 함께 멈추고 이어지는지 확인.
- 엔딩 중 "다시하기" 버튼 동작 확인(필요하면 엔딩 중엔 숨기기).
