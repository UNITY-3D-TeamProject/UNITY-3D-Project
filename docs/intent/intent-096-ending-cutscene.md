---
id: intent-096
title: 보스 처치 후 라이브 앱 엔딩 컷씬(대화 교체 + TV 공연 영상 + 마무리 대화)
part: UIandCinematic / Cutscene / Live
status: open
created: 2026-10-30
resolved: null
---

> 기획 원본: `FID_엔딩_컷씬_기획안.pdf` (4쪽, 16컷). 이 문서는 그 기획을 지금 프로젝트 구조에 맞춘 **설계안**이다.

## 문제 (Problem)
- 스토리가 "보스 처치 후 플레이어는 죽지 않는다"로 바뀌었는데, 보안 클리어~라이브 입장 대사(28~32번 컷씬)는 옛 스토리(플레이어 데이터가 깨져 소멸)거나 `[임시]` 문구다.
- 라이브 앱 스크린(`LiveEndingScreen`)에는 테스트 영상(`Art/TestVideo/Roblox 2026-09-30 14-24-56.mp4`)이 연결되어 있고, VideoPlayer 오디오 출력이 `None`이라 소리가 안 나며, 재생 중 씬 BGM이 겹친다.
- 영상이 끝나면 바로 플레이어 카메라로 돌아가고 끝난다. 기획의 11~16컷(TV 앞 복귀 → 짧은 대화 3번 → 여운 → 크레딧)이 없다.

## 현재 구조 (조사 결과)
| 번호 | SO | 위치·호출 | 현재 내용 |
|---|---|---|---|
| 28 | `SOCutscene_Security_Clear` | `Map_4_Security` 발판(TriggerPlate) | 보스 카메라샷 + 대사 7줄 (플레이어 소멸 암시) |
| 29 | `SOCutscene_Lobby_ReturnSecurity` | `Map_1_Lobby` 발판 | 대사 5줄, 2번째 줄 카메라 `Lobby_LivePortal` ("다리에 힘이 안…") |
| 30 | `SOCutscene_Lobby_BeforeLive` | 로비 라이브 포탈 E (입장 전 대화) | 대사 4줄 ("마지막 데모가 아니야", "여기까지 같이 와 줘서") |
| 31 | `SOCutscene_Live_Enter` | `Map_5_Live` `Cutscene_31_LiveEnter` | `[임시]` 대사 + `Live_Overview` 샷 |
| 32 | `SOCutscene_Live_BeforeScreen` | `Cutscene_32_LiveBeforeScreen` | `[임시]` 대사 3줄 |
| 33 | `SOCutscene_Live_ScreenReveal` | `Cutscene_33_LiveScreen` → `LiveEndingScreen.Play()` | `Live_Pool07Player`(2.5초) → `Live_Screen`(3600초) 샷 |

- `LiveEndingScreen`(Map/Live, 양준범 작성): 33번 컷씬 카메라가 `Live_Screen`에 도착하면 스크린 머티리얼을 영상용으로 바꾸고 재생 → 영상 끝 → `CinematicManager.Stop()` → `_onVideoFinished`(UnityEvent, 현재 비어 있음) 호출.
- 크레딧 UI·타이틀 복귀 코드는 프로젝트에 없다.
- `AudioManager`에는 BGM을 멈추는 공개 메서드가 없다(`ChangeBgm`·`PlayNextBgm`만 있음).

## 기대 결과 (Proposed outcome) — 설계
기획 16컷을 기존 컷씬 번호에 대응시키고, 새로 필요한 것만 추가한다.

