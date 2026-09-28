---
id: intent-009
title: 몸통 회전 채널 도입 — 조종부 요청 → RotateMediator → 실행 컴포넌트
part: Character
status: open
created: 2026-09-28
resolved: null
---

## 문제 (Problem)

`Sensor`는 시야 방향을 `_eye.forward`로 판정하는데 프로젝트에 몸통 회전 코드가 없어 적의 시야 부채꼴이 스폰 시점 forward에 고정돼 있었다. 회전을 정식 통신 채널로 세워 AI 이동 방향에 몸통이 따라가도록 한다.

> **2026-09-28 재작성 메모**: 이 작업은 9/26~27에 한 차례 구현·커밋됐으나(카메라 피벗 추종까지 포함), 이후 팀원의 Mediator 구조 개선 PR(#11)이 머지되며 `feature/Rotation` 브랜치가 `git reset`/`pull`로 그 이전 커밋들을 잃고 develop 최신 상태로 재설정됐다. 이번 재작성은 **카메라 연동 없이, 조종부가 보낸 월드 기준 입력 방향만 바라보는 버전**을 팀원의 새 Mediator 구조(`MoveDirectionCalculator`, `CameraMediator.OnViewForwardChanged` 등) 위에 다시 얹은 것이다. 카메라 피벗 추종(과거 D8·D12)은 이번 범위에 포함하지 않는다 — 재도입이 필요해지면 별도 문서로 다룬다.

## 기대 결과 (Proposed outcome)

```
조종부 (IBodyRotateController 구현 — 회전을 요청하는 조종부만)
   ↓ 요청: "이 방향을 봐라" (Vector2, 월드 기준)
RotateMediator  (중재자 — Awake 에서 GetComponentInParent 로 자기 컨트롤러를 직접 찾아 배선)
   ↓ CommandRotate → SetLookDirection(Vector3)
CharacterRotator  (실행 — 실제 Transform yaw 회전, 기준 자세 보존)
```

**요청 측만 인터페이스로 분리한다.** 회전이 필요 없는 조종부(`PlayerInputComponent`)는 아무것도 구현하지 않는다. 실행 측은 구현체가 하나뿐이라 구체 클래스(`CharacterRotator`)로 시작한다.

**의도한 결과:** 적이 걸어가는 방향을 몸통이 바라보고 시야 부채꼴도 함께 돈다.

### 결정 사항

| # | 결정 | 근거 |
|---|---|---|
| D1 | 회전은 별도의 선택적(capability) 인터페이스 `IBodyRotateController`(`SetBodyRotateRequest`/`ClearBodyRotateRequest`)로 만든다. 회전 요청을 실제로 내는 조종부만 이를 추가로 구현한다 | 현재 조종부 계약은 채널별로 나뉜다(`IMoveController`=`MoveMediator.cs`, `IRotateController`=`CameraMediator.cs`의 카메라 Look). 몸통 회전은 그중 어디에도 속하지 않는다 |
| D2 | 회전 전용 중재자 `RotateMediator` 신설. `MoveMediator`에 끼워넣지 않는다 | `CameraMediator`가 시점 회전으로 분리돼 있는 기존 구성과 결이 같다. 이동과 회전은 나중에(조준) 반드시 어긋난다 |
| D3 | 회전 명령은 절대 방향 1개다 — `void SetLookDirection(Vector3 worldDirection)` | 해석("어디를 볼지")은 조종부가 이미 끝낸 값을 그대로 받는다. AI는 이동 방향(desired velocity)을 그대로 넘긴다 |
| D3-a | 실행 측은 인터페이스 없이 구체 클래스 `CharacterRotator`로 시작한다. `RotateMediator`가 구체 타입을 직접 참조한다 | `MoveMediator.cs`가 `CharacterMotor`를 구체 타입으로 참조하는 기존 구성과 대칭. 구현체가 하나뿐이라 인터페이스 추출은 후속 작업(D3-b)으로 미룬다 |
| D4 | 요청 페이로드는 `Vector2`(월드 기준, x: 좌우, y: 전후) | `MoveMediator`의 `SetMoveRequest` 콜백과 동일한 관례. `AIController`가 이동 입력과 같은 값을 회전에도 그대로 보낸다 |
| D5 | 구현체는 AI용 `CharacterRotator` 하나만. `PlayerInputComponent`는 `IBodyRotateController`를 구현하지 않는다(수정 0건) | 플레이어 몸통 회전 정책(카메라 추종 여부 등)은 아직 팀 합의 전이라 이번 범위 밖 |
| D6 | 방향이 zero면 직전 방향을 유지한다 | 순찰 도착 후 Wait 중에는 이동 입력이 0이 된다. 0을 그대로 받으면 회전이 튀거나 초기화된다 |
| D7 | 회전 대상은 자기 `transform`이 아니라 캐릭터 루트다. `CharacterMotor`와 같은 "명시적 참조 + 부모 탐색 fallback" 방식을 쓴다 | 회전 스크립트는 `MovePrefab`처럼 루트 아래 자식 프리팹에 놓일 수 있다. `CharacterMotor.Awake`가 이미 `GetComponentInParent<CharacterController>()`로 같은 문제를 해결해 뒀다 |
| D9 | 몸통 회전 기준은 **이동 입력 방향**이다(AI). 카메라 기준 변환은 이번 범위에 포함하지 않는다 | 카메라 연동(과거 D8·D12)은 재작성 메모 참고 — 재도입 시 별도 문서 |
| D13 | `CharacterRotator`는 로컬 X/Z(기준 자세)를 보존하고 yaw(Y)만 회전시킨다. `Awake`에서 `_restRotation`/`_yaw`를 저장해 두고 `Update`에서 yaw만 갱신한다 | 플레이어처럼 스킨 메시 루트의 평상시 로컬 회전이 0이 아닌 캐릭터에도 재사용 가능해야 한다. AI는 `_body`가 루트이고 X/Z가 0이라 `_restRotation`이 identity가 되어 기존 `LookRotation` 거동과 결과가 동일하다 |

## 영향 범위 (Affected users and systems)

- **누가 사용하는가**: 적 AI(이번 구현체). 이후 플레이어(회전 정책이 정해지면 `IBodyRotateController` 구현 추가)
- **어떤 시스템/모듈이 건드려지는가**
  - 신규: `Assets/_Project/Scripts/Mediator/SubMediators/RotateMediator.cs`(`IBodyRotateController` 도 같은 파일에 선언), `Assets/_Project/Scripts/Rotation/CharacterRotator.cs`
  - 수정: `Assets/_Project/Scripts/AI/AIController.cs` — 예전(팀원 Mediator 리팩터 이전) `ICharacterController`/`IRotateRequestSource` 기반 코드가 브랜치 리셋으로 되살아나 있었던 것을, 현재 구조의 `IMoveController`/`IBodyRotateController`로 다시 정리
  - 수정 없음: `PlayerInputComponent.cs`, `MoveMediator.cs`, `CameraMediator.cs`, `CharacterMediator.cs`, `CharacterMotor.cs`
  - `[USER]` 영역: 적 프리팹에 `CharacterRotator`/`RotateMediator` 배치·배선(`RotationPrefab`이 이전 작업에서 만들어졌으나 이번 재설정으로 git에 없음 — 재생성 필요), `Sensor._sightAngle` 정상값 복귀, `_rotateSpeed` 튜닝

## 제약 (Constraints)

- `.unity`/`.prefab`/`.asset`/`.controller` 직접 편집 금지 → 프리팹 배선은 `[USER]` 티켓
- 새 폴더 `Rotation`은 `Movement`를 참조하지 않는다(`CharacterController`는 `UnityEngine` 타입만 사용)

## 열린 질문 (Open questions)

- 실행 측 `ICharacterRotator` 인터페이스 추출 시점(D3-b) — 두 번째 구현체(포탑, 회전 제한, root motion)가 생길 때
- 플레이어 몸통 회전 정책(이동 방향 vs 카메라 추종) — 팀원과 재협의 필요, 과거 D8·D12 논의 참고

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
