---
id: intent-095
title: 보안맵 2라운드 특수몹을 발판을 밟을 때 소환하고, 지나온 발판은 없앤다
part: Stage / SecurityApp / Map
status: open   # open | resolved
created: 2026-10-30
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
`Map_4_Security` 2라운드(`SpecialMobRound`)는 발판 N 을 깨면 다음 발판 N+1 이 나타나고, **그 즉시 N+1 의 특수몹을 미리 만들어 제자리에 묶어 둔다**(`PrepareCurrentStep` → `CombatZone.Prepare`).
그래서 플레이어가 다음 발판으로 건너가기 전에 원거리 총으로 미리 쏴 잡을 수 있다(`CombatZone` 은 대기 중에 다 잡아도 깬 것으로 본다).
또 지나온 발판이 그대로 남아 있어 뒤로 돌아갈 수 있다.

## 현재 구조 (조사 결과)
- 발판 4개 + 다음 라운드 발판: `Platform_1`(x 242) · `Platform_2`(274) · `Platform_3`(305) · `Platform_4`(337) · `Platform_6`(368). 폭 25 m, 사이 틈 약 6.5 m.
- 2라운드 체크포인트(RespawnPoint x 241.28)는 **`Platform_1` 위**에 있고, `Platform_1` 의 `CombatZone`(x 234.7~254.6) 안에 들어 있다. 특수몹 스포너는 x 248.35 (리스폰 자리에서 약 7 m).
- 발판 1~4 에는 이미 `StateMover`(-40 m, 3초)가 붙어 있다. 보스 라운드 시작 때 `BossRound.LowerPlatforms` 가 이걸로 내리고, 다 내려가면 `HideLoweredPlatforms` 가 끈다.
- 사망: `StageManager.HandlePlayerDeath` → 라운드 체크포인트로 재스폰 + `OnPlayerRespawnRequested` → `SpecialMobRound.ResetUnclearedSteps`(못 깬 발판만 리셋, 깬 발판·나타난 발판은 유지).
- 낙사: `StageManager.HandleFallZoneEntered` → **도달한 가장 높은 체크포인트로 순간이동 + 낙하 데미지**만 하고 라운드 리셋 이벤트는 없다.
- `FallingHazardSpawner` 는 켜져 있는(`activeInHierarchy`) 발판에만 떨어뜨리므로, 꺼진 발판은 자동으로 빠진다.

## 기대 결과 (Proposed outcome)
### 1. 소환 시점 — 발판을 밟을 때
- `SpecialMobRound` 에서 미리 만들기(`PrepareCurrentStep`)를 없앤다. 특수몹은 플레이어가 그 발판의 `CombatZone` 트리거에 들어온 순간(`StartZone` → `Prepare`) 만들어진다. `CombatZone` 은 이미 "대기 없이 들어오면 그때 만든다"를 지원하므로 고치지 않는다 (1라운드 `MobRoomRound` 영향 없음).
- 다음 발판은 지금처럼 앞 발판을 깨면 나타나지만, 그 위에는 아무것도 없다.

### 2. 지나온 발판 없애기
- 발판 N 의 싸움이 시작되면(`CombatZone.OnStarted`) 바로 뒤 발판 N-1 을 `StateMover.Activate` 로 가라앉히고, 다 내려가면 끈다 (보스 라운드와 같은 방식, -40 m·3초).
- `SPlatformStep` 에 "이 구역이 놓인 발판(`StateMover`)" 필드를 추가해 명시적으로 연결한다.
- `Platform_1` 은 뒤 발판이 없다. `Platform_4` 는 `Platform_6`(3라운드 체크포인트) 도착 때 `BossRound.LowerPlatforms` 가 지금처럼 내린다.

### 3. 사망 시 — 2라운드 체크포인트에서, 발판은 앞에 하나만
- 재스폰 위치는 지금과 같은 2라운드 체크포인트(`Platform_1` 위).
- `ResetUnclearedSteps` 를 바꿔 발판 상태를 처음처럼 되돌린다: `Platform_1` 은 원위치(`ResetImmediately`)+켜기, `Platform_2~4`·`Platform_6` 은 원위치+끄기.
- 깬 발판 진행도 처리 방식은 열린 질문 1.

### 4. 낙사 처리 (새로 생기는 문제)
- 뒤 발판을 없애면, 지금 낙사 처리(2라운드 체크포인트로 순간이동)로는 **이미 가라앉은 `Platform_1` 자리 허공으로 보내져 계속 떨어진다** (보스 아레나에서 있었던 무한 낙하와 같은 문제).
- 처리 방식은 열린 질문 2.

### 5. 소환 연출 (보류 — 열린 질문 3 참고, 아래는 검토했던 안)
특수몹 프리팹에 붙이는 작은 컴포넌트 하나로 처리한다 (`CombatZone`·`MonsterSpawner` 는 그대로).
- 풀에서 꺼내질 때(`OnEnable`) 스케일을 거의 0(0.01)에서 원래 크기로 키운다 (약 0.6초, 끝에 살짝 튀는 ease-out).
- 자라는 동안 바닥에 DTT `CircleWarnning`(낙하물 예고와 같은 원형 채움)을 같이 채워 "여기서 나온다"를 보여 준다.
- **스케일이 작을 때 공격하는 문제**: 자라는 동안 매 프레임 `AIController.Hold` 를 걸어 이동·공격을 막는다 (`CombatZone` 이 대기 중 적을 묶는 것과 같은 방식). `StartZone` 이 소환 직후 `ReleaseHold` 를 부르므로 한 번이 아니라 매 프레임 다시 건다.
- 주의: 스케일 0 그대로는 콜라이더·NavMeshAgent 경고가 날 수 있어 0.01 에서 시작한다. 자라는 도중 죽거나 리셋(풀 반납)되면 다음에 꺼낼 때 원래 크기로 다시 시작하게 `OnEnable` 에서 항상 초기화한다.
- 피격은 막지 않는다 (작아서 맞추기 어려운 정도로 둔다). 무적이 필요하면 열린 질문 3.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안맵 플레이어, 맵 배치 담당
- 어떤 시스템/모듈이 건드려지는가:
  - `Core.Stage.Security.SpecialMobRound` (미리 만들기 제거, 뒤 발판 가라앉히기, 리셋 시 발판 복구)
  - `Core.Stage.SecurityApp` (낙사 처리 방식에 따라)
  - `Core.Stage.StageManager` (낙사 처리 방식에 따라: 낙사 알림 이벤트 또는 복귀 지점 덮어쓰기)
  - `Map_4_Security.unity`: `_steps` 에 발판 연결

