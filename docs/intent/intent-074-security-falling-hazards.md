---
id: intent-074
title: 보안 맵 2라운드 발판 위로 떨어지는 위험물
part: Stage / SecurityApp / Map
status: open   # open | resolved
created: 2026-10-25
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
`Map_4_Security` 2라운드(`Stage_BossRun/04_Platforms`, 특수몹 발판)는 특수몹만 잡으면 되고 발판 위 위협이 없다.
사용자가 `0_Unity_Optimized`, `DigitalVirus_Unity_Optimized` 모델(`Assets/ThirdParty/Projectile/`)을 씬에 놓아 두었고, 이것들이 공중에서 떨어지게 하고 싶다.

## 기대 결과 (Proposed outcome)
- 2라운드 동안 일정 간격으로 두 모델 중 하나가 **켜져 있는 발판 위 무작위 위치**로 떨어진다.
- 떨어질 자리에 예고 표시가 점점 짙어지고, 떨어지는 위험물에 닿으면 피해를 입는다.
- 발판에 닿으면 사라진다.
- 보스 라운드(3라운드)에 들어가면 멈추고 남은 위험물도 지운다.
- 2라운드 중 죽으면 떨어지는 중인 위험물을 지우고 생성은 계속한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안 맵을 플레이하는 플레이어, 맵 담당자
- 어떤 시스템/모듈이 건드려지는가:
  - `Map.Gimmicks.FallingObstacle`: 닿으면 사라지는 옵션 `_isDestroyOnLand`, `FallDistance` 속성 추가 (기존 배치는 옵션 꺼짐이라 동작 그대로)
  - 신규 `Map.Gimmicks.FallingHazardSpawner`: 켜진 발판 중 무작위 위치에 위험물 생성
  - `Core.Stage.Security.SpecialMobRound`·`Core.Stage.SecurityApp`: 2라운드 시작·종료·리스폰 때 생성기 제어
  - 신규 프리팹 2개(`Prefabs/Map/Security/`), `Map_4_Security` 씬 연결

## 제약 (Constraints)
- 낙하·예고·피해는 기존 `FallingObstacle` + `EffectZone` 를 재사용한다.
- 씬·프리팹은 YAML 직접 수정 금지, Unity Editor MCP 로 작업한다.
- 모델 원본은 `Assets/ThirdParty/Projectile/` 에 있고 Git 제외 폴더다. SVN 원본(`D:\ThirdParty\ThirdParty`)에 이 폴더가 없어 팀원 PC에서는 모델이 빠진다.

## 열린 질문 (Open questions)
- `ThirdParty/Projectile` SVN 업로드 (사용자 조치 필요)
- 수치는 기본값으로 시작: 간격 2초, 1개씩, 낙하 높이 15 m, 예고 1초, 피해 10 (`Effect_Hazard_Damage10`). 플레이 손맛 확인 후 조정.
- 노리는 발판: 켜진 발판 전체에서 균등 무작위 (플레이어 발판 가중치 없음)

## 진행 상황 (2026-10-25)
- 후속 완료: 0·1 낙하 예고를 보스와 같은 DTT AOE `CircleWarnning` 원형 채움으로 교체. `FallingObstacle._warningCircle`에 두 프리팹의 중첩 원형 예고를 연결하고 기존 WarningMarker는 제거했다. 예고 1초·낙하·피해·풀링 설정과 다른 장애물의 Renderer 예고는 유지한다. 리컴파일 에러 0, 기존 PlayMode 테스트 5/5. Play에서 바닥 위치 표시·낙하 중 완전 채움과 위치 고정·착지/조기 반납 숨김·재사용 위치/채움 초기화·파괴 시 예고 정리 확인. 동시 생성한 동일 프리팹의 서로 다른 채움 값과 개별 머티리얼도 확인(검증용 대기 4초/8초, 저장된 프리팹은 1초 유지). 상세: `docs/HANDOVER/BF_Leers/2026-10-25/security-falling-dtt-warning.md`. 육안 손맛·SVN 업로드는 기존 항목대로 남는다.
- 구현: `FallingObstacle._isDestroyOnLand`·`FallDistance`, `FallingHazardSpawner`(부모 없이 생성 → `04_Platforms` 스케일 2 영향 없음), `SpecialMobRound.StartHazards/StopHazards`·리스폰 시 `ClearHazards`, `SecurityApp.OnRoundChanged` 는 2라운드면 시작·그 외 멈춤.
- 프리팹: `Prefabs/Map/Gimmicks/Security/Security_FallingZero`·`Security_FallingVirus` (모델 2배, 높이 2 m, 낙하 15 m, 예고 1초, `Effect_Hazard_Damage10`).
- 씬: `04_Platforms/FallingHazardSpawner`(간격 2초, 1개, Platform_1~6), `SecurityApp` 연결, 최상위 원본 모델 2개 삭제.
- 검증: 리컴파일 에러 0, PlayMode `FallingHazardSpawnerTests` 4/4. Play: 1라운드 미생성 → 2라운드 생성(켜진 Platform_1 위만), 착지 시 예고와 함께 사라짐, 맞으면 HP 90 → 80, 리셋 시 위험물만 지움, 3라운드 진입 시 멈춤·전부 지움.
- 후속(2026-10-25): 프리팹 렌더러에 `M_DigitalProjectile_*`(SG) 머티리얼 지정, 위험물을 `ObjectPoolManager`(`_mapHazards` 카테고리, `FallingZero`·`FallingVirus`)에서 꺼내 쓰도록 전환 — `FallingObstacle.DropOnto`/`Despawn` 추가, 스포너는 매니저 없으면 Instantiate 폴백. `FallingHazardSpawnerTests` 5/5, Play 에서 인스턴스 8개 재사용 확인. 상세는 handover `security-falling-hazards.md`.
- 남음: 육안·손맛 확인(수치), SVN 업로드.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
