# `ICharacterController` 의미 확인 + AIController 미사용 메서드 제거

- **날짜**: 2026-09-24
- **브랜치**: `feature/AI/Controller`
- **관련 문서**: [AI 조종부 코어 구현](./ai-controller-core-implementation.md) · [intent-008 적 AI 시스템](../../../intent/intent-008-enemy-ai-system.md) · [캐릭터 공통 구조](../../../character-architecture.md)

---

## 1. 작업 요약 및 방법

사용자가 "`PlayerInputComponent`가 구현하는 `ICharacterController`를 왜 `AIController`도 구현하는가, `CharacterController`는 '캐릭터'에 대한 것이 아닌가"라고 지적했다. 코드를 확인해 **구조는 정상이고 이름이 오독을 유발한다**는 결론을 냈다.

확인 근거:

- `Assets/_Project/Scripts/Controller/ICharacterController.cs:11-31` — 멤버가 `SetMoveRequest(Action<Vector2>)` / `ClearMoveRequest()` 류의 **콜백 등록/해제뿐**이고 이동을 수행하는 메서드가 하나도 없다. 즉 "캐릭터가 가진 조작 부품"이 아니라 **조종 요청을 보내는 주체(발신자)** 의 계약이다.
- `Assets/_Project/Scripts/Mediator/CharacterMediator.cs:38-48` — 중재자는 발신자에게 `_moveMediator.CommandMove` 같은 **메서드 참조만 꽂아준다.** 발신자가 사람인지 AI인지 모른다.
- 따라서 `PlayerInputComponent`(사람)와 `AIController`(AI)가 나란히 구현하는 것이 이 구조의 **교체점 그 자체**다.

그 확인 과정에서 `AIController`에 **1차 AI 범위에서 호출처가 없는 공개 메서드 2개**(`RequestLook`, `RequestJump`)가 남아 있는 것을 발견해 제거했다.

수정 파일은 `Assets/_Project/Scripts/AI/AIController.cs` 하나이며, 삭제 내용은 `RequestLook(Vector2)` / `RequestJump()` 본문과 각 XML 주석뿐이다. 씬·프리팹·`.meta`는 건드리지 않았다.

## 2. 결정 사항

### 2-1. `ICharacterController` 이름 변경은 **보류**

이름이 `UnityEngine.CharacterController`(캡슐 콜라이더 + `Move()`를 가진 **피조종체** 부품)와 **뜻이 정반대인데 이름이 거의 같다**는 문제는 사실로 인정했다. `PlayerInput` 네임스페이스가 `UnityEngine.InputSystem.PlayerInput`과 겹쳐 파일 상단에 경고 주석을 달아둔 것(`PlayerInputComponent.cs:6-7`), 그리고 `AI.Sensor`가 `UnityEngine.InputSystem.Sensor`와 겹치는 것과 **같은 계열의 반복 실수**다.

후보안은 `ICharacterInputSource` / `ICharacterCommandSource`(= 조종 명령의 공급원). 다만 **AI 1차 구현을 실제로 굴려본 뒤 판단**하기로 사용자가 결정했다. 이번 세션에서는 코드·문서 어느 쪽도 개명하지 않았다.

### 2-2. 인터페이스 계약에 필요한 것은 남긴다

`RequestLook` / `RequestJump`(공개 편의 메서드)만 지웠고, 다음은 **의도적으로 유지**했다.

- `SetLookRequest` / `ClearLookRequest` / `SetJumpRequest` / `ClearJumpRequest` — `ICharacterController` 구현 필수. `CharacterMediator.OnEnable`이 직접 호출한다.
- `_onLookRequested` / `_onJumpRequested` 이벤트 필드 — 위 Set/Clear의 저장소. 대입이 있으므로 미사용 경고 대상이 아니다.
- `private RequestMove(Vector2)` — `Update`와 `StopMove`/`OnDisable`에서 사용 중.

## 3. 현재 상태 및 이슈

- `grep`으로 `Assets/_Project/Scripts` 전체에서 `RequestLook` / `RequestJump` 참조 **0건** 확인. 인터페이스 Set/Clear 메서드와 이벤트 필드는 잔존 확인.
- **Unity 컴파일 미확인.** `rider` MCP 서버 연결이 거부돼(ConnectionRefused) 에이전트가 컴파일을 돌릴 수 없었다. 이전 세션부터 이어지는 동일 상태다.
- 메서드 본문만 삭제했으므로 새 컴파일 에러가 생길 가능성은 낮으나, 앞선 세션의 `Sensor`/`AIController` 신규 코드 자체가 아직 컴파일 검증을 통과한 적이 없다는 사실은 그대로다.
- 앞선 핸드오버 기록에는 이 두 메서드가 이미 제거된 것으로 남아 있었으나 **디스크 파일에는 그대로 있었다.** 기록이 실제 저장보다 앞서 나간 경우이므로, 앞으로 "제거 완료"는 파일 `grep` 결과로 확인한 뒤에만 적는다.

## 4. 다음 할 일 (Next Steps)

1. **Unity 에디터에서 컴파일 0에러 확인** (최우선, 이전 세션에서 이어짐). `rider` MCP 연결 복구 또는 사용자 직접 확인.
2. `Sensor` + `AIController`를 실제 적 오브젝트에 붙여 순찰·감지·수색 동작 확인 (`[USER]` 티켓 — 프리팹/씬 작업).
3. BT 액션·조건 노드 작성 (`com.unity.behavior` 1.0.16 설치 완료 상태).
4. AI 1차 동작을 굴려본 뒤 `ICharacterController` 개명 여부 판단 (2-1 보류 항목).
5. 미커밋 워킹 트리 정리: `manifest.json` / `packages-lock.json` / `EditorBuildSettings.asset` / `SceneTemplateSettings.json` + `Assets/_Project/Scripts/AI/`.
