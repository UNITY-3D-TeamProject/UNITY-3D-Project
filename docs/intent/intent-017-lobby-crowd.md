---
id: intent-017
title: 로비 군중(보행자·차량) 순환 이동과 정지
part: AI / Crowd
status: open   # open | resolved
created: 2026-10-10
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
로비(`Map_1_Lobby`)에 인도와 차도는 있지만 그 위를 다니는 보행자·차량이 없다. 씬의 `Road`, `Car` 오브젝트는 모델 묶음일 뿐 동작이 없고, 군중용 조종부·경로·생성 수단도 없다.

## 기대 결과 (Proposed outcome)
- 보행자는 인도, 차량은 차도에서만 각자의 닫힌 경로(Splines)를 한 방향으로 계속 돈다.
- 플레이어나 같은 종류의 개체가 앞에 있으면 멈추고, 그 뒤 개체들도 연쇄로 멈춰 대열이 생긴다. 앞이 비면 바로 다시 출발한다.
- 부딪히면 막히기만 하고 피해·넉백은 없다. 플레이어가 비켜 가야 한다.
- 레인(=구역)마다 속도와 개수를 Inspector에서 정하고, 씬 시작 시 레인이 개체를 생성한다.

상세 설계와 결정 근거: [grilling 기록](../planning/crowd-system.md)

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 로비를 돌아다니는 플레이어, 로비 레벨 디자이너(레인 배치)
- 어떤 시스템/모듈이 건드려지는가:
  - 신규: `Scripts/AI/Crowd/CrowdController.cs`(조종부), `Scripts/AI/Crowd/CrowdLane.cs`(레인 + 생성)
  - 재사용(수정 없음 목표): `CharacterMediator`, `MoveMediator`, `RotateMediator`, `CharacterMotor`, `CharacterRotator`, `AttributeSet`
  - 패키지: `com.unity.splines` 추가 (`Packages/manifest.json`, `packages-lock.json`)
  - 프로젝트 설정: 레이어 `Pedestrian`, `Vehicle`, `VehicleBody` 추가와 충돌 행렬
  - 에셋: `SOAttributeData_NPC1`(보행자), `SOAttributeData_NPC2`(차량), 보행자·차량 프리팹, 로비 레인 배치

## 제약 (Constraints)
- 예산/기한: 성능 관문 — PC, 150개, 군중 스크립트 합계 프레임당 2ms 이하. 넘으면 감지 주기 분산 → 그래도 넘으면 거리 기반 비활성화 순으로 대응.
- 지켜야 할 정책:
  - `docs/character-architecture.md` 중재자 구조. 조종부는 중재자에 요청만 보낸다.
  - 속도는 `MoveSpeed` 어트리뷰트. 레인이 생성 직후 `CharacterMediator.SetAttribute`로 넣는 값이 유일한 속도다 (SO 값은 모터에 전달되지 않음 — `MediatorBase.InitValue()` 주석 처리). `SOAttributeEffect.Apply` 대신 `SetAttribute`를 쓰는 것은 사용자 결정에 따른 예외.
  - 수명은 레인(=씬)이 소유한다. 풀·DontDestroyOnLoad를 쓰지 않는다.
- 쓰지 말아야 할 것:
  - `SplineAnimate` (Transform을 직접 옮겨 중재자·모터·충돌을 건너뜀)
  - 기존 `Sensor` 재사용·수정 (자기 자신 감지, 부채꼴 시야, 게이지/유예가 군중과 맞지 않음)
  - NavMesh
- 범위 밖: 횡단보도/교차, 다른 행동 NPC, 애니메이션, 피해·넉백, 상호작용, EditMode 자동 테스트

## 열린 질문 (Open questions)
- 없음 (Phase 1 grilling에서 전부 확정). 아래는 검증 결과에 따라 바뀔 수 있는 조건부 결정이다.
  - 재출발 시 움찔거림이 보이면 짧은 지연 후 재출발로 바꾼다.
  - 경로 이탈이 보이면 가장 가까운 점 찾기(`GetNearestPoint`)를 몇 초에 한 번 보정용으로 추가한다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
