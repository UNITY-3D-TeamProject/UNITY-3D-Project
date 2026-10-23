# 스폰·체크포인트 방향 화살표와 SetLookDirection 확인 (intent-059, 진행 중)

## 1. 작업 요약 및 방법
- 플레이어 담당(이세훈)이 추가한 `PlayerFacade.SetLookDirection(Vector3)`(`673044e9`)를 확인했다. 몸 방향을 **고정**(`CharacterRotator.LockLookDirection`)하고 카메라 yaw를 맞추며, 고정은 생성 연출이 끝날 때(`CharacterMediator.OnSpawnEffectEndedCallback`)만 풀린다.
- TestScene Play(에디터 루프가 멈춰 `EditorApplication.Step()` 반복으로 진행)로 확인:
  - 최초 스폰: 코드 수정 없이 스폰 지점 방향 49.7°로 루트·몸·카메라 피벗이 맞고, 생성 연출이 끝난 뒤에도 유지된다.
  - 사망 후 재스폰: 시점을 뒤로 돌린 뒤 체력 0 → 새 캐릭터의 몸·Main Camera 모두 49.7°(게임 화면 캡처로 확인).
  - TestScene 체크포인트 RespawnPoint는 0번(49.7°) 말고 전부 0°(월드 +Z)라, 진행 방향이 +Z가 아닌 곳에선 엉뚱한 방향으로 태어난다 → 맵에서 RespawnPoint를 돌려 둬야 한다.
  - 낙하 복귀 재현: 시점을 180°로 돌린 뒤 `MovePlayerTo` → 몸·카메라 180° 그대로(문제 재현).
  - `MovePlayerTo` 뒤 `SetLookDirection(spawnPoint.forward)` → 49.7°로 맞지만, 이후 시점을 270°로 돌려도 몸은 49.7°에 고정, `_isLookDirectionLocked` = true 유지. 이후 사용자 요청으로 `StageManager.MovePlayerTo` 끝에 `_playerFacade.SetLookDirection(destination.forward)` 한 줄을 넣었다(직접 체크용). 사용자 확인 결과 낙하 복귀 뒤 Look 프리팹(몸 방향)이 잠기는 문제가 남는다.
- 화살표 Gizmo:
  - 새 `Core/SpawnFacingGizmo.cs`(정적 `Draw(Transform, Color)`): 지점에 작은 구, 수평 정면으로 1.5 m 화살표.
  - `CheckpointTrigger.OnDrawGizmos`의 화살표 코드를 `SpawnFacingGizmo.Draw(_respawnPoint, Color.cyan)`로 교체(모양 같음).
  - `PlayerSpawner.OnDrawGizmos` 추가: `Is Lobby Spawner`가 켜진 스포너만 자기 위치(기본 스폰)와 `Lobby Spawn Points`의 각 Point를 초록 화살표로 그린다. 스테이지 스포너는 체크포인트 RespawnPoint에서 생성하므로 그리지 않는다.
  - `PlayerSpawner.cs`는 CP949 인코딩·CRLF를 그대로 유지해 수정했다.

## 2. 결정 사항
- 생성 방향은 지점 Transform의 Y 회전으로 정한다. `Instantiate(position, rotation)`만으로 몸·카메라가 그 방향을 따르므로 스폰 쪽에 `SetLookDirection` 호출은 넣지 않는다.
- 화살표 색: 체크포인트 하늘색, 로비 스포너 초록.

## 3. 현재 상태 및 이슈
- 컴파일 0에러, 콘솔 새 에러 없음. 씬 뷰 화살표는 캡처 도구가 Gizmo를 그리지 않아 육안 미확인.
- 로비 스포너 생성 방향은 같은 `Instantiate` 회전 방식이라 맞을 것으로 보지만 로비 씬 Play는 안 함(TestScene만 사용).
- 낙하 복귀: `MovePlayerTo`에서 `SetLookDirection`을 불러 방향은 맞지만, 생성 연출이 없어 고정이 풀리지 않아 이후 몸이 시점을 따라 돌지 않는다(**알려진 문제, 플레이어 쪽 수정 대기**).
- 커밋함. `ProjectSettings`·폰트 SDF·`UNITY-3D-Project.slnx` 변경은 Unity 자동 생성분이라 제외.

## 4. 다음 할 일 (Next Steps)
- 플레이어 담당(이세훈)에게 요청: `CharacterMediator.SetLookDirection`에서 생성 연출 중(`_isSpawnLocked`)이 아니면 `PublishViewForward()` 후 `UnlockLookDirection()`으로 바로 풀기(제안 코드, 미검증). 고쳐지면 Core 쪽은 그대로 두고 TestScene에서 낙하 복귀 후 시점 회전을 확인.
- 맵 담당: 체크포인트 RespawnPoint·로비 스포너/복귀 지점을 화살표가 원하는 방향을 보도록 돌려 두기.
