# 엔딩 TV 줌 연출이 "사라지던" 문제 — intent-096 연출 수정 7

## 작업 요약 및 방법
- 사용자 요청: TV 영상 몇 초 → 화면 안으로 들어가 전체화면 → 끝날 무렵 TV 밖으로 나와 폰 주인과 펫 → 대화 → 엔딩. 전체화면 시작·종료 시각을 인스펙터에서 조정. "전에 만든 게 사라졌다".
- 조사: `UIandCinematic/CameraShot/ScreenVideoZoom.cs`, `Map_5_Live`의 `Canvas_EndingCredits/FullscreenVideo` 컴포넌트, `CinematicCam_LiveScreenZoom`, 33번 발판의 `ScreenVideoZoom.Play` 연결 모두 커밋에 남아 있었다.
- 사용자 Play 중 런타임을 읽기만 해서 확인: 영상 8.3초에서 `isPaused = true`, `Application.runInBackground = false`, 연출은 이미 TV 밖(`CinematicCam_LiveScreenBack`)으로 나와 35번 대화 진행 중.
- 원인: 창 포커스를 잃으면 VideoPlayer가 자동 일시정지 → `ScreenVideoZoom`이 `isPlaying == false`를 영상 끝으로 보고 전체화면을 곧바로 걷어 냄.
- 수정: `ScreenVideoZoom.IsVideoRunning()`(`isPlaying || isPaused`)을 추가해 들어가기 전 대기·전체화면 유지 조건에 사용. 두 시각 필드 툴팁을 "전체화면 시작 시각 / 전체화면 종료 시각(폰 주인과 펫)"으로 정리.

## 결정 사항
- 씬·필드 값은 건드리지 않았다(현재 `Enter At Video Seconds` 5, `Exit At Video Seconds` 45).
- `LiveEndingScreen`(양준범)은 수정하지 않았다. 영상 끝 판단은 원래 `loopPointReached`라 일시정지 영향 없음.

## 현재 상태 및 이슈
- 리컴파일·Play 미확인(사용자 Play 중이라 강제 리컴파일 안 함). Play를 멈추면 자동 컴파일된다.
- 빌드에서도 Alt+Tab 등으로 창을 벗어나면 영상이 일시정지된다(이제 연출은 기다렸다가 이어진다).
- 커밋 안 함.

## 다음 할 일 (Next Steps)
- Play로 TV → 전체화면(5초) → 45초에 TV 밖 → 35번 대화 → 크레딧 흐름 확인, 중간에 창을 벗어났다 돌아와도 전체화면이 유지되는지 확인.
- 필요하면 `Canvas_EndingCredits/FullscreenVideo`의 `ScreenVideoZoom` 인스펙터에서 두 시각 조정.

## 추가 작업: E 길게 눌러 노래 영상 스킵 (같은 날, 사용자 요청)
- `ScreenVideoZoom`에 `Skip Hold Seconds`(기본 1.5초)·`IsSkipHoldComplete()`·`SkipVideoToExit()` 추가. 영상 중 E를 그만큼 누르면 영상을 `Exit At Video Seconds`(45초) 위치로 넘기고 바로 TV 밖으로 나와 35번 대화로 이어진다.
- 스킵 안내 UI(예: "E 길게 누르기 : 건너뛰기")는 아직 없다. 넣으려면 `Map_5_Live` 캔버스에 UI를 추가해야 한다.
- 리컴파일·Play 미확인, 커밋 안 함.

## 추가 작업 2: 스킵 안내 UI (같은 날, 사용자 요청)
- `ScreenVideoZoom._skipPrompt`(GameObject) 추가: 영상이 나오기 시작하면 켜고, TV 밖으로 나가기 시작할 때(스킵·나오는 시각·영상 정지) 끈다. `Awake`에서 꺼 둔다.
- `Map_5_Live`(사용자 지시): `Canvas_EndingCredits/SkipPrompt`를 크레딧 `FastForwardHint`에서 복사해 만들고(오른쪽 아래 (-60, 40), 600×60, 같은 폰트·색·28pt) 문구 "E 누르고 있기 : 건너뛰기", 꺼 둔 채 연결·저장. `FullscreenVideo`보다 뒤 형제라 전체화면 위에 보인다.
- 저장 때 Cinemachine이 에디터에서 옮겨 둔 `Main Camera` 프리팹 인스턴스 위치·회전 7줄이 함께 바뀌어 원래 값으로 되돌렸다. 참조 없는 `MonologuePresenter` stripped 항목 1개는 Unity가 저장 때 자동 정리했다(예전 독백 연결 흔적, 영향 없음).
- 검증: 리컴파일 에러 0, 씬 다시 불러와 연결·문구 확인. Play는 에디터 루프가 1프레임에서 멈춰(포커스 줘도 동일) 안내 표시·스킵 동작은 직접 확인 필요. 커밋 안 함.

