# 낙하 복귀 시 체크포인트 방향 바라보기 (intent-059, 진행 중)

## 1. 작업 요약 및 방법
- 원인: `StageManager.MovePlayerTo`가 루트에 `RespawnPoint` 회전을 넣어도, 보이는 몸(`SK_ZMike`)은 `CharacterRotator`가 카메라 시점 방향을 따라 돌리고, 카메라 yaw(`PlayerBaseCamera._yaw`)는 그대로라 떨어지기 전 방향을 유지한다.
- 1차 시도: `PlayerFacade.SetLookRotation` → Mediator → `PlayerBaseCamera`·`CharacterRotator`에 방향 설정 추가. TestScene에서 리플렉션으로 낙하 처리를 불러 270° 복귀를 확인했으나, **Core 밖 수정이라 사용자 요청으로 전부 되돌림**.
- 현재 남은 변경: `CheckpointTrigger.OnDrawGizmos` — RespawnPoint 위치에 작은 구, 수평 정면(파란 Z축)으로 1.5 m 하늘색 화살표를 씬 뷰에 항상 그린다.

## 2. 결정 사항
- 김성준 작업은 `Core` 폴더 코드만 수정한다. 플레이어 쪽(Facade·Mediator·CameraControl·Rotation)은 건드리지 않는다.
- 체크포인트 방향은 `RespawnPoint` Transform의 Y 회전으로 정한다(화살표가 그 방향).

## 3. 현재 상태 및 이슈
- 컴파일 0에러. 씬 뷰 캡처 도구가 Gizmo를 그리지 않아 화살표 육안 확인은 안 함.
- **낙하 복귀 방향 문제는 아직 그대로다.** Core만으로는 이미 있는 플레이어의 카메라 yaw를 바꿀 공개 API가 없다.
- 사망 후 재스폰은 `spawnPoint.rotation`으로 새로 생성하므로 코드상 지정 방향을 바라본다(Play 미확인).
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 사용자 결정: (1) 낙하 복귀도 새로 생성(체력·배터리 유지, Core만으로 가능) 또는 (2) 플레이어 담당에게 방향 설정 API 요청.
- 맵 담당이 체크포인트별 RespawnPoint 화살표를 원하는 정면으로 돌려 두기.
