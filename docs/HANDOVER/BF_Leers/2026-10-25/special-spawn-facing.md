# 특수몹 스폰 방향 수정 (2026-10-25)

## 1. 작업 요약 및 방법
- 증상(사용자 보고): 2라운드 특수몹이 스폰될 때 플레이어 쪽이 아니라 플레이어 기준 오른쪽(-Z)을 보고 있음.
- 처음 가설(스폰 포인트 회전)은 틀림: `Platform_1~5/CombatZone/SpawnPoint_1` 은 모두 이미 월드 -X(Y 270°, 플레이어가 다가오는 쪽)를 보고 있었다.
- 실제 원인: `Special.prefab` 의 `AIController._body` 가 같은 이름의 **눕혀진 메쉬 자식 `Nexus Robot`(로컬 X 270°, forward = 월드 위)** 을 가리키고 있었다.
  - `CombatZone.HoldWaitingEnemies` → `AIController.Hold(Vector3.zero)` 가 "지금 방향 유지"를 위해 `_body.forward` 를 쓰는데, 위쪽 벡터라 수평 성분이 0 → hold 방향 `(0,0)` → 회전 고정이 안 돼 다른 회전 요청에 밀려 -Z 쪽으로 돌았다.
  - 다른 AI 프리팹(Melee·Ranged·BossB·GC·Thief)은 모두 `_body` 가 로컬 회전 0 의 모델 루트(Animator 오브젝트)다.
- 수정: Unity MCP(`PrefabUtility.LoadPrefabContents`)로 `Special.prefab` 의 `_body` 를 Animator 가 붙은 `Nexus Robot`(루트 직속, 로컬 회전 0)으로 교체. 코드 변경 없음.
  - 저장 시 `Sensor._proximityRange: 0` 이 함께 기록됨(새 필드가 기본값으로 직렬화된 것, 동작 변화 없음).
- 실행자: Claude 직접(참조 1개 교체 + 이미 연결된 에디터로 검증까지 이어서 진행, Codex CLI 위임 비용 대비 작업이 작음).

## 2. 결정 사항
- AI 프리팹의 `AIController._body` 는 로컬 회전이 0 인 모델 루트(Animator 오브젝트)를 가리킨다. FBX 메쉬 자식(X 270° 등 축 보정된 것)을 가리키면 `_body.forward` 기반 로직(Hold 방향, 피격 돌아보기 등)이 깨진다.

## 3. 현재 상태 및 이슈
- Play 검증: 수정 전 스폰 직후 yaw 270° → 262.8°로 돌기 시작 / hold 방향 (0,0). 수정 후 hold 방향 (-1,0), 1.3초(66프레임) 동안 yaw 270° 유지.
- 에디터가 백그라운드일 때 Play 프레임이 진행되지 않아 `EditorApplication.Step` 으로 진행시켜 확인했다.
- **육안 확인(플레이어가 실제로 발판에 접근할 때 정면 확인)·게이트 루프 미실행, 커밋 전.**

## 4. 다음 할 일
- 사용자 육안 확인: 2라운드 발판 1~5 에서 특수몹이 다가오는 플레이어를 정면으로 보는지.
- 다른 맵에서 `Special` 을 쓰는 곳이 있으면 같이 확인(같은 프리팹이므로 자동 반영).
