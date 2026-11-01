---
id: intent-110
title: 엔딩 크레딧 왼쪽 무대 연출을 장면(Beat) 단위로 나눠 순서를 바꿀 수 있게 하기
part: UI / Ending Credits
status: resolved   # open | resolved
created: 2026-11-01
resolved: 2026-11-01
---

## 문제 (Problem)
`EndingCastDirector`(intent-109)에 연출 순서가 코드로 박혀 있어서, "플레이어·펫 인사 → 플레이어 춤 → 적 등장"처럼 장면을 늘리거나 순서를 바꾸려면 매번 스크립트를 고쳐야 한다.

## 기대 결과 (Proposed outcome)
- 무대 프리팹 `EndingCastStage/Beats` 아래 장면 오브젝트를 위에서부터 차례로 재생한다.
- 하이어라키에서 끌어 옮기면 순서가 바뀌고, 오브젝트를 끄면 그 장면은 건너뛴다.
- 장면마다 자기 인스펙터 값(시간·위치·애니메이션)을 가진다.
- 장면끼리 이어지는 방식: 장면마다 시작 배치(카메라·플레이어·펫 위치·방향·애니메이션, 펫을 머리에 태울지)를 따로 정해 두고, 장면이 시작될 때 무대가 그 자리로 옮긴 뒤 재생한다. 순서를 바꿔도 앞 장면이 끝난 자리에 영향받지 않는다. (사용자 선택)
- 새 장면은 `EndingCastBeat`를 상속한 작은 스크립트로 추가한다. 이동·회전·대기·애니메이션 재생은 무대(`EndingCastDirector`)의 공용 기능을 쓴다.
- 기존 등장 인사는 첫 장면 `01_Greeting`(`EndingCastGreetingBeat`)으로 옮긴다. 결과 화면은 그대로.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 엔딩 연출을 만드는 팀원(김성준)
- 어떤 시스템/모듈이 건드려지는가: `UI/Credits/EndingCastDirector.cs`, 새 `EndingCastBeat.cs`·`EndingCastGreetingBeat.cs`, `Prefabs/UI/Credits/EndingCastStage.prefab`

## 제약 (Constraints)
- `Map_5_Live` 씬은 수정하지 않는다 (씬 인스턴스는 루트 값만 오버라이드).
- 실제 시간(unscaled)으로 움직이는 방식 유지.
- 지금은 구조만 바꾸고 새 장면(춤·적 등장)은 만들지 않는다.

## 열린 질문 (Open questions)
- 없음 (사용자가 "장면마다 오브젝트 하나" 방식 선택)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `EndingCastBeat`(추상, 시작 배치 공통 필드 + `CoPlay`) / `EndingCastDirector`(등장인물·머리 위 펫 추적·이동·회전·대기 공용 기능, `_beatsRoot` 자식 순서대로 재생, 꺼진 장면 건너뜀) / `EndingCastGreetingBeat`(기존 등장 인사). 프리팹에 `Beats/01_Greeting` 추가. 임시 Play에서 결과 화면 동일, 장면 2개일 때 두 번째 장면 시작 배치로 다시 잡힘 확인.
- 관련 커밋/PR: 커밋 전
