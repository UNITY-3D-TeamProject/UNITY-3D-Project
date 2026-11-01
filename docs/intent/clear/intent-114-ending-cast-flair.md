---
id: intent-114
title: 엔딩 크레딧 무대: 추격 전에 플레이어 Flair(비보잉) 5초
part: UI / Ending Credits
status: resolved   # open | resolved
created: 2026-11-01
resolved: 2026-11-01
---

## 문제 (Problem)
엔딩 크레딧 왼쪽 무대가 인사 → Thriller 춤(1번) → 추격 순서로 바로 넘어간다. 사용자가 추격 전에 믹사모 `Flair`(비보잉) 동작을 5초쯤 넣고 싶어 한다.
`Flair`는 1초짜리 반복 동작인데, 지금 춤 장면(`EndingCastPlayerDanceBeat`)은 "클립 길이 - 0.5초마다 0.5초 섞으며 처음부터 다시"라서 1초 클립이면 절반이 섞이는 구간이 된다.

## 기대 결과 (Proposed outcome)
- Thriller 춤 다음에 플레이어가 Flair를 약 5초(5바퀴) 이어서 돌고, 그다음 추격 장면이 시작된다.
- 펫은 춤 때 자리에서 계속 같이 춤춘다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 엔딩 크레딧을 보는 플레이어
- 어떤 시스템/모듈이 건드려지는가:
  - `Scripts/UI/Credits/EndingCastPlayerDanceBeat.cs`: 반복(Loop Time) 클립이면 한 번 틀고 `횟수 × 길이`만큼 기다림
  - `Prefabs/UI/Credits/EndingCastStage.prefab`: `Beats/03_Flair` 추가, 기존 `03_Chase` → `04_Chase`
  - `Animations/Ending/EndingCast_Player.controller`: `EndingCast_Player_Flair` 상태 추가
  - `ThirdParty/Mixamo/EndingCast_Player_Flair.anim`: 경로 앞에 `SK_ZMike/`를 붙인 변환 클립 (Thriller와 같은 방식, Git 제외)

## 제약 (Constraints)
- 지켜야 할 정책: 맵 씬(`Map_5_Live`)은 열지도 수정하지도 않는다. 프리팹만 수정.
- 쓰지 말아야 할 것: 변환 클립 키 줄이기(흔들림 원인이었음). 원본 FBX 곡선을 그대로 쓴다.

## 열린 질문 (Open questions)
- 없음 (5초 = Flair 5바퀴로 정함)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `Beats/03_Flair`(EndingCastPlayerDanceBeat, Flair 변환 클립 Loop Time 켬, 5바퀴 = 5초, 섞기 0.3초). 시작 상태 이름을 비워 Thriller 끝 자세에서 바로 섞어 들어가고, 펫은 춤 자리에서 시작. 반복 클립이면 한 번 틀고 `횟수 × 길이`만큼 기다리도록 스크립트에 분기 추가.
- 관련 커밋/PR: (커밋 전)
