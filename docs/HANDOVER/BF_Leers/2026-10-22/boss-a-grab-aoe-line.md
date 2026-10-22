# 2026-10-22 — 보스 A 그랩 조준 예고를 DTT AOE Line(GrapWarnning)으로 교체 (intent-064)

## 1. 작업 요약 및 방법
- 원인: `Map_4_Security` 의 `BossArena._linePrefab`·`_circlePrefab` 이 비어 있어 그랩 직선·어깨포 원 예고가 보이지 않았다(테스트 머티리얼 문제 아님).
- `AoeVfxPlayer.cs` 를 `Assets/Test/AoeVfxTool/Runtime/` → `Assets/_Project/Scripts/Boss/VFX/` 로 `.meta` 째 이동, 네임스페이스 `TestProject.AoeVfx` → `Boss.VFX`(사용자 지시).
  - 로컬 테스트 툴(Git 제외)의 asmdef 2개(`TestProject.AoeVfx.Runtime`/`.Editor`)가 Assembly-CSharp 를 참조할 수 없어 삭제(백업 `%TEMP%\aoevfx-asmdef-backup\`), `AoeVfxToolWindow.cs` 에 `using Boss.VFX;` 추가.
- 셰이더 `AoeIndicatorURP.shader` → `Assets/ThirdParty/DTT/Boss/Shaders/`, 머티리얼 `Material_0·1.mat` → `Assets/ThirdParty/DTT/Boss/AoeLine3/`(사용자 지시, 에디터로 이동, GUID 유지). 툴 창 셰이더 경로(249행) 갱신.
- 사용자가 `Assets/_Project/Prefabs/AI/Boss/GrapWarnning.prefab` 저장(루트에 `LineRegion` + `AoeVfxPlayer`).
- 코드(Codex CLI 가 이 PC 에 설치돼 있지 않아 Claude 가 직접 수정, 예외 사유: 도구 미지원):
  - `IBossArena.ShowLine(from, to, width, fillSeconds = 0)` 인자 추가.
  - `BossArena`: 직선 풀 생성 시 `LineRegion`·`AoeVfxPlayer` 캐시. `LineRegion` 이 있으면 늘리지 않고 시작점에 놓은 뒤 `Angle 0`·`Length`·`Width` 지정, `PlayLineFill` 로 `fillSeconds` 동안 채움(0 이하면 가득). 없으면 기존 늘리기 방식.
  - `BossAGrab`: 조준 시간 `_remaining` 을 먼저 계산해 `ShowLine` 에 넘김.
- 테스트 씬 `Assets/Test/Boss/Map_4_Security_Boss_Phase_T.unity` 의 `BossArena._linePrefab` = `GrapWarnning`(메모리에서만 설정, **씬 저장 안 함** — 사용자의 미저장 변경이 있었음).

## 2. 결정 사항
- 그랩 예고만 `ShowLine` 을 쓰므로 내려치기(칸 예고)·B 돌진 직선은 영향 없음.
- `Boss.VFX` 는 `AI.Boss.*` 안에서 한정 이름(`Boss.VFX.X`)으로 쓰면 `AI.Boss` 로 해석되므로 파일 맨 위 `using` 으로만 참조.

## 3. 현재 상태 및 이슈
- 리컴파일 통과. Play(테스트 씬)에서 `ShowLine` 직접 호출로 시작점·끝점(`EndPosition`)·몸통 28 m + 화살촉 2 m = 30 m 일치, 4초 채움 진행, 바닥 위 청록색 직선 보임 확인. 실제 그랩 패턴 중 육안 확인은 안 함.
- **`Map_4_Security` 연결 미완료**: 테스트 씬과 함께 additive 로 열면 같은 NavMesh 데이터(`NavMesh-03_Round1_Battle`)를 쓰는 `NavMeshSurface` 가 중복으로 판단해 Map_4 쪽 참조를 비운 채 저장됨. 재설정해도 같아서 Map_4 를 닫고 `git checkout` 으로 원상 복구했다. Map_4 를 **단독으로** 열고 연결해야 한다.
- Play 중 에디터 비활성 + `runInBackground` 꺼짐으로 프레임이 멈춰, Play 동안만 켰다가 끝난 뒤 `PlayerSettings.runInBackground = false` 로 되돌림(파일 값 0 유지).
- 이번 작업과 무관한 변경: `ProjectSettings.asset`(Standalone 정의 기호에 `UNIVERSAL_PACKAGE` 추가, 13:53 수정), `ShaderGraphSettings.asset`. 원인 미확인(DTT 에디터 스크립트 추정), 손대지 않음.
- `ThirdParty/DTT/Boss/` 는 Git 제외 → 팀원에게 따로 전달 필요.
- 게이트 루프 미실행, 커밋 전.

## 4. 다음 할 일 (Next Steps)
1. 테스트 씬 저장 여부는 사용자 판단.
2. 실제 패턴(그랩·어깨포·B 돌진·도약)에서 색·두께·채움 속도 육안 확인 → intent-064 resolved.

## 5. 추가 작업 (같은 날, 보스 B 돌진·도약 + 어깨포 원)
- 사용자가 `CircleWarnning.prefab`(AOE Circle 6, `CircleRegion` + `AoeVfxPlayer`)을 만들고, `Map_4_Security` 를 단독으로 열어 `_linePrefab`=`GrapWarnning`·`_circlePrefab`=`CircleWarnning` 연결·저장(diff 2줄, NavMesh 참조 유지 확인).
- AOE Circle 6 머티리얼 2개 → `Assets/ThirdParty/DTT/Boss/AoeCircle6/`(에디터 이동, GUID 유지).
- `BossArena.ShowCircle(center, radius, fillSeconds = 0)`: 루트를 지름으로 늘리던 방식이 DTT 원(안쪽에서 2×Radius 로 그림)과 겹쳐 4배로 커지던 문제 → `CircleRegion` 이면 `Radius` 만 넣음. 직선·원 공용 `PlayFill(player, seconds)`. `BossShoulderCannon` 이 예고 시간 `_remaining` 을 넘김.
- `BossBTelegraph`: `Show` 에서 `LineRegion`·`CircleRegion` 캐시. `Place` 는 직선이면 중심에서 반 길이 뒤로 물린 시작점 + `Length`/`Width`, 원이면 `Radius = 지름/2`, 아니면 기존 늘리기. `SetFade(progress)` 는 DTT 면 `FillProgress`. `BossBCharge`·`BossBLeap` 코드는 그대로.
- `BossB.prefab`: `BossBCharge._linePrefab` = `GrapWarnning`(임시 공유), `BossBLeap`(Leap) `_circlePrefab` = `CircleWarnning`. Evade 는 원래 예고 없음이라 건드리지 않음. 저장 시 기존 필드 `_spawnEffectDriver: {fileID: 0}` 가 한 줄 직렬화됨(기본값).
- 검증: 리컴파일 통과. Play 에서 `BossBTelegraph` 직접 호출로 직선 시작·끝점, 원 반지름, 진행도 채움 확인(화면 캡처로 직선·원 보임). `ShowCircle` 은 Play 중 리플렉션으로 원 풀을 `CircleWarnning` 으로 바꿔 반지름 3 → 메시 지름 6 m, 3초 뒤 채움 1 확인. 실제 B 스킬·어깨포 발동 중 육안 확인은 안 함.
