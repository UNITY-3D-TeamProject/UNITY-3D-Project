---
id: intent-075
title: 보스 B 를 3라운드에서 A 머리 위 Spell 연출로 등장시키기
part: AI / Boss / Stage
status: open   # open | resolved
created: 2026-10-25
resolved: null
---

## 문제 (Problem)
`Map_4_Security` 에서 보스 A·B 가 2라운드(특수몹 발판)부터 함께 미리 만들어져 서 있어, 2라운드 내내 B(작은 보스)가 보인다. 3라운드(보스전)가 시작되면 연출 없이 바로 전투가 시작된다.

## 기대 결과 (Proposed outcome)
- 2라운드에서는 B 가 보이지 않는다 (A 만 서 있음).
- 3라운드가 시작되면 B 가 A 머리 위 공중에 나타나 Spell 모션을 재생한 뒤, 쿵 하고 바닥(B 생성 위치)에 착지한다.
- 착지가 끝난 뒤에 A·B 의 BT 를 켜고 보스전(`BossEncounter.Begin`)을 시작한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안 앱(Map_4_Security) 보스전을 플레이하는 플레이어
- 어떤 시스템/모듈이 건드려지는가: `BossFightSpawner`(미리 생성은 A 만, B 는 등장 연출 후 보스전 시작), `BossBController`(`PlayEntrance`·`OnEntranceLanded`), 신규 `BossBEntrance` 스킬, `BossBSkillBase.BeginRun`(아레나 없이도 시작 가능한 인자), `BossController.HeadTransform`, `BossB.controller`·`BossB.prefab`(Spell 상태·`Entrance` 스킬·EnemyAnimator 매핑), 신규 클립 `BossB_Spell.anim`

## 제약 (Constraints)
- 사용자 확정 (2026-10-25): 사망 후 재도전 때도 연출을 매번 보여준다 / 연출 중 무적 / 하강은 자유낙하로 쿵 착지 / 연출 중 플레이어 이동 잠금.
- 중재자 구조 유지: 스킬은 `AIController.ExecuteSkill` 요청 경로(`EnemyAnimator` 스킬 매핑으로 모션 재생).
- `ThirdParty` 는 Git 제외라 Spell 클립은 `_Project/Animations/Enemy/Clips/` 로 복사한 `.anim` 을 쓴다.

## 열린 질문 (Open questions)
- 공중 위치 높이(`_entranceHeightAboveHead` 6 m), 낙하 중 모션(Spell 이 끝나면 Idle), 착지 카메라 흔들림(1), 착지 후 멈춤(0.5초)은 임시값. 육안으로 조정 필요.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
