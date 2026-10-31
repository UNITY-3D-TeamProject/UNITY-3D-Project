---
id: intent-088
title: 특수몹 약점이 맞지 않음 — 약점 연결 누락 + 피격 캡슐 밖 후보 위치
part: AI / Enemy / Combat
status: open   # open | resolved
created: 2026-10-29
resolved: null
---

## 문제 (Problem)
- AI_TODO2 잡몹-8: 특수몹 약점 5곳 중 일부는 플레이 중 아예 맞힐 수 없다.
- 조사(2026-10-29, Play `Map_4_Security`) 결과 원인은 두 가지다.
  1. `Special.prefab` 의 중첩 프리팹 `CombatPrefab` 안 `CharacterCombat._weakPoint` 가 비어 있다(처음부터 오버라이드 없음). Melee·Ranged 는 자기 `WeakPoint` 구를 연결해 두었다. 그래서 특수몹은 어디를 맞혀도 치명타가 나지 않는다.
  2. 치명타 판정은 총알이 CharacterController 에 닿은 지점에서 탄도를 연장해 약점 구(반지름 0.4, 씬 스케일 2 → 0.8m)를 지나는지 본다. 총알이 닿는 캡슐은 월드 반지름 0.6m·높이 0~2.2m 인데 로봇 모습은 폭 2.8m·높이 3.3m 라 후보 일부가 캡슐 밖에 있다.
     - 정면 조준 141칸 시뮬레이션(약점 임시 연결): 1번(머리 중심) 25칸, 2번(몸통 중심 뒤쪽) 24칸, 3번(왼팔) 1~2칸, 4번(오른팔) 4칸, **5번(머리 위 2.47m) 0칸**.
     - 1·2번은 정면에서 몸에 가려 보이지 않는다.

## 기대 결과 (Proposed outcome)
- `CombatPrefab` 의 `_weakPoint` 에 `WeakPoint` 의 SphereCollider 를 연결한다.
- 후보 5곳을 피격 캡슐 안이면서 정면에서 보이는 곳으로 옮긴다(사용자 결정: 캡슐을 키우거나 피격 콜라이더를 추가하지 않고 후보만 이동).
- 5곳 모두 정면 조준 시뮬레이션에서 치명타 칸이 충분히 나오고, 정면에서 약점 위치가 보인다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어 사격·스캔, 특수몹
- 어떤 시스템/모듈이 건드려지는가: `Special.prefab`(후보 Transform 위치·부모, `CombatPrefab` 오버라이드). 코드 변경 없음.

## 제약 (Constraints)
- 프리팹은 YAML 직접 수정 없이 Unity Editor(MCP)로 수정한다.
- CharacterController 크기·피격 판정 코드는 바꾸지 않는다(이동·NavMesh 영향).
- 후보는 뼈를 따라 움직이도록 뼈 자식으로 둔다.

## 결정 사항
- 2026-10-29: `CombatPrefab` 의 `CharacterCombat._weakPoint` 오버라이드에 `WeakPoint` SphereCollider 연결.
- 2026-10-29: 후보 위치(프리팹 루트 기준, 씬에서는 ×2). 1번은 약점 구가 머리 앞면까지 닿고 판정도 나와 그대로 둠.
  - 1번 `RigHead` (0, 0.93, 0.06) — 유지
  - 2번 `RigMain` 몸통 뒤 → `RigWingBottom` 아래 지느러미 (0, 0.55, 0)
  - 3번 `RigLArm1` 팔 바깥 → 같은 뼈 어깨 안쪽 (-0.2, 0.8, 0.1)
  - 4번 `RigRArm1` 팔 바깥 → 같은 뼈 어깨 안쪽 (0.2, 0.8, 0.1)
  - 5번 `RigWingTop` 머리 위 → `RigHead` 정수리 (0, 1.05, 0.03), 캡슐 꼭대기(1.1) 안쪽
- 검증(Play `Map_4_Security`, Idle 자세, 임시 연결 없이 `IsCriticalHit` 로 정면·측면·위쪽 141~164칸 조준): 1번 27~33, 2번 48~53, 3번 31~43, 4번 31~34, 5번 10~14칸. 수정 전 5번 0칸, 3·4번 1~11칸.
- 피격 캡슐이 작아(반지름 0.6m·높이 2.2m) 약점 구(0.8m)끼리 많이 겹친다. 특히 1·5번은 머리 부근이라 표시가 비슷하다. 캡슐을 키우지 않기로 한 결정의 한계.

## 열린 질문 (Open questions)
- 없음(위치 방식은 2026-10-29 사용자 결정). 직접 플레이로 스캔 표시·조준감 확인 남음.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
