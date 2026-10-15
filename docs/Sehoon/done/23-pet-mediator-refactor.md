# 23. 펫 Mediator 구조 정리

- **우선순위**: 미정
- **상태**: 완료
- **관련 작업**: [19. 펫 시스템](19-pet-system.md) — 이 구조를 만든 작업. [07-3. 스캔 스킬](07-3-scan-skill.md) — 같은 구조에 스캔을 덧붙였다.

## 목표
펫 모듈을 다른 SubMediator(`MoveMediator`, `AnimationMediator`)와 같은 패턴으로 맞춘다. Mediator 가 컴포넌트를 직접 참조하고, 컴포넌트끼리는 서로 모르게 한다.

## 현재 상황 (작업 전)
- 펫은 `PetSpawner.Start` 에서 월드 오브젝트로 생성되므로 `PetMediator` 가 `[SerializeField]`/`ResolveComponent` 로 펫 컴포넌트를 참조할 수 없었다.
- 그래서 19 에서 `CharacterMediator → PetMediator → PetSpawner → PetAnimator` 경로로 정했고, 그 결과:
  - `PetSpawner` 가 생성 외에 `PlayJump/Land/Hit/SpecialIdle/Death` 전달과 07-3 의 스캔 이동 순서(`PlayScan`)까지 맡았다.
  - `PetMediator` 는 `PetSpawner` 로 그대로 넘기기만 해 전달이 두 번 일어났다.
  - `PetFollower` 가 `PetAnimator` 를 직접 참조해 `SetMoveSpeed` 를 호출했다 (이동 컴포넌트가 애니메이션을 앎).

## 확정 사항
- M1-A: `PetSpawner` 는 생성·파괴만 하고 생성한 펫을 넘긴다. `PetMediator` 가 `PetFollower`·`PetAnimator` 를 직접 호출한다.
- M2-A1: `PetFollower` 는 이동 속도를 이벤트로만 알리고, `PetMediator` 가 `PetAnimator` 로 전달한다.
- `PetMediator` 의 공개 메서드는 유지해 `CharacterMediator` 는 수정하지 않는다.
- 새 공개 인터페이스: `PetSpawner.Pet`, `PetSpawner.Pivot`, `PetSpawner.OnPetSpawned(PetFollower)`, `PetFollower.OnMoveSpeedUpdated(float)`, `PetFollower.MoveAndHold(Transform, Action<Vector3>)`.
- 스캔 이동 코루틴(이동 → 머묾 → 복귀)은 `PetFollower.MoveAndHold` 로 둔다. 07-3 에서 추가한 `ChangeTarget`·`MoveToTarget`·`HoldPosition` 은 private 으로 바꾼다.
- 이동·머묾 시간(0.3 / 0.5초)은 `PetFollower` 직렬화 필드(`_moveDuration`, `_holdDuration`)로 둔다. `_scanPivot` 은 `Player.prefab` 안의 Transform 이라 런타임 생성되는 `Pet.prefab` 에서 참조할 수 없으므로 `PetMediator` 에 두고 인자로 넘긴다.

## 결과 (2026-10-14)

### 구조
- `PetSpawner`: `Start` 에서 펫 생성 → `SetTarget(_pivot)` → `OnPetSpawned` 발행. 파괴 시 펫 파괴. `Pet`, `Pivot` 프로퍼티.
- `PetFollower`: `_petAnimator` 참조 제거. 매 `LateUpdate` 의 수평 속도와 머묾 시작 시 0 을 `OnMoveSpeedUpdated` 로 알린다.
  - `MoveAndHold(target, onArrived)`: `_moveDuration` 동안 target 으로 시간 보간 이동(원래 추종 대상 기준 벽 보정) → `onArrived(위치)` → `_holdDuration` 머묾 → `SetTarget` 으로 정한 원래 대상 추종 복귀. `SetTarget`·`StopFollow`·재호출 시 진행 중인 이동을 취소한다.
- `PetMediator`:
  - `Awake` 에서 `OnPetSpawned` 구독(이미 생성됐으면 바로 연결), 받은 펫에서 `PetAnimator` 를 얻고 `OnMoveSpeedUpdated` → `PetAnimator.SetMoveSpeed` 연결. `OnDestroy` 에서 해제.
  - `Notify*` 는 `PetAnimator` 를 직접 호출. `NotifyDeath` 는 `StopFollow`(스캔 이동 취소 포함) → 사망 애니메이션.
  - `RequestScan` 은 `_pet.MoveAndHold(_scanPivot, onReady)` 만 호출한다. 펫이나 `_scanPivot` 이 없으면 pivot 위치로 바로 알린다. 동작은 07-3 과 같다.

### 변경 파일
- `PetSpawner.cs`, `PetFollower.cs`, `PetMediator.cs`
- `Player.prefab`: `PetSpawner._scanPivot` 오버라이드 제거, `PetMediator._scanPivot` 에 `PetScanPivot` 연결.
- `Pet.prefab`: 제거된 `PetFollower._petAnimator` 직렬화 값 정리 (재저장).
- `CharacterMediator.cs` 변경 없음.

### 검증 (플레이 모드, `Map_1_Lobby`)
- 펫 생성 후 `PetMediator` 가 `PetFollower`·`PetAnimator` 를 연결함.
- 6 m/s 이동 중 펫 Animator `Speed` 5.26, 정지 후 0.02 (이동 속도 전달).
- 이동 중 스캔(`MoveAndHold` 이전 후 재확인): 요청 0.30초 뒤 도착 알림, 앞 지점 오차 0.02m, 0.5초 머문 뒤 PetPivot 추종 복귀.
- `NotifyJumpStarted` → Jump 상태로 전이, `NotifyHit(0)` → 피격 상태로 전이.
- 스캔 이동 중 `NotifyDeath` → 코루틴 취소(도착 콜백 없음), 추종 정지, `Death` 트리거 설정.
- 신호는 `PetMediator` 메서드를 직접 호출해 확인했다 (실제 점프·피격 입력은 확인하지 않음).

## 완료 기준
- 펫 컴포넌트끼리 직접 참조하지 않고, `PetSpawner` 에 전달 메서드가 없다.
- 플레이 모드에서 이동 애니메이션·점프·피격·특수 대기·사망·스캔이 이전과 같이 동작한다. 컴파일 에러 없음.
