---
id: intent-117
title: 메인 메뉴 배경을 최신 로비 맵과 동기화
part: UI / MainMenu / Lobby
status: resolved
created: 2026-11-01
resolved: 2026-11-01
---

## 문제 (Problem)
`MainScene`의 `LobbyMenuBackdrop`은 이전 `Map_1_Lobby` 구성에서 복사되어 현재 로비 맵과 차이가 있다.

## 기대 결과 (Proposed outcome)
메인 메뉴 배경의 로비 지형과 장식이 현재 `Map_1_Lobby`를 반영한다. 기존 카메라 위치와 이동 경로, 전시 플레이어 위치는 유지한다.

## 영향 범위 (Affected users and systems)
- 메인 메뉴를 보는 플레이어
- `LobbyMenuBackdrop.prefab`, 필요 시 `MainScene.unity`

## 제약 (Constraints)
- `Map_1_Lobby.unity` 원본과 메인 메뉴 카메라·플레이어 설정을 변경하지 않는다.
- 메인 메뉴 전용 오브젝트와 기존 메인 메뉴 시각 조정은 유지한다.

## 열린 질문 (Open questions)
- 없음. 현재 로비 씬의 맵 관련 루트 오브젝트를 기준으로 동기화한다.

## 해결 기록 (Resolution)
- 최종 결정: `Map_1_Lobby`의 맵 루트 24개를 `LobbyMenuBackdrop`에 다시 복사하고 새 `LobbyFloorGapPlanes`를 추가했다. 메인 메뉴 전용 플레이어와 조명, `MainScene`의 카메라·전환·UI 설정은 유지했다. 프리팹 계층 수 일치, Missing Script 0개, Play 화면과 카메라 목적지, 컴파일·콘솔 오류 0개를 확인했다.
- 관련 커밋/PR: 없음.
