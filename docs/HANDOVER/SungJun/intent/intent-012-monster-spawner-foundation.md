---
id: intent-012
title: 몬스터 스포너 기본 생성과 초기값 설정
part: Monster Spawner
status: resolved
created: 2026-10-04
resolved: 2026-10-04
---

## 문제 (Problem)
플레이어와 별도로 몬스터 프리팹을 생성하고 개체의 시작 능력치를 설정하는 스포너가 없다.

## 기대 결과 (Proposed outcome)
몬스터 스포너가 지정한 프리팹을 자신의 위치에 생성하고 시작 능력치를 적용한다. 플레이어와 몬스터의 공통 처리만 공유한다. 두 스포너에는 향후 비활성 생성 후 활성화하는 코드의 삽입 위치를 주석으로 표시한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 씬에 몬스터 스포너를 배치하는 개발자
- 어떤 시스템/모듈이 건드려지는가: PlayerSpawner, MonsterSpawner, AttributeSet 연동

## 제약 (Constraints)
- 기존 플레이어 스폰 및 저장값 복원 흐름을 유지한다.
- 현재 요청의 구현 범위는 단일 몬스터 프리팹과 스포너 위치의 기본 생성이다. 위치별 프리팹 대응 및 랜덤 선택은 후속 단계다.
- 비활성 생성 코드는 아직 실행하지 않고 주석으로만 둔다.

## 열린 질문 (Open questions)
- 없음. 사용자가 프리팹의 기본 SO를 읽고 스포너의 SOAttributeEffect 목록으로 개체별 값을 덮어쓰는 방식을 선택했다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: SpawnerBase는 프리팹 생성, 자식 AttributeSet 조회, Effect 적용만 담당한다. MonsterSpawner는 시작 시 지정한 프리팹 하나를 자신의 위치에 생성한다. 지연 활성화는 주석으로 위치와 선행 조건만 남긴다.
- 관련 커밋/PR: 없음 (작업 트리 변경)
