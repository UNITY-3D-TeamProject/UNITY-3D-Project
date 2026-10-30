---
id: intent-092
title: 보스B 그로기 중 큰 보스 손을 때려야 한다는 안내 이펙트
part: AI / Boss / VFX
status: open   # open | resolved
created: 2026-10-30
resolved: null
---

## 문제 (Problem)
- B 가 첫 다운으로 그로기에 들어가면 A 는 손만 피격 가능(`HandsOnly`)해지지만, 플레이어가 이를 알 단서가 없다. (AI_TODO2 보스-21)

## 기대 결과 (Proposed outcome)
- 그로기 동안 A 의 피격 가능한 손 위에 반복 표시 이펙트가 나오고, 그로기가 끝나면 사라진다.

## 영향 범위 (Affected users and systems)
- `BossController._handHintVfx` 추가. `SetDamageMode` 가 `HandsOnly` 면 `Hand` 부위 `BossHitRelay` 마다 `PlayLoop`, 그 외 모드·`OnDisable` 에서 `Stop`.
- 신규: `Prefabs/Effect/AI/BossHandHint.prefab`·`M_BossHandHint.mat`, `SOEffectData_BossHandHint.asset`. `BossA.prefab` 에 연결.

## 제약 (Constraints)
- 표현은 손 위치 이펙트만(사용자 선택). 사운드·UI 문구는 하지 않음.
- 이펙트 모양은 임시(노란 펄스 링). 교체는 SO 의 프리팹만 바꾸면 됨.

## 열린 질문 (Open questions)
- 이펙트 크기·색·위치(손 중심)가 실제 보스전에서 눈에 띄는지 직접 확인 필요.

## 진행 기록
- 2026-10-30: 구현, 리컴파일 통과. Play 보스전 확인·게이트 루프 미실행.

## 해결 기록 (Resolution)
- 최종 결정:
- 관련 커밋/PR:
