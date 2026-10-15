---
id: intent-024
title: 플레이어 스폰 Effect를 PlayerFacade로 적용, 몬스터 스포너 고정/랜덤 소환 선택
part: Spawner (Core)
status: resolved   # open | resolved
created: 2026-10-15
resolved: 2026-10-15
---

## 문제 (Problem)
- `PlayerSpawner`가 스폰 직후 `SpawnerBase.ApplySpawnEffects`로 플레이어의 `AttributeSet`에 Effect를 직접 적용한다. 플레이어 외부(스포너)가 플레이어 내부를 건드리는 구조다.
- `PlayerFacade.ApplyEffect(SOAttributeEffect)`가 추가되어(이세훈, 39dcf44) 외부는 Facade를 통해 Effect를 적용할 수 있게 되었다.
- `MonsterSpawner`는 프리팹 하나만 소환할 수 있어 여러 몬스터 중 랜덤으로 소환할 방법이 없다.

## 기대 결과 (Proposed outcome)
- `PlayerSpawner`는 `PlayerFacade.ApplyEffect`로 시작 Effect를 적용한다. 적용 실패(대상 어트리뷰트 없음 등)는 경고 로그로 알린다.
- `MonsterSpawner`는 인스펙터에서 Fixed(기존 `_monsterPrefab`) / Random(`_randomMonsterPrefabs` 중 하나)을 고를 수 있다.
- 몬스터에는 Facade가 없으므로 Effect 적용은 지금처럼 `ApplySpawnEffects`(AttributeSet 직접)로 유지한다. 어떤 몬스터가 뽑히든 스포너의 `_monsterEffects`를 똑같이 적용한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 맵 담당(스포너 배치), 플레이어 스폰 흐름
- 어떤 시스템/모듈이 건드려지는가: `Scripts/Core/PlayerSpawner.cs`, `Scripts/Core/MonsterSpawner.cs`

## 제약 (Constraints)
- `Scripts/Core/` 코드만 수정한다 (PlayerFacade·CharacterMediator는 건드리지 않음).
- 기존 `_monsterPrefab` 칸을 유지해 MonsterSpawner 프리팹·Map_4_Security 스포너 13개 값을 깨지 않는다. 모드 기본값은 Fixed.
- `PlayerSpawner.cs`는 CP949 인코딩이므로 인코딩을 유지해 수정한다.

## 열린 질문 (Open questions)
- 없음 (Facade 없는 몬스터는 기존 방식 유지, 반복문은 스포너 안, Effect는 공용 목록으로 사용자 결정)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: PlayerSpawner는 `ApplyPlayerEffects`에서 `PlayerFacade.ApplyEffect`로 적용(실패 시 경고). MonsterSpawner는 `ESpawnMode`(기본 Fixed) + `_randomMonsterPrefabs` 추가, 몬스터 Effect는 기존 `ApplySpawnEffects` 유지.
- 관련 커밋/PR: 4a9a9e0
