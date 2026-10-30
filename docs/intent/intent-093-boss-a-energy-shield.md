---
id: intent-093
title: 보스A 피해 불가 상태를 에너지 실드로 표시
part: AI / Boss / VFX
status: open   # open | resolved
created: 2026-10-30
resolved: null
---

## 문제 (Problem)
- 최종 단계 전에는 A 의 몸·머리가 피해를 받지 못하지만(`Blocked`/`HandsOnly`) 플레이어가 알 단서가 없다. (AI_TODO2 보스-15)

## 기대 결과 (Proposed outcome)
- 피해 가능 모드(`All`, 최종 단계 단독 패턴)가 아니면 A 몸을 에너지 실드가 감싸고, `All` 진입 시 사라진다.

## 영향 범위 (Affected users and systems)
- `BossController._energyShield` 추가. `SetDamageMode` 가 `mode != All` 이면 켜고 `All` 이면 끈다.
- `BossA.prefab` 에 자식 `EnergyShield`(구체, 지름 34m, 중심 y=9.8, `M_Gimmick_Shield` 재사용, 콜라이더 없음) 추가·연결.

## 제약 (Constraints)
- 그로기(`HandsOnly`)에도 실드를 켠다: 몸·머리는 여전히 막혀 있고 손만 맞으므로, 손 안내 이펙트(intent-092)가 "여기만 맞는다"를 알린다. 손이 실드 밖에 있는지는 확인 필요.
- 실드 머티리얼·크기는 임시.

## 열린 질문 (Open questions)
- 그로기 중 실드를 켜 두는 방식이 맞는지(대안: 그로기엔 끄기), 구체가 몸 전체·손 위치와 어울리는지 보스전 육안 확인 필요.

## 진행 기록
- 2026-10-30: 구현, 리컴파일 통과, 프리팹 연결 확인. Play 보스전 확인·게이트 루프 미실행.

## 해결 기록 (Resolution)
- 최종 결정:
- 관련 커밋/PR:
