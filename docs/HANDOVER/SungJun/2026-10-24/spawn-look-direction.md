# 스폰·체크포인트 복귀 때 화살표 방향 보기 (intent-059, 진행 중)

## 1. 작업 요약 및 방법
- 플레이어 담당(이세훈)이 `SetLookDirection`을 고쳤다(`534f0311`): 생성 연출 중이면 연출 끝까지 방향 고정, 연출이 끝났거나 없으면 정렬 직후 바로 해제.
- 이를 이용해 Core 두 곳에 호출을 넣었다.
  - `PlayerSpawner.SpawnPlayer`: PlayerFacade 확인 직후 `playerFacade.SetLookDirection(spawnPoint.forward)`. 스테이지 최초 스폰·사망 재스폰(체크포인트 RespawnPoint)과 로비 스폰(스포너 자신·Lobby Spawn Points)이 모두 이 경로를 탄다. 파일은 CP949·CRLF 그대로 유지.
  - `StageManager.MovePlayerTo`(낙하 복귀): 이동 뒤 `_playerFacade.SetLookDirection(destination.forward)`.
- TestScene Play로 확인(에디터 루프가 멈춰 `EditorApplication.Step()` 반복으로 진행):
  - 최초 스폰: 피벗·몸 49.7°(RespawnPoint 방향), 생성 연출 종료 뒤 고정 해제.
  - 낙하 복귀: 시점 229.7°에서 `MovePlayerTo` → 피벗·몸 49.7°, 고정 없음. 이후 시점 -120° → 몸이 289.7°로 따라 돎.
  - 사망 재스폰: 새 캐릭터 피벗·몸·Main Camera 49.7°, 고정 해제.
  - 이번 Play로 생긴 콘솔 에러 없음.

## 2. 결정 사항
- 스폰 방향은 지점 Transform의 정면(씬 뷰 화살표, `SpawnFacingGizmo`)을 따른다. 스폰·낙하 복귀 모두 `PlayerFacade.SetLookDirection` 하나로 맞춘다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. 로비 씬·맵 씬 Play는 안 함(TestScene만 사용). 실제 마우스 조작·낙하 구역 진입은 eval로 대신했다.
- TestScene 체크포인트 RespawnPoint는 대부분 0°라 다른 방향 체크포인트에서의 확인은 49.7° 한 방향뿐이다.
- 플레이어 쪽 남은 가장자리: `SetLookDirection`을 `CharacterMediator.Start` 전에 불렀는데 Start 시점에 생성 연출이 이미 건너뛰어진 상태(준비 실패)면, Start가 고정을 풀지 않아 방향이 고정된 채 남을 수 있다(코드 읽기로만 추정, 재현 안 함).
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 맵 담당: 체크포인트 RespawnPoint·로비 스포너/복귀 지점을 화살표가 원하는 방향을 보도록 돌려 두기.
- 맵 씬에서 실제로 떨어져 보고 방향 확인 → 문제없으면 intent-059 resolved 처리.
- 위 준비 실패 가장자리는 필요하면 플레이어 담당에게 공유.
