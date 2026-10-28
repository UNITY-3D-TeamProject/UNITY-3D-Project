# 로비 군중 FPS 측정·프로파일링 (2026-10-25)

## 1. 작업 요약 및 방법
- 질문: 보행자의 `CharacterMotor` 를 `TransformMotor` 로 바꾸면 FPS 가 70~80 → 100 이 되는가? (코드·씬 수정 없음, 측정만 함)
- 환경: 에디터 Play 모드, `Map_1_Lobby`, 군중 51명(보행자 41·차 5·그 외), 구성마다 150프레임 워밍업 후 500~600프레임 평균.
- 측정 방법: `unity-editor-mcp` eval 로 `EditorApplication.update` 콜백에서 구성을 순서대로 적용하고 프레임/시간을 기록. 마커별 비용은 `ProfilerRecorder`(`SumAllSamplesInFrame` 옵션 필수)와 `FrameTimingManager` 로 수집.
- 실행자: Claude 직접(측정·에디터 조작). 예외 사유 = 이 PC 에 Codex CLI 미설치, 코드 수정 없는 일회성 측정.

## 2. 결정 사항
- 결정된 아키텍처 규칙 없음. 아래는 측정 결과다.
  - **군중 비용(에디터, 51명)**: 전체 약 3.4 ms. `CrowdController` 로직+이동 부수비용 2.4 ms, `CharacterMotor` 0.4 ms(정지 상태), Animator 0.4 ms, 나머지 0.3 ms.
  - **TransformMotor 방식 시뮬레이션**(CharacterController 끄고 CapsuleCollider+kinematic Rigidbody, Transform 직접 이동): 65.9 → 69.3 FPS (약 -0.7 ms). 모터 교체 효과는 작다.
  - **100 FPS 를 막는 주원인은 군중이 아니다.** 군중 0명일 때 메인 스레드 약 14 ms 중 게임 `PlayerLoop` 는 5.4 ms, 에디터 창 다시 그리기(`GUIView.RepaintAll`)가 10.5 ms(Scene 뷰 5.6 ms + Game 뷰 4.8 ms). `FrameTimingManager`: main 4.8 ms / render 3.2 ms / GPU 2.9 ms.
  - **Game 뷰 최대화(`EditorWindow.maximized`) 시 군중 0명 기준 75.6~81.1 → 188 FPS(5.3 ms).** 에디터 FPS 는 Scene 뷰가 보이는 한 실제 성능을 반영하지 않는다. 판단은 최대화한 Game 뷰나 빌드에서 한다.
  - **재확인 (군중 44명, 실제 로비 구성, 구성별 150프레임 워밍업+500프레임)**: 일반 창+군중 63.5 / 64.2 FPS(약 15.7 ms), Game 뷰 최대화+군중 110.8 / 113.6 FPS(약 8.9 ms), 최대화+군중 없음 194 FPS(5.15 ms), 최대화+TransformMotor 방식 시뮬레이션 121.2 FPS(8.25 ms). 즉 최대화 기준 군중 비용은 약 3.8 ms, 모터 교체로 약 0.65 ms(+9 FPS) 절감. **Game 뷰 최대화 시 이미 100 FPS 를 넘고, 에디터 일반 레이아웃에서는 군중을 어떻게 해도 100 에 못 닿는다.** 시뮬레이션은 중력·지면 따라가기 없이 Transform 만 옮긴 근사치다.
  - **군중 LOD 사전 측정 (군중 44명, eval 의 `Stopwatch`, 개체 1명·1회당 / 44명 프레임당 환산)**: `CrowdController.AdvanceProgress` 25.4 µs / 1.12 ms, `GetDirectionToTarget` 8.4 µs / 0.37 ms, 감지 `SphereCastNonAlloc` 0.9 µs / 0.04 ms, `SyncTransforms`+`CharacterController.Move`(전진) 16.5 µs / 0.73 ms(정지 시 3.5 µs / 0.15 ms), Animator 약 0.4 ms(이전 측정). **스플라인 샘플링(Advance+Direction) 약 1.5 ms 가 최대 항목이고 감지 `SphereCast` 는 무시할 수준**이다. 합계 약 2.7 ms 로 군중 전체 3.8 ms 중 약 1.1 ms 는 `MoveMediator`·회전·애니메이션 중재자 등으로 귀속하지 못했다.
  - **분포**: 군중은 약 75×112 m 에 퍼져 있다. 플레이어가 개체 위치에 선다고 가정하면 반경 20 m 안 평균 6.3명(최소 2·최대 13), 30 m 안 평균 11.9명, 50 m 안 평균 24.4명. 플레이어 시작 위치(`(-35, 16, 91)`)에서는 44명 전부 30 m 이상이고 카메라 시야 안은 37명이다(카메라가 3인칭이라 화면 컬링만으로는 줄지 않음). `Player` 태그 오브젝트는 없어 플레이어는 `CharacterMotor` 중 군중 아닌 것으로 찾았다(루트 `Player(Clone)`, 레이어 `Player`).
