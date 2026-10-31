# 튜토리얼 스킬 해금 씬 이벤트 연결

## 작업 요약 및 방법
- `Map_0_Tutorial`의 대시·스캔·탈것 안내 트리거에 기존 `Core.Stage.Tutorial` 해금 메서드를 연결했다.
- 대시는 `PetGuide_3_Dash`의 `PlayMonologue(9)` 다음에 `UnlockRoll`, 스캔은 `PetGuide_7_Scan`의 `PlayCutscene(8)` 다음에 `UnlockScan`, 탈것은 `PetGuide_8_Vehicle`의 `PlayMonologue(11)` 다음에 `UnlockVehicle`을 호출한다.

## 결정 사항
- 안내가 시작되는 기존 일회성 이벤트에서 해금한다. 기존 스킬 상태 관리 및 재스폰 재적용 코드는 유지한다.

## 현재 상태 및 이슈
- 씬 파일에서 세 이벤트의 대상 컴포넌트와 메서드가 연결된 것을 확인했고 `git diff --check`를 통과했다.
- Unity Play에서 시작 잠금, 안내 후 해금, HUD 표시, 재스폰 및 이어하기를 직접 확인해야 한다.

## 다음 할 일
- 튜토리얼 씬을 Play하여 세 안내를 순서대로 진행하고 해금 및 HUD 표시를 확인한다. 확인 후 `intent-102`를 해결 처리한다.