## 제약 (Constraints)
- 씬·프리팹은 YAML 직접 수정 금지, Unity Editor MCP로 수정한다.
- `CombatZone` 은 1라운드와 공유하므로 동작을 바꾸지 않는다 (필요하면 추가만).
- 플레이어 생성 연출(`SpawnEffectDriver`, 아래→위로 몸이 그려짐)은 `Start` 에서 한 번만 재생되고 플레이어 셰이더 전용 사본을 쓰므로, 풀링되는 몹에 그대로 쓸 수 없다. 재사용하려면 재생 API·특수몹 셰이더 사본이 필요해 이번 범위에서 뺀다.

## 열린 질문 (Open questions)
1. (결정 2026-10-30) 사망 후 진행도: **2라운드 전체 처음부터.** 깬 구역도 리셋한다 (`CombatZone` 에 깬 구역까지 되돌리는 리셋 추가 필요). 특수몹 4마리·아이템 드롭 모두 다시.
2. (결정 2026-10-30) 낙사: **지금 발판(마지막으로 싸움이 시작된 발판)으로 복귀 + 낙하 데미지, 특수몹 유지.** `StageManager` 에 낙사 복귀 지점을 덮어쓰는 방법 추가 필요 (2라운드가 끝나거나 사망 리셋되면 원래대로).
3. (결정 2026-10-30) 소환 연출: **일단 연출 없이 즉시 소환.**
   - 스케일 업 + 바닥 원 + Hold 를 적 프리팹 컴포넌트(`EnemySpawnAppear`)로 만들어 Play 로 동작까지 확인했으나, 사용자가 "새 코드·적 컴포넌트 없이 되는 방법이 없으면 일단 그냥 소환"을 택해 되돌렸다 (스크립트 삭제, `Special.prefab` 원복).
   - 새 코드 없이 되는 방법은 없다 (Unity 기본 기능에 소환 시 자라기는 없고, Special 에 등장 애니메이션 클립도 없다). 나중에 다시 넣는다면 같은 로직을 적 쪽이 아니라 `CombatZone`(소환한 적의 `AIController` 를 이미 들고 있음)에 넣는 방법도 있다.
4. (결정 2026-10-30) 뒤 발판: **가라앉히기** (기존 `StateMover`, -40 m·3초) 후 끄기.
5. (참고) 리스폰 자리가 `Platform_1` 구역 안이라 재스폰하자마자 특수몹이 약 7 m 앞에서 소환된다. 소환 연출 시간 동안은 공격하지 않으므로 그대로 둘 예정.

## 진행 기록
- 2026-10-30 구현:
  - `SpecialMobRound`: 미리 만들기 제거(구역 트리거 진입 때 `CombatZone` 이 만듦). `SPlatformStep` 에 `Platform`(StateMover)·`FallReturnPoint` 추가. 구역 시작(`OnStarted`) 때 뒤 발판 `Activate`(가라앉힘) + 낙사 복귀 자리 설정. `ResetUnclearedSteps` → `ResetSteps`(깬 구역까지 `ResetAll`, 가라앉힌 발판 `ResetImmediately`, 첫 발판만 켜기). `HideLoweredPlatforms`·`ClearFallReturnPoint` 추가.
  - `CombatZone.ResetAll` 추가 (깬 구역도 처음 상태로).
  - `StageManager.SetFallReturnPoint` 추가: 지정돼 있으면 낙사 때 체크포인트 대신 그곳으로 복귀.
  - `SecurityApp`: 2라운드 동안 `HideLoweredPlatforms`, 다른 라운드가 되면 `ClearFallReturnPoint`, `Begin(_stageManager)`, 사망 시 `ResetSteps`.
  - 씬: `Platform_1~4` 자식으로 `FallReturnPoint`(발판 중심 x −8 m, y 0.2, +X 방향) 추가, `_steps` 에 발판·복귀 자리 연결.
  - Play 확인(프레임 진행·텔레포트·HP 0 으로 강제 진행): P1 진입 시에만 P1 몹 생성 / P1 클리어 후 P2 는 비어 있음 / P2 진입 시 P2 몹 생성·P1 이 -40 m 로 내려가 꺼짐 / P2 싸움 중 낙사 → P2 복귀 + 데미지 10 / 사망 → P1 만 원위치로 켜지고 진행도 초기화, 다시 태어난 자리에서 P1 몹 생성 / 1~4 클리어 후 3라운드 체크포인트 → 낙사 복귀 해제, 1~4 모두 내려감. 에러 0.
  - 남음: 직접 조작(점프로 건너가기·실제 전투) 확인, 리스폰 직후 P1 몹이 약 7 m 앞에서 바로 움직이는 손맛 확인.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
