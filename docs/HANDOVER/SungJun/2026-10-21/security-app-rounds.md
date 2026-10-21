# 보안 앱(전투 맵) 3라운드 진행 로직 — 잡몹 방 클리어 문, 특수몹 발판, 보스 체력바·탈출 문

intent: [intent-057](../../../intent/intent-057-security-app-rounds.md)

## 작업 요약 및 방법
- 사용자 요청: `Map_4_Security` 전투 맵에 라운드 로직.
  - 1라운드(잡몹 방 4개): 방의 적을 다 잡으면 다음 문이 자동으로 열림. 방마다 적 수를 나중에 바꿔도 로직이 깨지지 않게.
  - 2라운드(발판 구간): 특수몹을 잡아야 다음 발판으로 넘어감.
  - 3라운드(보스): 두 보스 체력바(때릴 수 있는 쪽은 위·크게, 아닌 쪽은 아래·작게), 다 잡으면 탈출 문 등장.
- 처음엔 `CombatZone` 하나에 적 생성·문·패널·발판·사망 리셋을 다 넣었다가, 사용자 지시("너무 많은 걸 담고 있다")로 책임을 나눴다. 라운드 공통 부모(`SecurityRoundBase`)도 한 번 만들었다가 사용자 지시("SecurityApp이 이미 지금 라운드를 아니 라운드마다 로직만 구현하면 된다")로 없앴다. 이어서 사용자 지시("FileApp처럼 일반 클래스로, SecurityApp 필드에서 값 설정")로 라운드 3개를 `[Serializable]` 일반 클래스로 바꿔 SecurityApp이 필드로 들게 했다.

### 구조
```
StageManager        스테이지 전체 진행·체크포인트·사망 복구 (변경 없음)
└ SecurityApp       CurrentRoundIndex 로 직접 분기 (FileApp 과 같은 방식, 라운드 번호 상수 3개)
                    IsRoundCleared: 라운드별 컴포넌트의 IsCleared, 사망 시 그 라운드의 못 깬 구역 리셋
                    StartStage 에서 세 라운드 Begin, OnDestroy 에서 End
   ├ _mobRoomRound    (MobRoomRound)    1R: 방 목록. 방을 깨면 출구 문짝 StateMover.Activate, 출구 패널 잠금 해제. ResetUnclearedRooms
   ├ _specialMobRound (SpecialMobRound) 2R: 발판 목록. 발판을 깨면 꺼 두었던 다음 발판 켜기. ResetUnclearedSteps
   └ _bossRound       (BossRound)       3R: BossFightSpawner.OnBossDefeated → 탈출 문 켜기
     (셋 다 [Serializable] 일반 클래스, FileApp 의 NumberPuzzleRound·ThiefChaseRound 와 같은 형식. 공통 부모 없음)
CombatZone          트리거 진입 시 적 생성, 만든 적이 다 죽으면 OnCleared. ResetIfNotCleared 로 리셋. 문·발판·StageManager 모름
```
- 파일: `Core/Stage/Security/`(`CombatZone`, `MobRoomRound`, `SpecialMobRound`, `BossRound`, 네임스페이스 `Core.Stage.Security`), `Core/Stage/SecurityApp.cs`(다른 앱과 같은 위치), `UI/BossHealth/`(체력바).
- `CombatZone`: `적 종류(SOMonsterData) + 마리 수` 목록, `Spawn Points`를 돌아가며 사용(비우면 구역 위치), 풀 매니저가 있으면 풀에서 꺼냄(`MonsterSpawner`와 같은 방식). 적 0마리면 들어오자마자 클리어.
- 패널 잠금: 클리어 전에는 출구 패널 콜라이더를 꺼서 조준·E가 안 됨. `InteractButton`·`StateMover`는 수정하지 않음.
- `BossEncounter`(보스 담당 파일, 읽기 전용 프로퍼티 3줄만 추가): `IsGroggy`, `BossAMediator`, `BossBMediator`.
- 보스 체력바 `UI/BossHealth/BossHealthView`·`BossHealthPresenter` + `Canvas_InGame/BossHealth`(기존 오브젝트 변경 없이 추가만).
  - Presenter가 매 프레임 `BossEncounter`를 읽는다(보스가 재스폰마다 새로 만들어져 이벤트 대신 폴링). 보스전이 아니거나 격파(Defeated)면 숨김.
  - 차례: 1-1 → B, 그로기 → A, 1-2 → B, 최종 → A (기존 `BossFightModel` 단계 그대로).
  - 차례인 바는 위(y -30)·크기 1·불투명, 아닌 바는 아래(y -100)·0.65배·알파 0.5, 자리 바꿈은 부드럽게. 채움은 앵커(Anchor Max X)로 줄인다(HP 칸과 같은 Cut Frame 스프라이트).
  - 이름: `거대 보스`(A), `필드 보스`(B), 글꼴 `_Project/Art/Fonts/Pretendard-Bold SDF`.

