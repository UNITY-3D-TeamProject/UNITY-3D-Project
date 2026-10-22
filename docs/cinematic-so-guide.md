# 컷씬·카메라 샷·독백 SO 제작 가이드

맵·스테이지를 만드는 파트가 **컷씬(대화만), 컷씬(대화+카메라), 카메라 샷, 독백**을 직접 만들어 씬에 넣을 수 있도록 정리한 문서다. 코드를 고치지 않고 **SO 에셋 만들기 → 번호 표에 등록 → 발판이나 코드에서 번호로 부르기** 세 단계로 끝난다.

> 관련 구현: [intent-066 연출·대화 SO 분리](./intent/intent-066-cinematic-so-split.md), [intent-067 Dialogue → Cutscene 이름 변경](./intent/clear/intent-067-dialogue-to-cutscene-naming.md), [작업 기록](./HANDOVER/SungJun/2026-10-22/cinematic-so-split.md), 작성자 김성준
> 스크립트 위치: `Assets/_Project/Scripts/UIandCinematic/`, 네임스페이스 `UIandCinematic`

## 1. 한눈에 보기

| 종류 | 만드는 SO | 등록하는 표 | 부르는 함수 | 게임 시간 | 조작 | 씬 카메라 필요 |
|---|---|---|---|---|---|---|
| 1. 컷씬(대화만) | `SOCutscene` | `SOCutsceneTable` | `CinematicManager.PlayCutscene(번호)` | 멈춤 | 막힘(`Cutscene` 입력 맵: E로 대사 넘김, ESC 설정창) | 없음 |
| 2. 컷씬(대화+카메라) | `SOCutscene` + `SOCameraShot` | `SOCutsceneTable` | `CinematicManager.PlayCutscene(번호)` | 멈춤 | 막힘 | **있음** (`CinematicCamera` + 키) |
| 3. 카메라 샷 | `SOCameraShot` | `SOCameraShotTable` | `CinematicManager.PlayCameraShot(번호)` | 샷마다 선택 | 막힘 | **있음** (`CinematicCamera` + 키) |
| 4. 독백 | `SOMonologue` | `SOMonologueTable` | `UIManager.PlayMonologue(번호)` | 그대로 흐름 | 그대로 움직임 | 없음 |

- 모든 SO는 Project 창에서 **우클릭 → Create → UIandCinematic → …** 으로 만든다.
- SO 에셋은 종류별로 아래 폴더에 둔다.

| 종류 | 폴더 (`Assets/_Project/ScriptableObjects/UIandCinematic/`) | 파일 이름 예 |
|---|---|---|
| 컷씬 | `Cutscene/` | `SOCutscene_Tutorial_Intro` |
| 카메라 샷 | `CameraShot/` | `SOCameraShot_Tutorial_BridgeView` |
| 독백 | `Monologue/` | `SOMonologue_Feed_FirstEnter` |
| 표 (공용, 하나씩) | `CutsceneTable/SOCutsceneTable`, `CameraShotTable/SOCameraShotTable`, `MonologueTable/SOMonologueTable` | 새로 만들지 않고 있는 표에 줄을 추가한다 |

## 2. 공통 준비 (씬마다 한 번)

### 2-1. Canvas_InGame (UIManager)
- `Assets/_Project/Prefabs/UI/Canvas_InGame.prefab`이 씬에 있어야 한다. 이 안의 `UIManager`가 대사창을 띄우고, 독백 표(`SOMonologueTable`)도 이미 연결돼 있다.
- 컷씬·카메라 샷도 `UIManager`가 없으면 `씬에 대화를 재생하는 쪽(UIManager)이 없어…` 경고를 남기고 재생하지 않는다.
- 프리팹 안에 컷씬·독백을 그리는 부품이 이미 연결돼 있어 **보통은 건드릴 필요가 없다.** 얼굴·펫 위치 등을 바꾸고 싶을 때만 아래 칸을 본다.

