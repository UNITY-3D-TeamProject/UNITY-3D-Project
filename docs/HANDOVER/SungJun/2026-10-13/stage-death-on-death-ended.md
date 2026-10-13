# StageManager 사망 처리를 OnDeathEnded 구독으로 변경

## 1. 작업 요약 및 방법
- `StageManager.BindPlayer` / `UnbindPlayer`에서 구독하는 이벤트를 `PlayerFacade.OnDeath` → `PlayerFacade.OnDeathEnded`로 바꿨다.
  - 사망 애니메이션이 끝난 뒤에 라운드 복구(체크포인트 되돌리기, `PrepareSpawn`, `OnPlayerRespawnRequested`)가 시작된다.
  - `OnDeathEnded` 직후 `CharacterMediator.EndDeath`가 플레이어를 `Destroy`하고, 재스폰은 다음 프레임에 일어나므로 순서는 기존과 같다.
- `HandleFallZoneEntered`의 사망 확인 방식을 `_isRecovering` 확인 → 현재 체력(`CURRENT_HP_KEY`) `<= 0` 확인으로 바꿨다.
  - 기존: 낙하 데미지로 죽으면 `OnDeath`가 같은 프레임에 발생해 `_isRecovering = true`가 되었고, 그래서 체크포인트 이동을 건너뛰었다.
  - 변경 후: `OnDeathEnded`는 애니메이션이 끝난 뒤에 발생하므로 그 시점의 `_isRecovering`은 아직 false다. 체력으로 확인하지 않으면 죽은 플레이어가 체크포인트로 순간이동한다.
- 관련 주석 2곳을 갱신했다.
- 이어서 플레이어 파괴 책임을 `CharacterMediator`에서 `StageManager`로 옮겼다.
  - 사용자가 `CharacterMediator.EndDeath()`의 `Destroy(gameObject)`를 주석 처리했다.
  - `StageManager.HandlePlayerDeath()`에서 `_isRecovering = true`와 체크포인트 되돌리기 뒤, 재스폰 요청 직전에 `Destroy(_playerFacade.gameObject)`를 호출한다.
  - `Player.prefab`은 `CharacterMediator`와 `PlayerFacade`가 둘 다 프리팹 루트에 있어서, Facade의 GameObject가 곧 플레이어 인스턴스다.
  - `transform.root`를 쓰지 않은 이유: 나중에 플레이어가 다른 오브젝트(움직이는 발판 등)의 자식이 되면 root가 그 부모를 가리켜 부모까지 파괴된다.
  - 파괴는 프레임 끝, 재스폰은 다음 프레임(`PlayerSpawner.CoSpawnPlayerNextFrame`)이라 "이전 플레이어 파괴 → 새 플레이어 생성" 순서가 유지된다. 이전 플레이어가 남아 있으면 PlayerInput이 입력 장치를 새 플레이어에 연결하지 못한다.

## 2. 결정 사항
- 플레이어의 사망 → 파괴 → 재스폰은 `StageManager`가 관리한다. `CharacterMediator`는 플레이어를 파괴하지 않는다.
- 스테이지 복구(재스폰)는 사망 애니메이션이 끝난 뒤(`OnDeathEnded`)에 시작한다.
- 사망 애니메이션 동안에는 `IsLivingPlayerCollider`가 체력 0 이하를 걸러 내므로, 체크포인트나 낙하 판정이 일어나지 않는다.

## 3. 현재 상태 및 이슈
- 컴파일 OK (Unity recompile에서 에러 없음).
- Play로 직접 확인하지 않음: 전투 사망, 낙하 사망 모두 애니메이션이 끝난 뒤 라운드 시작점에서 재스폰되는지 확인해야 한다.
- **적·NPC(Melee, Ranged, Car, GC, Pedestrian)도 `CharacterMediator`를 쓰므로, `Destroy` 주석 처리 후에는 죽어도 씬에 남는다.** 사용자 결정으로 이번 작업에서는 보류했다.
- `HandlePlayerDeath` 앞쪽 조건(`!IsRunning`, 라운드 체크포인트 데이터 없음)에서 return하면 플레이어가 파괴되지 않는다. StageManager가 없는 씬(로비)에서 죽어도 파괴되지 않는다.
- `CharacterMediator.OnDeathEnded`·`EndDeath`, `PlayerFacade.OnDeathEnded` 주석의 "파괴" 문구가 실제 동작과 다르다 (수정하지 않음).
- 사망 애니메이션 종료 이벤트(`CharacterAnimator.OnDeathAnimationEnded`)가 오지 않으면 재스폰도 일어나지 않는다. 플레이어 사망 클립의 종료 이벤트 연결을 함께 확인해야 한다.

## 4. 다음 할 일 (Next Steps)
- Play 모드에서 전투 사망과 낙하 사망 모두 확인한다: 이전 플레이어가 Hierarchy에서 사라지는지, 새 플레이어가 하나만 생기고 조작되는지, 체력·배터리가 복원되는지.
- 적·NPC 사망 후 파괴(또는 풀 반납) 처리를 정한다.
