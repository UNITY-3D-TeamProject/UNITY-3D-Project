---
id: intent-044
title: 스테이지 첫 입장 시 플레이어·펫 대화 연출 (시네머신 카메라 + Space 진행)
part: UI / Cutscene / Input
status: open
created: 2026-10-18
resolved: null
---

## 문제 (Problem)
스테이지나 튜토리얼에 처음 들어가도 아무 연출 없이 바로 조작이 시작된다.
플레이어와 펫이 대화하며 상황을 알려주는 장면이 없고, 대사를 넘기는 동안 조작을 막는 방법도 없다.
지금 있는 입력 차단은 설정창(`UIManager.OpenSettings`)뿐이고, 펫 안내 패널(`PetGuidePanel`)은 문구만 바꿀 수 있어 말하는 사람 이름·초상화를 바꿀 수 없다.

## 기대 결과 (Proposed outcome)
- 스테이지별로 처음 들어갈 때 한 번만 대화가 나온다 (PlayerPrefs 저장, 재시작해도 다시 안 나옴).
- 대화 중에는 이동·시점·점프·스킬·사격이 막히고, Space로 다음 대사로 넘어간다. ESC로 설정창은 열 수 있고, 닫으면 대화로 돌아온다.
- 대사마다 지정한 씬 CinemachineCamera로 카메라가 블렌드되고, 대화가 끝나면 플레이어 카메라로 돌아온다.
- 대화창은 기존 `PetGuidePanel`을 재사용하고, 이름 라벨과 초상화를 대사마다 바꾼다.
- 시간은 멈추지 않는다 (timeScale 1). 입력만 막는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(연출 시청), 기획(대사 SO 작성), 맵 담당(씬에 연출 카메라 배치)
- 어떤 시스템/모듈이 건드려지는가:
  - `Assets/FID_InputSystem.inputactions`: `Dialogue` 맵(`Advance` Space, `OpenSettings` ESC)
  - `UI/`: `SODialogueSequence`(새), `DialoguePresenter`(새), `PetGuideView`(이름·초상화), `PetGuidePresenter`, `UIManager`(PlayerInput 직접 참조, Dialogue 맵 전환)
  - `Cutscene/`(새): `DialogueDirector`(연출), `DialogueCutscene`(컷씬 단위 재생·본 기록), `StageIntroDialogue`(스테이지 시작 트리거)
  - `Prefabs/UI/Canvas_InGame.prefab`, `Scenes/Map_0_Tutorial.unity`, 대사 SO 에셋

## 제약 (Constraints)
- 지켜야 할 정책: UI(Canvas 프리팹)는 카메라를 모르고, 씬 연출 컴포넌트는 UI 내부를 모른다. 기존 `ShowPetGuide(id)` 동작은 그대로 둔다.
- 쓰지 말아야 할 것: Timeline(이번 범위에서는 대사별 카메라 번호 방식), timeScale 정지.
- PlayerFacade는 다른 파트 담당이라 건드리지 않는다. Dialogue 맵 전환과 Space/ESC 구독은 UIManager가 플레이어의 PlayerInput을 직접 참조해서 한다.

## 열린 질문 (Open questions)
- (사용자 확정) 카메라는 대사마다 번호 지정, 대화창은 PetGuidePanel 재사용, 처음 한 번만, 시간은 흐름.
- (확인 완료) 입력 맵 전환 시 Move가 (0,0)으로 풀려 이동값 잔류 없음.
- (사용자 확정) 대화 중 ESC로 설정창을 열어야 한다. UI 맵은 ESC가 '닫기'라 겹치므로 `Dialogue` 맵을 따로 둔다.
- Brain 기본 블렌드 0.2s가 연출에 짧으면 `Main Camera.prefab`에 CustomBlends 추가 여부 (공용 프리팹이라 별도 확인).
- (해결) 대화 중 펫이 플레이어 앞에 나와 플레이어를 보고, 말하는 사람을 비추는 카메라로 전환한다 (PetPivot 이동, CameraIndex -1 = 자동).
- 대화 중 플레이어 HUD(크로스헤어 등)를 숨길지 미정.
- (해결) 원하는 위치에서 컷씬 재생: `DialogueDirector`(씬 1개)·`DialogueCutscene`(컷씬마다 Play)·`StageIntroDialogue`(스테이지 시작 트리거)로 분리, 발판은 `TriggerPlate` → `DialogueCutscene.Play` 연결.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
