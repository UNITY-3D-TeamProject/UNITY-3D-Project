---
id: intent-045
title: 보스 B 스킬별 애니메이션 연결
part: AI / Boss / Animation
status: open
created: 2026-10-19
resolved: null
---

## 문제 (Problem)
보스 B 는 기본 5종(Idle·Move·Hit·Death·Attack)만 `BossB_Override` 로 장착돼 있다. 스킬 4개 중 `Combo` 만 `Attack` 트리거에 매핑돼 `Left Punch Attack` 한 클립을 3번 재생하고, `Charge`·`Leap`·`Evade` 는 재생되는 애니메이션이 없다. 스킬 코드(`BossB*`)는 애니메이터를 직접 호출하지 않고, `EnemyAnimator.PlaySkill(스킬 이름)` 매핑으로만 재생된다.

## 기대 결과 (Proposed outcome)
스킬마다 고유 모션이 재생된다.

| 스킬 | 모션 | 출처 |
|---|---|---|
| Combo | `Left Punch Attack` → `Right Punch Attack` → `Slam Ground Attack` (타마다 다름) | Tanker Robot 기존 클립 |
| Charge | 예비(오른팔을 어깨에 올려 'ㄴ'자, 허리 살짝 굽힘) → 같은 자세로 달리는 돌진 | 사용자가 직접 제작 |
| Leap | `Jump Slam Ground Attack` | Tanker Robot 기존 클립 |
| Evade | 회피 모션 | 사용자가 직접 제작 (모션 미정) |

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스 B 전투(1-1 ~ 1-2)
- 어떤 시스템/모듈이 건드려지는가: 보스 B 전용 애니메이터 컨트롤러(신규), `BossB_Override`, `BossB.prefab` 의 `EnemyAnimator._skillAnimations`, `BossBMeleeCombo`(타별 모션이 필요하면)

## 제약 (Constraints)
- `EnemyBase.controller` 는 다른 적과 공유하므로 수정하지 않는다. 보스 B 전용 컨트롤러를 만든다.
- 원본 FBX 는 `Assets/ThirdParty/`(Git 제외)에 있으므로, 공유되는 컨트롤러가 이를 참조하는 방식은 기존 `BossB_Override` 와 같다.
- 컨트롤러·프리팹은 YAML 직접 수정 금지, Unity Editor MCP/CLI 로만 수정한다.
- 공용 스킬 프레임워크(`SkillBase`·`SkillController`·`SkillMediator`)는 플레이어와 적이 함께 쓰므로 이 기능(적 전용)을 위해 수정하지 않는다. 스킬이 `EnemyAnimator` 를 직접 부르는 것도 금지(중재자 규칙).

## 결정 사항 (2026-10-19)
- Leap: 예고부터 `Jump Slam Ground Attack` 재생. Charge·Evade: 슬롯(트리거·상태)만 만들고 클립은 사용자 제작 후 연결.
- Combo: 타마다 트리거. 공용 코드를 바꾸지 않기 위해 콤보를 스킬 3개(`Combo1`·`Combo2`·`Combo3`)로 분리하고 BT 에서 순서대로 호출한다. 마지막 타(`Combo3`)만 넉백.

## 결정 사항 (2026-10-19, Leap 구간별 모션)
- 문제: `Jump Slam Ground Attack` 클립(약 1.05초)이 스킬 길이(예고 1.2 + 비행 1.0 + 후딜 1.5초)보다 짧아 클립이 끝난 뒤 Idle 로 서 있다. 구간별로 모션을 나눈다.
- 구간: 예고 = 웅크리는 대기 모션(`Leap` 트리거, 기본안 `Cast Spell`), 비행 = `Jump Slam Ground Attack`(`LeapFlight` 트리거), 후딜 = Idle.
- 신호 경로(중재자 규칙 준수): `BossBLeap` 이 비행 진입 시 `AIController.ExecuteSkill("LeapFlight")` 을 요청 → `SkillMediator` → `OnSkillUsed` → `EnemyAnimationMediator` → `EnemyAnimator.PlaySkill`. `LeapFlight` 는 모션 알림 전용 스킬(`BossBMotionSkill`, 동작 없음). 컨트롤러가 애니메이터를 직접 부르지 않는다. 회피 도약은 제외.
- 실행자: C# 은 Claude 직접, 컨트롤러·프리팹 편집과 검증은 Codex(사용자 지정 2026-10-19).
- 예고가 중간에 끊겨도 `LeapTelegraph` 에 갇히지 않게 종료 시간 전환(약 1.2초)을 둔다.

## 열린 질문 (Open questions)
- Charge 끝에서 달리기 모션을 빠져나오는 신호(공용 코드 없이 컨트롤러 안에서 해결할 방법, BT 가 `StopSkill` 을 보내는지).
- BT 그래프(`SmallBoss`)의 Combo 가지를 3노드 시퀀스로 바꾸는 편집은 누가 하는지.

## 진행 (2026-10-19)
- 구현 완료: 콤보 3분리·전용 컨트롤러·Leap 연결. 남은 것은 BT Combo 가지 교체(사용자)·Play 확인·Charge/Evade 클립 연결. 상세는 [HANDOVER](../HANDOVER/BF_Leers/2026-10-19/boss-b-skill-animations.md).