| `Canvas_InGame` 안 오브젝트 · 컴포넌트 | 하는 일 | 바꿀 수 있는 칸 |
|---|---|---|
| `UIManager` · `UIManager` | 컷씬 대사·독백·펫 안내를 띄우는 창구. 켜질 때 `CutsceneController.Current`에 자기를 등록해 `CinematicManager`가 찾게 한다 | 독백 표(`Monologue Table`) |
| `UIManager` · `InputMapController` | 컷씬·카메라 샷·펫 안내 동안 입력 맵을 `Cutscene`(E 넘기기·ESC 설정창)으로 바꾼다 | 없음 |
| `PetGuidePanel` · `CutscenePresenter` | 컷씬 대사 한 줄을 패널에 그린다 | **Speaker Default Portraits**: `PET`·`PLAYER`의 기본 얼굴. SO에서 Portrait를 비운 줄은 이 얼굴을 쓴다<br>**Next / Last Line Label**: `E ▶ 다음` / `E ▶ 닫기` |
| `CutsceneDirector` · `CutsceneDirector` | 컷씬 동안 펫을 플레이어 앞에 세우고, 말하는 사람을 비추는 카메라를 켠다 | **Player / Pet Speaker Name**: `PLAYER` / `PET` (SO의 Speaker Name과 같아야 함)<br>**Pet Stand Offset**: 펫이 설 위치<br>**Player / Pet Shot Offset**: 말하는 사람 카메라 위치 |
| `MonologuePresenter` · `MonologuePresenter` | 독백을 같은 패널에 띄우고 게이지가 다 줄면 넘긴다 | **Default Line Duration**: SO의 Line Duration이 0일 때 쓰는 시간(기본 3초)<br>**Speaker Default Portraits**: 독백용 기본 얼굴 |

### 2-2. CinematicManager (컷씬·카메라 샷을 쓰는 씬)
`CinematicManager`는 프리팹이 없어서 **씬에 직접 만든다**.

1. Hierarchy에서 빈 GameObject를 만들고 이름을 `CinematicManager`로 짓는다.
2. `Add Component → CinematicManager`를 붙인다.
3. Inspector에서 표 두 개를 연결한다.
   - **Cutscene Table** ← `ScriptableObjects/UIandCinematic/CutsceneTable/SOCutsceneTable`
   - **Camera Shot Table** ← `ScriptableObjects/UIandCinematic/CameraShotTable/SOCameraShotTable`
4. 씬에 **하나만** 둔다. 두 개면 `한 씬에 CinematicManager가 두 개 있습니다.` 오류가 난다.
5. 오브젝트와 컴포넌트를 **켜 둔다**. 꺼져 있으면 `꺼져 있어 연출을 재생하지 못했습니다.` 경고가 나고 재생하지 않는다.

> 독백만 쓰는 씬이면 CinematicManager는 없어도 된다.

### 2-3. 메인 카메라
- Main Camera에 `CinemachineBrain`이 있어야 카메라 전환·블렌드가 된다 (플레이어 카메라를 쓰는 씬이면 이미 있다).

## 3. 씬 카메라 만들기 — CinematicCamera (2번·3번에 필요)

SO는 씬 오브젝트(카메라)를 직접 가리킬 수 없다. 그래서 **씬 카메라에 키(문자열)를 붙이고, SO에는 그 키를 적어** 이어 준다.

1. Hierarchy에서 `우클릭 → Cinemachine → Cinemachine Camera`로 카메라를 만든다.
   - 이름 예: `CinematicCam_BridgeView`
   - 정리용으로 씬에 `CinematicCameras` 같은 빈 부모를 만들어 그 아래에 모아 두면 찾기 쉽다.
2. 보여 줄 위치·각도로 옮긴다. (Scene 뷰에서 원하는 화면을 잡고 카메라 선택 후 `GameObject → Align With View`를 쓰면 편하다.) 대상을 따라가게 하려면 Cinemachine Camera의 Tracking Target 등을 평소처럼 설정한다.
3. **Priority를 플레이어 카메라보다 높게** 둔다. 플레이어 카메라는 Base 0, Aim 10이므로 **20 이상**으로 둔다. 낮으면 켜도 화면이 안 바뀐다.
4. 같은 오브젝트에 `Add Component → CinematicCamera`를 붙이고 **Key**를 적는다.
   - 예: `Tutorial_BridgeView`
   - **씬 안에서 겹치지 않게** 짓는다. `{씬/맵 이름}_{장면}` 형식을 권장한다.
   - SO에 적는 키와 **대소문자·띄어쓰기까지 똑같아야** 한다.
