---
id: intent-094
title: 보스A 그랩 손(집게)이 바닥보다 높이 떠서 날아감
part: AI / Boss / BossA
status: open
created: 2026-10-30
resolved: null
---

## 문제 (Problem)
- AI_TODO2 보스-26: 그랩 때 손이 나가는 위치가 위로 올라가 있다.
- 원인: `BossHand` 는 날아가는 동안(`Flying`, 손가락 펴짐) 손 본(Palm, 하위 `GrabPoint` 포함)을 손 오브젝트보다 `_boneLift`(8m) 위에 둔다. `BossAGrab` 은 손 오브젝트를 바닥 + `_hookHeight`(1m)로 보내므로, 집게와 끌려가는 플레이어(`GrabPoint` 기준)는 바닥 + 약 9m 에 뜬다.

## 기대 결과 (Proposed outcome)
- 그랩 자세에서는 손 본이 손 오브젝트 높이에 놓여, 집게 턱(`GrabPoint`)이 바닥 + `_hookHeight` 에서 날아간다.
- 잡힌 플레이어도 그 높이로 끌려갔다 돌아온다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스A 전투의 플레이어
- 어떤 시스템/모듈이 건드려지는가: `BossHand`(그랩 자세의 손 본 높이 `_grabLift` 추가)

## 제약 (Constraints)
- 손 오브젝트(콜라이더) 위치와 `BossAGrab` 로직은 바꾸지 않는다.
- 기본값 0 으로 두어 프리팹 직접 수정 없이 적용한다. 값은 육안으로 조정.

## 열린 질문 (Open questions)
- 손 모델이 바닥에 파묻히는 정도는 임시값 `_hookHeight`(1m)·`_grabLift` 를 육안으로 보며 조정.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
