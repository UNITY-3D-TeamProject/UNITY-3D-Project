# 메인 메뉴 전시 플레이어 프리팹 교체 (2026-11-01)

## 작업 요약 및 방법
- `LobbyMenuBackdrop.prefab`의 복사본 캐릭터를 `Assets/_Project/Prefabs/Player/Player.prefab`의 연결된 인스턴스로 교체했다.
- 기존 메뉴 캐릭터의 위치·회전·크기와 `MainMenuIdle.controller`를 유지했다. 메뉴에서 플레이어의 입력·이동·카메라 등 동작 컴포넌트는 비활성화했다.
- 원본 플레이어의 `SpawnEffectDriver.Awake`가 비활성화된 상태에서도 몸체를 마스크 재질로 바꾸는 문제를 피하려고, 메뉴 인스턴스의 `PlayerSpawnEffect` 자식만 비활성화했다.
- 사용자 정정에 따라 `MainMenu_PlayerKeyLight`를 기존 위치와 조명 값으로 복원했다.

## 변경된 파일
- `Assets/_Project/Prefabs/UI/MainMenu/LobbyMenuBackdrop.prefab`

## 결정 사항
- 로비 맵·메인씬 카메라·조명은 기존 상태를 유지하고 메뉴 캐릭터만 원본 플레이어 프리팹 인스턴스로 교체한다.

## 현재 상태 및 이슈
- Play에서 캐릭터가 기존 위치에 표시되고 몸체가 원본 재질 셰이더를 사용하는 것을 확인했다. 카메라는 기존 목적지 `(-42.2, 0.9, 49)`에 도착했다.
- Unity 연결 서버의 `.unity-pipeline-port` 파일 공유 오류와 일시적인 요청 시간 초과가 콘솔에 남아 있다. 플레이어 코드 오류는 확인되지 않았다.
- `Player.prefab`, `MainScene.unity`, `Map_1_Lobby.unity`는 수정하지 않았다.

## 다음 할 일
- 사용자가 메뉴 화면의 캐릭터 반짝임 정도를 확인한다.
