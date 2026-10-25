# 보스-14: 보스전 진입 시 2라운드 발판 내리기 (2026-10-24)

관련: [intent-069](../../../intent/intent-069-boss-arena-stage2-sink.md), AI_TODO 보스-14

## 1. 작업 요약 및 방법
- `Map_4_Security` 보스전(3라운드)에 들어가면 2라운드 특수몹 발판(`Stage_BossRun/04_Platforms/Platform_1~6`)이 아래로 내려가 사라지게 했다.
  - 3라운드 체크포인트 도착(라운드 번호 → 보스 라운드) → `Platform_1~5` 내려감.
  - 보스 바닥 첫 줄 트리거(`05_Boss/BossArenaEntrance`, `TriggerPlate`)에 닿으면 → `Platform_6` 내려감.
  - 3라운드 중 사망 → `Platform_6` 만 즉시 원위치(리스폰 자리), 다시 바닥에 올라서면 또 내려감. 1~5 는 내려간 채.
- 코드 (실행자: Claude — Codex CLI 가 이 PC 에 설치돼 있지 않음(`codex` 명령·npm 전역 없음), 정책상 "도구 미지원" 예외)
  - `Map/Gimmicks/StateMover.cs`: `ResetImmediately()` 추가(기존 동작 변경 없음). `ApplyPose` 는 `Time.deltaTime` 기반이라 시간 정지 중 안 움직이므로 위치를 직접 넣음.
  - `Core/Stage/Security/BossRound.cs`: `_sinkPlatforms`·`_respawnPlatform`·`_arenaEntrance` 필드, `LowerPlatforms()`·`RestoreRespawnPlatform()`, `Begin`/`End` 에서 트리거 구독·해제.
  - `Core/Stage/SecurityApp.cs`: `OnRoundChanged` 재정의(보스 라운드면 `LowerPlatforms`), `HandlePlayerRespawnRequested` 에 보스 라운드 case 추가.
  - 테스트: `Assets/Test/Stage/BossRoundPlatformTests.cs`(Git 제외 폴더) 3개.
- 씬 (Editor MCP, YAML 직접 수정 없음)
  - Platform_1~6: Batching Static 만 해제(Everything → -5, 보스 타일과 같은 값), `StateMover`(`_moveOffset` (0,-40,0), `_duration` 3초, 임시값) + `TransformMotor` 자동 추가.
  - `05_Boss/BossArenaEntrance`: BoxCollider(Trigger) 월드 12×3×60 m(부모 스케일 2 라 로컬 6×1.5×30), 보스 바닥 첫 줄(x 322.6~334.6, z -20~40, y 0~3), 레이어 Ignore Raycast(사격 레이캐스트를 막지 않게, Player 레이어와 충돌은 켜져 있음), `TriggerPlate`(`_isOneShot` 끔).
  - `SecurityApp` 3라운드 칸 연결, 씬 저장.

## 2. 결정 사항
- 6번 발판은 보스 바닥에 올라서면 내리고, 3라운드 리스폰 때만 즉시 올린다(사용자 결정). 1~5 는 3라운드 체크포인트 도착 때 내린다.
- 새 범용 컴포넌트 없이 기존 `StateMover`·`TriggerPlate` 재사용. `StateMover` 에는 메서드 추가만(작성자 양준범에게 알려야 함).
- 발판은 끄지 않고 FallZone(y=-12)보다 깊게(-40 m) 내려 둔다.

## 3. 현재 상태 및 이슈
- 리컴파일 에러 0. PlayMode 18/18 통과(새 테스트 3개 포함). EditMode 는 테스트 0개로 잡혀 게이트가 BLOCKED 를 냄(이 프로젝트 테스트는 asmdef 없이 PlayMode 로만 잡힘, 기존부터 그랬음).
- Play 수치 확인(순간이동·사망 처리 직접 호출로 진행):
  - 3라운드 진입 → P1~5 y 0 → -40, P6 그대로, 플레이어 P6 위 유지.
  - 바닥 진입 → P6 내려감. `HandlePlayerDeath` → P6 즉시 y 0, 플레이어 (317.1, 0.2) 리스폰·유지. 다시 바닥 진입 → P6 다시 내려감.
- 주의: 2라운드를 깨지 않고 순간이동하면 P2~6 이 꺼져 있어(`SpecialMobRound` 가 시작 때 끔) 확인이 안 된다. 테스트 때는 발판을 켜고 진행했다.
- 2라운드 체크포인트·3라운드 체크포인트 근처에서 컷씬(`CinematicManager`)이 시간을 멈춰, 확인 중 `Stop()` 으로 끊었다.
- **육안 확인·게이트 루프·커밋 전.** 실제 조작으로 걸어 들어가는 동선도 아직 확인 안 함.

