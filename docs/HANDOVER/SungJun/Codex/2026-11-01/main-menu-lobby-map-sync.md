# 메인 메뉴 로비 맵 동기화 (2026-11-01)

## 작업 요약 및 방법
- `Map_1_Lobby.unity`의 맵 루트 계층을 `LobbyMenuBackdrop.prefab`에 반영했다. 최신 씬에만 있는 `LobbyFloorGapPlanes`를 추가하고 기존 로비 맵 루트 24개를 갱신했다.
- `MainScene.unity`와 `Map_1_Lobby.unity`는 수정하지 않았다. 메인 메뉴 전용 `MainMenu_Player`와 `MainMenu_PlayerKeyLight`는 프리팹에 남겼다.

## 변경된 파일
- `Assets/_Project/Prefabs/UI/MainMenu/LobbyMenuBackdrop.prefab`
- `docs/intent/clear/intent-117-main-menu-lobby-map-sync.md`, `docs/intent/INTENT_POINTER.md`

## 결정 사항
- 로비 씬의 현재 맵 구성을 메인 메뉴 배경의 기준으로 사용한다.
- 기존 메인 메뉴 카메라 위치·이동 방식·전시 플레이어 위치는 보존한다.

## 현재 상태 및 이슈
- 프리팹 맵 루트별 오브젝트 수가 로비 씬과 일치하며 Missing Script 0개다.
- MainScene Play에서 최종 카메라 `(-42.2, 0.9, 49)` 도착, 메뉴와 플레이어 표시, 컴파일 및 콘솔 오류 0개를 확인했다.
- Unity 프리팹 재저장으로 YAML의 파일 ID 및 줄 순서가 크게 바뀌어 diff가 크다. 프리팹 GUID와 MainScene 연결은 유지된다.
- 첫 전체 복제 시 Unity 에디터 연결이 끊겼으나 프리팹 파일은 저장되지 않았다. 에디터 재연결 후 맵 루트를 나눠 반영하고 Play를 완료했다.

## 다음 할 일
- 메인 메뉴에서 배경 구도를 사용자 화면에서 최종 확인한다.
