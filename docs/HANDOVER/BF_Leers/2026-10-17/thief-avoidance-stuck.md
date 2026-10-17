# 도둑끼리 웨이포인트에서 맴돌며 겹쳐 붙는 문제 (에이전트 회피 끄기)

## 작업 요약 및 방법
- 증상(사용자 보고): 도둑 한 마리가 길에 서 있고 → 뒤에 온 도둑이 못 지나가다 겹쳐 붙고 → 다른 도둑들도 끼어 한 덩어리가 된다.
- 확인한 사실
  - Enemy ↔ Enemy 물리 충돌은 꺼져 있다(intent-022 3차). CC끼리 끼는 것이 아니다.
  - `Thief.prefab` `NavMeshAgent`의 Obstacle Avoidance가 High Quality였다. `AIController`가 쓰는 `desiredVelocity`에는 이 회피가 섞인다.
  - `Thief.prefab` 루트 스케일이 2라서 CC 실제 반지름은 0.6인데, `AllySeparationPusher._pushRadius`는 0.9였다(두 몸이 닿는 거리 1.2m).
- Play 진단(`Assets/Test/AI/Map_3_File 1.unity`, MCP eval)
  - 같은 웨이포인트 (50,165)를 목적지로 잡은 도둑 7마리가 0.4m 안에 겹쳐, 남은 거리 0.26~0.36m에서 맴돌았다(도착 판정 0.25m). `desiredVelocity` 방향이 경로 방향과 크게 달랐다 → 서로를 회피하느라 도착하지 못한다.
  - 회피만 런타임으로 끄자 약 30초 안에 덩어리가 풀렸다.
- 수정(프리팹 값만, MCP `set_serialized_field` + `AssetDatabase.SaveAssets`, 코드 변경 없음)
  - `Thief.prefab` `NavMeshAgent` Obstacle Avoidance Type → None
  - `Thief.prefab` `AllySeparationPusher._pushRadius` 0.9 → 1.4
- 검증(새 Play마다 약 250초, 도둑 20마리 순찰만, 0.5초 간격 샘플)
  - 1.2m 안에 동료가 있는 비율 13.1% → 5.0%, 0.6m 안에 겹친 비율 5.9% → 2.6%, 최대 덩어리 4마리 → 3마리
  - 게임 콘솔 에러 0(콘솔의 에러 2건은 내가 MCP에 enum 값을 잘못 넣은 명령 실패 로그다)

## 결정 사항
- 도둑의 NavMeshAgent는 경로 계산 전용으로 두고 에이전트 회피를 쓰지 않는다. 도둑끼리 겹침은 `AllySeparationPusher`만 맡는다.
- `_pushRadius`는 스케일을 반영한 CC 실제 반지름 합보다 조금 크게 둔다(툴팁 규칙).

## 현재 상태 및 이슈
- 도망 중(플레이어가 쫓을 때)은 검증하지 못했다. 플레이어를 NavMesh 경로로 자동 이동시키려 했지만 플레이어 위치 (45,0)에서 미로로 가는 경로가 잡히지 않아 플레이어가 움직이지 않았다.
- `GC`·`Melee`·`Special`·`Ranged`도 회피가 High Quality이고, Melee·Special은 스케일 2 + `_pushRadius` 0.9라 같은 조건이다. 바꾸지 않았다.
- MCP로 Play를 켜면 첫 프레임에서 멈춘다(frameCount 1 고정). `EditorApplication.update`에 "일시정지 상태면 `Step()`" 콜백을 걸고 `isPaused = true`로 두면 진행된다. 도둑 스포너는 `_activeRoundIndex`(1)라 라운드 0에서 안 나오므로, 측정 때는 런타임에 0으로 바꿨다(저장 안 함).
- 이 세션의 변경은 커밋하지 않았다.

## 다음 할 일 (Next Steps)
- 사용자가 Play에서 도둑을 쫓아 미로 좁은 길에서 엇갈리는 장면을 육안으로 확인한다. 서로 통과하면서 옆으로 밀려나는 것은 기존 설계상 정상이다.
- 다른 적 프리팹의 회피도 끌지 사용자와 정한다.
