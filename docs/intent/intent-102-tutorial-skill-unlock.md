---
id: intent-102
title: 튜토리얼 대사 진행에 맞춰 플레이어 스킬 해금
part: Core / Stage / Tutorial / Player Skill
status: open
created: 2026-10-31
resolved: null
---

## 문제 (Problem)
튜토리얼 입장 직후 플레이어의 모든 스킬을 사용할 수 있어 안내 대사보다 먼저 사용할 수 있다.

## 기대 결과 (Proposed outcome)
- 시작 시 이동·2단 점프(`MultiJump`)·상호작용(`Interact`)·사격(`Attack`)을 허용한다.
- `Roll`, `Scan`, `SpawnVehicle`은 처음에는 막고 각 사용 안내가 시작되는 이벤트에서 하나씩 해금한다. 이어하기로 튜토리얼에 들어오면 모두 해금한다.
- `Core.Stage.Tutorial`이 해금 상태를 보유하고 현재 플레이어와 재스폰 플레이어에 `PlayerFacade.SetSkillEnabled`로 적용한다.
- 해금 이벤트는 중복 호출되어도 안전하며 해금 후에는 다시 잠그지 않는다. 스테이지를 처음부터 재시작하면 초기 상태로 시작한다.

## 설계 초안
1. `Tutorial.StartStage`와 `GameManager.OnPlayerSpawned`에서 잠금 상태를 적용한다. `MultiJump`·`Interact`·`Attack`은 켜고 나머지 세 스킬은 현재 해금 여부에 맞춰 켜거나 끈다.
2. 공개 해금 메서드를 안내 지점의 기존 대사 재생 UnityEvent에 함께 연결한다. `PetGuide_3_Dash`에서는 `Roll`, `PetGuide_7_Scan`에서는 `Scan`, `PetGuide_8_Vehicle`에서는 `SpawnVehicle`을 연다. 대사 시작과 같은 트리거에서 처리하면 대사 종료 직후 바로 연습할 수 있다.
3. 펫 비활성 중 스캔 요청을 막는 기존 규칙은 별도로 유지한다. 스캔 안내에 도달할 때 펫이 이미 해금되어야 실제 사용이 가능하다.
4. HUD의 `Roll`·`Scan`·`SpawnVehicle` 카드는 `PlayerFacade.IsSkillEnabled`가 false인 동안 `CanvasGroup.alpha = 0.3`으로 어둡게 표시한다.

## 진행 기록
- `Tutorial.cs`에 초기 잠금·대사별 해금·재스폰 적용을 추가했다. 다른 파트가 튜토리얼 씬을 작업 중이므로 `Map_0_Tutorial`의 안내 트리거 연결은 뒤로 미뤘다.
- 사용자 수정에 따라 `Attack` 잠금과 상호작용 안내의 해금 이벤트를 제거했다.
- 이어하기로 튜토리얼에 입장하면 세 스킬과 펫을 처음부터 활성화한다. 튜토리얼 이후 씬의 기본 스킬 상태는 건드리지 않는다.
- HUD 카드 3장의 잠금 표시를 연결했다. `dotnet build Assembly-CSharp.csproj` 오류 0개, 기존 경고 87개. 실제 Unity Play 조작 확인은 남았다.

## 영향 범위 (Affected users and systems)
- 플레이어의 튜토리얼 스킬 사용 시점 및 재스폰 후 사용 가능 상태.
- `Core/Stage/Tutorial.cs`와 `Map_0_Tutorial`의 안내 지점 이벤트 연결. 기존 `PlayerFacade` API를 호출한다.

## 제약 (Constraints)
- 일반 스테이지와 공용 플레이어 프리팹의 기본 스킬 설정은 변경하지 않는다.
- 스킬 자체의 쿨타임, 코스트, 입력 처리 방식은 변경하지 않는다.
- 기존 씬의 다른 미커밋 변경을 보존한다.

## 열린 질문 (Open questions)
- 튜토리얼 씬 담당자가 `PetGuide_3_Dash` → `UnlockRoll`, `PetGuide_7_Scan` → `UnlockScan`, `PetGuide_8_Vehicle` → `UnlockVehicle`을 기존 대사 재생 이벤트에 연결해야 한다.
- 연결 후 실제 플레이에서 스킬 안내와 HUD 잠금 표시가 함께 바뀌는지 확인한다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
