# 상단 메뉴 진행 단계 바로 들어가기 — intent-098

## 작업 요약 및 방법
- 신규 에디터 스크립트 `Assets/_Project/Scripts/Core/Editor/ProgressStepMenu.cs`(`Core.Editor`). 상단 메뉴 `Tools > 진행 단계`에 `SceneLoader` 순서 9칸 + `저장 지우기`.
- 단계를 누르면: 이어하기 저장(PlayerPrefs `GameProgress.SaveVersion=1`, `SequenceStep=단계`) → `EditorSceneManager.playModeStartScene = MainScene` → Play → Play 진입 후 시작 씬 지정 해제 + 다음 에디터 프레임에 `SceneLoader.TryContinueGame()`.
- 로비 단계(2·4·6·8번)는 로비가 불러와질 때(`SceneManager.sceneLoaded`, PlayerSpawner.Start 전) `GameManager.SpawnStage`를 직전 스테이지로 리플렉션으로 맞춰 복귀 위치(`Position(Security)` 등)에서 시작한다.
- 대기 단계는 `SessionState`에 둬서 Play 진입 도메인 리로드에도 남는다.

## 결정 사항
- 런타임 코드(`SceneLoader`, `GameManager`)와 씬은 고치지 않았다. 저장 키 문자열은 `SceneLoader`의 private 상수를 복사했으므로 그쪽이 바뀌면 같이 바꾼다.
- Play 중에 누르면 경고만 남기고 무시한다.

## 현재 상태 및 이슈
- 리컴파일 통과, 메뉴 10개 등록 확인. `8. 로비 (보안 후)` 실행 시 Play 진입 → 시작 씬 지정 해제 → 자동 이어하기 → 로딩 씬까지 확인.
- Unity 창이 비활성이라 Play 루프가 2프레임에서 멈춰 로비 도착·복귀 위치 스폰·29번 대화 자동 재생은 확인하지 못했다(Play는 켜 둔 채 둠, Unity 창을 누르면 진행).
- 이어하기라 `SceneLoader.PreviousScene`이 비어 로딩 영상(피드·파일 → 로비)과 파일 복귀 전용 BGM은 재현되지 않는다.
- 커밋 안 함.

## 다음 할 일 (Next Steps)
- Unity 창을 켠 상태에서 `8. 로비 (보안 후)` → 보안 복귀 위치 스폰 + 29번 대화 바로 재생 확인.
- 다른 단계(특히 2·4·6번 로비)도 복귀 위치·복귀 대화 확인.
