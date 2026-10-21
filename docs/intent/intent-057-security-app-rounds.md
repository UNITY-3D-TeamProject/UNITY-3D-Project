---
id: intent-057
title: 보안 앱(Map_4_Security) 3라운드 진행 로직 — 잡몹 방 클리어 문, 특수몹 발판, 보스 체력바·탈출 문
part: Stage / SecurityApp / UI
status: open   # open | resolved
created: 2026-10-21
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
보안 앱(전투 맵 `Map_4_Security`)은 `SecurityApp`이 비어 있어 라운드 진행 로직이 없다.
- 1라운드(잡몹 방 4개): 적을 다 잡아도 다음 방 문이 열리지 않는다. 방마다 적을 몇 마리 둘지 정하는 구조도 없다. 맵의 `MonsterSpawner`는 씬 시작(Start) 때 한 번 만들 뿐, 잡혔는지 알 수 없다.
- 2라운드(발판 구간): 특수몹을 잡아야 다음 발판으로 넘어가는 장치가 없다.
- 3라운드(보스): 단계 진행(B만 → 그로기 때 A → 다시 B → A, A 격파로 끝)은 `BossEncounter`·`BossFightModel`에 이미 있지만, 두 보스 체력바가 없고 다 잡은 뒤 나갈 문도 없다.

## 기대 결과 (Proposed outcome)
- **전투 구역(`CombatZone`)** 하나로 1라운드 방과 2라운드 발판을 모두 처리한다.
  - 구역마다 인스펙터에서 `몬스터 데이터(SOMonsterData) + 마리 수` 목록과 생성 위치를 정한다. 숫자·종류·위치를 나중에 바꿔도 클리어 판정은 "이 구역에서 실제로 만든 적이 모두 죽었는가"로만 하므로 로직이 깨지지 않는다.
  - 구역의 적이 모두 죽으면 연결한 문 오브젝트를 끄고(열림), 연결한 다음 발판 등을 켠다.
  - 클리어 전에 플레이어가 죽으면 그 구역만 처음 상태로 돌아간다(남은 적 제거, 다시 시작). 이미 깬 구역은 그대로 열려 있다.
- **SecurityApp**: 1·2라운드 구역 목록을 들고 사망 리셋·라운드 클리어 판정을 맡는다. 3라운드 보스 격파(`BossFightSpawner.OnBossDefeated`) 시 탈출 문 오브젝트를 켠다.
- **보스 체력바(`Canvas_InGame`)**: 보스전 동안 A(거대 보스)·B(필드 보스) 체력바를 함께 띄운다. 지금 때릴 수 있는 보스의 바는 위쪽·크게·진하게, 못 때리는 보스의 바는 아래쪽·작게·흐리게 둔다. 보스전이 아니면 숨긴다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안 앱 플레이어, 맵 담당(구역·문·적 수 배치), 보스 담당
- 어떤 시스템/모듈이 건드려지는가:
  - 신규: `Core/Stage/Security/`(`CombatZone`·`MobRoomRound`·`SpecialMobRound`·`BossRound`), `UI/BossHealth/BossHealthPresenter.cs`·`BossHealthView.cs`
  - 수정: `Core/Stage/SecurityApp.cs`(빈 클래스 → 라운드 로직), `AI/Boss/Fight/BossEncounter.cs`(체력바용 읽기 전용 프로퍼티 추가만), `Canvas_InGame` 프리팹(보스 체력바 UI 추가)
  - 검증: 개인 TestScene에 구역·보스전 테스트 공간

## 제약 (Constraints)
- 최초 코드 구현 단계에서는 맵 씬을 열거나 수정하지 않고 TestScene에서 검증했다. 2026-10-21 후속 작업 요청으로 `Map_4_Security.unity`와 필요한 NavMesh 에셋의 배치·연결을 Codex에 허용했다. 스크립트·Canvas_InGame 프리팹·다른 씬은 변경하지 않으며, 커밋·push는 별도 요청 전 수행하지 않는다.
- 보스 단계·데미지 규칙(`BossFightModel`, `BossController.SetDamageMode`)은 바꾸지 않는다. 체력바는 기존 상태를 읽기만 한다.
- 기존 `MonsterSpawner`·`RoundSpawnerBase`는 수정하지 않는다.

## 열린 질문 (Open questions)
- ~~구역의 적이 나타나는 시점~~ → 구역(트리거)에 들어갈 때 (사용자 결정, 2026-10-21)
- ~~문이 열리는 모양~~ → 맵에 이미 있는 방식(패널 `InteractButton` → 문짝 `StateMover.Activate`)을 그대로 쓴다. 클리어하면 구역이 연결된 문짝의 `StateMover.Activate`를 직접 부른다 (사용자 결정)
- ~~클리어 전 패널(E)로 문을 여는 우회~~ → 구역에 연결한 출구 패널은 클리어 전까지 콜라이더를 꺼서 조준·상호작용이 안 되게 하고, 클리어하면 다시 켠다. `InteractButton`·`StateMover` 수정 없음 (사용자 결정)
- ~~2라운드 "다음 발판으로 넘어가기"~~ → 다음 발판이 나타남. 연결한 오브젝트를 시작 때 꺼 두고 클리어하면 켠다 (사용자 결정)

