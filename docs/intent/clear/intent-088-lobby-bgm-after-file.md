---
id: intent-088
title: 파일맵 복귀 로비 전용 BGM 연결
part: Audio / Lobby / SceneTransition
status: resolved
created: 2026-10-30
resolved: 2026-10-30
---

## 문제 (Problem)
파일맵에서 로비로 돌아왔을 때의 분위기에 맞는 BGM이 따로 연결되어 있지 않다.

## 기대 결과 (Proposed outcome)
- 파일맵에서 로비로 돌아왔을 때에만 `Digital Integrity Check.wav`를 재생한다.
- 그 외 로비 진입에서는 기존 3곡 플레이리스트를 유지한다.

## 영향 범위 (Affected users and systems)
- 파일맵을 마치고 로비로 복귀하는 플레이어.
- `AudioManager` 프리팹, 로비 BGM 데이터, 씬 전환 출발지 기록.

## 제약 (Constraints)
- 씬 파일 변경은 이번 커밋에서 제외한다.
- WAV 파일은 사용자 지정 위치 `Assets/ThirdParty/Audio/Bgm/`에 둔다.
- 기존 로비 플레이리스트의 곡은 추가하거나 제거하지 않는다.

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution)
- `AudioManager`가 로비에서 BGM을 시작할 때 `SceneLoader.PreviousScene`이 File이면 전용 1곡 SO를 선택한다. 일반 로비 플레이리스트는 3곡 그대로다.
- Unity 스크립트 컴파일 오류 0건, 전용 SO와 WAV 참조 확인.
- 관련 커밋/PR: 이번 작업의 Audio 커밋.