| 기획 컷 | 씬 | 구현 방법 |
|---|---|---|
| 01 보스 처치 | 보안 | 기존 보스 사망 연출 그대로 (변경 없음) |
| 02 보안 앱 정상화 | 보안 | 28번 대사 교체 (소멸 내용 삭제 → 정상화·곡 무사) |
| 03 로비 복귀 / 04 공연 알림 발견 | 로비 | 29번 대사 교체. 알림 줄에서 전광판 카메라로 전환 (기존 `Lobby_LivePortal` 키 재사용 또는 전광판 카메라 추가 — 열린 질문 4) |
| (로비 → 라이브 입장 전) | 로비 | 30번 대사 교체 (작별 뉘앙스 제거) |
| 05 라이브 앱 도착 | 라이브 | 31·32번 `[임시]` 대사 교체 |
| 06~10 TV 공연 | 라이브 | 33번 + `LiveEndingScreen` 그대로. 클립을 공연 영상으로 교체, VideoPlayer 오디오 출력 켜기, 재생 동안 씬 BGM 정지 |
| 11 TV 앞 복귀 | 라이브 | **새 34번** `SOCutscene_Live_AfterShow` 첫 단계: 새 카메라 `Live_ScreenBack`(TV 테두리 + 두 캐릭터 뒷모습) 샷 |
| 12~14 짧은 대화 | 라이브 | 34번 대사 6줄 (기획안 문구 그대로) |
| 15 여운 | 라이브 | 34번 마지막 단계: 새 카메라 `Live_Linger`(뒤로 천천히 멀어짐), 대사창 없이 약 5~8초, 시간 흐름 |
| 16 크레딧 | 라이브 | **새 UI** `UI/Credits/EndingCredits`: 화면 페이드 아웃 → 제작진 명단이 아래에서 위로 스크롤 → 끝나면 `GameManager.ReturnToMainMenu()` |

### 크레딧 설계 (결정: 2026-10-30)
- `UI.Credits.EndingCredits` (MonoBehaviour, `Map_5_Live` 캔버스에 배치)
  - `Play()`: 34번 컷씬(`_cutsceneId`)을 `CinematicManager.PlayCutscene`으로 재생 → `IsPlaying`이 꺼질 때까지 대기 → 검은 화면 `CanvasGroup` 페이드 인(`_fadeSeconds`) → 명단 `RectTransform`을 `_scrollSpeed`로 위로 이동, 명단이 화면 위로 다 나가면 `_endHoldSeconds` 대기 → `SceneLoader.TryLoadMainMenu()`.
  - 크레딧 동안 플레이어 조작은 막는다(기존 시간·입력 요청 방식 사용, 구현 시 `GameManager`/`InputMapController` API 확인).
  - 명단 문구는 인스펙터 TMP 텍스트로 직접 입력(코드에 이름을 넣지 않음).
- 연결: `LiveEndingScreen._onVideoFinished` → `EndingCredits.Play()` 하나만 씬에서 연결. 34번 재생도 `EndingCredits`가 맡아 "마무리 대화 → 크레딧" 순서를 한 곳에서 보장한다.

### 연결 방식
- 34번은 `LiveEndingScreen._onVideoFinished` → `EndingCredits.Play()`가 재생한다(**씬에서 연결**) → `Map/Live` 코드 수정 없음.
  - 영상 끝에 `Stop()`이 먼저 불려 샷 카메라가 꺼지지만 같은 프레임에 34번이 시작돼 Brain은 `Live_Screen` → `Live_ScreenBack`으로 블렌드한다(검증 필요).
- BGM: `AudioManager`(Core)에 `StopBgm(float fadeSeconds)` 추가. 영상 시작 시 호출은 `LiveEndingScreen`에 훅이 없으므로, 33번 트리거(`Cutscene_33_LiveScreen` UnityEvent)에 함께 연결한다.
- 공연 중 화면: 지금처럼 `Live_Screen` 카메라가 스크린을 꽉 채우는 방식 유지(기획 "카메라 컷 또는 영상 전체화면"의 전자). UI 전체화면 오버레이는 만들지 않는다.
- 입력: 기존 `Cutscene` 입력 맵 그대로 — 이동·사격 막힘, 대사 넘기기만 허용. 공연 영상 스킵은 없음(열린 질문 5).

