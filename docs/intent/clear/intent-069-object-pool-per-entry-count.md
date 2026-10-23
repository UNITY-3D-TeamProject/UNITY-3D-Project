---
id: intent-069
title: 오브젝트 풀 개수 설정을 항목(키)별로
part: Core / ObjectPool
status: resolved
created: 2026-10-23
resolved: 2026-10-23
---

## 문제 (Problem)
`PrefabPool`의 `Default Capacity`·`Prewarm Count`·`Max Size`가 카테고리마다 하나뿐이라, 같은 카테고리의 모든 키가 같은 개수를 쓴다.
예: Crowd에서 Car는 20개, Pedestrian은 50개가 필요한데 따로 정할 수 없다. 개수를 나누려면 카테고리를 새로 만들어야 한다.
또 `Prewarm Count`라는 이름이 무슨 뜻인지 바로 알기 어렵다.

## 기대 결과 (Proposed outcome)
- 항목(키·프리팹)마다 처음 만들 개수와 최대 보관 개수를 인스펙터에서 따로 정한다.
- `Prewarm Count`는 `Initial Count`(씬 시작 때 미리 만들어 두는 개수)로 이름을 바꾼다.
- `Default Capacity`는 인스펙터에서 없애고, 코드에서 처음 만들 개수를 그대로 넘긴다. (목록 칸 수일 뿐이고 모자라면 알아서 늘어나 직접 정할 이유가 없음)
- 카테고리는 인스펙터에서 묶어 보여 주는 역할만 한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 풀을 배치하는 씬 담당자 (Enemies·Bullets·Crowd 등록)
- 어떤 시스템/모듈이 건드려지는가: `Core/ObjectPool/PrefabPool.cs`, `ObjectPoolManager.cs` 주석, `ObjectPoolManager.prefab` 값

## 제약 (Constraints)
- 지켜야 할 정책: Get/Release 사용법은 바꾸지 않는다. 맵 씬은 열지 않는다.
- 쓰지 말아야 할 것: 카테고리 값 + 항목 덮어쓰기 같은 이중 구조

## 열린 질문 (Open questions)
- 없음. (Default Capacity는 코드에서만, 이름은 Initial Count로 결정)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `PrefabPool.SPoolEntry`에 `_initialCount`(Initial Count)·`_maxSize`를 두고 카테고리의 `_defaultCapacity`·`_prewarmCount`·`_maxSize`는 삭제. 풀의 `defaultCapacity`는 코드에서 Initial Count(Max Size까지)를 넘긴다. Max Size가 0인 항목은 경고 후 1로 보정. `ObjectPoolManager.prefab`은 기존 값(10/50, Crowd 10/100)을 항목마다 옮김. 컴파일 0에러, 미리보기 씬에서 항목별 개수(Car 20)·0 보정 확인.
- 남은 일: `Map_1_Lobby`의 옛 오버라이드(`_crowd._prewarmCount` 40 등)는 더 이상 적용되지 않으므로 로비 담당이 Car·Pedestrian 항목에 값을 다시 넣어야 함. (맵 씬은 열지 않음)
- 관련 커밋/PR: 커밋 안 함
