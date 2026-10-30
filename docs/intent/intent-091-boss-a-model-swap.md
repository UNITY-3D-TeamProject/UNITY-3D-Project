---
id: intent-091
title: 보스A 모델을 새 FBX(Virus_King_Boss)로 교체
part: AI / Boss / BossA / Art
status: open   # open | resolved
created: 2026-10-30
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
AI_TODO2 보스-24·25. 보스A 는 `ThirdParty/Gripper Robot` 의 스킨드 모델(본 15개, Idle·Take Damage·Die 클립)을 쓴다.
새로 받은 `ThirdParty/CuteRobots/New Boss/Virus_King_Boss.fbx` 는 같은 배치의 리모델링이지만 본·애니메이션이 없는 메시 5조각(몸통 1, 팔 2, 손 2)이다.
그래서 그냥 바꿔 끼우면 다음이 깨진다.
- `BossHand._bone`(손바닥 본)·`_fingerCurls`(손가락 본)·`_restPoint`(팔 본 밑 `HandAnchor`)·`_grabPoint`
- `BossAMaterialSpace`·`BossAGlitch`: `SkinnedMeshRenderer` 와 바인드 포즈 전제
- `M_BossACorruptedArmor`: Gripper 팔레트 텍스처의 UV 띠로 색을 다시 칠하는 셰이더
- 약점(`RigHead` 밑 `HeadWeakPoint`), `EnemyAnimator`(Gripper 아바타·클립)
- 새 FBX 는 각 조각의 피벗이 모델 중앙(x=0)이라 손을 그대로 `_bone` 으로 쓰면 손이 엉뚱한 곳에서 돈다.

## 기대 결과 (Proposed outcome)
- `BossA.prefab` 의 모델이 새 FBX 로 바뀌고, 기존 패턴(내려찍기·주먹·그랩·박수·그로기)이 같은 방식으로 동작한다.
- 사용자 결정(2026-10-30):
  1. 손이 날아갈 때 팔은 그대로 둔다(지금처럼 손만 움직임).
  2. 오염 장갑 셰이더와 칸 글리치를 새 텍스처 3장(몸통·팔·손)에 맞춰 이어간다.
  3. 원본과 같은 형식(오버라이드 컨트롤러의 Death 슬롯 클립)으로 사망 동작을 만든다.
  4. 약점은 가슴의 빛나는 육각 코어로 옮긴다.
- 구현 방침:
  - 크기는 옛 모델과 맞춘다(배율 45, 높이·너비가 거의 같음). 프리팹 안에서는 모델을 언팩해 몸통 피벗(옛 `RigHead` 역할) 밑에 팔·손 피벗을 두는 본 같은 계층을 만든다. 손 피벗은 손 메시 위치에 둔다.
  - 손가락이 따로 움직이지 않으므로 `_fingerCurls` 는 비운다(주먹·그립은 손 전체 회전만).
  - `BossAMaterialSpace` 는 `MeshRenderer` 도 받게 한다(앵커 로컬을 셰이더 규약 Z 위·-Y 앞으로 돌림). `BossAGlitch` 는 여러 렌더러가 같은 칸을 공유하게 한다.
  - 셰이더는 같은 hlsl 에 새 텍스처용 함수를 추가하고, 그래프는 복사해 함수 이름만 바꾼다(옛 그래프·머티리얼은 그대로 둔다). 색은 텍스처를 그대로 쓰고, 바이저·가슴 코어 빨간 발광과 칸 글리치를 잇는다.
  - `BossHand` 는 Animator 가 매 프레임 손 본 회전을 써 준다는 전제로 회전을 더하므로, 사망 클립 외에 손·몸 자세를 잡아 주는 Idle 클립도 만든다(원본 Idle 처럼 살짝 위아래로 움직임).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안 맵(`Map_4_Security`) 보스전. `BossFightSpawner._bossAPrefab` 이 런타임에 생성하므로 씬 오버라이드는 없다.
- 어떤 시스템/모듈이 건드려지는가:
  - `Assets/_Project/Prefabs/AI/Boss/BossA.prefab`
  - `Scripts/AI/Boss/BossA/BossAMaterialSpace.cs`, `BossAGlitch.cs`
  - `Art/Shaders/Boss/BossACorruptedArmor.hlsl`, 새 `SG_BossAVirusKingArmor.shadergraph`, 새 머티리얼 3개
  - `Animations/Enemy/BossA_Override.overrideController`, 새 클립(Idle·Die)
  - 확인 대상: 보스-16·17 이펙트 위치, 보스-18 칸 글리치

## 제약 (Constraints)
- 예산/기한: 없음
- 지켜야 할 정책: 프리팹·씬·에셋은 Unity Editor(MCP) 로만 수정한다. 캐릭터 중재자 구조는 건드리지 않는다.
- 쓰지 말아야 할 것: ThirdParty 원본 FBX·머티리얼 수정(사용자가 만든 `Metrial/BigBoss*.mat` 은 그대로 둔다).
- 새 FBX·텍스처는 `Assets/ThirdParty` 라 Git 에 올라가지 않는다. 다른 PC 에서 쓰려면 SVN 업로드가 필요하다.

## 열린 질문 (Open questions)
- 없음(위 사용자 결정 4개로 진행). 바이저·코어 발광 세기, 글리치 강도 등 시각 수치는 프리뷰를 보고 조정한다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
