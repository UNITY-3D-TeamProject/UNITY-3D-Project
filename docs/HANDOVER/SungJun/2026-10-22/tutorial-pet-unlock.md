# 튜토리얼 펫 켜기(첫 상호작용 대화 후) — 2026-10-22

## 1. 작업 요약 및 방법
- 요청: 튜토리얼 시작 시 펫이 없다가, 맨 처음 E 상호작용 대화가 끝나면 펫이 나타나게. 튜토리얼 씬은 못 고치므로 TestScene에서 시험.
- intent-068: [intent-068](../../../intent/intent-068-tutorial-pet-unlock.md)
- 튜토리얼 첫 상호작용 구조(씬 파일 텍스트로만 확인): `ResumeSpawnerCube`(InteractButton, 한 번만) → `PlatformSpawner.ResumeSpawning`, `TutorialAtmosphere.Reveal`, `UIManager.PlayDialogue`.
- `Core/Stage/Tutorial.cs` 수정
  - `StartStage`: `GameManager.OnPlayerSpawned` 구독 + 현재 플레이어에 `PlayerFacade.SetPetEnabled(_isPetUnlocked)`.
  - `UnlockPet()`(UnityEvent용): 한 프레임 넘긴 뒤 `CutsceneController.Current.IsDialoguePlaying`이 false가 될 때까지 기다렸다가 펫을 켬.
  - `OnDestroy`에서 구독 해제.
- 처음 만든 `Core/Stage/PetUnlockPoint.cs`(맵 펫 모델 + 컷씬 방식)는 실제 구조와 맞지 않아 삭제. TestScene의 `CutsceneTest/5_PetUnlock`·`Test_PetMeet` 카메라·`SOCutscene_TestPetMeet`·표 3번 항목도 삭제.
- TestScene `TutorialPetTest`: `TutorialStage`(Tutorial, 기본 꺼짐), `ResumeSpawnerCube_Test`(Interact 레이어, InteractButton 한 번만: `UIManager.PlayDialogue(1)` → `Tutorial.UnlockPet`), 표지판.
- 적용 가이드: `docs/pet-unlock-setup-guide.md`

## 2. 결정 사항
- 펫 켜기·끄기는 `PlayerFacade.SetPetEnabled`만 사용(사용자 지시). 펫 오브젝트 직접 접근 안 함.
- 펫 로직은 튜토리얼 StageBase(`Tutorial.cs`)가 맡고, 씬 담당자는 `On Interacted`에 `UnlockPet`만 연결.
- 대화 끝 이벤트가 없어 `IsDialoguePlaying`을 매 프레임 확인.

## 3. 현재 상태 및 이슈
- 컴파일 0에러.
- TestScene Play(FileApp 끄고 TutorialStage 켠 상태) 확인:
  - 시작 시 펫 꺼짐 ✔ / 펫 켜기 전 재스폰 → 꺼짐 ✔
  - 큐브 상호작용 → 대화 중 펫 꺼짐 유지 ✔ → 대화 끝(`StopDialogue`로 종료) 다음 프레임 펫 켜짐 ✔
  - 펫 켠 뒤 재스폰 → 켜짐 ✔
- 이번 Play는 에디터가 백그라운드라 프레임이 진행되지 않아, 일시정지 + `EditorApplication.Step`으로 한 프레임씩 넘기며 확인. 실제 조작(조준 + E, E로 대사 넘기기)은 미확인.
- 시험 후 TestScene은 `FileApp` 켬, `TutorialStage` 끔으로 되돌려 저장.
- **사고 기록**: 정리 eval이 활성 씬을 확인하지 않고 저장해서, 사용자가 열어 둔 `Map_0_Tutorial`이 한 번 저장됨. 내용 변경은 없었고(줄바꿈만) `git checkout`으로 즉시 원상 복구, git status 깨끗함 확인.
- 브랜치 `feature/system-tutorial-pet`에 커밋(Tutorial.cs·문서만). DTT 임포트로 바뀐 `ProjectSettings`·폰트 에셋·`.slnx`는 커밋에서 뺄 것.

## 4. 다음 할 일 (Next Steps)
- 사용자가 TestScene에서 직접 조작으로 확인(FileApp 끄고 TutorialStage 켜기).
- 튜토리얼 씬 담당자가 `ResumeSpawnerCube` `On Interacted` 끝에 `Tutorial.UnlockPet` 연결. 펫 안내·스캔 안내 순서 확인.
- 적용 후 intent-068 resolved 처리.
