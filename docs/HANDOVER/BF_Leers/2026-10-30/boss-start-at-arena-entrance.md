# 보스전 시작을 아레나 진입으로 변경 (AI_TODO2 보스-29, intent-095)

## 작업 요약 및 방법
- 원인: `RoundSpawnerBase.Update` 가 `CurrentRoundIndex == _activeRoundIndex(2)` 이면 바로 `Spawn()` 을 불러, 3라운드 체크포인트에 도착하자마자 `BossFightSpawner` 가 B 등장 연출·`BossEncounter.Begin` 을 시작했다.
- 수정:
  - `BossFightSpawner`: `AllowStart()`/`_isStartAllowed` 추가. `Spawn()` 은 허가가 없으면 대기(매 프레임 재시도). `Despawn`·`OnDisable` 에서 허가를 거둔다.
  - `BossRound.HandleArenaEntered`: 아레나 진입 트리거(`_arenaEntrance`) 발동 시 `AllowStart()` 호출.
- 씬·프리팹 변경 없음. 트리거 `_isOneShot: 0` 확인(재진입 때 재발동).

## 결정 사항
- 새 트리거를 만들지 않고 기존 아레나 진입 트리거(벽·발판 담당)를 재사용한다. `BossFightSpawner` 는 `Map_4_Security` 에서만 쓰여 항상 허가 필요 방식으로 바꿨다.
- 사망 시 `Despawn` 이 허가를 거두므로 리스폰 후 다시 아레나에 올라서야 보스전이 재시작된다.

## 현재 상태 및 이슈
- 리컴파일 통과(오류 0, 로그 `.claude/codex-review/check-ZiItR3/`). **Play·게이트 루프 미실행, 커밋 전.**
- 체크포인트에서 미리 세워 둔 A(BT 정지)를 쏘면 맞는지·트리거 박스가 체크포인트/리스폰 자리와 겹치지 않는지 미확인.

## 다음 할 일 (Next Steps)
- 플레이: 체크포인트에서는 보스전 미시작, 아레나 진입 시 처음 단계부터 진행, 사망 후 재진입 시 재시작되는지 확인.
- 확인 후 AI_TODO2 보스-29 체크, intent-095 를 `resolved` 로 바꿔 `clear/` 로 이동.
