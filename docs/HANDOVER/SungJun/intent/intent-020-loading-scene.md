---
id: intent-020
title: 씬 이동 시 로딩씬을 거쳐 목적지 씬 켜기 렉을 로딩 화면 뒤로 숨김
part: Scene Transition
status: open   # open | resolved
created: 2026-10-15
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- 포탈(`ScenePortal`)로 다음 맵에 가면 `SceneLoader.TryLoad`가 목적지를 `LoadSceneAsync`로 바로 불러온다.
- 불러오기(progress 0 → 0.9)는 백그라운드에서 끝나지만, 켜기 단계(이전 씬 정리 + 새 씬 오브젝트 생성 + `Awake`·`Start`)는 메인 스레드에서 한 번에 처리된다. 이 순간 게임 화면이 그대로 멈춘다.
- `Map_1_Lobby`(약 4MB)로 갈 때 특히 심하다.

## 기대 결과 (Proposed outcome)
- 포탈을 밟으면 가벼운 `Loading` 씬이 먼저 켜지고 이전 씬은 꺼진다.
- 로딩씬의 `LoadingScreen`이 목적지를 켜지 않은 상태(`allowSceneActivation = false`)로 불러오며 진행 바(`progress / 0.9`)를 채운다.
- 준비가 끝나고 최소 0.5초가 지나면 목적지 씬을 켠다. 켜는 순간의 멈춤은 로딩 화면 뒤에서 일어난다.
- 씬 이동 순서(`Sequence`), 전환 직전 이벤트(능력치 저장·`SpawnStage`), 스폰 흐름은 그대로다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(맵 이동), 맵 담당(포탈 배치, 변경 없음)
- 어떤 시스템/모듈이 건드려지는가:
  - `Scripts/SceneTransition/SceneLoader.cs`(수정)
  - `Scripts/SceneTransition/LoadingScreen.cs`(신규)
  - `Scenes/Loading.unity`(신규)
  - `ProjectSettings/EditorBuildSettings.asset`(6번에 추가)

## 제약 (Constraints)
- `ESceneType` 값 = Build Settings 번호이므로 로딩씬은 Build Settings 맨 끝에 둔다. `ESceneType`에는 넣지 않는다(포탈 목적지로 고를 수 없게).
- `ScenePortal`, `MapTestHud`는 수정하지 않는다. `GameManager`는 Additive 로드를 무시하는 한 줄만 추가한다(2026-10-15, 로딩씬을 겹쳐 열면서 필요해짐).
- 첫 라운드 사망 재시작(`StageManager`의 `SceneManager.LoadScene`)은 로딩씬을 거치지 않는다(사용자 결정).
  - (2026-10-15) 피드에서 확인: 씬만 다시 불러오고 SceneLoader 순서 단계·`IsLoading`은 그대로였다.
- 렉 자체를 줄이는 것(Start 분산, 셰이더 워밍업)은 범위 밖이다.

## 열린 질문 (Open questions)
- ~~진행 바가 많이 튀면 `MoveTowards`로 부드럽게 바꿀지 여부~~ → (2026-10-15) 로딩 창이 너무 빨리 지나간다는 피드백으로 적용했다. 진행 바는 최소 `_fillDuration`(1초)에 걸쳐 차고, 끝까지 찬 뒤 `_holdAfterFull`(0.2초) 머문 다음 목적지를 켠다.
- ~~문에 닿을 때 로딩씬이 켜지기 전에 잠깐 멈춤~~ → (2026-10-15) 로딩씬을 Additive로 겹쳐 띄우고 페이드인 뒤 이전 씬을 정리하도록 변경했다. 나올 때는 로딩 화면을 DontDestroyOnLoad로 남겨 로비 위에서 페이드아웃한다.
  - 남은 멈춤: 로딩씬 자체가 켜지는 프레임 0.08~0.15초(에디터 측정, 첫 로드가 더 김). 빌드에서 다시 재 본다.
- **컷씬(나중):** 로딩 화면 위에서 재생하며 뒤에서 목적지를 불러오는 방식을 추천했다(대기 시간 = 컷씬과 로딩 중 긴 쪽).
  - 컷씬은 이전 씬 정리 뒤에 시작하고, 목적지 켜기는 "진행 바 완료 + 컷씬 끝" 이후에 한다.
  - 재생 중에는 `Application.backgroundLoadingPriority`를 `Low`로 둔다.
  - 끊기면 "목적지를 다 불러온 뒤 컷씬 → 켜기" 순서로 바꾼다.
  - **결정 (2026-10-15, 사용자): 2번, 로딩이 다 끝난 뒤 컷씬을 보여준다.** 컷씬이 불러오기와 겹치지 않아 끊길 위험이 없는 대신, 대기 시간이 로딩 + 컷씬으로 늘어난다.
  - 구현할 때 순서(`LoadingScreen.CoLoad`):
    1. 진행 바가 끝까지 참(목적지는 0.9, 아직 켜지 않음)
    2. 진행 바·텍스트를 숨기고 로딩 화면 위에서 컷씬 재생
    3. 컷씬이 끝나면(또는 스킵) 목적지 켜기. 켜는 순간의 멈춤은 검은 배경 뒤에서 일어난다.
    4. 페이드아웃
  - 컷씬을 로비가 켜진 뒤 로비 위에서 틀지는 않는다. 로비 오브젝트가 뒤에서 돌아가고 켜기 멈춤이 컷씬 직전에 보이기 때문이다.
  - 아직 열린 것: 어느 전환에서 어떤 컷씬을 틀지(전환마다 다른지, 특정 전환만인지), 스킵 허용 여부.
  - **구현 (2026-10-15, 사용자 요청: SO로 분리)**
    - 신규 `SOLoadingCutsceneData`: `SLoadingCutscene { From, To, Clip }` 배열, `TryGetClip(from, to, out clip)`. 같은 이동이 여러 칸이면 위쪽 칸을 쓴다.
    - 에셋은 `ScriptableObjects/SceneTransition/SOLoadingCutsceneData.asset`(빈 목록)이다.
    - `LoadingScreen`이 이 SO를 받아 진행 바 완료 뒤 해당 이동의 영상을 `CoPlayCutscene`으로 재생하고, 끝나면 목적지를 켠다.
    - 목록에 없는 이동은 영상 없이 넘어간다.
    - 스킵은 아직 넣지 않았다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