- 에디터 측정 시 알아둘 점: 에디터 창이 포커스를 잃으면(`Application.runInBackground=false`) Play 프레임이 멈춘다. 측정 중에만 `Application.runInBackground=true` 로 켜고 끝나면 원복한다(PlayerSettings 는 바뀌지 않음 확인).

### 군중 LOD(경로 계산 주기 분산) 구현 (같은 날, 사용자 결정: 이동 방식은 그대로, 주기 분산만)
- 변경: `Scripts/AI/Crowd/CrowdController.cs`. 경로 계산(`AdvanceProgress`·갈림길·끝 판정·`GetDirectionToTarget`)을 `_pathUpdateDistance`(기본 0.1m, 0 이하면 매 프레임)만큼 이동할 때마다 하고, 그 사이에는 직전 `_moveDirection` 을 재사용한다. 다음 계산까지의 거리는 매번 0.5~1.5배로 뽑아 같은 프레임에 몰리지 않게 한다. 멈춰 있으면 계산하지 않는다. 감지 `SphereCast`·점프·재출발 대기 로직과 `CharacterMotor`/중재자 경로는 그대로다. intent-017 에 결정·결과를 추가했다(새 intent 없음).
- 결과: 위 `intent-017` 참고. 군중 약 69명, 최대화 상태에서 14.54 → 11.96 ms(+약 15 FPS), 경로 이탈 평균 +2 cm.
- 구현 실행자: Claude 직접. 예외 사유 = 이 PC 에 Codex CLI 미설치. 검증: `gate.cjs check recompile` 통과 + Play A/B. 새 EditMode 테스트는 없다(intent-017 이 자동 테스트를 범위 밖으로 둠).
- **측정 정정**: 로비는 시작 시 대화·펫 가이드·연출 요청으로 `GameManager` 가 `Time.timeScale=0` 으로 둔다. 사용자가 프리팹을 다시 채운 뒤의 측정(군중 44명, 위 "재확인" 포함)과 LOD 1차 A/B 는 **게임 시간이 멈춘 상태(군중이 움직이지 않음, 44명은 초기 배치 인원 그대로)** 에서 한 것이라 군중 비용·모터 교체 효과·LOD 효과가 실제와 다르다(LOD 효과 +30 FPS 는 과대). 이후 `Time.timeScale=1` 로 돌리고 군중이 실제로 이동하는 상태에서 다시 쟀다. 움직이는 군중 기준 개략치: 69명일 때 군중 전체 비용이 약 8 ms 로 인원당 약 0.12 ms 이며(정확한 군중 비활성 기준값은 인원 변화 때문에 못 얻음), 정지 상태 추정(44명 3.8 ms)보다 크다.
- 위 측정 수치 중 "TransformMotor 방식 시뮬레이션 0.65 ms 절감"은 정지 상태에서 잰 값이라 신뢰하지 않는다. 움직이는 군중으로 재측정 필요.

