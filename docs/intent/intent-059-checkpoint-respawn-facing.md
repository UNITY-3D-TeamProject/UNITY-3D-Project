---
id: intent-059
title: 낙하 복귀 시 체크포인트가 지정한 방향을 바라보기
part: Stage / Player / Camera
status: open   # open | resolved
created: 2026-10-22
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
플레이어가 떨어져 체크포인트로 돌아오면, 체크포인트 `RespawnPoint`의 방향이 아니라 떨어지기 직전에 보던 방향을 그대로 바라본다.

- `StageManager.MovePlayerTo`는 플레이어 루트에 `RespawnPoint`의 위치·회전을 넣는다.
- 하지만 보이는 몸(`SK_ZMike`)은 `CharacterRotator`(Rotation)가 루트 기준 yaw로 따로 돌리고, 그 목표는 카메라 시점 방향이다. 카메라 시점은 `PlayerBaseCamera._yaw`(CameraControl)에 쌓인 값이라 순간 이동해도 바뀌지 않는다.
- 그래서 몸과 카메라가 떨어지기 전 방향을 그대로 바라본다.
- 사망 후 재스폰은 새 인스턴스를 `spawnPoint.rotation`으로 생성하므로 코드상 지정 방향을 바라본다(Play 미확인).

## 기대 결과 (Proposed outcome)
- 맵에서 체크포인트마다 플레이어가 바라볼 방향을 화살표로 보고 정할 수 있다.
- 낙하 복귀 시 카메라와 몸이 그 방향을 바로 바라본다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(낙하 복귀), 체크포인트를 배치하는 맵 담당
- 어떤 시스템/모듈이 건드려지는가: `Core/Stage/CheckpointTrigger`, `Core/Stage/StageManager`

## 제약 (Constraints)
- 지켜야 할 정책: 작업자(김성준)는 `Core` 폴더 코드만 수정한다. `PlayerFacade`·Mediator·`PlayerBaseCamera`·`CharacterRotator`는 수정하지 않는다.
- 쓰지 말아야 할 것: Core 밖 private 필드를 리플렉션으로 건드리는 방식.

## 열린 질문 (Open questions)
- Core만으로는 이미 있는 플레이어의 카메라 yaw를 바꿀 공개 API가 없다. 다음 중 무엇으로 할지 사용자 결정 필요:
  - 낙하 복귀도 사망처럼 새로 생성(체력·배터리는 유지) — Core만으로 가능, 생성 연출·펫·쿨타임이 매번 초기화됨
  - 플레이어 담당에게 방향 설정 API(`PlayerFacade.SetLookRotation` 같은 것) 추가를 요청
- 1차 시도(Facade~카메라·회전 코드에 `SetLookRotation` 추가)는 TestScene에서 동작을 확인했으나, Core 밖 수정이라 되돌렸다.

## 해결 기록 (Resolution) — 해결 후 작성
- 진행: `CheckpointTrigger.OnDrawGizmos`로 RespawnPoint 정면을 씬 뷰에 하늘색 화살표로 표시(컴파일 확인, 씬 뷰 육안 미확인).
- 최종 결정:
- 관련 커밋/PR:
