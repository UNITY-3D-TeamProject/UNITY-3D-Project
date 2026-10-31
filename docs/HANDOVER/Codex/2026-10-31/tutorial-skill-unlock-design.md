# 튜토리얼 스킬 해금 구현

## 작업 요약 및 방법
- `PlayerFacade.SetSkillEnabled`, `Core.Stage.Tutorial`, 플레이어 스킬 프리팹과 튜토리얼 씬의 안내 이벤트를 읽고 구현 전 설계를 정리했다.
- `Tutorial.cs`에서 초기 잠금과 대사별 해금 상태를 관리하고, 재스폰마다 새 플레이어에 재적용하도록 했다.
- 튜토리얼 씬은 다른 파트 작업과 겹치지 않도록 이벤트 연결 변경을 되돌렸다. HUD 카드 3장은 잠겨 있으면 어둡게 표시한다.

## 변경된 파일
- `Assets/_Project/Scripts/Core/GameManager.cs`, `Assets/_Project/Scripts/Core/Stage/Tutorial.cs`
- `Assets/_Project/Scripts/UI/PlayerHud/PlayerHudPresenter.cs`, `PlayerHudView.cs`, `Assets/_Project/Prefabs/UI/Canvas_InGame.prefab`
- `docs/intent/intent-102-tutorial-skill-unlock.md`

## 결정 사항
- 이동·`MultiJump`·`Interact`·`Attack`은 처음부터 허용한다.
- `Roll`은 대시 안내, `Scan`은 스캔 안내, `SpawnVehicle`은 탈것 안내에서 해금한다.
- 이어하기로 튜토리얼에 들어오면 세 잠금 스킬과 펫을 모두 켜고, 튜토리얼 이후 씬은 플레이어 기본 설정을 사용한다.

## 현재 상태 및 이슈
- `dotnet build Assembly-CSharp.csproj`는 오류 0개로 통과했다. 기존 경고 87개가 있다.
- 실제 Unity Play에서 안내 순서, 사망 후 재스폰, 이어하기, 카드 색을 확인해야 한다.
- 씬 이벤트 연결 전에는 세 스킬이 자동으로 해금되지 않는다.

## 다음 할 일
- 튜토리얼 씬 담당자가 `PetGuide_3_Dash`에 `Tutorial.UnlockRoll`, `PetGuide_7_Scan`에 `Tutorial.UnlockScan`, `PetGuide_8_Vehicle`에 `Tutorial.UnlockVehicle`을 추가한다.
- 연결 후 실제 플레이에서 안내 시작과 HUD 잠금 표시가 동기화되는지 확인한다.
- 튜토리얼 시작·각 안내·재스폰·이어하기·씬 재시작의 스킬 사용과 HUD를 직접 확인한다.
