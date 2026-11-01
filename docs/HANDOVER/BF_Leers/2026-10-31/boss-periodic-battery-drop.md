# 보스 주기적 배터리 드롭 (AI_TODO2 보스-37, intent-105)

## 작업 요약 및 방법
- 신규 `AI.Boss.Fight.BossBatteryDropper`: `BossEncounter.IsBegun` 동안 `_intervalSeconds`(20)마다 `BossArena` 의 `!IsHoleOrTelegraphed` 칸 중 무작위 하나의 중심 +1m 에 `Item_ChargeBattery` 를 `Instantiate`. 동시 `_maxCount`(2), `_itemLifetime`(15초) 뒤 `Destroy`.
- `IsBegun` 이 꺼지면(보스전 종료·체크포인트 재시작의 `Despawn`→`End`) 타이머 0, 남은 아이템 제거.
- `Map_4_Security.unity` 의 `/Stage_BossRun/05_Boss/BossFight` 에 컴포넌트 추가 후 `_encounter`·`_arena`·`_batteryPrefab` 연결(MCP), 씬 저장.

## 결정 사항
- 사용자 확정: 20초, 아레나 안전 타일 무작위, 최대 2개·15초 소멸, 보스전 내내 유지(페이즈별 정지 없음).

## 현재 상태 및 이슈
- 리컴파일 통과(`gate.cjs check recompile` 종료 코드 0). **Play·직접 플레이·게이트 루프 미실행, 커밋 전.**
- 드롭 위치는 타일 중심 고정(20m 칸이라 칸 안 무작위 오프셋은 넣지 않음).
- `Pretendard-Bold SDF.asset` 변경은 이 작업과 무관(에디터가 건드린 것으로 보임), 커밋에 포함하지 말 것.

## 다음 할 일 (Next Steps)
- 보스전에서 20초마다 배터리가 떨어지는지, 획득 시 에너지 회복, 재시작 시 타이머·아이템 초기화 확인 후 주기·개수 조정.
- 확인되면 intent-105 를 resolved 로 `docs/intent/clear/` 이동.
