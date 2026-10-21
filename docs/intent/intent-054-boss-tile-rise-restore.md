---
id: intent-054
title: 보스 아레나 칸이 복구될 때 아래에서 덜컥 솟아오르게
part: AI / Boss / Arena
status: open   # open | resolved
created: 2026-10-21
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
보스 A 패턴으로 무너진 칸이 복구 시간이 지나 돌아올 때, 렌더러·콜라이더가 한 번에 켜져 그 자리에 툭 나타난다. 칸이 다시 생겼다는 느낌이 약하다.

## 기대 결과 (Proposed outcome)
복구되는 칸이 아래(기본 4 m)에서 빠르게 솟아올라 제자리를 살짝 넘었다가 내려앉는다(덜컥, EaseOutBack). 솟는 동안은 NavMesh 구멍을 유지해 보스 B 가 아직 올라오지 않은 칸 위로 걷지 않게 한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스전 플레이어
- 어떤 시스템/모듈이 건드려지는가: `BossTile.cs`(솟아오르기 연출), `Map_4_Security.unity`(타일 25개 루트의 Batching Static 해제 — 정적 배칭된 메시는 런타임에 움직이지 않음)

## 제약 (Constraints)
- 지켜야 할 정책: 칸 상태 판단(`ArenaGrid`)은 그대로 두고 겉모습만 바꾼다. 처음 시작(`BossArena.Awake`) 때는 연출하지 않는다.
- 쓰지 말아야 할 것: 다른 정적 플래그(Navigation·GI 등)는 건드리지 않는다.

## 열린 질문 (Open questions)
- 깊이 4 m·0.35초·넘침 계수 2 는 임시값. Play 육안 확인 후 조정.
- 테스트 씬(`Assets/Test/Boss`) 타일은 Git 밖이라 정적 플래그를 확인·수정하지 않았다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
