# 보스 테스트 씬 시작 라운드 설정 (2026-10-20)

## 1. 작업 요약 및 방법
- 요청: 보스 테스트를 위해 씬에 빠진 세팅을 해달라.
- 점검: `Assets/Test/Boss/Map_4_Security_Boss_T`의 `BossFightSpawner`·`BossEncounter`·`BossArena`·타일 9개·`StageManager`·`PlayerSpawner`·`BossArenaFallZone` 참조는 모두 연결돼 있었다. `BossA`·`BossB` 프리팹의 빈 참조는 `BossAGrab`·`BossAGroggyPalm`·`BossBLeap(Evade)`의 `_hitEffect`/`_circlePrefab`뿐이며 피해가 없는 스킬이라 의도된 값으로 판단했다.
- 원인: `BossFightSpawner._activeRoundIndex`가 2라서 `StageManager`의 1·2라운드 체크포인트를 순서대로 통과해야 보스가 생성된다(텔레포트만으로는 라운드가 오르지 않음). 처음 Play 시 보스가 없었다.
- 변경(씬 저장, `Assets/Test`라 Git 미추적):
  - `/Stage_BossRun/05_Boss/BossFight` `BossFightSpawner._activeRoundIndex`: 2 → 0
  - `/StageManager` `_roundCheckpointIndices`: [0,1,2] → [2,1,2] (첫 스폰이 `Checkpoint_Round3_Start`)
- 검증: Play 시 플레이어 (231,0.28,10), `BossA(Clone)` (320,0,10), `BossB(Clone)` (289.25,0.05,10) 생성 확인. 코드·프리팹 변경 없음.

## 2. 결정 사항
- 테스트 씬에서만 시작 라운드를 보스 라운드로 당겨 둔다. 원래 흐름으로 되돌릴 때는 위 두 값을 원복한다.

## 3. 현재 상태 및 이슈
- 보스 전투 동작(패턴·낙사·예고 원 육안)은 이번에 검증하지 않았다.
- Play 중 콘솔 오류 집계 1건이 있으나 버퍼에서 원문을 확인하지 못했다(세션 M에 적힌 기존 Pipeline 오류로 추정).
- Play 초기 `ObjectPoolManager` 프리워밍에서 "Failed to create agent because it is not close enough to the NavMesh" 경고가 반복된다(보스 외 풀링 프리팹, 기존 현상으로 추정).
- 게이트 루프는 실행하지 않았다(코드 변경 없음, 씬은 Git 미추적).

## 4. 다음 할 일 (Next Steps)
- 보스 전투 Play 체크리스트(USER-4) 진행.
- 정식 흐름으로 복귀할 때 `_activeRoundIndex`=2, `_roundCheckpointIndices`=[0,1,2]로 원복.