## 5. 추가 변경: 다 내려간 발판 끄기 (사용자 요청)
- 다 내려간 발판은 `SetActive(false)` 로 끄고, 3라운드 리스폰 때 `Platform_6` 은 원위치로 놓은 뒤 `SetActive(true)`.
- `StateMover.IsMoving`(읽기 전용, 신규) 추가. `BossRound.HideLoweredPlatforms()` 가 "내려가는 쪽(`IsActivated`)이고 멈춘(`!IsMoving`)" 발판만 끄며, `SecurityApp.Update`(부모 `StageBase.Update` 호출 뒤)가 보스 라운드 동안 매 프레임 부른다. `RestoreRespawnPlatform` 은 `ResetImmediately` 후 켠다.
- 테스트 2개 추가(총 5개). 리컴파일 에러 0, PlayMode 20/20(첫 시도는 컴파일 직후 HTTP 400, 에디터 ready 확인 후 재실행 통과).
- Play 수치: 3라운드 진입 후 P1~5 y -40·꺼짐, P6 켜짐·플레이어 그 위 / 바닥 진입 → P6 -40·꺼짐 / 사망 처리 → P6 y 0·켜짐, 플레이어 (317.1, 0.2) / 재진입 → 다시 -40·꺼짐. 이번 실행 콘솔 에러 0.

## 6. 추가 변경: 1라운드 구간 숨기기 (사용자 요청)
- 보스 라운드가 시작되면(3라운드 체크포인트 도착) `Stage_BossRun/03_Round1_Battle` 을 `SetActive(false)`. 사망·리스폰해도 다시 켜지 않는다.
- `BossRound._hiddenAreas`(GameObject 배열)·`HideAreas()` 추가, `SecurityApp.OnRoundChanged` 가 `LowerPlatforms` 와 함께 호출. 씬에 `03_Round1_Battle` 연결·저장.
- 끄기 전 확인: `CombatZone` 은 `OnDisable` 처리가 없고, 바깥에서 `03_Round1_Battle` 안을 참조하는 것은 `SecurityApp._mobRoomRound`(1라운드에만 동작)뿐.
- 테스트 1개 추가(총 6개), 리컴파일 에러 0, PlayMode 21/21. Play: 3라운드 진입 후 `03_Round1_Battle` 꺼짐, P1~5 꺼짐·P6 켜짐 / 사망 후에도 꺼진 채, P6 위 리스폰.

## 7. 추가 변경: 남아 보이던 1라운드 쪽 오브젝트 숨기기 (사용자 지적)
- 사용자 스크린샷: 보스전 중 뒤쪽에 폭발 통·커버 생성기·시작 방이 남아 보임. `03_Round1_Battle` 밖(씬 최상위 등)에 있던 것들.
- `_hiddenAreas` 를 15개로 늘림(씬 연결만, 코드 변경 없음): `03_Round1_Battle`, `Stage_BossRun/01_PlayerSpawn`, `02_Corridor`, `Checkpoints/Checkpoint_Round1_Start`(시작 발판 큐브), `Security_CoverGenerator` 2개, `Security_ExplosiveBarrel` 9개.
- 바깥 참조 확인: 새로 넣은 것 중 외부 참조는 `StageManager._checkpoints[0]`(1라운드 체크포인트)뿐. 3라운드 리스폰은 `[2]` 를 쓰고, 0번은 꺼져도 위치만 읽으므로 영향 없음.
- 확인: Play 에서 `HideAreas()` 를 직접 호출 → x<229 에 남은 렌더러는 플레이어·펫·1라운드 미리 생성된 적 4마리(아직 1라운드라서, 실제 흐름에선 처치됨)뿐. 이번 Play 는 에디터가 포커스를 잃어(`Application.isFocused` false, `runInBackground` false) 프레임이 1에서 멈춰 순간이동 기반 흐름 확인은 못 함 — 라운드 전환 → `HideAreas` 호출 흐름은 6절에서 확인됨.
- 참고: 스크린샷의 흰 선 상자는 트리거 기즈모로 보임(Game 뷰 Gizmos), 빌드에는 안 보임.

## 4. 다음 할 일 (Next Steps)
- 사용자 육안 확인: 내려가는 속도·깊이(40 m·3초 임시값), 보스 바닥에서 P6 이 사라지는 타이밍.
- 실제 조작으로 Platform_6 → 보스 바닥 진입, 3라운드 사망 후 리스폰 확인.
- 양준범에게 `StateMover.ResetImmediately` 추가 공유.
- 확인 후 intent-069 resolved, AI_TODO 보스-14 체크.
