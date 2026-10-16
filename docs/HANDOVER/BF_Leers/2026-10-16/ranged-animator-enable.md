# Ranged 애니메이션 미재생 수정 (2026-10-16)

## 1. 작업 요약 및 방법
- 증상: EnemyAnimationPrefab·CharacterMediator 배선을 마쳤는데 Ranged 애니메이션이 전혀 재생되지 않음.
- 원인: `Ranged.prefab` 모델의 Animator 컴포넌트가 꺼져 있었다(`m_Enabled: 0`). 배선(`_enemyAnimationMediator`, `_enemyAnimator`, `_speedValueKey: MoveSpeed`, 컨트롤러 `Ranged.controller`)은 정상.
- 수정 (`Ranged.prefab`만, 코드 무변경):
  - Animator `m_Enabled` 0 → 1
  - Animator Apply Root Motion 1 → 0 (보행자·Special과 같은 이유로 끔)
  - `EnemyAnimator._skillAnimations`에 `AttackWindup` → `IsWindup`(IsHold) 추가. 사용자가 넣은 항목의 스킬 이름 끝 공백(`'AttackWindup '`)을 제거했다(공백이 있으면 `AIRangedAttackAction`의 `"AttackWindup"`과 매칭되지 않음).

## 2. 결정 사항
- 적 프리팹의 모델 Animator는 켜 두고 Root Motion은 끈다(이동은 CharacterController/AI가 담당).

## 3. 현재 상태 및 이슈
- 재임포트 후 프리팹 값 확인: Animator enabled=True, rootMotion=False, 매핑 2개(`Attack`, `AttackWindup`).
- **Play에서 실제 Idle/Move/Windup/Attack/Hit/Death 재생 육안 확인은 미완** (열린 씬에 적 인스턴스 없음, 풀 스폰).

## 4. 다음 할 일
- Play에서 Ranged 스폰 후 조준(IsWindup) → 사격(Attack) 전환과 피격·사망 애니메이션 확인.
