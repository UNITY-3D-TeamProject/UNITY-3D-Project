# 튜토리얼 펫 켜기 세팅 가이드

튜토리얼에서 "처음엔 펫이 없다가, 첫 상호작용(E) 대화가 끝나면 펫이 따라옴"을 만드는 방법이다.
코드는 `Core.Stage.Tutorial`(튜토리얼 스테이지 스크립트)에 들어 있고, **씬에서는 이벤트 하나만 연결하면 된다.**

- 스크립트: `Assets/_Project/Scripts/Core/Stage/Tutorial.cs`
- 펫은 `PlayerFacade.SetPetEnabled`로만 켜고 끈다.

## 동작
1. 튜토리얼 스테이지가 시작되면 펫을 끈다.
2. 펫을 켜기 전에 죽어 다시 생긴 플레이어도 펫을 끈다(다시 생긴 플레이어는 펫이 켜진 채로 나오기 때문).
3. `Tutorial.UnlockPet()`이 불리면, 지금 재생 중인 대화가 끝날 때까지 기다렸다가 펫을 켠다.
4. 펫을 켠 뒤에는 죽어도 펫이 켜진 채로 다시 생긴다.

## Map_0_Tutorial에 연결하는 법 (씬 담당자)
1. 맨 처음 E로 누르는 `ResumeSpawnerCube`를 선택한다.
2. `Interact Button` → `On Interacted` 목록 **맨 끝**에 `+`로 칸을 하나 추가한다.
3. 오브젝트 칸에 `Tutorial` 컴포넌트가 붙은 오브젝트(튜토리얼 StageBase)를 넣고, 함수는 `Tutorial → UnlockPet ()`을 고른다.
4. 끝. 지금 목록에 있는 `UIManager.PlayDialogue`가 띄운 대화가 끝나면 펫이 켜진다.

> 순서가 `PlayDialogue`보다 앞이어도 동작한다(한 프레임 기다린 뒤 대화 여부를 본다). 그래도 맨 끝에 두는 것을 권장한다.

## TestScene에서 시험하는 법
- `TutorialPetTest` 구역: `TutorialStage`(Tutorial 컴포넌트, 기본 꺼짐), `ResumeSpawnerCube_Test`(InteractButton: `UIManager.PlayDialogue(1)` → `Tutorial.UnlockPet`).
- 시험할 때만 루트의 `FileApp`을 끄고 `TutorialPetTest/TutorialStage`를 켠 뒤 Play 한다. 끝나면 다시 `FileApp` 켬, `TutorialStage` 끔으로 돌려놓는다(다른 시험이 FileApp을 쓴다).

## 주의
- 펫이 꺼진 동안은 스캔 스킬도 막힌다(기존 동작). 스캔 안내가 첫 상호작용보다 앞에 있으면 안 된다.
- 펫 안내(PetGuide 발판)가 첫 상호작용보다 앞에 있으면 펫 없이 펫 안내 문구가 뜬다. 배치 순서를 확인한다.
- 첫 라운드에서 죽으면 씬을 다시 불러오는 설정(`StageManager`의 첫 라운드 사망 시 재시작)이면, 씬이 처음부터 다시 시작되어 펫도 다시 꺼진다.
