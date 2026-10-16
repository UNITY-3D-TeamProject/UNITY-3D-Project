---
id: intent-025
title: AI(적·군중) 애니메이션 — EnemyAnimator ↔ EnemyAnimationMediator
part: AI / Animation / Mediator
status: open   # open | resolved
created: 2026-10-15
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
애니메이션이 붙어 있는 것은 플레이어(`CharacterAnimator` ↔ `AnimationMediator`)와 펫뿐이다. 적과 군중 NPC는 같은 `CharacterMediator` 허브를 쓰지만 애니메이션 층이 없다.
intent-017(군중)과 intent-021(GC)은 애니메이션을 범위 밖으로 두었다. 이 문서가 그 부분을 이어받는다.

`CharacterAnimator`는 플레이어 전용이라 재사용할 수 없다.
- 플레이어 뼈 이름(`spine_01`, `SK_ZMike` 등)과 UpperBody 레이어에 묶여 있다.
- MoveX/MoveY를 카메라 시점 기준으로 계산하는데, AI에는 카메라가 없다.
- 사망 상태가 다시 켜질 때 초기화되지 않아 풀 재사용에 맞지 않는다.

## 기대 결과 (Proposed outcome)
플레이어와 같은 구조로 AI 전용 쌍 **`EnemyAnimator` ↔ `EnemyAnimationMediator`**를 두고, `CharacterMediator`가 연결한다.
- 대상: 근접, 원거리, 특수, 보행자, GC, 도둑, 보스
- 공통: Idle, 이동, 공격, 사망(보행자 제외) → `EnemyAnimator`
- 피격 → `EnemyAnimator`(대부분의 전투 객체가 씀)
- 스킬별 모션(원거리 예비동작, 특수 Wind, 보스 스킬 등) → `EnemyAnimator`의 **스킬 이름 → 파라미터 매핑 데이터**. 객체가 늘어도 코드는 바뀌지 않는다.
- 특정 객체만 쓰는 코드 로직(보행자 점프) → **기능별 별도 컴포넌트**(`JumpAnimator`). Mediator가 선택 참조로 들고 있다. 상속은 쓰지 않는다.
- 컨트롤러 에셋: 베이스(`EnemyBase.controller`) + 객체별 AnimatorOverrideController. 전용 상태가 필요한 객체만 베이스를 복사해 상태를 추가한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: AI 프리팹(Melee, Ranged, Pedestrian, 이후 GC·Special·Thief·Boss)
- 어떤 시스템/모듈이 건드려지는가:
  - 신규: `CharacterAnimation/EnemyAnimator.cs`, `CharacterAnimation/JumpAnimator.cs`, `Mediator/SubMediators/EnemyAnimationMediator.cs`
  - 수정(추가만): `Mediator/CharacterMediator.cs`(필드·구독·피격·사망), `Mediator/SubMediators/SkillMediator.cs`(`OnSkillStopped` 이벤트)
  - 에셋 [USER]: `Animations/Enemy/` 컨트롤러, 프리팹 Animator 설정

## 제약 (Constraints)
- `CharacterMediator`와 `SkillMediator`는 이세훈 담당이다. 추가만 하고 기존 줄은 바꾸지 않으며, 변경 내용을 공유한다.
- `feature/AI/Enemy` 브랜치도 `CharacterMediator`를 수정하므로(`ResetForReuse`, `OnHitFrom`) 머지 충돌에 주의한다.
- 클립 원본은 `Assets/ThirdParty`(Git 제외)에 있다. 사용할 클립은 `_Project`로 복제해서 쓴다.
- 컨트롤러는 MCP로 만들지 않는다. 사용자가 가이드(`docs/features/enemy-animation-controller-guide.md`)를 보고 에디터에서 직접 만든다.
- Animator의 Apply Root Motion은 끈다(CharacterMotor와 충돌).

## 열린 질문 (Open questions)
- 특수·도둑·보스는 코드와 프리팹이 없다. 생기면 각각 무엇을 할지 정한다: 매핑 항목 추가, 컨트롤러(Override 또는 복사본) 제작, 전용 컴포넌트가 필요한지 여부.
- GC는 모델(SkinnedMesh)이 없다. 모델이 확정되면 Override를 붙인다.
- 이 브랜치에는 적이 사망 후 풀로 돌아오는 흐름(`ResetForReuse`)이 없다. `feature/AI/Enemy`와 머지한 뒤 재사용 시 애니메이션 초기화를 다시 확인한다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