5. **GameObject는 켜 둔다.**
   - 시작할 때 `CinematicCamera`가 `CinemachineCamera` **컴포넌트만** 알아서 꺼 두므로, 작업 중에 켜 둔 카메라가 플레이어 화면을 덮지 않는다.
   - GameObject 자체를 꺼 두면 키 목록에 올라가지 않아 `키가 '…'인 연출 카메라(CinematicCamera)가 씬에 없습니다.` 경고가 나고 그 카메라를 못 찾는다.

## 4. 컷씬(대화만) — SOCutscene

플레이어와 펫이 대사를 주고받는 장면. 게임 시간이 멈추고, 펫이 플레이어 앞에 와서 서며, 말하는 사람을 비추는 카메라로 자동 전환된다. **E**를 눌러 한 줄씩 넘긴다.

### 4-1. SO 만들기
1. `ScriptableObjects/UIandCinematic/Cutscene/`에서 `우클릭 → Create → UIandCinematic → Cutscene`.
2. Inspector 항목을 채운다.

| 항목 | 설명 |
|---|---|
| **Id** | 본 기록 저장 키. 컷씬·카메라 샷끼리 **겹치지 않게** 짓는다. 예: `Tutorial_Intro`. Should Play Once를 켤 때 꼭 필요하다 |
| **Should Play Once** | 켜면 한 번 본 뒤로는 다시 재생하지 않는다 (PlayerPrefs `Cinematic.Seen.{Id}`에 저장) |
| **End Blend Seconds** | 컷씬이 끝나고 플레이어 카메라로 돌아가는 시간(초). 기본 1. 이 시간이 끝날 때까지 시간 정지·조작 막기가 유지된다 |
| **Steps** | 단계 목록. 위에서부터 순서대로 재생한다 |

3. Steps에 단계를 추가하고, 대화만 할 때는 모든 단계를 **Type = Line**으로 둔다. Line 단계는 위쪽 `Line` 칸만 쓰고 `Camera` 칸은 비워 둔다.

| Line 칸 | 설명 |
|---|---|
| **Speaker Name** | 말하는 사람. **`PLAYER` 또는 `PET`을 대문자로 정확히** 적는다. 이 이름으로 말하는 사람 카메라와 기본 얼굴을 고른다. 다른 이름을 적으면 이름은 표시되지만 말하는 사람 카메라·기본 얼굴이 없다 |
| **Portrait** | 비워 두면 기본 얼굴. 표정을 바꿀 때만 스프라이트를 넣는다 |
| **Camera Key** | **대화만 할 때는 비워 둔다.** 비우면 말하는 사람(PLAYER·PET) 카메라를 쓴다 |
| **Message** | 대사 |

예시:

| # | Type | Speaker Name | Camera Key | Message |
|---|---|---|---|---|
| 0 | Line | PET | (비움) | 여기가 어디지? |
| 1 | Line | PLAYER | (비움) | 일단 앞으로 가 보자. |

### 4-2. 표에 등록
1. `ScriptableObjects/UIandCinematic/CutsceneTable/SOCutsceneTable`을 연다.
2. **Entries**에 줄을 하나 추가하고 **Id**(번호)와 **Cutscene**(방금 만든 SO)을 넣는다.
   - 번호는 표 안에서 겹치지 않게 짓는다. 다른 파트와 겹치지 않도록 맵별로 번호대를 나누는 것을 권장한다 (예: 튜토리얼 100번대, 피드 200번대).
   - 표의 Id(정수, 부를 때 쓰는 번호)와 SO의 Id(문자열, 본 기록 키)는 **서로 다른 값**이다.

