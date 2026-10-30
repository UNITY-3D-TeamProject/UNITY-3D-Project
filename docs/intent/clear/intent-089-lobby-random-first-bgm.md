---
id: intent-089
title: 일반 로비 첫 BGM 두 곡 중 무작위 선택
part: Audio / Lobby
status: resolved
created: 2026-10-30
resolved: 2026-10-30
---

## 문제 (Problem)
일반 로비에 들어갈 때마다 첫 곡이 `Digital City Morning`으로 고정된다.

## 기대 결과 (Proposed outcome)
- 일반 로비 진입 때 `Digital City Morning`과 `Colorful City Morning` 중 한 곡을 첫 곡으로 무작위 선택한다.
- 첫 곡 이후에는 기존 플레이리스트 순서대로 재생한다.
- 파일맵 복귀 전용 `Digital Integrity Check`는 별도 BGM으로 유지한다.

## 영향 범위 (Affected users and systems)
- 일반 로비 진입 플레이어, `SOAudioData_BgmLobby`, `SOAudioData`, `AudioManager`.

## 제약 (Constraints)
- 기존 일반 로비 4곡 목록과 다른 맵의 BGM 설정은 유지한다.
- 전체 셔플은 켜지 않는다.

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution)
- `SOAudioData`에 BGM의 첫 곡 후보 개수를 추가하고 일반 로비 에셋에 `2`로 설정했다. `AudioManager`는 시작 시 앞의 두 곡 중 한 곡을 고르고, 이후에는 `_isShuffle: 0`에 따라 등록 순서대로 재생한다.
- 파일맵 복귀 전용 BGM은 다른 SO이므로 기존 단일 곡 반복을 유지한다.
- Unity 컴파일 오류 0건. AssetDatabase에서 일반 로비 4곡, 시작 후보 2개, 전체 셔플 해제, 전용 BGM 후보 0개를 확인했다.
- 관련 커밋/PR: 로비 BGM 목록과 첫 곡 선택 변경 커밋.
