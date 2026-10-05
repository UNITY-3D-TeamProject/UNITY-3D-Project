---
id: intent-012
title: 적 원거리 공격 (프로토타입, 스킬 직접 생성 방식)
part: AI / Combat
status: open   # open | resolved
created: 2026-10-04
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
전투 프로토타입(월요일 마감)에 적이 근거리 한 종류뿐이라 전투 양상이 단조롭다.
플레이어 사격 파이프라인(Fire → SkillMediator → SpawnMediator → BulletFactory)은 발사 방향을 `CharacterMediator`가 `MoveMediator.GetViewDirection`(카메라 시점)으로 주입하는데, 카메라가 없는 AI는 시점이 zero 라 항상 월드 z+ 로 쏘게 된다.

## 기대 결과 (Proposed outcome)
- 시야·사거리 안의 플레이어를 향해 일정 간격(CooldownCost)으로 총알을 쏘는 원거리 적이 생긴다.
- 기존 AIController·BT 노드(`AIIsTargetWithinRangeCondition`, `AIAttackTargetAction`)를 그대로 쓰고 사거리만 다르게 준다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 적 AI (원거리형 프리팹)
- 어떤 시스템/모듈이 건드려지는가: `Skill/Skills/EnemyRangedAttack.cs` 신규 1개. 중재자·AIController·BT·BulletController 무변경

## 제약 (Constraints)
- 예산/기한: 월요일 전투 프로토타입
- 지켜야 할 정책: 공용 중재자(이세훈 담당) 무수정
- 쓰지 말아야 할 것: Fire/SpawnMediator 경로 수정 (협의 시간 없음)

## 결정 사항
- `EnemyMeleeAttack` 과 같은 방식: 스킬이 `BulletController` 를 직접 생성하고 `Initialize(_firePoint.forward, ...)` 를 호출한다.
- 발사 방향은 `_firePoint.forward`(월드). FirePoint 는 몸통 자식이므로 AIController 가 월드 좌표로 돌린 정면을 따른다.

## 기술 부채 (풀링 도입 시 처리)
- 이세훈 커밋 `e01a017` 은 "스킬은 알림만, 생성은 SpawnMediator → BulletFactory, 방향·주체는 CharacterMediator 주입" 구조다. 근접(`EnemyMeleeAttack`)·원거리(`EnemyRangedAttack`) 모두 이를 벗어나 있다.
- 오브젝트 풀링 도입 시 둘을 함께 Factory 경로로 이전한다. 원거리는 Fire 스킬로 교체하고, 발사 방향은 `RotateMediator` 의 방향(시점 있으면 시점 수평 정면, 없으면 월드 회전 입력)을 `CharacterMediator` 가 주입하도록 바꾼다. `AIController.Attack()` 이 발동 직전 회전 입력으로 대상 방향을 보내므로 그 값이 곧 조준 방향이 된다.

## 열린 질문 (Open questions)
- 몸통 회전 보간 때문에 첫 발이 대상 정면을 다 보기 전에 나갈 수 있음 — Play 에서 체감되면 대응
- 높낮이 차이는 고려하지 않음 (수평 발사)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
