---
id: intent-017
title: PlayerFacade 기반 낙하 복귀와 사망 복구 구현
part: Stage / Player State
status: open
created: 2026-10-09
resolved: null
---

## 문제 (Problem)
- 낙하 구역(`_fallZones`, `_fallDamage`)은 필드만 있고 처리 코드가 없다.
- `StageManager.HandlePlayerDeath()`는 사망 시 스테이지를 실패 처리(`EndStage`, `OnStageFailed`)한다. 요구는 "라운드 체크포인트에서 이어서 진행"이다.
- `PlayerState`, `GameManager`, `StageManager`가 `AttributeSet`, `CombatMediator`를 직접 잡고 있다. 플레이어와의 접점을 `PlayerFacade`로 일원화해야 한다.
- 사망 시 `CharacterMediator.OnDeathCallback`이 `Destroy(gameObject)`를 호출한다(`CharacterMediator.cs:239`, Core 밖). 사망한 플레이어는 이동시킬 수 없고 새로 스폰해야 한다.

## 기대 결과 (Proposed outcome)
규칙(확정):
| 상황 | 위치 | 체력·배터리 | 일반 진행도 |
|---|---|---|---|
| 낙하 후 생존 | 도달한 체크포인트 중 가장 높은 인덱스 (StageManager가 기존 플레이어 이동) | 낙하 데미지 받은 현재값 유지 | 유지 |
| 사망 (낙하·전투 모두) | 현재 라운드의 라운드 체크포인트 (StageManager가 위치 결정, PlayerSpawner가 새로 스폰) | 라운드 체크포인트 최초 접촉 시 값으로 복원 | 라운드 체크포인트로 되돌림 |

설계:
- 저장 데이터는 **라운드 스냅샷(체력·배터리)뿐**이다. 일반 체크포인트는 복원에 쓰이지 않으므로 값 저장 없이 `StageManager`의 인덱스(`_currentCheckpointIndex`)만 갱신한다.
- `PlayerState`는 기존 `Save/Restore/Has/ClearRoundSnapshot` 구조를 유지하고 `AttributeSet` 참조를 `PlayerFacade`로 교체한다 (`HasAttribute`/`GetAttribute`/`SetAttribute` 사용).
- `GameManager.CompletePlayerSpawn`, `OnPlayerSpawned`, `CurrentPlayerState`와 스포너 시그니처를 `PlayerFacade` 기준으로 맞춘다.
- `StageManager`는 플레이어 참조를 `PlayerFacade` 하나로 두고 `PlayerFacade.OnDeath`를 구독한다. 사망 시 스테이지 실패 처리를 하지 않는다.
- 낙하: `SetAttribute`로 체력을 깎는다. 사망 이벤트가 발생했는지를 한 bool로 확인해 사망이면 이동하지 않고, 생존이면 `facade.transform`과 `CharacterController`를 잠시 껐다 켜서 이동한다. `PlayerFacade`는 수정하지 않는다.
- 사망 후 재생성: `StageManager`가 스폰 위치(라운드 체크포인트)를 정하고, `PlayerSpawner`는 그 위치에 스폰만 한다. 기본 Effect 적용 후 라운드 스냅샷을 복원한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 스테이지를 플레이하는 플레이어, HUD(스폰 시 재연결).
- 어떤 시스템/모듈이 건드려지는가: `Core/PlayerState`, `Core/GameManager`, `Core/PlayerSpawner`(+`SpawnerBase`), `Core/Stage/StageManager`. `CheckpointTrigger`는 변경 없음. 씬 Inspector 재연결(`_fallZones` 등) 필요.

## 제약 (Constraints)
- 게임 코드 수정은 `Core` 안에서만 한다. `PlayerFacade`, `CharacterMediator`, `Map/Gimmicks`는 수정하지 않는다.
- `StageManager.cs`는 CP949 인코딩이므로 유지한다.
- 과설계 금지. 앞선 설계(`2026-10-08/stage-round-recovery-design.md`)에서 아래는 **채택하지 않는다**:
  - `ERecoveryState` 상태 머신과 `_hasPendingDeath` → 중복 방지용 bool 하나로 대체
  - 스냅샷 키를 체크포인트 인덱스로 일반화, `ClearSnapshotsExcept` → 일반 체크포인트 값은 저장하지 않으므로 불필요
  - `BeginStageSession`/`TryCapture…` 등 PlayerState API 이름 변경 → 기존 이름 유지
  - 사망 시 "기존 플레이어 이동·부활" 경로 → 사망 시 플레이어가 파괴되므로 해당 없음
- 옛 낙하 코드(`Map/Gimmicks`의 `RespawnZone`, `PlayerRespawner`, `Checkpoint`)는 건드리지 않는다. 같은 씬에 있으면 충돌하므로 사용 여부만 확인한다.
- 기존 사망 구독 흐름은 `feature/System/DeathSystem` 브랜치에 있으므로 병합 순서를 팀과 확인한다.

## 열린 질문 (Open questions)
- 해결됨: 사망 시 Destroy 유지·파괴 후 재스폰으로 확정, 스포너는 위치를 판단하지 않음(StageManager.OnPlayerRespawnRequested 구독), 스폰 사유는 EPlayerSpawnReason.RoundRecovery 추가, 낙하 감지는 FallZoneTrigger 신규, 낙하 데미지는 SetAttribute로 OnDeath까지 동기 전달됨을 코드로 확인.
- 해결됨: 같은 프레임 재스폰 시 PlayerInput이 장치를 연결하지 못해 이동 불가 → 재스폰을 다음 프레임으로 지연(PlayerSpawner.CoSpawnPlayerNextFrame).
- Play 검증 필요: 생존 낙하, 낙하 사망, 일반 체크포인트 이후 사망 시나리오, 낙하 후 CharacterMotor 잔여 낙하 속도(Core 밖). HUD 재연결은 UI 작업 때 확인.

## 해결 기록 (Resolution)
- 구현 완료(코드): PlayerState/GameManager/PlayerSpawner/StageManager를 PlayerFacade 기준으로 전환, FallZoneTrigger 추가, 사망은 파괴 후 라운드 체크포인트 재스폰 + 스냅샷 복원. dotnet build 오류 0. Inspector 재연결과 Unity Play 검증 대기이므로 open 유지.
- 관련 커밋/PR: 없음