## 3. 현재 상태 및 이슈
- **(해결됨, 같은 날 재확인)** 처음 측정 때 `Map_1_Lobby` 의 CrowdLane `_prefabs` 가 잘못 채워져(`Car_Lane_1` 만 ThirdParty 모델 2개, 나머지 `[]`) 군중이 생성되지 않았다. 사용자가 다시 채운 뒤 확인: 9개 레인 모두 `ready=True`, 에러 없음, `Car_Lane_*` = [Car, Car_2, Car_3], `Pedstrian_Lane_*` = [Pedstrian]. 씬은 에디터에서 dirty(저장 전)이고 `Car.prefab`·`Car_2.prefab` 도 수정 상태다. 이 세션에서 씬·프리팹은 수정·저장하지 않았다. 레인 목표 합계는 약 131명이나 반납/재생성이 균형을 이뤄 동시 활성은 44명 안팎에서 더 늘지 않았다.
- `Assets/Test/Stage/BossRoundPlatformTests.cs`(git 제외 로컬 파일)는 처음엔 현재 코드에 없는 메서드를 호출해 컴파일이 실패했다(측정 때 임시로 프로젝트 밖으로 옮겼다 되돌림). 재확인 시점에는 컴파일이 통과했다(사용자가 고친 것으로 보임).
- 리컴파일 때문에 `ProjectSettings/ProjectSettings.asset` 의 Standalone 정의에 `UNIVERSAL_PACKAGE` 가 추가됐다(DTT 에셋 자동 도구로 추정, 되돌리지 않음). `EditorBuildSettings.asset` 도 변경으로 표시되나 줄바꿈만으로 보인다.
- 그 외 이 세션에서 만든 임시 오브젝트·파일은 모두 정리했다.

## 4. 다음 할 일 (Next Steps)
- `Map_1_Lobby` 레인 9개의 `_prefabs` 를 위 기준으로 채워 저장(에디터 MCP/CLI 작업).
- FPS 목표 판단은 Game 뷰 최대화 상태나 빌드에서 다시 잰다. 에디터 일반 레이아웃 수치(70~80)는 목표 기준으로 쓰지 않는다.
- 군중 LOD 방향(사용자 요청: 이동 방식은 그대로, LOD 계열 최적화): **감지 `SphereCast` 는 0.04 ms 로 싸서 주기를 건드리지 않는다.** 줄일 대상은 `AdvanceProgress`+`GetDirectionToTarget`(약 1.5 ms)이며, 매 프레임 재계산하지 않고 N프레임마다(개체별 시작 프레임 분산) 갱신하며 사이에는 직전 방향 입력을 재사용한다. 거리 단계는 선택 사항이다. 추정(미측정): 전체 1/3 주기 갱신 시 약 1.0 ms 절감, 거리 단계(가까움 매 프레임·먼 곳 1/5)를 더하면 약 1.0~1.1 ms. 보조: 먼 개체 Animator 를 끄고 `Animator.Update(누적 dt)` 를 낮은 주기로 호출하면 최대 약 0.3 ms. `CharacterMotor`/`CC.Move`(0.73 ms)는 공용 모터라 건드리지 않는다. 구현 전 intent 문서(intent-017 추가 또는 신규)와 거리 기준(카메라/플레이어) 결정이 필요하다. 군중 수를 150 까지 늘렸을 때의 비용은 아직 재지 않았다.
- 군중 0명 상태에서도 프레임당 GC 할당 약 113 KB(1,076회)가 찍혔다. 에디터 IMGUI 가 대부분일 가능성이 커서 빌드에서 확인한다.
- `BossRoundPlatformTests.cs` 가 가리키는 `BossRound`·`StateMover` 변경이 어느 브랜치에 있는지 확인해 로컬 테스트를 정리한다.
