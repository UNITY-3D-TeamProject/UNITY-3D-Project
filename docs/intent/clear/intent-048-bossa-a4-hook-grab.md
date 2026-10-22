---
id: intent-048
title: 보스 A A4 를 블리츠크랭크 Q 식 그랩(한 손이 날아가 잡아당김) + 반대 손 내려치기로 재설계
part: AI / Boss / BossA
status: resolved   # open | resolved
created: 2026-10-20
resolved: 2026-10-21
---

## 문제 (Problem)
지금 A4(`BossAGrab`)는 표적 칸 위에 손을 띄웠다가 내려치며, 반경 안의 플레이어를 아레나 바깥쪽 판 밖으로 던진다.
의도한 A4 는 블리츠크랭크 Q(Rocket Grab)처럼 **손이 직선으로 날아가 플레이어를 잡아 보스 쪽으로 끌어당기는 그랩**인데 방향이 반대다(투사체형 vs 칸 내려찍기형, 끌어당김 vs 던짐).

## 기대 결과 (Proposed outcome)
- 한 손(그랩 손)이 플레이어 쪽으로 **직선 경로**를 따라 날아간다. 경로는 예고로 미리 보인다(플레이어는 경로 밖으로 비켜 피한다).
- 경로에서 처음 닿은 플레이어를 잡아 **보스 쪽으로 끌어당긴다**.
- 끌어당긴 뒤 **반대쪽 손이 내려친다**. 왼손이 그랩하면 오른손이, 오른손이 그랩하면 왼손이 친다.
- 내려치기는 **한 칸만** 무너뜨린다.
- A4 연속 금지는 유지한다. 모서리 칸 규칙(선택기 규칙 3)은 직선 경로 방식에 맞지 않아 제거한다(사용자 확인).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스 A 와 싸우는 플레이어, 보스 밸런스를 조정하는 팀원.
- 어떤 시스템/모듈이 건드려지는가: `BossAGrab`(재작성), `BossHand`(날아가기·끌려오는 동안 동행 동작 추가), `BossAPatternSelector`(규칙 3 제거, A4 선택 결과), `SBossAPatternChoice`, `BossController`. 경로 예고 표현에 따라 `BossArena`/`IBossArena`, 프리팹(`BossA.prefab`의 `A4`)이 더해질 수 있다.

## 제약 (Constraints)
- 예산/기한: 없음.
- 지켜야 할 정책: 플레이어 이동·물리 조작은 기존 `CharacterMotor.AddExternalDisplacement`(`BossKnockback`)만 쓴다. 새 플레이어 기능이 필요하면 범위 확대로 보고 별도 intent 로 나눈다. 순수 로직(`Logic/`)은 Unity 를 참조하지 않는다.
- 쓰지 말아야 할 것: 확정되지 않은 수치의 고정 값(임시값은 `Tooltip`에 "임시값"으로 표기).

## 결정 사항 (2026-10-20, 사용자 "권장대로")
1. 끌려오는 칸 = 그랩 손이 떠난 쪽에서 가장 가까운 멀쩡한 칸(`FindNearestSafe`), 칸 중심으로 끌려온다. 내려칠 칸도 이 칸이다.
2. 새 플레이어 기절·이동 잠금은 만들지 않는다. 내려치기 예고(임시 1.5초, 잡은 순간부터)로 비킬 수 있게 한다. 처음 권장은 0.7초였으나 칸이 12 m 라 걸어서 나가기엔 짧아 1.5초로 시작한다(Play 조정 대상).
3. 직선 예고는 새 판 프리팹 `BossLineTelegraph` + `IBossArena.ShowLine/HideLine` 풀.
4. 붕괴 제한 폴백에서 A4 를 뺀다(1페이즈는 Rest).
5. 그랩 손은 기존 `CreateSingleHand` 규칙. 빗나가면 손만 회수하고 내려치기 없음. 수치는 전부 임시값. Grip 클립은 이번 범위에서 쓰지 않는다. 피해는 내려치기만 기존 A1 `_hitEffect`.

## 열린 질문 (Open questions)
- Play 육안 확인 남음: 직선 예고가 보기 좋은지, 날아가는 손 모양·높이(`_hookHeight`), 끌려오는 속도감, 내려치기 예고 시간 1.5초가 적당한지.
- 씬의 `BossArena._linePrefab` 연결은 테스트 씬(저장소 밖)에 했다. 저장하지 않았고, 다른 씬에 보스를 쓰려면 직접 연결해야 한다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 블리츠크랭크 Q 식 그랩. 이후 intent-059 에서 매달려 끌고 가고 오기로 다듬음. 사용자 육안 확인 통과(2026-10-21).
- 관련 커밋/PR: 커밋 전 → [기록](../../HANDOVER/BF_Leers/2026-10-20/boss-a-a4-hook-grab.md)