## 추가 작업 3: 스킵 안내를 로딩 컷씬 모양 + 원형 게이지로 교체 (같은 날, 사용자 요청)
- 추가 작업 2의 글자만 있는 `SkipPrompt`를 지우고, 로딩 컷씬 `Loading.unity`의 `VideoSkipPrompt`와 같은 값으로 `Canvas_EndingCredits/VideoSkipPrompt`를 만들었다(로딩 씬은 열지 않고 파일 값만 읽음). 두 캔버스 모두 기준 해상도 1920×1080.
  - 패널 150×60, 오른쪽 아래 (-107, 62), 색 (0.015, 0.025, 0.045, 0.8)
  - `SkipRingTrack`(링 스프라이트 `VideoSkipRing`, 흰색 25%) / `SkipRingFill`(같은 스프라이트, 하늘색, Filled Radial360·위에서 시계 방향)
  - `SkipLabel` "E 꾹 누르기\n건너뛰기", Pretendard-Bold SDF 16pt, 왼쪽 정렬
- `ScreenVideoZoom._skipProgress`(Image) 추가: `IsSkipHoldComplete()`가 누른 시간 비율로 `fillAmount`를 채우고, 떼면 0.
- 저장 때 다시 바뀐 `Main Camera` 프리팹 인스턴스 7줄은 원래 값으로 되돌렸다.
- 검증: 리컴파일 에러 0, 편집 모드 임시 렌더(캔버스를 잠시 카메라 모드로, 저장 안 함)로 모양·게이지 60% 확인. Play는 33번 대사 도중 에디터 루프가 멈춰 실제 E 스킵·게이지 차는 모습은 직접 확인 필요. 커밋 안 함.

## 추가 작업 4: 대화 끝 → 천천히 페이드 아웃 → 크레딧 (같은 날, 사용자 요청)
- 원인: 35번 대화 컷씬이 끝나는 순간 카메라가 플레이어로 돌아가고 HUD가 다시 뜬 뒤에야 크레딧이 2초 페이드로 덮였다. 소리도 즉시 전환.
- 35번 컷씬 끝에 카메라 단계 `SOCameraShot_Live_EndingHold`(`Live_ScreenBack` 3600초) 추가 → 대화가 끝나도 TV 뒷모습 화면 유지.
- `ScreenVideoZoom`이 그 마지막 카메라 단계 시작(`CinematicManager.OnCutsceneStepStarted`)을 대화 끝으로 보고 크레딧 시작.
- `EndingCredits`: 검게 어두워짐(3초, 영상 소리 같이 줄어듦) → 검은 화면에서 무대·명단 드러남 + 노래 커짐(`_revealSeconds` 2초). 덮개 `FadeOutCover`를 시작·끝 모두에 쓴다. 씬 구조 변경 없음.
- 씬 저장 때 함께 바뀐 `Canvas_InGame` 프리팹 인스턴스 레이아웃 값 71줄·`CreditsRoll` 높이 1줄(에디터 재계산 값)과 `Main Camera` 7줄은 원래 값으로 되돌렸다. 씬에 남은 변경은 스킵 안내·필드 연결·크레딧 시간(3/2초)·찌꺼기 항목 정리뿐.
- 검증: 리컴파일 에러 0, 디스크 버전 다시 열어 값 확인. Play는 2프레임에서 멈춰 실제 페이드·소리 전환은 직접 확인 필요. 커밋 안 함.
- 커밋 시 `SOCameraShot_Live_EndingHold.asset`+`.meta`, `SOCutscene_Live_AfterShow.asset` 포함. RenderTexture 2개·Bold 폰트·ShaderGraphSettings는 제외.
