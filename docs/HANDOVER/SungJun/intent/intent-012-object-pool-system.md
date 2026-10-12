---
id: intent-012
title: 오브젝트 풀링 시스템 구축
part: Systems/ObjectPool
status: open
created: 2026-10-04
resolved: null
---

## 문제 (Problem)
몬스터, 총알, 이펙트, NPC 등 반복 생성·소멸이 잦은 오브젝트를 Instantiate/Destroy로 처리하면
GC 스파이크 및 프레임 드랍이 발생한다.

## 기대 결과 (Proposed outcome)
- ~~ObjectPoolManager 싱글톤 하나로 몬스터·총알·이펙트·NPC 풀을 통합 관리한다.~~ (2026-10-12 변경)
- 로비·각 스테이지 씬마다 ObjectPoolManager를 하나씩 둔다 (싱글톤·DontDestroyOnLoad 아님). 씬이 내려가면 풀도 함께 정리된다.
- 카테고리: Monster, Npc, Effect, PlayerBullet, EnemyBullet, BossSkill. 각 카테고리는 프리팹 여러 종류를 지원한다(PrefabPool).
- 꺼내는 쪽은 매니저 참조 + 키 문자열로 `Get("Monster1", pos, rot)` (프리팹은 매니저 인스펙터에 키와 함께 등록, 쓰는 쪽은 프리팹을 몰라도 됨). 매니저 참조는 씬 배치 오브젝트는 인스펙터, 런타임 생성 오브젝트는 생성한 쪽이 직접 전달.
- 반납은 오브젝트에 자동으로 붙는 PooledObject로 처리한다 (매니저를 몰라도 됨).
- GC Alloc 없이 오브젝트를 재사용하여 런타임 성능을 안정화한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 몬스터 스포너, 플레이어 전투, 이펙트 재생, NPC 스포너
- 어떤 시스템/모듈이 건드려지는가: Systems/ObjectPool (신규), 향후 Monster/Bullet/Skill 스크립트에서 참조

## 제약 (Constraints)
- Unity 내장 UnityEngine.Pool.ObjectPool<T> 활용 (서드파티 금지)
- 프리팹 설정은 ObjectPoolManager SerializedField로 Inspector에서 직접 연결
- 모든 카테고리가 프리팹별 복수 풀 지원 (Dictionary<GameObject, ObjectPool<GameObject>>), 키는 씬 안의 전 카테고리에서 유일
- 이펙트(Effect)는 ParticleSystem Stop Action = Callback → PooledObject.OnParticleSystemStopped로 자동 반환 (기존 코루틴 방식은 재대여 인스턴스 조기 반납 버그로 폐기)
- 플레이어 본체는 풀 대상에서 제외 (씬에 1개, 재스폰 드묾)

## 열린 질문 (Open questions)
- 없음. 2026-10-12 구조 재설계 확정·풀 코드 정리 완료. 실제 사용처 연결(Spawner, BulletFactory·EnemyRangedAttack 등)은 각 파트에 `docs/HANDOVER/SungJun/2026-10-12/object-pool-guide.md`로 요청, 연결 후 resolved 처리.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