### 4-3. 부르기
`CinematicManager.PlayCutscene(번호)` → [8장](#8-어디서-부르나) 참고.

## 5. 컷씬(대화+카메라) — SOCutscene + CinematicCamera (+ SOCameraShot)

대화 중간에 특정 장소를 보여 주는 장면. 방법이 두 가지이고 한 컷씬 안에서 섞어 써도 된다.

| 방법 | 언제 | 하는 일 |
|---|---|---|
| **A. Line 단계에 Camera Key** | 대사를 띄운 채로 다른 곳을 보여 줄 때 | 그 줄이 떠 있는 동안 키의 카메라를 켠다. E로 넘기면 다음 단계로 |
| **B. Camera 단계에 SOCameraShot** | 대사창 없이 몇 초간 장면만 보여 줄 때 | 대사창을 숨기고 카메라 샷을 끝까지 재생한 뒤 다음 단계로 자동으로 넘어간다 |

### 5-1. 먼저 할 일
- [3장](#3-씬-카메라-만들기--cinematiccamera-2번3번에-필요)대로 씬에 `CinematicCamera`를 만들고 키를 정해 둔다.
- B 방법을 쓰려면 [6장](#6-카메라-샷--socamerashot)대로 `SOCameraShot`도 만들어 둔다 (이 경우 카메라 샷 표 등록은 안 해도 된다).

### 5-2. SO 만들기
4장과 같은 방법으로 `SOCutscene`을 만들고 Steps를 채운다.

- **A 방법**: Type = `Line`, **Camera Key**에 `CinematicCamera`의 키를 적는다.
- **B 방법**: Type = `Camera`, 아래쪽 **Camera Shot** 칸에 `SOCameraShot`을 넣는다. Line 칸은 비워 둔다.

예시:

| # | Type | Speaker Name | Camera Key | Camera Shot | Message |
|---|---|---|---|---|---|
| 0 | Line | PET | (비움) | | 저기 다리 보여? |
| 1 | Line | PET | `Tutorial_BridgeView` | | 저 다리를 건너야 해. |
| 2 | Camera | | | `SOCameraShot_Tutorial_BridgeFly` | |
| 3 | Line | PLAYER | (비움) | | 알겠어, 가 보자. |

- 1번 줄: 대사가 뜬 채로 `Tutorial_BridgeView` 카메라 화면이 나온다.
- 2번 단계: 대사창이 사라지고 카메라 샷이 끝까지 재생된다.
- 3번 줄: 다시 말하는 사람 카메라로 돌아와 대사가 이어진다.

### 5-3. 주의
- Camera 단계에 넣은 `SOCameraShot`의 **Id·Should Play Once·End Blend Seconds는 무시**되고 컷씬의 값을 쓴다. 샷 목록(Shots)만 쓴다.
- Camera Key를 잘못 적거나 씬에 그 카메라가 없으면 경고를 남기고 **말하는 사람 카메라로 대신** 보여 준다. 컷씬은 멈추지 않으니 Console 경고를 확인한다.
- Camera 단계의 Camera Shot이 비어 있으면 경고를 남기고 그 단계를 건너뛴다.
- Line 단계 사이의 카메라 전환은 `CinemachineBrain`의 기본 블렌드를 쓴다.

### 5-4. 표 등록·부르기
4장과 같다. `SOCutsceneTable`에 번호로 등록하고 `PlayCutscene(번호)`로 부른다.

## 6. 카메라 샷 — SOCameraShot

대사 없이 카메라만 움직여 보여 주는 장면 (예: 문이 열리는 곳 보여 주기, 맵 전경 훑기). 혼자 재생할 수도 있고, 컷씬의 Camera 단계에 넣을 수도 있다.

### 6-1. SO 만들기
1. `ScriptableObjects/UIandCinematic/CameraShot/`에서 `우클릭 → Create → UIandCinematic → Camera Shot`.
2. Inspector 항목을 채운다.

| 항목 | 설명 |
|---|---|
| **Id** | 본 기록 저장 키. 컷씬·카메라 샷끼리 겹치지 않게 (컷씬 안에서 쓸 때는 무시) |
| **Should Play Once** | 켜면 한 번만 재생 (컷씬 안에서 쓸 때는 무시) |
| **End Blend Seconds** | 끝나고 플레이어 카메라로 돌아가는 시간(초) (컷씬 안에서 쓸 때는 무시) |
| **Shots** | 샷 목록. 위에서부터 순서대로 보여 준다 |

| Shot 칸 | 설명 |
|---|---|
| **Camera Key** | 보여 줄 씬 카메라의 키 (`CinematicCamera`의 Key와 똑같이) |
| **Duration** | 이 샷을 보여 주는 시간(초). **블렌드 시간도 여기에 포함**되므로 Blend In Seconds보다 길게 둔다 |
| **Blend In Seconds** | 이전 화면에서 이 카메라로 넘어가는 시간(초). 0이면 바로 끊는다 |
| **Should Pause Time** | 켜면 이 샷 동안 게임 시간을 멈춘다. 끄면 화면 속 물체(함정, 움직이는 발판 등)가 계속 움직인다 |

예시 (`SOCameraShot_Tutorial_BridgeFly`):

| # | Camera Key | Duration | Blend In Seconds | Should Pause Time |
|---|---|---|---|---|
| 0 | `Tutorial_BridgeStart` | 2.5 | 1.0 | 켬 |
| 1 | `Tutorial_BridgeEnd` | 3.0 | 2.0 | 켬 |

### 6-2. 주의
- **연출 중 플레이어는 무적이 아니다.** Should Pause Time을 끈 샷은 **적이 없는 곳에서만** 쓴다.
- 조작은 샷 동안 항상 막힌다. 끝 블렌드(End Blend Seconds)가 끝난 뒤에 풀린다.
- 플레이어 카메라로 돌아오는 끝 블렌드 동안은 마지막 단계의 시간 설정을 따른다. 마지막 샷의 Should Pause Time을 끄면 돌아오는 동안에도 시간이 흐른다. 로비 전경처럼 시간은 흐르고 조작만 막으려면 모든 샷에서 끈다.
- 설정창(ESC)이 열린 동안은 샷 시간을 세지 않는다.
- Camera Key가 씬에 없으면 경고를 남기고 그 샷을 건너뛴다.

### 6-3. 표 등록·부르기
1. `ScriptableObjects/UIandCinematic/CameraShotTable/SOCameraShotTable`의 **Entries**에 **Id**(번호)와 **Camera Shot**을 추가한다.
2. `CinematicManager.PlayCameraShot(번호)`로 부른다.

> 컷씬의 Camera 단계에만 쓸 샷이면 표에 등록하지 않아도 된다.

## 7. 독백 — SOMonologue

게임을 멈추지 않고 화면에 대사를 잠깐 띄우는 것 (예: 새 구역에 들어갈 때 펫이 한마디). 조작·카메라·게임 시간은 그대로이고, 패널 아래 게이지가 다 줄면 다음 줄로 넘어가 마지막 줄 뒤에 저절로 사라진다. 넘기는 키는 없다.

### 7-1. SO 만들기
1. `ScriptableObjects/UIandCinematic/Monologue/`에서 `우클릭 → Create → UIandCinematic → Monologue`.
2. Inspector 항목을 채운다.

| 항목 | 설명 |
|---|---|
| **Line Duration** | 한 줄을 보여 주는 시간(초). **0이면 기본 3초** (`MonologuePresenter`의 기본값) |
| **Lines** | 대사 목록 |

| Line 칸 | 설명 |
|---|---|
| **Speaker Name** | `PLAYER` 또는 `PET` (기본 얼굴을 이 이름으로 찾는다) |
| **Portrait** | 비워 두면 기본 얼굴, 표정을 바꿀 때만 넣는다 |
| **Message** | 대사 |

### 7-2. 표 등록
1. `ScriptableObjects/UIandCinematic/MonologueTable/SOMonologueTable`의 **Entries**에 **Id**(번호)와 **Monologue**를 추가한다.
2. 이 표는 `Canvas_InGame` 프리팹의 `UIManager`에 **이미 연결**돼 있어 따로 연결할 필요가 없다.

### 7-3. 부르기
`UIManager.PlayMonologue(번호)` → [8장](#8-어디서-부르나) 참고. CinematicManager가 아니라 **UIManager**에서 부른다는 점에 주의한다.

### 7-4. 동작 규칙
- 독백 중에 다른 독백을 부르면 **새 독백으로 바꿔** 처음부터 띄운다.
- 대화·컷씬·카메라 샷·펫 안내 중에는 **무시**된다 (같은 패널을 쓰기 때문).
- 독백이 떠 있을 때 컷씬이 시작되면 독백은 꺼진다.
- 게이지는 게임 시간으로 줄어서 설정창·펫 안내로 게임이 멈추면 같이 멈춘다.

## 8. 어디서 부르나

### 8-1. 발판(TriggerPlate)에서 부르기 — 코드 없이

`Map.Gimmicks.TriggerPlate`는 플레이어가 닿으면 `On Player Entered` UnityEvent를 실행한다. 여기에 바로 연결한다.

1. 발판 오브젝트에 `Collider`(**Is Trigger 켬**)와 `TriggerPlate`를 붙인다.
2. **On Player Entered**의 `+`를 눌러 줄을 추가한다.
3. 오브젝트 칸에 씬의 오브젝트를 드래그하고 함수를 고른 뒤 번호를 적는다.

| 하고 싶은 것 | 드래그할 오브젝트 | 고를 함수 | 숫자 칸 |
|---|---|---|---|
| 컷씬 | 씬의 `CinematicManager` | `CinematicManager → PlayCutscene (int)` | 컷씬 표 번호 |
| 카메라 샷 | 씬의 `CinematicManager` | `CinematicManager → PlayCameraShot (int)` | 카메라 샷 표 번호 |
| 독백 | 씬의 `Canvas_InGame` 안 `UIManager` 오브젝트 | `UIManager → PlayMonologue (int)` | 독백 표 번호 |

- 한 번만 보여 줄 거면 SO의 **Should Play Once**를 켠다 (게임을 껐다 켜도 유지). 독백에는 이 옵션이 없으므로, 독백을 한 번만 띄우려면 발판의 **Is One Shot**을 켠다 (씬을 다시 불러오면 초기화).
- 발판과 CinematicManager·UIManager는 **같은 씬**에 있어야 드래그로 연결된다.

### 8-2. 코드에서 부르기

다른 파트 스크립트(보스 등장, 퍼즐 완료, 스테이지 시작 등)에서 부를 때:

```csharp
using UI;               // UIManager
using UIandCinematic;   // CinematicManager

// 컷씬 (번호는 SOCutsceneTable)
if (CinematicManager.Instance != null)
    CinematicManager.Instance.PlayCutscene(101);

// 카메라 샷 (번호는 SOCameraShotTable)
if (CinematicManager.Instance != null)
    CinematicManager.Instance.PlayCameraShot(101);

// 독백 (번호는 SOMonologueTable)
if (UIManager.Instance != null)
    UIManager.Instance.PlayMonologue(101);
```

- `Instance`는 씬에 배치하지 않았거나 Awake 전이면 `null`이므로 꼭 확인한다. `Start` 이후에 부르는 것을 권장한다.
- 세 함수 모두 반환값이 없다. 재생 여부는 아래 이벤트나 `IsPlaying`으로 확인한다.

### 8-3. 끝난 뒤에 무언가 하기 (컷씬·카메라 샷)

컷씬이 끝나고 문을 열거나 적을 등장시키려면 `CinematicManager`의 static 이벤트를 쓴다.

```csharp
private void OnEnable()
{
    CinematicManager.OnPlayFinished += HandleCinematicFinished;
}

private void OnDisable()
{
    CinematicManager.OnPlayFinished -= HandleCinematicFinished;
}

private void HandleCinematicFinished(CinematicManager manager)
{
    // 연출이 끝났다. 어떤 연출이었는지는 알려 주지 않으므로, 필요하면 부른 쪽에서 직접 기억해 둔다.
}
```

| 멤버 | 설명 |
|---|---|
| `CinematicManager.OnPlayStarted` | 연출이 시작될 때 (static event) |
| `CinematicManager.OnPlayFinished` | 연출이 끝날 때 (static event). 이 시점에는 아직 플레이어 카메라로 돌아오는 블렌드 중이라 **조작·시간은 End Blend Seconds 뒤에 풀린다** |
| `CinematicManager.Instance.IsPlaying` | 지금 연출 중이면 true |
| `CinematicManager.Instance.Stop()` | 재생 중인 연출을 바로 끝낸다 |
| `UIManager.Instance.StopMonologue()` | 떠 있는 독백을 바로 숨긴다 |

### 8-4. 불렀는데 재생되지 않는 경우

컷씬·카메라 샷은 아래 중 하나면 **조용히 무시**된다 (일부는 Console 경고).

- 이미 다른 컷씬·카메라 샷이 재생 중
- 대화나 펫 안내가 떠 있음
- 설정창이 열려 있음 (게임 상태가 Playing이 아님)
- 플레이어가 아직 없음 (스폰 전)
- Should Play Once인데 이미 본 적 있음
- CinematicManager가 꺼져 있음 / UIManager가 없음 (경고)
- 표에 그 번호가 없음 (경고 `{번호}번 컷씬이 테이블에 없습니다.`)

독백은 표에 번호가 없을 때 경고를 남기고, 대화·컷씬·펫 안내 중이면 무시된다.

## 9. 문제 해결

| 증상 | 확인할 것 |
|---|---|
| 카메라가 안 바뀐다 | ① SO의 Camera Key와 `CinematicCamera`의 Key가 똑같은지 ② 카메라 **GameObject가 켜져 있는지** ③ Cinemachine Camera **Priority가 20 이상**인지 ④ Console에 `연출 카메라(CinematicCamera)가 씬에 없습니다` 경고가 있는지 |
| 한 번 보고 다시 안 나온다 | Should Play Once가 켜져 있으면 정상. 테스트 중 다시 보려면 Unity 메뉴 `Edit → Clear All PlayerPrefs` (다른 저장값도 지워지니 주의) |
| Should Play Once인데 계속 나온다 | SO의 **Id**가 비어 있는지 확인 (`Id가 비어 있어 본 기록을 남기지 못했습니다` 경고) |
| 카메라 샷이 너무 빨리 끝난다 | Duration에 블렌드 시간이 포함된다. Duration을 Blend In Seconds보다 넉넉히 |
| 대사에 얼굴·말하는 사람 카메라가 안 나온다 | Speaker Name이 `PLAYER`/`PET`(대문자)인지 |
| 발판에 함수 목록이 안 보인다 | 드래그한 오브젝트가 CinematicManager(컷씬·카메라 샷) / UIManager(독백)가 붙은 오브젝트인지, Prefab 에셋이 아니라 **씬 오브젝트**를 끌었는지 |
| 카메라가 돌아오는 동안 못 움직인다 | 정상. End Blend Seconds 동안은 조작·시간이 막혀 있다. 짧게 하려면 값을 줄인다 |

## 10. 참고: 기존 구조

- 옛 대화 방식 `SODialogue`·`SODialogueTable` + `UIManager.PlayDialogue(번호)`와 씬 컴포넌트 `CinematicSequence`는 **새로 만들지 않는다.** 튜토리얼(`Map_0_Tutorial`)이 대사를 `SOCutscene`으로 옮기고 발판을 `PlayCutscene`으로 바꾸면 폴더(`ScriptableObjects/UIandCinematic/Dialogue`·`DialogueTable`)와 함께 지울 예정이다. 그때까지는 그대로 동작한다.
- 이전 이름과 지금 이름 (intent-067): `IDialogueController` → `ICutsceneController`, `DialogueController` → `CutsceneController`, `DialoguePresenter` → `CutscenePresenter`, `DialogueDirector` → `CutsceneDirector`, 입력 맵 `Dialogue` → `Cutscene`. 동작은 같다.
- 예시는 개인 TestScene `CutsceneTest` 공간의 발판 4개(컷씬 대화만 `PlayCutscene(2)`, 대화+카메라 `PlayCutscene(1)`, 카메라 샷 `PlayCameraShot(1)`, 독백 `PlayMonologue(1)`)에 있다. TestScene은 Git에 올라가지 않으므로 필요하면 김성준에게 요청한다.
