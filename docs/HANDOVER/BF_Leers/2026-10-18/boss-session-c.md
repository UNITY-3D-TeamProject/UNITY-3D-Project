# 보스전 세션 C — LOGIC-5 (2026-10-18)

- 스펙: `spec/boss.md`(로컬 전용) · 이전 기록: [보스 세션 B](./boss-session-b.md)
- 브랜치: `feature/AI/Boss`

## 1. 작업 요약 및 방법

- `Logic/`에 `BossAPatternSelector`, `EBossAPattern`, `SBossSituation`, `SBossAPatternChoice`를 만들었다(각 `.meta` 포함).
- `Choose(in SBossSituation)` 순서: Defeated면 Rest → 붕괴 제한(붕괴 ≥ 4: 최종 A6 / 1페이즈 A4 / 없으면 Rest) → 상황 규칙 1~5 → 기본 순서(A1→A2→A4→A5→A6, 최종 A1은 A1Double).
- 몰기(D1·P3): 붕괴 칸만 구멍으로 본다. 구멍이 두 방향 이상이면 무작위, 반대쪽 이웃이 판 밖·붕괴·예고면 기본 표적으로 복귀. A1Double 두 번째 칸, A2 기본 줄에 적용.
- 테스트 `Assets/Test/Boss/BossAPatternSelectorTests.cs`(Git 제외) 44건. 컴파일 에러 0, EditMode 88/88 통과.

## 2. 결정 사항 (사용자 확인)

- A1·A4 한 손 배정은 같은 규칙, "직전 손" 기록 공유(기록 없으면 왼손).
- 규칙 5의 줄이 몰기보다 우선. 가로·세로 둘 다 3초 이상이면 더 오래 머문 줄(같으면 무작위).
- 기본 순서 인덱스는 기본 순서로 고를 때만 진행.
- 스펙과 달라진 점: `Tiles`는 정확한 길이 `int[]`, `ChargeSign` 입력 제거(안 쓰임). 상세는 스펙 세션 표 C행 메모.

## 3. 현재 상태 및 이슈

- 커밋 안 함(검증 게이트 대기). Git에 보이는 새 파일: 선택기·enum·struct 4개와 `.meta`. 테스트는 Git 제외.
- `intent-044-boss-fight.md`가 디스크·Git 이력에 없는 문제는 그대로(세션 B부터 미결).

## 4. 다음 할 일 (Next Steps)

- 세션 D: UNITY-1(`BossArena`·`BossTile`·`IArenaQuery`/`IBossArena`), UNITY-2(낙사), UNITY-3(`BossKnockback`).
- intent-044 문서 처리 결정.
