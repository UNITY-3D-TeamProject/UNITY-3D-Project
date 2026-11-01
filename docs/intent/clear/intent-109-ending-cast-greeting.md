---
id: intent-109
title: 엔딩 크레딧 옆 연출을 플레이어·펫 등장 인사로 교체
part: UI / Ending Credits
status: resolved   # open | resolved
created: 2026-11-01
resolved: 2026-11-01
---

## 문제 (Problem)
`Map_5_Live` 엔딩 크레딧 왼쪽 무대(intent-101 `EndingCastStage`)의 장난·추격·커튼콜·피날레 연출을 사용자가 원하지 않는다. 처음부터 새 연출을 쌓아 갈 예정이다.

## 기대 결과 (Proposed outcome)
기존 연출(적·NPC 커튼콜 포함)을 없애고 아래만 남긴다. 다른 연출은 나중에 이어서 붙인다.
1. 플레이어가 왼쪽 화면 밖에서 뛰어 들어온다.
2. 펫은 플레이어 머리 위에 앉아 같이 온다.
3. 펫이 플레이어 옆으로 날아가고, 둘이 서로 마주 본다.
4. 둘이 화면(카메라)을 보고 인사한다. (플레이어 `Wave_Loop`, 펫 `R11_EmoHello`)

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 엔딩까지 본 플레이어
- 어떤 시스템/모듈이 건드려지는가: `UI/Credits/EndingCastDirector.cs`, `Prefabs/UI/Credits/EndingCastStage.prefab`(Cast_* 적 6개 제거), `Animations/Ending/EndingCast_*` 적 컨트롤러 6개 삭제

## 제약 (Constraints)
- `Map_5_Live` 씬은 열거나 고치지 않는다. 씬 인스턴스는 루트 위치·이름·꺼짐만 오버라이드하므로 프리팹·스크립트 수정으로 반영된다.
- 실제 시간(unscaled)으로 움직이는 기존 방식 유지.

## 열린 질문 (Open questions)
- 없음 (인사 뒤에는 둘 다 제자리 대기로 멈춤, 이후 연출은 사용자가 추가 예정)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `EndingCastDirector`를 등장(머리 위 펫) → 마주 보기 → 인사 3단계로 새로 작성. 펫은 달리는 동안 `LateUpdate`에서 플레이어 머리 뼈(`head_01`) + 0.36m를 따라가 머리 흔들림에 맞춰 앉아 있고, 옆자리는 플레이어 눈높이(y 0.9). 인사 뒤 둘 다 Idle로 멈춤. 프리팹에서 Cast_* 6개, 적 컨트롤러 6개 삭제. `Map_5_Live`는 수정 안 함.
- 관련 커밋/PR: 커밋 전
