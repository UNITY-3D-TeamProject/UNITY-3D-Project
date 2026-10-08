---
id: intent-012
title: 스폰 이유에 따른 플레이어 초기화와 저장값 복원
part: Player Spawn
status: open
created: 2026-10-04
resolved: null
---

## 문제 (Problem)
PlayerSpawner에 연결한 초기화 Effect가 적용되지 않으며 저장 데이터 존재 여부를 조회할 수 없다.

## 기대 결과 (Proposed outcome)
새 게임은 Set + Float Effect로 초기화하고, 씬 이동은 저장값을 복원한다. 같은 씬의 사망은 재생성 없이 라운드 스냅샷으로 복구하며 스폰 사유에서 제외한다. OnPlayerSpawned는 최종값 준비 후 발생한다.

## 영향 범위 (Affected users and systems)
- PlayerSpawner: NewGame일 때 초기화 Effect 적용.
- PlayerState: SetCurrentPlayer, RestoreSavedAttributes, ClearSavedAttributes로 참조 연결/복원/삭제를 분리한다.
- GameManager: CompletePlayerSpawn에서 참조를 연결하고 SceneTransition이면 복원, 새 게임이면 저장값만 삭제한 후 스폰 이벤트를 알린다.
- GameManager: 기존 SceneLoader.OnBeforeSceneChange 이벤트를 구독해 씬 이동 전 저장한다.

## 제약 (Constraints)
- 사용자가 추가한 _playerEffects 배열과 기존 변경을 유지한다.
- 초기화 Effect는 Set + Float 설정으로 SO의 Amount를 적용한다. 플레이어의 MaxHp를 읽는 Attribute 방식은 사용하지 않는다.
- Attribute 코드와 SO 에셋은 변경하지 않는다. 능력치 증가, LoadGame, 사망 후 재생성 동작 추가는 제외한다.
- 저장 데이터는 기존 메모리 딕셔너리 기준이며 디스크 저장 기능을 추가하지 않는다.

## 열린 질문 (Open questions)
- 없음. 사용자가 새 게임 초기화, 씬 이동 복원, 같은 씬 사망 시 기존 플레이어 재사용으로 확정했다.

## 검증
- 초기화 적용이 등록/스폰 알림보다 앞서며, 씬 이동 시에만 Effect 호출을 건너뛰고 저장값을 복원하는지 확인한다.
- Unity 플레이 모드에서 Inspector 배선과 동작 확인이 필요하다.

- 2026-10-08: 이전 Respawn enum 및 스포너 분기 제거. .NET 빌드 오류 0개, Unity Play Mode 검증은 남음.
