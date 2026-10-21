# 보안 앱 — Codex 맵 연결 확인, 적 미리 생성·뒷문 닫힘·정보 패널

intent: [intent-057](../../../intent/intent-057-security-app-rounds.md)

## 작업 요약 및 방법
- **Codex 맵 연결 확인:** [Codex 기록](../Codex/2026-10-21/security-map-round-wiring.md)
  - `Map_4_Security.unity`를 에디터로 열지 않고 YAML로 교차 확인했다.
  - `SecurityApp`의 방 4개·발판 5개·보스 스포너·탈출 문 참조가 모두 실제 오브젝트를 가리킨다. 문 = StateMover, 패널 = InteractButton, 구역 = CombatZone이다.
  - 구역마다 적 데이터(근접·원거리·특수)·마리 수·생성 위치가 들어 있다. Room_1은 트리거 박스 2개가 모두 Is Trigger다.
  - 스크립트 변경은 없다.
- **사용자 피드백 3건 반영 (코드)**
  1. `CombatZone`: 대기(`Prepare`) → 시작(트리거 진입) → 클리어 단계로 나눴다.
     - `Prepare`는 적을 만들고 바로 `AIController.Hold`로 묶는다. 대기 중에는 매 프레임 0.5초씩 다시 묶는다.
     - 시작하면 `ReleaseHold`하고 `OnStarted`를 알린다.
     - 이벤트 `OnStarted`/`OnCleared`는 구역을 넘긴다(`Action<CombatZone>`). `IsStarted`를 추가했다.
  2. `MobRoomRound`: 방에 입구 문·입구 패널 칸(`EntranceDoors`, `EntrancePanels`)을 추가했다.
     - 지금 방(아직 못 깬 첫 방)만 `Prepare`한다. 스테이지 시작 때 1번 방, 앞 방을 깨면 다음 방.
     - 들어오면 입구를 닫고 입구 패널을 잠근다. 깨면 출구를 열고 출구 패널을 푼다(그 방만 처리).
     - 리셋하면 싸우던 방의 입구 패널만 풀고 지금 방을 다시 `Prepare`한다.
     - `RoomCount`·`CurrentRoomNumber`·`RemainingEnemyCount`·`OnProgressChanged`를 추가했다.
  3. `SpecialMobRound`: 발판도 순서대로 `Prepare`한다(다음 발판을 켠 뒤 그 위 특수몹 생성). 진행 수치·이벤트는 방과 같다.
  4. `BossRound`: `OnProgressChanged`(격파 시)를 추가했다. `SecurityApp`은 세 라운드 진행 알림을 모아 `NotifyProgressChanged`하고, 라운드 프로퍼티를 공개한다.
  5. `InfoPanelPresenter`: 보안 앱 문구를 추가했다(`구역 2/4 · 남은 적 5명`, `발판 3/5 · 남은 적 1명`, `보스를 쓰러뜨리세요`, `탈출 문이 열렸습니다`, `모든 구역을 해제했습니다`).

## 결정 사항
- 적 생성: 미리 만들고 들어올 때까지 제자리 (사용자 결정).
- 뒷문 패널: 방을 깨도 계속 잠금, 죽었을 때만 해제 (사용자 결정).
- 패널: 번호 + 남은 적 (사용자 결정).
- 앞 방 출구와 다음 방 입구가 같은 문이면 같은 StateMover·패널을 두 칸에 넣는다. 클리어 처리는 깬 방만 하므로 다음 방이 잠근 입구가 다시 풀리지 않는다.

## 현재 상태 및 이슈
- 컴파일 에러 0.
- TestScene Play(런타임 임시 오브젝트, 씬 저장 안 함), 방 2개(1번 출구 = 2번 입구 공유)·발판 2개·보스로 확인:
  - 시작: 1번 방 2마리 대기(묶임), 2번 방 비어 있음, 출구 패널 잠김, 패널 문구 `구역 1/2 · 남은 적 2명`.
  - 진입: 입구 닫힘·잠김, 묶음 해제.
  - 1마리 처치 후 사망: 1번 방 다시 2마리 대기, 입구 패널만 풀림(문은 닫힘).
  - 클리어: 출구 열림·출구 패널 풀림, 2번 방 3마리 대기, `구역 2/2`.
  - 2번 방: 진입 시 공유 문 닫힘·잠김, 깬 뒤에도 공유 패널 잠김 유지, `모든 구역을 해제했습니다`.
  - 발판: 들어가기 전 처치도 클리어, 다음 발판 켜짐과 그 위 특수몹 대기, 문구 `발판 1/2`→`2/2`→완료.
  - 보스: `보스를 쓰러뜨리세요` → 격파 신호 → 탈출 문, `탈출 문이 열렸습니다`.
  - NavMesh 위 대기 확인: 생성 프레임부터 묶임. 플레이어를 3m에서 보고 있어도 제자리이고, 풀리면 바로 움직인다.