### 대사 (2026-10-30 초안대로 승인·적용)
**28 Security_Clear** (보스 카메라샷 뒤)
- PET: 해냈어! 바이러스 왕이 쓰러졌어!
- PLAYER: …끝난 거야? 경고등이 꺼지고 있어.
- PET: 응. 보안 앱이 정상으로 돌아왔어. 감염된 데이터도 전부 치료됐고.
- PLAYER: 주인의 곡은?
- PET: 무사해. 파일 하나도 안 지워졌어.
- PLAYER: 다행이다. 로비로 돌아가자.

**29 Lobby_ReturnSecurity**
- PLAYER: …돌아왔네. 평소의 로비야.
- PET: (전광판 카메라) 저기 봐! 전광판에 알림이 떴어.
- PET: 주인의 LIVE 공연이 곧 시작한대!
- PLAYER: 무대에 서기로 했구나.
- PET: 라이브 앱에서 볼 수 있어. 같이 보러 가자!

**30 Lobby_BeforeLive**
- PLAYER: …들려. 그 노래.
- PET: 응. 이제 데모가 아니야.
- PET: 들어가자. 곧 시작해.

**31 Live_Enter**
- PET: 여기가 라이브 앱이야.
- (Live_Overview 샷)
- PLAYER: 저 끝에 큰 TV가 있네. 가까이 가 보자.

**32 Live_BeforeScreen**
- PET: TV에 불이 들어오려나 봐.
- PLAYER: 시작한다.

**34 Live_AfterShow (신규)** — 기획안 12~14컷 그대로
- PET: 와… 진짜 무대에 섰네. / PLAYER: 그러게. 처음 들었던 노래잖아.
- PET: 우리가 한 일이 도움이 된 걸까? / PLAYER: 그래도 다시 들을 수 있어서 좋아.
- PET: 이제 어떻게 할 거야? / PLAYER: 조금만 더 듣고 가자.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 게임을 끝까지 진행한 플레이어.
- 건드리는 것:
  - SO: 컷씬 28~32 대사 수정, 34번 신규 + `SOCutsceneTable` 등록, 카메라샷 SO 신규(`Live_AfterShow`용 2개).
  - 코드: `Core/Audio/AudioManager.cs`(`StopBgm` 추가), `UI/Credits/EndingCredits.cs`(신규).
  - 씬: `Map_5_Live`만 (VideoPlayer 클립·오디오, 카메라 2개, 크레딧 UI, `_onVideoFinished` 연결). 로비·보안 씬은 수정하지 않는다.
  - 에셋: 공연 영상 mp4를 `Assets/ThirdParty/Audio/CutScene/`에 임포트(Git 제외, 팀원에게 직접 공유).

## 제약 (Constraints)
- 코드는 `Scripts/Core`, `Scripts/UI`, `Scripts/UIandCinematic`만 수정한다. `Map/Live/LiveEndingScreen.cs`(양준범)는 수정하지 않고 UnityEvent 연결로 해결한다.
- 맵 씬은 사용자가 명시적으로 허락한 씬·범위만 수정한다.
- 플레이어 사망·소멸 연출은 쓰지 않는다(기획 핵심 설정).

## 결정 사항 (2026-10-30 사용자 답변)
- 맵 씬은 `Map_5_Live`만 수정한다. 로비 전광판 장면은 29번 대사의 기존 `Lobby_LivePortal` 카메라 키를 그대로 쓴다.
- 공연 영상은 `Assets/ThirdParty/Audio/CutScene/`에 둔다(로딩 영상과 같은 위치, Git 제외).
- 크레딧: 마무리 대화는 컷씬 SO(34번)로, 그 뒤 화면이 어두워지고 제작진 명단이 위로 올라가는 UI, 끝나면 메인 메뉴로 복귀.
- 보안 앱 정상화·로비 LIVE 알림은 시각 연출 추가 없이 대사만 교체한다.

