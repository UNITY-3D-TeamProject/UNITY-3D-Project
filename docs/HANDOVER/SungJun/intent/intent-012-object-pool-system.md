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
- ObjectPoolManager 싱글톤 하나로 몬스터·총알·이펙트·NPC 풀을 통합 관리한다.
- 각 시스템은 ObjectPoolManager를 통해 Get/Release 하나로 오브젝트를 빌리고 반납한다.
- GC Alloc 없이 오브젝트를 재사용하여 런타임 성능을 안정화한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 몬스터 스포너, 플레이어 전투, 이펙트 재생, NPC 스포너
- 어떤 시스템/모듈이 건드려지는가: Systems/ObjectPool (신규), 향후 Monster/Bullet/Skill 스크립트에서 참조

## 제약 (Constraints)
- Unity 내장 UnityEngine.Pool.ObjectPool<T> 활용 (서드파티 금지)
- 프리팹 설정은 ObjectPoolManager SerializedField로 Inspector에서 직접 연결
- 총알(Bullet)은 종류별 복수 풀 지원 (Dictionary<GameObject, ObjectPool<T>>)
- 이펙트(Effect)는 ParticleSystem 완료 시 코루틴으로 자동 반환

## 열린 질문 (Open questions)
- 없음. 설계 확정 후 구현 진행.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
