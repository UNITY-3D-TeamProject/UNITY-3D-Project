---
id: intent-069
title: 보스전 진입 시 2라운드 발판(스테이지2)을 아래로 내려 없애기, 3라운드 리스폰 때만 리스폰 발판 복구
part: Stage / SecurityApp / Map
status: open   # open | resolved
created: 2026-10-24
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
`Map_4_Security` 보스전(3라운드)에 들어가도 2라운드 특수몹 발판(`Stage_BossRun/04_Platforms/Platform_1~6`)이 그대로 남아 있다 (AI_TODO 보스-14).
- 3라운드 체크포인트(`Checkpoint_Round3_Start`)의 트리거와 리스폰 위치가 `Platform_6` 위에 있어, 6번 발판을 그냥 내리면 보스전 시작·리스폰 때 플레이어가 함께 떨어진다.
- 발판 6개가 Static(Everything)이라 Batching Static 을 끄지 않으면 움직여도 화면에서는 그대로 보인다.

## 기대 결과 (Proposed outcome)
- 3라운드 체크포인트에 도착하면(보스 라운드 시작) `Platform_1~5` 가 아래로 내려간다.
- 플레이어가 보스 바닥에 올라서면(`TriggerPlate`) `Platform_6` 이 내려간다.
- 3라운드 중 죽으면 `Platform_6` 만 즉시 원위치로 올라오고 그 위에 리스폰한다. 다시 보스 바닥에 올라서면 또 내려간다. 1~5 는 내려간 채로 둔다.
- 움직임은 기존 `StateMover`(Activate)를 쓰고, 즉시 원위치는 `StateMover.ResetImmediately()` 를 새로 추가한다. 깊이 40 m·3초(임시값).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안 앱 플레이어, 맵 담당, `StateMover` 작성자(양준범)
- 어떤 시스템/모듈이 건드려지는가:
  - 수정: `Map/Gimmicks/StateMover.cs`(메서드 추가만), `Core/Stage/Security/BossRound.cs`(필드·메서드 추가), `Core/Stage/SecurityApp.cs`(`OnRoundChanged` 재정의, 3라운드 사망 처리 추가)
  - 씬: `Map_4_Security.unity` 발판 6개 Batching Static 해제·`StateMover` 추가, 보스 바닥 입구 `TriggerPlate` 추가, `SecurityApp` 보스 라운드 칸 연결

## 제약 (Constraints)
- 지켜야 할 정책: `StateMover` 기존 동작 변경 금지(추가만). 씬은 YAML 직접 수정 금지, Editor MCP 로만.
- 쓰지 말아야 할 것: 새 범용 컴포넌트 추가 금지(기존 `StateMover`·`TriggerPlate` 재사용).
- 실행자: Codex CLI 가 이 PC 에 설치돼 있지 않아(도구 미지원) Claude 가 직접 구현·연결한다.

## 열린 질문 (Open questions)
- ~~6번 발판 처리~~ → 보스 바닥에 올라서면 내리고, 3라운드 리스폰 때만 즉시 올림 (사용자 결정, 2026-10-24)
- ~~1~5번 내리는 시점~~ → 3라운드 체크포인트 도착(보스 라운드 시작) 때 (사용자 확인, 2026-10-24)
- ~~StateMover 수정 방식~~ → 즉시 원위치 메서드 1개 추가 (사용자 확인, 2026-10-24)
- 깊이 40 m·시간 3초는 임시값. 육안 확인 때 조정.

## 진행 상황 (2026-10-24)
- 코드·씬 연결 완료, 리컴파일 에러 0, PlayMode 18/18(새 테스트 3개). Play 수치: 3라운드 진입 시 P1~5 → -40 m, P6 유지 / 바닥 진입 시 P6 내려감 / 사망 처리 시 P6 즉시 원위치·그 위 리스폰 / 재진입 시 다시 내려감.
- 추가(사용자 요청): 다 내려간 발판은 `SetActive(false)`, 3라운드 리스폰 때 `Platform_6` 은 원위치 후 `SetActive(true)`. `StateMover.IsMoving`(읽기 전용) 추가, `SecurityApp.Update` 가 보스 라운드 동안 `BossRound.HideLoweredPlatforms` 호출. PlayMode 20/20, Play 꺼짐·켜짐 확인.
- 추가(사용자 요청): 보스 라운드 시작 시 `03_Round1_Battle` 도 `SetActive(false)`(`BossRound._hiddenAreas`·`HideAreas`), 사망 후에도 유지. PlayMode 21/21, Play 확인.
- 추가(사용자 지적): 남아 보이던 `01_PlayerSpawn`·`02_Corridor`·`Checkpoint_Round1_Start`·`Security_CoverGenerator` 2·`Security_ExplosiveBarrel` 9 도 `_hiddenAreas` 에 연결(총 15).
- 남은 것: 육안 확인(속도·깊이, 남는 오브젝트 없는지), 실제 조작 동선 확인. 확인 전까지 open 유지. → [작업 기록](../HANDOVER/BF_Leers/2026-10-24/boss-arena-stage2-sink.md)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