## 열린 질문 (Open questions)
- 2026-10-30 "초안대로 시작" 답변으로 기본값 적용: 영상 `다시,_무대.mp4`(180초), 영상 뒤 음악 무음, 명단 `[임시] 이름`, 영상 스킵 없음, 대사 초안 승인.
- 남은 확인: `다시,_무대 (1).mp4`(193초)가 최종본인지, 실제 제작진 명단.
- 2026-10-30 사용자 지시로 `Map_5_Live`의 `DialogueBoxes`(31번 입장 대화 트리거, 양준범 `f0d88e62`에서 꺼짐)를 켰다. Play에서 31번 대사 → `Live_Overview` 샷 → 마지막 줄 → 조작 복귀 → 32번 트리거로 이어짐 확인.

## 구현 기록 (2026-10-30)
- 코드: `AudioManager.StopBgm(float)`, 신규 `UI/Credits/EndingCredits.cs`(34번 재생 → 여운 카메라 도착 후 `_lingerHoldSeconds` → 검은 화면 페이드 → 명단 스크롤 → `GameManager.ReturnToMainMenu()`). 크레딧 동안 34번 컷씬의 여운 샷(3600초)이 계속 재생되어 조작 막기·HUD 숨김이 유지된다.
- SO: 28·29·30·31·32번 대사 교체, `SOCutscene_Live_AfterShow`(34번, 표 등록), `SOCameraShot_Live_ScreenBack`(4초, 블렌드 3초), `SOCameraShot_Live_Linger`(3600초, 블렌드 6초).
- `Map_5_Live`: VideoPlayer 클립 교체·오디오 Direct·FitInside, 카메라 `CinematicCam_LiveScreenBack`(0.6, 3.6, 10)·`CinematicCam_LiveLinger`(0, 6, -4), `Canvas_EndingCredits`(정렬 100), `_onVideoFinished` → `EndingCredits.Play`, 33번 트리거 → `AudioManager.StopBgm(2)`.
- 영상: 원본(824×1464, 10fps)은 Windows 디코더에서 첫 프레임 뒤 멈춰, ffmpeg로 1920×1080(세로 영상 가운데 + 양옆 검정)·30fps·H.264 Baseline으로 다시 인코딩한 뒤 Unity 임포트에서 VP8 트랜스코딩을 켰다(테스트 영상과 같은 설정). `Assets/ThirdParty/Audio/CutScene/4_Ending.mp4`, GUID `3da896f593430c7468572868443a314e`. ThirdParty는 Git 제외라 **mp4와 .meta를 함께** 팀원에게 공유해야 씬 참조가 유지된다.
- 오디오: AudioSource 출력은 이 프로젝트에서 `AudioSampleProvider buffer overflow`로 디코딩이 멈춰, 로딩 영상과 같은 Direct 출력으로 했다(BGM 볼륨 설정은 적용되지 않음).
- 검증(Play, `Map_5_Live` 단독): 33번 트리거 → BGM 정지 → 스크린 영상 재생(시간 진행 확인) → 끝 2초 전으로 넘김 → 34번 뒷모습 샷·대사 6줄(E) → 여운 샷 → 크레딧 페이드·스크롤 → `MainScene` 도착(커서 해제, timeScale 1), 이 흐름에서 에러 0. 28~32번 대사는 SO 값만 확인했고 보안·로비 씬에서 실제 재생은 하지 않았다. 영상 소리는 직접 들어 확인하지 못했다.

