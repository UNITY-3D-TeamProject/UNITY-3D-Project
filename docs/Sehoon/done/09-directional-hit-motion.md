# 09. Hit 시 방향별 모션

- **우선순위**: 높음
- **상태**: 완료
- **관련 작업**: [10. 피격 깜빡임 + 무적](10-hit-blink-invincibility.md)

## 목표
피격 방향에 따라 다른 피격 모션을 재생한다.

## 현재 상황
- 구현 완료 (사용자 확인).
- 흐름: 데미지 → `CharacterMediator.OnAttributeChangeCallback` 이 `data.Context` 를 `_lastChangeContext` 에 보관 → `CombatMediator` → `CharacterCombat.OnHit` → `CharacterMediator.OnHitCallback` 이 `GetHitSource`(HitPoint → Cursor 위치 → null)로 기준 위치를 골라 `AnimationMediator.NotifyHit(Vector3?)` → 깜빡임 + `CharacterAnimator.PlayHit(Vector3?)`.
- 방향 판정: (기준 위치 − 몸 위치) 수평 투영을 몸(SK_ZMike) 정면과 비교. ±45° 이내 앞, ±135° 밖 뒤, 나머지 좌/우. null 이면 앞.
- Animator: 파라미터 `Hit`(Trigger), `HitDirection`(Int: 0 앞, 1 뒤, 2 왼쪽, 3 오른쪽). 레이어 `HitUpper`(Override, weight 1, `UpperBodyMask`) — Any State → HitFront/HitBack/HitLeft/HitRight (0.1초), exit time 1.0 → Empty (0.15초).
- 기존 `AnimationMediator.NotifyHit()` 는 제거했다. `CharacterCombat`·`CombatMediator`·공격 측 코드·프리팹은 바꾸지 않았다.
- 테스트: `Assets/Test/TestScene` 의 `DirectionalHitTester`(ContextMenu 로 6가지 경우 실행, git 추적 안 됨).
- 알려진 사항
  - 근접(`MeleeHitController`)은 HitPoint 를 채우지 않아 공격자(Cursor) 위치로 판정한다.
  - 사격 자세 중 피격 시 오른팔 조준 보정이 피격 포즈 위에 겹친다.

## 확정 사항
- 방향: 4방향(앞/뒤/좌/우). 클립은 데모 씬 번호 기준 전방 26(`HitRegister_Front`), 뒤 25(`HitRegister_Back`), 오른쪽 39(`HitRegister_R`), 왼쪽 38(`HitRegister_L`). 모두 제자리 클립.
- 방향 기준: `CharacterMediator` 가 받은 어트리뷰트 변경 `SEffectContext.HitPoint` 를 쓴다. HitPoint 가 없으면(근접 등) `Cursor`(공격자) 위치로 계산하고, Cursor 도 없으면 전방 모션.
- 판정: (기준 위치 − 캐릭터 위치)를 수평 투영해 **몸 정면**(SK_ZMike)과 비교한다.
- 전달 방식: `CharacterCombat`(이식 모듈)·`OnHit` 시그니처는 바꾸지 않는다. `CharacterMediator` 에서 context 를 읽어 `AnimationMediator` 로 넘긴다.
- 범위: 상체 전용 레이어(UpperBodyMask). 경직 없음, 이동·사격은 끊기지 않는다.
- 플레이어만 적용.

## 열린 질문
- [x] 방향 구분 → **4방향**(앞/뒤/좌/우).
- [x] 사용할 애니메이션 클립은? → 26/25/39/38 (확정 사항 참고).
- [x] 방향 전달 방식 → `CharacterMediator` 의 context(HitPoint) 활용. 이벤트 시그니처 변경 없음.
- [x] 방향 기준 → HitPoint, 없으면 Cursor 위치, 둘 다 없으면 전방. 몸 정면 기준.
- [x] 경직·레이어 → 상체 전용 레이어, 경직 없음.
- [x] 적에게도 적용? → 플레이어만.

## 완료 기준
- 플레이 모드에서 방향별로 다른 피격 모션이 재생되는지 확인.