## 결정 사항
- 적 등장 시점: 구역에 들어갈 때. 문 열기: 맵의 패널 → `StateMover.Activate` 방식 그대로, 클리어 전 출구 패널 잠금. 2라운드: 다음 발판이 나타남. (모두 사용자 결정)
- 책임 분리: StageManager(전체) → SecurityApp(라운드 클래스를 필드로 들고 지금 라운드로 분기·클리어 판정, 인스펙터에서 라운드 설정) → 라운드 일반 클래스(라운드마다 로직만, 공통 부모 없음) → CombatZone(적 묶음, 씬 컴포넌트) (사용자 결정).
- 폴더: 보스 체력바는 `UI`, 나머지는 `Core/Stage/Security` (사용자 결정). `SecurityApp`은 FileApp 등과 같이 `Core/Stage`에 둠.

## 현재 상태 및 이슈
- 컴파일 에러 0.
- TestScene Play(런타임 임시 오브젝트, 씬 저장 안 함, 실제 StageManager 사용, 일반 클래스로 바꾼 뒤 SecurityApp 인스펙터 필드만으로 설정해 다시 확인):
  - 방 진입 → 2마리 생성 → 1마리 처치 후 플레이어 사망 → StageManager → SecurityApp(라운드 0 분기) → _mobRoomRound.ResetUnclearedRooms → CombatZone 경로로 리셋(남은 적 제거, 대기 복귀) → 재진입·전부 처치 → 문 StateMover 열림·패널 콜라이더 켜짐·`IsRoundCleared(0)` true.
  - 발판 구역 처치 → 다음 발판 켜짐·`IsRoundCleared(1)` true. `OnBossDefeated` → 탈출 문 켜짐·`IsRoundCleared(2)` true.
  - (구조 변경 전) 체력바: 가짜 보스와 리플렉션 `BossEncounter`로 1-1 → 그로기 → 1-2 → 최종 → 격파(숨김) 화면 캡처 확인. 체력바 코드는 구조 변경에서 바뀌지 않음.
- 실제 보스전(진짜 A·B)과 실제 사격·걷기는 미확인.
- **`Map_4_Security`는 열거나 수정하지 않음** → 연결 안 됨. 맵 루트의 `MonsterSpawner` 5개는 씬 시작 때 바로 적을 만들므로 구역 방식으로 바꾸면 정리가 필요할 수 있다(맵 담당 판단).
- 커밋 안 함. `ProjectSettings/EditorBuildSettings.asset`·`ShaderGraphSettings.asset`은 에디터가 줄바꿈만 바꾼 것(내용 변경 없음, 커밋 대상 아님).

## 맵 연결 방법 (Map_4_Security)
1. 방·발판마다 빈 오브젝트 + 구역 전체를 덮는 `BoxCollider` + `CombatZone`. `Enemies`에 몬스터 데이터와 마리 수, `Spawn Points`에 생성 위치. 적 프리팹은 씬 `ObjectPoolManager`에 등록돼 있어야 함.
2. `SecurityApp` 인스펙터에서:
   - `1라운드 · 잡몹 방` → `Rooms`마다 `Zone`(방 구역), `Doors`(출구 문짝 `StateMover`, 패널 `InteractButton`의 On Interacted에 걸려 있던 대상), `Locked Panels`(그 출구 문 패널).
   - `2라운드 · 특수몹 발판` → `Steps`마다 `Zone`(발판 구역), `Next Platform`(다음 발판, 스테이지 시작 때 자동으로 꺼짐).
   - `3라운드 · 보스` → `Boss Fight Spawner`(`05_Boss/BossFight`), `Exit Door`(탈출 문).

## 다음 할 일 (Next Steps)
- 맵 담당(사용자)이 위 방법대로 `Map_4_Security`에 연결하고 적 수 조정.
- 실제 맵 Play에서 방 진입·클리어·문 열림, 보스전 체력바 전환을 눈으로 확인. 바 위치·크기·이름은 프리팹 `Canvas_InGame/BossHealth`에서 조정.
- 필요하면 정보 패널(`Active Mission`)에 보안 앱 라운드·남은 적 수 표시(`CombatZone.AliveCount`·`OnProgressChanged` 있음, 요청 범위 밖이라 안 함).