## 진행 상황 (2026-10-21)
- Codex 맵 연결 결정(사용자 확인): 실제 동선인 통로 → 큰 방 → 작은 방 2개 순서로 Rooms를 연결한다. 통로는 전투 구역 하나에 여러 트리거 박스를 사용한다. Platform_1~5에 특수몹 1마리씩 두고 처치 시 다음 발판(2~6)을 켠다. 보스 아레나 뒤쪽에 로비행 탈출문을 두고, 기존 루트 MonsterSpawner 5개는 비활성화한다. 실제 3라운드 체크포인트 트리거는 Platform_6 위에 있다. 1라운드 NavMeshSurface 활성화, 발판 NavMesh 베이크 및 보스 스포너 연결을 적용했다.
- 코드 구현·컴파일 통과, `Canvas_InGame`에 `BossHealth` 추가. TestScene 런타임 검증 완료(구역 생성·클리어·리셋, 탈출 문, 체력바 단계 전환). 자세한 내용은 [작업 기록](../HANDOVER/SungJun/2026-10-21/security-app-rounds.md).
- ~~구역은 스스로 사망 리셋을 구독하므로 `SecurityApp`에 구역 목록을 두지 않았다~~ → 같은 날 구조 변경(사용자 지시: "CombatZone이 너무 많은 걸 담고 있다"):
  - `StageManager`(스테이지 전체, 변경 없음) → `SecurityApp`(라운드 번호에 맞는 라운드에 사망 리셋 전달, 라운드 클리어 판정·진행 알림 모음) → 라운드(`MobRoomRound` 1R 문·패널, `SpecialMobRound` 2R 다음 발판, `BossRound` 3R 탈출 문). 공통 부모 `SecurityRoundBase`는 만들었다가 사용자 지시로 제거: `SecurityApp`이 `CurrentRoundIndex`로 직접 분기(FileApp과 같은 방식). 라운드 3개는 컴포넌트가 아니라 `[Serializable]` 일반 클래스로, `SecurityApp`이 필드로 들고 인스펙터에서 설정(사용자 결정) → `CombatZone`(트리거 진입 시 적 생성·사망 집계·`ResetIfNotCleared`만).
  - 파일 위치: 라운드·구역은 `Core/Stage/Security/`(네임스페이스 `Core.Stage.Security`), `SecurityApp`은 다른 앱과 같은 `Core/Stage`, 보스 체력바는 `UI/BossHealth` (사용자 결정).
- Codex 맵 연결 완료: 방 4개·특수몹 발판 5개·보스 뒤쪽 로비행 탈출문, 기존 스포너 5개 비활성화, 발판 NavMesh 베이크. Play에서 생성·문/발판 해제·사망 복구·실제 보스 객체의 체력바 단계·격파 후 탈출문 표시를 확인했다. 컴파일 오류 및 Missing Script/Reference 0. [맵 연결·검증 기록](../HANDOVER/SungJun/Codex/2026-10-21/security-map-round-wiring.md).
- 남은 것: 실제 입력으로 걷기·사격·점프·보스전 통과, 로비 도착 확인. 기존 풀 사전 생성 NavMesh 경고와 보스 선택 컴포넌트 경고는 코드/프리팹 수정 범위 밖이므로 기록만 했다. 이 확인 전까지 open 유지.

- 2026-10-21 후속(사용자 피드백 3건, 김성준):
  1. 방이 열리자마자 적이 생기는 게 보여 어색함 → 지금 방 적을 앞 방을 깨는 순간(1번 방은 스테이지 시작) 닫힌 문 뒤에 미리 만들고, 플레이어가 구역에 들어오기 전까지 `AIController.Hold`로 제자리에 묶음(사용자 결정). 묶인 적도 사거리 안이면 제자리 공격은 함.
  2. 적이 뒷문으로 도망감 → 방에 들어오면 입구 문(`Entrance Doors`) `Deactivate`, 입구 패널(`Entrance Panels`) 잠금. 방을 깨도 계속 잠금, 죽어서 리셋될 때만 풀림(사용자 결정). 앞 방 출구 = 다음 방 입구인 공유 문은 깬 방만 처리해 잠금이 다시 풀리지 않게 함.
  3. 정보 패널: `N라운드` / `구역 2/4 · 남은 적 5명`, `발판 3/5 · 남은 적 1명`, 3라운드 `보스를 쓰러뜨리세요`·`탈출 문이 열렸습니다`, 라운드 완료 `모든 구역을 해제했습니다`(사용자 결정).
  - TestScene 런타임 확인 완료. 이어서 사용자 지시로 `Map_4_Security`에 방 4개 입구 문·패널을 직접 연결하고, 트리거를 입구에서 2m 안쪽으로 줄이고, 생성 위치 방향을 맞췄다. 잡몹(근접·원거리·특수) 체력은 10으로 낮췄다. 맵 Play에서 Room_1~3 진입 시 입구 닫힘 확인. [작업 기록](../HANDOVER/SungJun/2026-10-21/security-zone-prespawn-lockin.md)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
