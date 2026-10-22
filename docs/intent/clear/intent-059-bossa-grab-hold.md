---
id: intent-059
title: 보스 A 그랩(A4)에 잡히면 잠시 꼼짝 못 하고 화면이 흔들림
part: AI / Boss / BossA, Player / Mediator
status: resolved
created: 2026-10-21
resolved: 2026-10-21
---

## 문제 (Problem)
- AI_TODO 보스-13. 그랩에 잡혀 끌려온 직후에도 바로 걸어서 내려치기 예고를 피할 수 있어 잡힌 느낌이 없다.
- 잡히는 순간의 타격감(화면 흔들림)이 없다.

## 기대 결과 (Proposed outcome)
- 잡히면 끌려오는 동안(0.4초) + 끌려온 뒤 일부 시간(임시값 0.7초) 이동·점프·구르기를 막고, 그 뒤 풀어 준다. 남은 예고 시간 동안은 탈출할 수 있다.
- 잡히는 순간 플레이어 사격에 쓰는 카메라 임펄스(`PlayerBaseCamera._fireImpulseSource`)를 세기만 바꿔 재사용해 화면을 흔든다(반동은 없음).

## 영향 범위 (Affected users and systems)
- 플레이어: `MoveMediator`(이동 잠금), `CharacterMediator`·`PlayerFacade`(잠금·흔들기 창구), `CameraMediator`·`PlayerBaseCamera`(임펄스만 재생)
- 보스: `IBossSituation`·`BossEncounter`(플레이어 창구 전달), `BossAGrab`(잠금·해제·흔들기)

## 제약 (Constraints)
- `docs/character-architecture.md`: 보스는 `PlayerFacade` → `CharacterMediator` → 하위 중재자 순으로만 요청한다.
- TODO 방법1(Move 중재자를 껐다 켜기)은 쓰지 않는다. 입력은 값이 바뀔 때만 오므로(`PlayerInputComponent.OnMove`) 다시 켰을 때 누르고 있던 이동키가 사라지고, 사망 처리의 비활성화와 섞인다. 방법2(중재자 함수)로 하되 끄지 않고 잠금 플래그를 둔다.
- 패턴이 취소·중단돼도 잠금은 반드시 풀린다(`Stop`·`OnDisable`).

## 열린 질문 (Open questions)
- 없음(2026-10-21 사용자 결정: 끌려온 뒤 일부만 구속, 잡힐 때 화면 흔들기). 구속 시간·흔들기 세기는 임시값으로 두고 육안 확인하며 조정.

## 해결 기록 (Resolution) — 해결 후 작성
- 진행(2026-10-21): 구현·리컴파일·Play 수치 확인 완료, 사용자 육안 확인 대기 → [기록](../../HANDOVER/BF_Leers/2026-10-21/boss-a-grab-frequency-hold.md)
- 재확인(2026-10-21): 사용자 육안 확인에서 "잡혀도 끌려오지 않음". 원인은 그랩 손 콜라이더(6×17×6 m, 비트리거)와 플레이어 CharacterController 충돌 — 날아오는 손이 플레이어를 밀어낸 뒤 잡고(x 240 → 221), 끌려오는 동안 앞서 돌아가는 손에 막혀 목표 칸보다 16 m 앞에서 멈춤. 그랩 손이 날아갔다 돌아오는 동안만 `Physics.IgnoreCollision`으로 손–플레이어 충돌을 끄도록 수정, Play 에서 제자리 포획·목표 칸 중심까지 끌려옴 확인. 구속·흔들기 육안 확인 다시 대기 → [기록](../../HANDOVER/BF_Leers/2026-10-21/boss-a-grab-frequency-hold.md)
- 변경(2026-10-21, 사용자 요청·확정): 닿은 순간부터 손에 매달려(중력 끔) 판 반대편 끝까지 끌려갔다가 같은 속도로 돌아와 보스 쪽 가장 가까운 멀쩡한 칸 중심에 놓임. 놓인 뒤 0.7초 구속. 플레이어 쪽은 `SetPlayerHeld` → Facade → 중재자 → `MoveMediator.SetHover(0)`. Play 확인 완료, 육안 확인 대기 → [기록 6절](../../HANDOVER/BF_Leers/2026-10-21/boss-a-grab-frequency-hold.md)
- 최종 결정: 닿은 순간부터 손에 매달려(중력 끔) 판 반대편 끝까지 끌려갔다가 같은 속도로 돌아와 보스 쪽 칸 중심에 놓임, 놓인 뒤 0.7초 구속, 잡힐 때 사격 임펄스 흔들기(2), 그랩 손–플레이어 충돌 무시. 사용자 육안 확인 통과(2026-10-21), 임시값 유지.
- 관련 커밋/PR: 커밋 전 → [기록](../../HANDOVER/BF_Leers/2026-10-21/boss-a-grab-frequency-hold.md)
