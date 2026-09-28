# Rotate 기능 재구현 — 브랜치 리셋으로 유실된 작업 복구

## 1. 작업 요약 및 방법

세션 초반에 사용자 요청으로 몸통 회전(Rotate) 채널에서 카메라 연동 부분만 제거하는 롤백 작업을 했다(`RotateMediator.cs`/`CharacterMediator.cs` 편집, intent-009/HANDOVER 갱신). 그런데 세션 도중 **사용자가 다른 브랜치 작업을 병행하며 `feature/Rotation` 브랜치 자체가 바뀌었다** — `git reflog` 확인 결과:

1. 누군가 `git reset --hard 147ff2a`로 회전 기능 커밋 자체(e65fe11, 5157b51)를 브랜치 히스토리에서 제거
2. 이후 `feature/Movement`, `develop` 등을 오가다 `feature/Rotation`으로 돌아와 `git pull`을 실행, 팀원의 "Mediator 구조 개선" PR(#11, `CharacterMediator`/`MoveMediator`를 `MoveDirectionCalculator`·이벤트 기반으로 재구성)이 머지된 `develop` 상태(`704855c`)로 fast-forward

결과: `RotateMediator.cs`가 git에서 완전히 사라지고, `CharacterMediator.cs`/`MoveMediator.cs`는 팀원의 새 구조로 대체됐다. `Assets/_Project/Scripts/Rotation/`, `Assets/_Project/Scripts/AI/`는 git에 없는 **untracked 잔해 파일**로 디스크에만 남아 있었는데, 그중 `AIController.cs`는 한 세대 전(`ICharacterController`/`IRotateRequestSource` — `IBodyRotateController`로 개명되기 전) 버전이라 **현재 코드베이스와 맞지 않아 컴파일이 깨진 상태**였다.

사용자 확인 후("예전 커밋 복구 후 새 구조에 재적용") 다음을 재작성했다:

- **`RotateMediator.cs`** 신규 작성(`Assets/_Project/Scripts/Mediator/SubMediators/`) — 카메라 연동 없이 조종부의 월드 기준 입력 방향만 `CharacterRotator.SetLookDirection`에 전달. `.meta`는 e65fe11 당시 guid(`136e6af8...`)를 재사용해 연속성 유지.
- **`AIController.cs`** 수정 — `using Controller;`/`ICharacterController`/`IRotateRequestSource`(존재하지 않는 구식 인터페이스)를 현재 구조의 `Mediator.SubMediators.IMoveController`/`IBodyRotateController`로 교체. `SetLookRequest`/`ClearLookRequest`(카메라 Look, AI에는 불필요)는 제거.
- **`CharacterRotator.cs`**(`Assets/_Project/Scripts/Rotation/`)는 이미 디스크에 있던 파일이 D13(yaw 전용 회전, 기준 자세 보존)까지 반영된 최종본과 동일해 수정 없이 유지.
- **`CharacterMediator.cs`**는 새 구조에서 `RotateMediator`가 `Awake()`의 `GetComponentInParent<IBodyRotateController>()`로 자가 배선되므로 별도 주입 코드가 필요 없어 손대지 않음.
- `docs/intent/intent-009-body-rotation.md`, `docs/intent/INTENT_POINTER.md`를 새로 작성(기존 문서도 브랜치 리셋으로 함께 유실됨). 카메라 피벗 추종(과거 D8·D12)은 이번 재작성 범위에서 제외하고 "재도입 시 별도 문서"로 명시.

## 2. 결정 사항 (채택된 아키텍처 규칙)

- 몸통 회전은 카메라와 무관하게 **조종부가 보낸 월드 기준 입력 방향**만 바라본다(AI만 해당, 플레이어는 아직 미구현).
- `RotateMediator`는 팀원이 만든 새 Mediator 패턴(각 서브 중재자가 `Awake()`에서 자기 컨트롤러를 직접 찾는 방식)을 그대로 따른다 — `CharacterMediator`가 개입하지 않는다.
- `AIController`의 조종부 인터페이스는 반드시 현재 코드베이스 기준(`IMoveController`/`IBodyRotateController`)을 따른다 — 디스크에 남은 옛 버전 파일을 그대로 신뢰하지 않는다.

## 3. 현재 상태 및 이슈

- 코드 수정 완료: `RotateMediator.cs`(신규), `RotateMediator.cs.meta`(신규), `AIController.cs`(수정).
- `ICharacterController`/`IRotateRequestSource`/`using Controller;`/`SetLookRequest`(AI 쪽)/`ClearLookRequest`(AI 쪽) 잔여 참조 grep 확인 완료 — 0건.
- BT 노드(`Assets/_Project/Scripts/AI/BT/`)는 `AIController`의 공개 API(`MoveTo`, `HasArrived` 등)만 쓰고 있어 이번 수정과 무관, 영향 없음 확인.
- **`RotationPrefab.prefab`은 git에 없다.** 예전 커밋(e65fe11)에서 한 번 추가됐지만 브랜치 리셋으로 함께 사라졌다 — `[USER]` 영역에서 다시 만들어야 한다.
- **Unity 컴파일 / Play 모드 검증 미완 — 다음 세션 최우선.**
- 이번 일로 브랜치가 예고 없이 바뀔 수 있다는 게 확인됐다 — 다음 세션 시작 시 `git log --oneline -5`로 브랜치 상태를 먼저 확인하는 습관 권장.

## 4. 다음 할 일 (Next Steps)

1. Unity Editor 열어서 컴파일 에러 확인 (특히 `AIController.cs`, `RotateMediator.cs`).
2. `[USER]`: 적 프리팹에 `RotateMediator`+`CharacterRotator` 배치·배선 (`RotationPrefab` 재생성 또는 직접 배치), `Sensor._sightAngle` 정상값 복귀.
3. Play 모드에서 AI 순찰 시 이동 방향으로 몸통이 회전하는지 확인.
4. 플레이어 몸통 회전 정책(카메라 추종 여부)은 재협의 후 별도 intent 문서로 진행.
