# 낙하 복귀·사망 후 재스폰 구현 (intent-017)

## 작업 요약 및 방법
Core 안에서만 수정해 PlayerFacade 기반으로 낙하 복귀와 사망 복구를 구현했다. 설계·확정 내용은 [intent-017](../intent/intent-017-fall-death-recovery-playerfacade.md).

| 파일 | 변경 |
|---|---|
| `Core/PlayerState.cs` | `AttributeSet` 참조를 `PlayerFacade`로 교체 (`CurrentFacade`, `SetCurrentPlayer(PlayerFacade)`). 라운드 스냅샷 함수 이름·구조는 유지 |
| `Core/GameManager.cs` | `CurrentPlayerFacade` 추가, `CompletePlayerSpawn(PlayerFacade)`, `EPlayerSpawnReason.RoundRecovery` 추가. `CurrentPlayerState`·`OnPlayerSpawned`는 HUD(Core 밖)를 위해 `AttributeSet` 유지 (facade의 root에서 조회) |
| `Core/PlayerSpawner.cs` | `GetSpawnPoint()`로 위치를 받아 스폰만 수행. `StageManager.OnPlayerRespawnRequested` 구독, `RoundRecovery`에도 기본 Effect 적용, `PlayerFacade`를 찾아 `CompletePlayerSpawn`에 전달 |
| `Core/Stage/StageManager.cs` | 플레이어 참조를 `PlayerFacade` 하나로 교체, `GetSpawnPoint()`(현재 라운드 체크포인트), 낙하 처리, 사망 시 재스폰 요청 + 재스폰 후 라운드 스냅샷 복원, `EndStage()` 제거 |
| `Core/Stage/FallZoneTrigger.cs` | 신규. `CheckpointTrigger`와 같은 패턴의 낙하 구역 트리거 |

인코딩: `PlayerSpawner.cs`, `StageManager.cs`는 기존대로 CP949 + CRLF 유지.

## 결정 사항
- 사망 시 `CharacterMediator`가 플레이어를 파괴하므로(`CharacterMediator.cs:239`) 사망 복구는 이동이 아니라 재스폰. 위치는 `StageManager`, 스폰은 `PlayerSpawner`.
- 저장 데이터는 라운드 스냅샷(체력·배터리)뿐. 일반 체크포인트는 인덱스만 `StageManager`가 보관.
- 중복 처리 방지는 `_isRecovering` bool 하나로 처리 (상태 머신 미도입).
- 낙하 생존: 데미지 후 현재값 유지, 도달한 체크포인트 중 가장 높은 인덱스로 이동 (`CharacterController`를 잠시 껐다 켬).
- 재스폰은 사망 다음 프레임에 `PlayerSpawner`가 수행한다(`CoSpawnPlayerNextFrame`). 같은 프레임에 만들면 이전 플레이어가 아직 파괴 전이라 `PlayerInput`이 입력 장치를 새 플레이어에 연결하지 못해 이동이 되지 않았다. 한 프레임 지연으로 해결됨(Play에서 확인).
- 체력 감소는 `SetAttribute` → `AttributeSet` → `CombatMediator` → `CharacterCombat.Health` → `OnDeath`로 동기 전달됨을 코드로 확인.

## 현재 상태 및 이슈
- `dotnet build Assembly-CSharp.csproj --no-restore`: 오류 0. 새 경고: `OnStageFailed`가 더 이상 발생하지 않아 CS0067 (이벤트 선언은 외부 구독 호환을 위해 유지).
- 빌드 확인을 위해 Git에 올라가지 않는 `Assembly-CSharp.csproj`에 `FallZoneTrigger.cs`를 임시로 추가했다. Unity가 재생성한다.
- Play 확인됨: 사망 후 재스폰과 이동. 미검증(Unity MCP 없음): 나머지 시나리오(생존 낙하, 낙하 사망, 일반 체크포인트 이후 사망)와 HUD.
- 낙하 이동 후 `CharacterMotor`의 잔여 낙하 속도는 Core 밖이라 해결하지 못했다.
- 겹치는 낙하 구역 콜라이더에 동시에 닿으면 같은 프레임에 데미지가 두 번 들어갈 수 있다.
- `Map/Gimmicks`의 옛 `RespawnZone`/`PlayerRespawner`가 같은 씬에 있으면 충돌한다.

## 다음 할 일 (Next Steps)
1. 씬의 `StageManager`에서 `_fallZones`를 `FallZoneTrigger[]`로 다시 연결한다 (기존 BoxCollider 참조는 사라진다). 낙하 구역 오브젝트에 `FallZoneTrigger`를 붙이고 `_playerLayerMask`를 설정한다.
2. `_checkpoints`, `_roundCheckpointIndices` 연결을 확인한다.
3. Play 검증 남은 시나리오: 생존 낙하(데미지 유지·가장 높은 체크포인트), 낙하 사망, 일반 체크포인트 이후 사망(진행도·체력·배터리가 라운드 값으로 복원). HUD 재연결은 UI 작업 때 함께 확인한다.
4. 검증이 끝나면 intent-017을 `resolved`로 바꾸고 `intent/clear/`로 이동한다.
