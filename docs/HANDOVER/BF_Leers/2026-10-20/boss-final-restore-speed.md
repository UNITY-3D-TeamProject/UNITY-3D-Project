# 최종 단계(A 단독) 칸 복구 시간 단축

## 1. 작업 요약 및 방법
- 문제: A 단독 전투(최종 단계)에서 A2(한 줄 붕괴)가 연달아 나오면 칸이 6초 동안 복구되지 않아 플레이어가 설 칸이 없었다.
- `ArenaGrid.SetRestoreSeconds(float)` 추가: 복구 시간을 바꾸고, 이미 무너진 칸의 남은 시간이 새 값보다 길면 새 값으로 줄인다.
- `BossArena._restoreSecondsFinal`(기본 3초) 추가, `BossArena.SetStage(EBossStage)`로 최종 단계면 이 값을, 아니면 `_restoreSeconds`를 쓴다. `ResetAll`은 `_restoreSeconds`로 되돌린다(보스전 재시작 대비).
- `BossEncounter.HandleStageChanged`의 `Final` 분기에서 `_arena.SetStage(stage)` 호출.
- `docs/features/boss-balance-tuning.md` 2-3 표에 `_restoreSecondsFinal` 행 추가.
- 실행자: Claude 직접 수정(3개 파일 몇 줄짜리 변경이라 Codex 위임 비용이 더 큼).

## 2. 결정 사항
- "올라오는 속도" = 칸 복구 대기 시간으로 해석했다(칸은 애니메이션 없이 즉시 복구되므로).
- 1페이즈 값(6초)은 그대로, 최종 단계만 3초(임시값, 사용자 체감 확인 후 조절).

## 3. 현재 상태 및 이슈
- `gate.cjs check recompile` 통과. Editor eval로 `ArenaGrid` 수치 확인(1초 진행 후 3초로 변경 → 2.9초 뒤 붕괴 유지, 3.1초 뒤 복구 / 새 붕괴 칸도 3초에 복구).
- **Play 육안 확인·게이트 루프 미실행, 커밋 전.** 씬 `BossArena`의 직렬화 값에는 새 필드가 아직 저장되지 않았다(코드 기본값 3초가 쓰인다).
- 3초로도 A2 연속을 버티기 힘들면 선택기 쪽(최종 단계 A2 연속 제한, `collapseLimit`)을 봐야 한다.

## 4. 다음 할 일
- 테스트 씬에서 최종 단계 진입 후 A2 연속 시 설 칸이 남는지 육안 확인, `_restoreSecondsFinal` 값 조절.
