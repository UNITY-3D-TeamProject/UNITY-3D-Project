# 낙하 복귀 후 방향 잠금 해제 수정

- 일시: 2026-10-23 20:20 KST
- 브랜치: `feature/PlayerFacade/Rotate`
- 목적: 생성 연출 종료 뒤 `PlayerFacade.SetLookDirection`을 호출하면 몸 방향 잠금이 남는 문제 해결.
- 역할: 이전 원인 분석·대안 검토는 Claude Opus와 Codex, 이번 구현은 Claude Sonnet, 변경 검토·Unity 검증·이 문서는 Codex가 담당했다.

## 사용자 승인과 적용 내용

이전 검토에서 제안한 B안(기존 API 안에서 생성 연출 상태에 따라 잠금 수명 결정)을 사용자가 구현하도록 승인했다. 별도 해제 API를 추가하는 대안보다 기존 호출부를 유지하고 변경 범위를 줄일 수 있다.

- `CharacterMediator.SetLookDirection`: 몸과 카메라 방향을 즉시 맞춘다. 생성 연출 드라이버가 있고 아직 끝나지 않았다면 기존 종료 콜백까지 잠금을 유지한다. 드라이버가 없거나 종료했다면 `PublishViewForward()`로 정렬된 시점을 전달한 다음 `UnlockLookDirection()`을 호출한다.
- 생성 연출 상태는 `SpawnEffectDriver.IsEnded`로 확인한다. `Start` 전에는 `_isSpawnLocked`가 false일 수 있으므로 이 플래그로 판단하지 않는다.
- 0 또는 순수 수직 방향은 기존 하위 컴포넌트와 같은 수평 투영·`Mathf.Epsilon` 기준으로 무시한다. 무효 입력으로 기존 잠금을 해제하지 않는다.
- `PlayerFacade`와 `CharacterMediator`의 XML 설명을 실제 동작에 맞췄다. 공개 시그니처, 기존 생성 연출 종료 콜백, 씬·프리팹·스폰 호출부는 변경하지 않았다.

## 변경 파일과 근거

- [CharacterMediator.cs](../../../Assets/_Project/Scripts/Mediator/CharacterMediator.cs): `SetLookDirection`, 311행부터.
- [PlayerFacade.cs](../../../Assets/_Project/Scripts/Facade/Player/PlayerFacade.cs): `SetLookDirection` XML 설명, 231행부터.
- [이전 원인 분석·대안 검토](2026-10-23-201000-fall-return-look-lock-review.md).

## 실제 검증

Codex가 수행했다.

- Unity 스크립트 컴파일: `completed`, `failed: false`, `errors: []`, `compilationFailed: false`.
- Unity 임시 preview scene에서 실제 Facade·Mediator·Rotator·Camera 메서드를 호출한 검증: **18/18 통과**. 비공개 참조·상태와 lifecycle은 reflection으로 구성했다.
  - 드라이버가 없을 때 몸·카메라 정렬, 즉시 해제, 해제 전 시점 발행 순서.
  - 해제 후 새 카메라 방향이 Rotator의 목표 방향으로 전달됨.
  - 0·수직 방향 입력 시 기존 잠금·몸·카메라·시점 캐시·이벤트 유지.
  - 생성 연출 대기 중에는 `Start` 이전에도 잠금 유지, 시점 조기 발행 없음.
  - 기존 생성 연출 종료 콜백의 시점 발행·잠금 해제 유지.
  - 연출 완료 뒤 재호출 시 즉시 해제.
  - `StageManager.MovePlayerTo` 후 Facade 방향 지정 시 위치·방향 적용 및 기존 잠금 해제.
  - 수직 성분이 섞인 방향의 수평 정렬, 카메라 pitch 유지, 참조 누락 시 기존 조기 반환 유지.
- `git diff --check`: 통과. 제품 변경은 위 두 파일뿐이다.
- 테스트 오브젝트와 preview scene은 `finally`에서 정리했다. 테스트용 소스·씬·에셋은 저장하지 않았다.

## 검증 한계와 중계 상태

- 실제 Play Mode에서 입력·애니메이션·VFX를 포함한 낙하 복귀 전체 흐름은 미검증이다. 다른 작업자가 추가한 실제 낙하 복귀 호출부가 현재 브랜치에 없어, 위치 이동 뒤 기존 Facade API를 호출하는 순서를 임시 씬에서 검증했다.
- 구르기 등 별도 동작의 잠금 소유권을 구분하는 구조는 추가하지 않았다. 연출 종료 후 유효한 방향 지정은 기존 공유 방향 잠금을 해제하는 승인된 B안의 동작이다.
- Claude의 코드 저장은 diff로 확인했다. 다만 최초 호출과 동일 UUID 재개 모두 Remote Control 활성 증거 검증에 실패하여 `completed`/exit 0의 정상 완료 응답은 받지 못했다. 앱 원문 공개 성공이나 Claude 최종 응답 수신으로 보고하지 않는다. 실패한 소유 세션은 정리했고, 저장된 코드의 검토·컴파일·동작 검증은 Codex가 독립 수행했다.
- 추가 수정 필요 사항은 이번 검토·검증 범위에서 발견되지 않았다. 커밋·푸시는 수행하지 않았다.
