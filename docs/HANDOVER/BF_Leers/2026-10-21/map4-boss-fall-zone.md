# Map_4_Security 보스 아레나 낙사 시 옆 칸 복귀

## 1. 작업 요약 및 방법
- 문제: `Map_4_Security` 보스 아레나에서 떨어지면 항상 체크포인트(`Checkpoint_Round3_Start`, x≈317)로 돌아갔다.
- 원인: 씬에는 맵 전체 낙사 구역 `FallZone`(`FallZoneTrigger` → `StageManager.HandleFallZoneEntered` → 현재 체크포인트로 이동) 하나만 있었다. 칸 기준으로 되돌리는 `BossArenaFallZone`은 테스트 씬(`Assets/Test/Boss/...`)에만 붙어 있었다.
- 조치: 코드 수정 없이 `Stage_BossRun/BossArenaFallZone`(BossArena 바로 다음 형제)를 만들어 기존 `BossArenaFallZone` 컴포넌트를 붙였다. Unity MCP로 작업하고 씬 저장.
  - 트리거 월드 범위: x 322.64~390.64, y -8~-6, z -28~48 (아레나 60×60 m에서 발판 쪽(-x)을 뺀 세 방향으로 8 m 여유). 부모 스케일이 2라 BoxCollider size는 (34, 1, 38).
  - 높이는 칸 솟아오르기 깊이(4 m)보다 아래, 맵 `FallZone`(y -13~-11)보다 위라서 이 구역이 먼저 처리한다.
  - `_arena` = BossArena, `_fallEffect` = `SOAttributeEffect_BossFall`, `_playerLayers` = Player(6), `_respawnHeight` 0.5.
- 실행자: Claude 직접(Codex CLI가 PATH에 없어 위임 불가).

## 2. 결정 사항
- 복귀 칸은 기존 `ArenaGrid.FindNearestSafe` 규칙을 그대로 쓴다: 무너짐·예고 칸·B 착지 예약 칸을 빼고, 떨어진 위치에서 칸 중심이 가장 가까운 칸. 무너진 칸에서 떨어지면 보통 바로 옆 칸이 되고, 옆 칸이 모두 막혀 있으면 그다음으로 가까운 칸으로 간다.
- 낙사 피해로 HP가 0이 되면 이동하지 않고 기존 사망 흐름(체크포인트 재시작)에 맡긴다(기존 코드 동작).

## 3. 현재 상태 및 이슈
- Play 확인(에디터 포커스가 없어 프레임이 멈춰 `OnTriggerEnter`를 리플렉션으로 호출): 가운데 칸(12) 무너짐 + 7·11 예고 상태에서 떨어짐 → 13번 칸(364.64, 0.5, 10)으로 이동. 동쪽 바깥으로 떨어짐 → 14번 칸으로 이동. 치명 피해(임시 복제 SO) → HP 0, 이동 안 함.
- 실제 물리 낙하로 트리거가 발동하는 장면은 프레임 정지 때문에 확인하지 못했다.
- `SOAttributeEffect_BossFall`은 툴팁상 "최대 HP 30%"지만 실제 값은 `_postCalcAmount` 0.01(최대 HP 1%)이다. 이번에 바꾸지 않았다.
- B 돌진 직선·A4 그랩 직선 예고는 칸 상태를 바꾸지 않아 복귀 칸 판정에서 빠지지 않는다.
- 게이트 루프 미실행, 커밋 전.

## 4. 다음 할 일 (Next Steps)
- Play에서 실제로 구멍/가장자리로 떨어져 옆 칸으로 복귀하는지 육안 확인.
- 낙사 피해량(1% vs 30%) 결정 후 SO 값 또는 툴팁 정리.