- 라운드 번호는 TestScene StageManager 값을 리플렉션으로 바꿔 확인했다. 패널 문구는 Presenter의 문구 함수를 직접 불러 확인했다. TestScene에는 FileApp이 있어 화면 패널은 FileApp 문구를 보여 준다.
- 묶인 적도 사거리 안이면 제자리에서 공격한다(테스트 중 플레이어 HP 감소). 문을 열었을 때 원거리 적이 쏠 수 있다.
- ~~**맵 미반영:**~~ (아래 "추가"에서 해결) 새 칸(입구 문·패널)이 비어 있어 `Map_4_Security`에서는 아직 뒷문이 닫히지 않는다. 생성 위치 방향(+Z)도 맞춰야 한다 → [Codex 요청 2](./codex-request-security-map-2.md).
- Codex의 맵 변경(`Map_4_Security.unity`, `NavMesh-04_Platforms`, Codex 기록)과 이번 코드 변경 모두 커밋 안 함.

## 추가: 맵 입구 문 직접 연결 + 잡몹 체력 10 (사용자 요청 "너가 고쳐줘")
- 증상: 실제 플레이에서 2번 방(큰 방) 입구가 안 닫혀 적이 도망감. 원인은 `Map_4_Security`의 `Entrance Doors`·`Entrance Panels`가 비어 있던 것(Codex 요청 2 미적용).
- `Map_4_Security.unity` 수정(사용자 지시로 직접, 요청 2 내용 대신 처리):
  | 방 | 입구 문 StateMover | 입구 패널 | 트리거 입구 쪽 경계 |
  |---|---|---|---|
  | Room_1 | x=70 `SpaceStation_Door_B` | 2개 | x 70.4 → 72.0 |
  | Room_2 | x=100 `Door_Left`·`Door_Right`(= Room_1 출구) | 2개(= Room_1 출구 패널) | x 100.4 → 102.0 |
  | Room_3 | x=150 `Door_Left`·`Door_Right` | 2개 | x 150.4 → 152.0 |
  | Room_4 | x=200 `SpaceStation_Door_B`(유리) | 2개 | x 200.4 → 202.0 |
  - 트리거는 문에서 2m 안쪽부터 시작하게 줄였다(닫히는 문에 끼이지 않게). 각 방 생성 위치는 입구 문을 보게, 발판 생성 위치는 Y 270(플레이어가 오는 -X 쪽)으로 돌렸다.
- 잡몹 체력: `SOMonsterData_Melee`(100→10)·`Ranged`(120→10)·`Special`(100→10)의 MaxHp·CurrentHp. 보스·도둑 데이터는 그대로. 이 SO를 쓰는 다른 씬 MonsterSpawner에도 적용된다.
- Map_4_Security Play 확인(순간이동·HP 0으로 대신): 시작 시 Room_1 4마리 대기(10/10). Room_1 입구 열고 진입 → 입구 닫힘·패널 잠금 → 처치 → 출구 열림, Room_2 6마리 대기. Room_2 진입 → 공유 문 닫혀 원래 위치(z 80.0/80.1)로 복귀·패널 잠금, 플레이어는 문에서 떨어진 방 안. Room_2 처치 → 출구 열림, 입구 패널 잠긴 채 유지. Room_3 진입 → 입구 닫힘. Room_4는 같은 방식으로 연결(Play 미확인). 실제 걷기·사격 미확인.

## 다음 할 일 (Next Steps)
- ~~Codex 요청 2~~ → 위 "추가"에서 직접 처리함.
- 실제 맵에서 걷기·사격으로 방 진입 → 뒷문 닫힘 → 클리어 → 다음 방, 패널 문구를 눈으로 확인.
- 커밋: 이번 코드(`Core/Stage/Security`, `SecurityApp`, `InfoPanelPresenter`)와 Codex 맵 변경은 성격이 달라 나눠 커밋하는 것을 권장.
