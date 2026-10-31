# 메인 메뉴 로비 충전소 지붕과 오디오 범위

## 작업 요약 및 방법
- `Map_1_Lobby.unity`의 `ChargeStation` 자식 지붕 FBX 인스턴스가 `LobbyMenuBackdrop.prefab`에 빠진 것을 확인하고 같은 부모·변환값으로 복원했다.
- 메인 메뉴 배경은 로비 씬 전체가 아니라 시각 오브젝트를 묶은 프리팹이다. 사용자의 정정에 따라 애니키프레스 후 카메라 목적지는 기존 플레이어 구도 `(-42.2, 0.9, 49)`로 복구했다. 후속 요청으로 메인 메뉴 배경의 번개 표시를 지붕 위로 4m 올렸다.
- `AudioManager`에서 `MainScene`의 3D `PlaySfx`·`PlaySfxLoop` 요청을 차단했다. BGM·UI 재생 경로는 유지했다.

## 변경된 파일
- `Assets/_Project/Prefabs/UI/MainMenu/LobbyMenuBackdrop.prefab`
- `Assets/_Project/Scripts/Core/Audio/AudioManager.cs`
- `docs/intent/clear/intent-091-main-menu-charge-station.md` 및 `docs/intent/INTENT_POINTER.md`

## 결정 사항
- 메인 메뉴 카메라 이동과 최종 플레이어 구도는 변경하지 않는다.
- 번개 표시의 높이는 메인 메뉴 배경 프리팹에서만 조정한다.
- 메인 씬에서는 3D 효과음만 차단하며 BGM과 UI 소리는 허용한다.

## 현재 상태 및 이슈
- Unity 컴파일 성공. Play에서 최종 카메라가 기존 목적지에 도착하고 플레이어가 보이는 화면을 확인했다.
- BGM 재생 확인. 3D 효과음 소스 재생 및 클립 할당은 0개였다.
- 로비 원본에 이미 쓰인 지붕 FBX는 `Assets/ThirdParty/`의 기존 에셋이므로 해당 에셋의 팀 공유 상태는 기존 로비 씬과 같다.
- `MainScene.unity`의 최종 Git 내용에는 변경이 없다. 기존 작업 트리의 다른 변경은 유지했다.

## 다음 할 일
- 사용자가 메인 메뉴의 카메라 이동 중 주유소가 보이는 정도를 직접 확인한다.
