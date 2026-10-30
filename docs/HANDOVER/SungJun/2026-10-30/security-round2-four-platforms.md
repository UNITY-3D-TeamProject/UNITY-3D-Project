# 보안맵 정리: 배치 아이템 제거 · 1라운드 몹 수 조정 · 2라운드 발판 4개 (intent-094)

## 1. 작업 요약 및 방법
- **배치 아이템 제거** (커밋 `ee6c4d55`): `03_Round1_Battle/Items`(회복·배터리 6개, 10-28 양준범 님 배치) 삭제. 아이템은 적 처치 드롭(근접·원거리 25%, 발판 클리어 확정 드롭)으로만 얻는다.
- **1라운드 몹 수 조정** (커밋 `fbf436ad`): 원거리 Room_1 +2, Room_2 +1, Room_3 +2, Room_4 +2, Room_4 근접 −1 → 6/7/5/4 = 22마리. 새 원거리 스포너는 방 트리거 안 NavMesh 위, 벽에서 2m 이상, 입구에서 먼 쪽 절반 중 다른 몹과 가장 먼 자리에 두고 입구를 보게 했다.
- **2라운드 발판 5→4** (intent-094, 커밋 전):
  - `Platform_5` 삭제. `Platform_6`, `05_Boss`, `BossArena`, `BossArenaFallZone`, `Checkpoint_Round3_Start`, `CinematicCam_SecurityBossArena`·`BossDown`, `Main Camera`를 x −31m 평행 이동 (Platform_6이 Platform_5 자리로).
  - 전체 `FallZone` 끝 x 462→431.
  - `SecurityApp._specialMobRound._steps` 4단계(4번 다음 = Platform_6), `_bossRound._sinkPlatforms` = Platform_1~4, `FallingHazardSpawner._platforms` = 1~4, 6.
  - `NavMesh-04_Platforms`·`NavMesh-05_Boss` 재생성.

## 2. 결정 사항
- 발판을 줄일 때는 뒤쪽(보스 구역 전체)을 당겨 발판 간격을 유지한다 (사용자 결정). 보스 구역 안의 상대 배치는 바꾸지 않았다.
- 씬에 x 좌표를 숫자로 저장한 설정은 없었다(직렬화된 Vector3/Bounds 검사). 모두 Transform 기준이라 평행 이동으로 충분하다.

## 3. 현재 상태 및 이슈
- 에디터에서 콜라이더를 옮긴 직후 NavMesh를 구우면 Physics 위치가 갱신 안 돼 옮긴 발판에 NavMesh가 안 생긴다. `Physics.SyncTransforms()` 후 다시 구워 해결했다.
- 확인: 특수몹 4곳·Platform_6 전면 NavMesh 있음. 보스 바닥 Boss NavMesh 25칸 중 16칸(바깥 줄 9칸 없음 — 이전 데이터와 동일).
- Play(프레임이 돌지 않아 처치·체크포인트를 직접 호출): 패널 `발판 1/4`→`4/4`, 4번 클리어 시 Platform_6 표시, 3라운드 체크포인트(Platform_6 위, x 373) 진입 시 라운드 2. 에러 0.
- **미확인**: 보스 등장, 2라운드 발판 내려가기, 보스전·탈출문은 프레임이 필요해 직접 확인 못 함. 직접 플레이 필요.
- 발판 수 변경·NavMesh는 아직 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- Unity를 앞에 두고 직접 플레이로 2라운드 → 보스 등장·보스전·탈출문까지 확인 후 intent-094 해결 처리.
- 양준범 님께 보스 구역 x −31m 이동 공유 (맵 담당 작업 위치가 바뀜).
