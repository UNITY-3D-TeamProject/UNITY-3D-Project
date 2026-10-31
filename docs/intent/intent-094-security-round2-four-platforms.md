---
id: intent-094
title: 보안맵 2라운드 특수몹 발판을 5개에서 4개로 줄이기
part: Stage / SecurityApp / Map
status: open   # open | resolved
created: 2026-10-30
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
`Map_4_Security` 2라운드는 특수몹 발판(Platform_1~5) 5개를 차례로 깨야 보스 라운드 체크포인트 발판(Platform_6)에 닿는다. 사용자가 적 발판을 4개로 줄이길 원한다.

## 기대 결과 (Proposed outcome)
- Platform_5(와 그 특수몹·기둥·구역)를 없앤다. Platform_4를 깨면 Platform_6이 나타난다.
- Platform_6과 그 뒤 보스 구역 전체를 Platform_5가 있던 자리까지 당겨서(x −31m) 발판 간격을 지금과 같게 유지한다.
  - 옮기는 것: `04_Platforms/Platform_6`, `05_Boss`(보스 바닥·보스 스포너·탈출문·조명·서쪽 벽 등), `BossArena`, `BossArenaFallZone`, `Checkpoint_Round3_Start`, 보스 시네마틱 카메라 2개, `Main Camera`
  - 전체 낙사 영역 `FallZone`은 끝(x 462)을 같은 만큼 줄인다.
- 참조 정리: `SpecialMobRound._steps`(4단계, 4번 다음 = Platform_6), `BossRound._sinkPlatforms`, `FallingHazardSpawner._platforms`에서 Platform_5 제거.
- 04_Platforms·05_Boss NavMesh 재생성.
- 패널은 `발판 n/4`로 바뀐다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안맵 플레이어, 맵 배치 담당(양준범 님 발판 2.5배 작업 위)
- 어떤 시스템/모듈이 건드려지는가: `Scenes/Map_4_Security.unity`, `Map_4_Security/NavMesh-04_Platforms.asset`, `NavMesh-05_Boss.asset`. 코드 변경 없음.

## 제약 (Constraints)
- 지켜야 할 정책: 보스 구역 안의 상대 배치는 바꾸지 않는다(통째로 평행 이동).
- 쓰지 말아야 할 것: 발판 간격을 넓혀 재배치하는 방식 (사용자 결정)

## 열린 질문 (Open questions)
- 없음. (사용자 결정 2026-10-30: Platform_5 삭제 + 뒤쪽 당기기)

## 진행 기록
- 2026-10-30 구현: Platform_5 삭제, Platform_6·05_Boss·BossArena·BossArenaFallZone·Checkpoint_Round3_Start·보스 시네마틱 카메라 2개·Main Camera를 x −31m 이동, FallZone 끝 462→431. `_steps`(4단계, 4번 다음=Platform_6)·`_sinkPlatforms`(1~4)·`FallingHazardSpawner._platforms`(1~4, 6) 정리. 04_Platforms·05_Boss NavMesh 재생성.
  - 주의: 에디터에서 콜라이더를 옮긴 직후 굽으면 Physics 위치가 갱신 안 돼 Platform_6 NavMesh가 비었다. `Physics.SyncTransforms()` 후 다시 구워 해결.
  - 확인: 특수몹 스포너 4개·Platform_6 전면 NavMesh, 보스 바닥 Boss NavMesh 16/25칸(바깥 9칸 없음은 이전 데이터와 동일). Play에서 발판 1/4→4/4 진행·4번 클리어 시 Platform_6 표시·3라운드 체크포인트 진입(라운드 2) 확인, 에러 0.
  - 남음: 에디터가 백그라운드라 Play 프레임이 돌지 않아 보스 등장·발판 내려가기·보스전은 직접 확인 못 함.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
