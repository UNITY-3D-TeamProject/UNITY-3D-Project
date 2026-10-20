# 보스 B 스킬별 애니메이션 (intent-045)

## 작업 요약 및 방법
- 콤보를 스킬 3개(`Combo1`·`Combo2`·`Combo3`)로 분리. `BossBMeleeCombo` 를 1타 스킬로 단순화(`_hitCount`·`_hitInterval` 제거, `_hitDelay`(구 `_firstHitDelay`, `FormerlySerializedAs`)·`_recoverySeconds`·`_isLastHit` 추가). 마지막 타만 넉백.
- 보스 B 전용 컨트롤러 `Animations/Enemy/BossB.controller` 신규(Idle·Move·Hit·Death·Combo1~3·Leap, 스킬은 AnyState 트리거, 끝나면 IsMoving 으로 Idle/Move 복귀). 트리거 `Charge`·`Evade` 는 파라미터만 만들고 상태는 없음.
- `BossB_Override` 의 베이스를 `BossB.controller` 로 교체, 클립 연결: Combo1 `Left Punch Attack`, Combo2 `Right Punch Attack`, Combo3 `Slam Ground Attack`, Leap `Jump Slam Ground Attack`. 원본 슬롯용 빈 클립 `Clips/BossB_Combo1~3.anim`, `BossB_Leap.anim` 추가.
- `BossB.prefab`: Combo 자식 3개(Combo1: 0.4/0.43, Combo2: 0.4/0.43, Combo3: 0.5/0.67 + 넉백), `EnemyAnimator._skillAnimations` 에 Combo1~3·Leap 매핑.
- 실행자: Claude 직접(Codex CLI 0.163 은 있으나 소규모 편집이라 위임 비용이 더 커서). 공용 스킬 프레임워크는 수정하지 않음(적 전용 기능이라서).

## 결정 사항
- Leap 은 예고부터 클립 재생. Charge·Evade 는 사용자가 클립 제작, 슬롯만 준비.
- 콤보 타별 모션은 스킬 분리로 해결(공용 `SkillBase`·`SkillMediator` 불변, 스킬→EnemyAnimator 직접 호출 금지).

## 현재 상태 및 이슈
- **BT 가 깨진 상태**: `SmallBoss` 그래프의 Combo 가지가 옛 스킬 이름 `Combo` 를 부른다(사용자 편집 예정): `Combo1` → `Combo2` → `Combo3` 순서의 `스킬 사용 후 대기` 노드 3개로 교체해야 한다.
- Leap 클립은 1.17초인데 스킬은 예고 1.2 + 비행 1.0 + 후딜 1.5초라 클립이 끝난 뒤 Idle 이 이어진다(예고부터 재생을 택한 결과).
- Combo 타 간격은 이전 0.6초 → 지금 약 0.9초가 아니라 `recovery + 다음 hitDelay`(0.83초) + 노드 전환 틈. Play 로 모션·판정 일치 확인 필요.
- Charge 끝에서 달리기 모션을 빠져나오는 신호는 미해결(클립 제작 후).
- `spec/boss.md` 의 "2~3타" 서술이 이제 틀림(미수정).
- 게이트 루프: 컴파일 통과·콘솔 에러 없음. 이전 3회 루프는 임시 `InitTestScene*` 때문에 `blocked`, USER-1·USER-4 대기. 이 변경 이후 루프는 미실행, **커밋 전**.

## 다음 할 일
1. 사용자: `SmallBoss` 그래프 Combo 가지를 Combo1·2·3 시퀀스로 교체.
2. Play 로 Combo 3타 모션·판정 시점, Leap 모션 확인(T5).
3. 사용자 제작 Charge(예비 'ㄴ'자 → 달리기)·Evade 클립 연결 + Charge 종료 신호 설계.
4. 게이트 루프, 그 후 커밋.
