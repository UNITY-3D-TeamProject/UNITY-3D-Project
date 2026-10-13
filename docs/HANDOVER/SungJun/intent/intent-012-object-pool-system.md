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
- ~~로비·각 스테이지 씬마다 ObjectPoolManager를 하나씩 둔다 (싱글톤·DontDestroyOnLoad 아님). 씬이 내려가면 풀도 함께 정리된다.~~ (2026-10-13 변경)
- 풀이 필요한 씬에만 매니저를 배치하고(로비·피드 등은 배치 안 함), `ObjectPoolManager.Instance`로 접근한다. Instance는 씬 한정이며 DontDestroyOnLoad가 아니다 — 씬이 내려가면 풀·풀에서 나온 오브젝트와 함께 정리되고 Instance는 null. (2026-10-13)
- ~~카테고리: Monster, Npc, Effect, PlayerBullet, EnemyBullet, BossSkill.~~ (2026-10-13 축소)
- 카테고리: Enemy(근접·원거리·특수), PlayerBullet, EnemyBullet. 각 카테고리는 키별로 프리팹 여러 종류를 지원한다.
- ~~꺼내는 쪽은 매니저 참조 + 키 문자열로 `Get("Monster1", pos, rot)` (프리팹은 매니저 인스펙터에 키와 함께 등록, 쓰는 쪽은 프리팹을 몰라도 됨). 매니저 참조는 씬 배치 오브젝트는 인스펙터, 런타임 생성 오브젝트는 생성한 쪽이 직접 전달.~~ (2026-10-13 변경)
- 꺼내는 쪽은 `ObjectPoolManager.Instance.Get("MeleeEnemy", pos, rot)` (프리팹은 매니저 인스펙터에 키와 함께 등록, 쓰는 쪽은 프리팹을 몰라도 됨). 매니저 참조 전달·인스펙터 연결은 하지 않는다.
- ~~반납은 오브젝트에 자동으로 붙는 PooledObject로 처리한다 (매니저를 몰라도 됨).~~ (2026-10-13 변경)
- 반납은 `ObjectPoolManager.Instance.Release(gameObject)`. 사용하는 쪽은 ObjectPoolManager만 안다. 돌아갈 풀·키는 풀이 생성 시 자동으로 붙이는 내부 컴포넌트 PooledObject가 기억하고, 풀 출신이 아니면 Destroy한다. 매니저가 없을 수 있는 공용 코드는 호출 쪽에서 Instance null을 확인한다. (2026-10-13)
- 구조는 ObjectPoolManager(키 → 카테고리 라우팅) → PrefabPool(카테고리별 [Serializable], 키별 풀 생성·인스턴스 생성/반납/파괴, Capacity·MaxSize) 2단으로 나눈다. (2026-10-13)
- ~~재사용 초기화는 IPoolable.OnGet/OnRelease~~ → IPoolable 삭제, 프리팹 컴포넌트가 OnEnable(리셋)·OnDisable(정리)에서 직접 처리한다. (2026-10-13)
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
