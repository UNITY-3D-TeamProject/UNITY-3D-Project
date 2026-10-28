---
id: intent-083
title: 잡몹이 죽으면 일정 확률로 아이템을 떨어뜨린다
part: AI / Enemy / Items
status: open   # open | resolved
created: 2026-10-28
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
적이 죽어도 아무 보상이 없다. 체력 회복·배터리 충전 아이템(`Item_HealHp`, `Item_ChargeBattery`)은 맵에 배치된 것만 얻을 수 있어, 전투 중 소모한 자원을 전투로 보충할 방법이 없다.

## 기대 결과 (Proposed outcome)
- 잡몹(Melee·Ranged·Special)이 죽으면 설정한 확률로 아이템이 죽은 자리에 떨어진다.
- 드랍 여부를 먼저 굴리고(`_dropChance`), 드랍이 정해지면 가중치로 아이템 하나만 고른다 (한 번에 최대 1개).
- 떨어진 아이템은 기존 `EffectItem`(Touch 방식)이라 플레이어가 닿으면 획득된다. 안 먹으면 일정 시간 뒤 사라진다.
- 확률·아이템 목록·가중치·수명은 몬스터 프리팹 인스펙터에서 조정한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(전투 보상), 밸런스 담당(확률 조정)
- 어떤 시스템/모듈이 건드려지는가:
  - 신규 `AI/EnemyItemDropper` (몬스터 프리팹에 붙는 컴포넌트, `CharacterMediator.OnDeath` 구독)
  - `Melee.prefab`, `Ranged.prefab`, `Special.prefab` (컴포넌트 추가)
  - 기존 아이템 프리팹은 수정하지 않는다.

## 제약 (Constraints)
- 캐릭터 구조: 컴포넌트는 `CharacterMediator`의 이벤트만 구독한다. 다른 컴포넌트를 직접 참조하지 않는다.
- 아이템은 `EffectItem`이 획득 시 스스로 `Destroy` 하므로 풀링하지 않고 `Instantiate` 한다.
- 이번 범위 밖: GC·Thief·보스 (같은 컴포넌트를 붙이면 되므로 필요해지면 프리팹에만 추가).
- 프리팹은 YAML 직접 수정 금지, Unity Editor MCP로 컴포넌트를 추가한다.

## 열린 질문 (Open questions)
- 기본 확률·가중치 값(아래 임시값)은 밸런스 담당이 정한다.
  - 임시값: 드랍 확률 25%, `Item_HealHp` 가중치 1, `Item_ChargeBattery` 가중치 1, 수명 15초
- 아이템 획득 시 소리 외 드랍 연출(반짝임, 떠오름)은 이번에 하지 않는다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
