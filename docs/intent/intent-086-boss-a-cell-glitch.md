---
id: intent-086
title: 보스A 몸을 3×3 칸으로 나눠 무작위 칸에 0·1 같은 띠 밀림 글리치·스캔라인 넣기
part: AI / Boss / Shader
status: open
created: 2026-10-29
resolved: null
---

## 문제 (Problem)
- AI_TODO2 [보스-18]. 보스A 몸체에는 바이러스 감염 느낌을 주는 움직이는 표현이 없다.
- 보안 맵 0·1 낙하물(`ThirdParty/Projectile/SG_DigitalProjectile`)은 가로 띠가 가끔 옆으로 튀는 버텍스 글리치와 스캔라인으로 "일렁"인다. 보스A에도 이 느낌을 몸 일부에만 넣고 싶다.

## 기대 결과 (Proposed outcome)
- 보스A 메시를 바인드 포즈 좌표 기준 3×3 칸(좌우 × 높이)으로 나눈다.
- 2~3초마다 2~3개 칸(개수도 무작위)을 새로 고른다. 항상 켜져 있다.
- 고른 칸의 정점은 가로 띠 단위로 가끔 옆으로 밀린다(0·1과 같은 방식: 1/8초마다 띠마다 일정 확률). 밝기 깜빡임은 넣지 않는다.
- 고른 칸 표면에는 보스B 손 스파크(`BossBHandSparks`·`LightningBolt`)처럼 짧은 지그재그 번개 2~3가닥이 가끔 번쩍인다. 셰이더로 표면에 그린다.
  - 처음에는 스캔라인이었으나 사용자 요청(2026-10-29)으로 바꿨다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스전 플레이어(시각 연출만, 판정 없음)
- 어떤 시스템/모듈이 건드려지는가:
  - `Art/Shaders/Boss/BossACorruptedArmor.hlsl` (버텍스 함수 추가, 표면 함수에 스캔라인 추가)
  - `Art/Shaders/Boss/SG_BossACorruptedArmor.shadergraph` (버텍스 Position 에 Custom Function 연결)
  - 신규 `AI.Boss.BossA.BossAGlitch`(칸 고르기·MaterialPropertyBlock 전달), `AI.Boss.BossA.BossAGlitchCells`(칸 마스크 뽑기)
  - `Prefabs/AI/Boss/BossA.prefab` (`Gripper Robot` 에 `BossAGlitch` 추가)

## 제약 (Constraints)
- 지켜야 할 정책: 0·1 셰이더는 `ThirdParty`(Git 제외)라 참조하지 않고 로직만 옮긴다. 프리팹은 YAML 직접 수정 금지(에디터 API 사용).
- 쓰지 말아야 할 것: 포스트 프로세싱, INab VFX(`BossARectangleNoise`).
- 셰이더 값은 `BossAMaterialSpace` 처럼 MaterialPropertyBlock 전역 값으로 받아 머티리얼 프로퍼티를 늘리지 않는다. 값이 없으면(마스크 0) 기존과 똑같이 그려진다.

## 열린 질문 (Open questions)
- 없음. 결정(2026-10-29, 사용자): 버텍스 방식(B), 주기 2~3초, 띠 밀림 + 스캔라인만, 항상 켬, 칸 수 2~3 무작위.
- 알려진 한계: 정점 3772개로 성겨서 띠 경계·칸 경계의 삼각형은 잘리지 않고 늘어난다. 칸은 `RigHead` 기준이라 손은 움직이면서 칸을 지나간다. 아래 가운데 칸(양손 사이)은 정점이 없으므로, 정점이 있는 칸만 후보로 뽑는다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