## 연출 변경 (2026-10-30, 사용자 요청)
- 요청 흐름: TV에 엔딩 영상 → 몇 초 뒤 화면 안으로 들어가는 카메라 → 전체화면 → 끝날 무렵 전체화면에서 나와 TV 밖으로 → TV 앞 두 캐릭터 → 대화 → 엔딩.
- 영상: SVN으로 들어온 공식 `ThirdParty/Audio/CutScene/4_Ending.mp4`(1920×1080, 68초, VP8 트랜스코딩, GUID `d15ef7e1888346949007f8740b54444f`)로 교체. 앞서 넣은 재인코딩본(GUID `3da896f5…`)은 SVN 업데이트로 덮어써져 더 이상 쓰지 않는다. 영상은 마지막 1초에 검게 페이드아웃된다.
- 신규 `UIandCinematic/CameraShot/ScreenVideoZoom.cs` (`Canvas_EndingCredits/FullscreenVideo`에 부착, 33번 발판 이벤트에서 `Play`):
  - 인스펙터 `Enter At Video Seconds`(기본 4): 영상 재생 위치가 이 값에 오면 `CinematicCam_LiveScreenZoom`(0, 5.5, 27)으로 1.5초 블렌드하며 들어가고, 끝 무렵 전체화면 UI(`RT_LiveScreen` RawImage)를 0.6초 페이드 인.
  - 인스펙터 `Exit At Video Seconds`(기본 63): 이 위치에서 전체화면을 걷고 `CinematicCam_LiveScreenBack`으로 3초 블렌드(TV 밖으로 나와 두 캐릭터 뒷모습). 영상 뒤 34번이 같은 카메라를 이어받는다.
  - 카메라는 재생 중인 33번 샷 카메라 위에 켜서 덮고, 블렌드 시간은 Brain 기본 블렌드를 잠시 바꿔 정한다(CinematicManager가 다음 샷·연출 끝에서 되돌림).
- `SOCameraShot_Live_ScreenBack` 길이 4초 → 1초(이미 그 카메라에 와 있으므로 대사 전 짧은 정지만).
- 검증(Play): 33번 → TV 영상 → 4.9초 시점 줌 카메라 블렌드 → 전체화면(알파 1) 재생 → 61초로 넘김 → 63초에 전체화면 해제·뒷모습 카메라 도착 → 영상 끝 → 34번 첫 대사 표시까지 확인. 그 뒤 대화 → 여운 → 크레딧 → 메인 메뉴는 에디터 플레이 루프가 멈춰(포커스 문제) 이번엔 다시 돌리지 못했다. 이 구간 코드는 앞선 검증 이후 바뀌지 않았다.

## 연출 수정 2 (2026-10-30, 사용자 요청)
- 입장 지점: 스폰·체크포인트를 z=-40.9 → -15.3(`Pool_03`)으로 옮겨 33번 발판(z=22)까지 약 37m, 이동 속도 6m/s 기준 약 6초.
- 31번 "여기가 라이브 앱이야": 트리거를 입장 지점으로 옮겨 등장 연출이 끝나자마자 재생(`EnableColliderOnSpawnEnd`).
- 32번 "시작한다" 트리거 삭제(SO·표 등록은 남아 있으나 부르는 곳 없음).
- 33번 연출 두 번째 샷을 `Live_Screen`(TV 정면) → `Live_ScreenBack`(TV를 보는 플레이어 뒷모습)으로 바꾸고, `LiveEndingScreen._screenCamera`도 뒷모습 카메라로 바꿔 그 컷이 잡히면 영상 시작. `ScreenVideoZoom` 들어가는 시각 4 → 5초.
- 흐름: 입장 → 31번 대화 → 약 6초 걸어 TV 앞 → 뒷모습 컷에서 영상 시작(5초 보여 줌) → TV로 다가가며 전체화면 → 63초에 전체화면 해제·TV에서 뒤로 빠져 뒷모습 컷 → 영상 끝 → 34번 대화 → 크레딧.
- 검증: 씬 값·저장만 확인. Play는 에디터 플레이 루프가 1프레임에서 멈춰 실행하지 못했다(직접 확인 필요).

