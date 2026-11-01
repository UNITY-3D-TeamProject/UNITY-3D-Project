---
id: intent-111
title: 엔딩 크레딧 무대에 믹사모 춤(Thriller Part 3) 장면 추가
part: UI / Ending Credits
status: resolved   # open | resolved
created: 2026-11-01
resolved: 2026-11-01
---

## 문제 (Problem)
엔딩 크레딧 왼쪽 무대가 등장 인사(`01_Greeting`) 뒤에 멈춰 있다. 사용자가 믹사모에서 플레이어 모델(MikeZ)에 맞춰 받은 `Thriller Part 3.fbx`로 춤을 추게 하고 싶어 한다.

## 기대 결과 (Proposed outcome)
- 인사가 끝나면 플레이어가 그 자리에서 춤을 처음부터 끝까지 추고, 끝나면 부드럽게 처음부터 다시 춘다.
- 펫은 춤 동작에 걸리지 않게 옆으로 비켜 구경한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 엔딩까지 본 플레이어
- 어떤 시스템/모듈이 건드려지는가: 새 `EndingCastPlayerDanceBeat.cs`, `EndingCast_Player.controller`(상태 추가), `EndingCastStage.prefab`(`Beats/02_PlayerDance`), `Assets/ThirdParty/Mixamo/`(원본 FBX·변환 클립)

## 제약 (Constraints)
- 믹사모 파일은 사용자 요청대로 `Assets/ThirdParty/Mixamo/`에 둔다 (Git 제외 폴더).
- `Map_5_Live` 수정 없음.

## 열린 질문 (Open questions)
- 없음 (처음엔 첫 130프레임 반복이었으나 사용자가 "끝까지 춤"으로 변경)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 믹사모 클립은 원래 뼈 이름을 유지하지만 경로에 `SK_ZMike/`가 빠져 있어(맨 위 노드 = `SK_ZMike` 역할) 그대로는 안 붙는다. 경로를 `SK_ZMike/...`로 바꾼 Generic 클립 `ThirdParty/Mixamo/EndingCast_Player_Thriller.anim`(767프레임, 25.6초, 변화 없는 키 제거·오차 0.0008 이하)을 만들어 플레이어 애니메이터 상태로 추가. 사람형 아바타는 필요 없었음. 클립이 약 23MB라 Git 제외 폴더에 둠. 반복은 같은 상태로 0.5초 크로스페이드해 처음으로 돌아감(`_repeatCount` 0 = 계속).
- 관련 커밋/PR: 커밋 전
