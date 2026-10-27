# 보스 아레나 낙사 후 허공 복귀·무한 낙하 원인 진단 (2026-10-26)

## 1. 작업 요약 및 방법
- 증상(사용자 보고): 보스 아레나 가장자리에서 떨어지면 안전한 칸이 아니라 아래쪽에서 나타나 계속 떨어진다.
- 수정 없음. 코드(`BossArenaFallZone`, `BossArena`, `StageManager`, `BossRound`)를 읽고 `Map_4_Security` 를 에디터 eval 로 수치 조회했다(Play 재현은 하지 않음).

조회 결과(에디터, Play 전 배치값)
| 대상 | 범위 |
|---|---|
| 아레나 칸 콜라이더 | X 322.64 ~ 382.64, Z -20 ~ 40, 윗면 y 0 |
| `BossArenaFallZone` 박스 | X **322.64** ~ 390.64, Z -28 ~ 48, y -8 ~ -6 |
| 스테이지 `FallZone`(StageManager) | X 22 ~ **372**, Z -50 ~ 70, y -13 ~ -11 |
| 3라운드 체크포인트 RespawnPoint | (317.12, 0.2, 10), 부모는 `Checkpoints/...` (Platform_6 자식 아님) |
| `Platform_6` (RespawnPoint 아래 유일한 바닥) | X 311.87 ~ 321.87, Z 5 ~ 15 |

## 2. 원인
1. `BossArenaFallZone` 박스가 X 방향으로 치우쳐 있다. Z 는 아레나 양옆 8 m 여유가 있지만 X 는 -X(입구 쪽) 여유 0 m, +X 여유 8 m. -X 가장자리에서 떨어지면 아레나 낙사 구역을 지나치고 그 아래 스테이지 `FallZone` 에 걸린다.
2. 스테이지 `FallZone` 은 `StageManager.HandleFallZoneEntered` 로 처리되어 현재 체크포인트(3라운드) RespawnPoint 로 옮긴다. 그런데 그 자리의 바닥 `Platform_6` 은 보스 바닥 진입(`BossRound.HandleArenaEntered`) 때 -40 m 내려가고 `HideLoweredPlatforms` 로 꺼진다. 복구(`RestoreRespawnPlatform`)는 **사망 리스폰 때만** 한다. 그래서 바닥 없는 허공(317, 0.2, 10)에 놓여 다시 떨어지고, 같은 `FallZone` 에 걸려 같은 자리로 되돌아가는 반복이 생긴다(낙사 피해로 죽기 전까지).
3. 추가 구멍: 스테이지 `FallZone` 은 X 372 까지만 있다. 아레나 +X 끝(382.64) 너머로 8 m 이상 날아가 떨어지면(빠른 이동·대시) 두 낙사 구역 모두 밖이라 아래에 아무것도 없어 정말로 끝없이 떨어진다.

## 3. 결정 사항
- 없음(진단만).

## 4. 현재 상태 및 이슈
- Play 재현 미실행. 위 결론은 배치 수치와 코드 경로로 확인한 것이다.
- 낙사 피해 후 무적 시간 등으로 피해가 안 들어가면 2번 반복이 죽지 않고 계속될 수 있으나 확인하지 않았다.

## 5. 다음 할 일
- 수정 방향(사용자 결정 필요): `BossArenaFallZone` 박스를 아레나 기준 사방 여유 있게(입구 쪽 포함) 넓히기, 스테이지 `FallZone` 이 보스 라운드에서 꺼진 `Platform_6` 자리로 보내지 않게 하기(또는 Platform_6 숨김 중 복귀 위치를 아레나로), +X 쪽 바닥 구멍 메우기.
- 수정 후 Play 로 -X·+X·Z 가장자리 낙하를 각각 확인.