## 연출 수정 3 (2026-10-30, 사용자 요청)
- 나오는 시각을 50초로(사용자 설정). TV 밖으로 나와 뒷모습 카메라에 도착하면(50 + 페이드 0.6 + 블렌드 3 ≈ 53.6초) `ScreenVideoZoom`이 독백 `SOMonologue_Live_AfterShow`(기획 12~14컷 대사 6줄, 줄당 2.3초 ≈ 67.4초 종료)를 `MonologuePresenter`로 직접 띄운다. `UIManager.PlayMonologue`는 연출 중 거절하므로 쓰지 않는다. 영상이 끝나면 다음 연출 시작 때 UIManager가 독백을 끄므로 68초 안에 끝나게 맞췄다.
- 34번은 대사를 빼고 여운 샷 한 단계만 남겼다(영상 끝 → 여운 → 페이드 → 크레딧). 쓰지 않게 된 `SOCameraShot_Live_ScreenBack` 삭제.
- 크레딧: 명단이 올라가는 동안 E를 누르고 있으면 `_fastForwardMultiplier`(기본 2)배 속도. 로딩 영상 스킵과 같이 `Keyboard.current.eKey`를 직접 읽는다.
- 씬 저장 때 같이 바뀐 Canvas_InGame 레이아웃 값·Main Camera 위치는 HEAD 값으로 되돌렸다.
- 검증: 사용자가 직접 하기로 함. 크레딧 2배속 코드는 에디터가 Play 중이라 컴파일을 확인하지 못했다.

## 연출 수정 4 (2026-10-30, 사용자 요청)
- 요청: TV가 켜진 채 노래가 나오는 동안(68초 전) 엔딩 크레딧이 시작돼야 한다. 그래서 TV 밖으로 나오는 시각을 앞당긴다.
- `EndingCredits`를 "영상 끝 이벤트 → 34번 컷씬 → 여운 → 크레딧"에서 "33번 발판에서 Play → 영상이 `Start At Video Seconds`(63초)에 오면 크레딧"으로 바꿨다. 영상이 먼저 끝나면 그때 시작. 영상 끝(68초)에 스크린 연출이 끝나며 조작 막기가 풀리므로, 크레딧을 시작할 때 `InputMapController.RequestMap(this, Cutscene)`으로 직접 막는다(메인 메뉴로 가며 씬과 함께 정리).
- 타이밍: 나오는 시각 45초 → 독백 약 48.6~62.4초 → 63초 크레딧 페이드(2초) 시작 → 68초 노래 끝.
- 크레딧 화면 오른쪽 아래 안내 `E 누르고 있기 : 2배속`(`CreditsGroup/FastForwardHint`). 배수를 바꾸면 문구도 직접 고친다.
- 크레딧 명단: 플레이어 이세훈 / AI, 적 이정빈 / 맵 양준범 / 시스템 김성준 (사용자 지정).
- 34번 컷씬은 영상 중에는 33번 연출이 돌고 있어 재생할 수 없으므로 없앴다: `SOCutscene_Live_AfterShow`·표 34번 등록·`SOCameraShot_Live_Linger`·`CinematicCam_LiveLinger` 삭제, `LiveEndingScreen._onVideoFinished` 연결 제거.
- 검증: 컴파일 0에러. Play는 사용자가 직접 확인.

## 대사 수정 5 (2026-10-30, 사용자 요청)
- 29번 `SOCutscene_Lobby_ReturnSecurity` 대사 5줄을 사용자 대본(로비 공연 알림 발견 → 마지막 목적지) 12줄로 교체했다. "저기 봐", "라이브 앱이 켜졌어", "공연이라니…"(플레이어가 라이브 앱을 바라봄) 세 줄은 `Lobby_LivePortal` 카메라, 나머지는 말하는 사람 카메라.
- 대본의 무대 지시(전광판이 "공연 방송 준비 중..."으로 바뀜, 꺼져 있던 라이브 앱에 조명이 켜짐)는 `Map_1_Lobby` 작업이라 넣지 않았다. 지금은 로비에 들어오면 라이브 포탈 빛기둥·로고가 처음부터 켜져 있다(`NextDestinationMarker`).
- 대화가 끝나면 조작이 돌아오고, 라이브 포탈에서 E → 30번 입장 전 대화 → 라이브로 이동하는 기존 흐름은 그대로다.
- 검증: 에셋 다시 불러와 12줄·카메라 키 확인. 로비 실제 재생은 미확인.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
