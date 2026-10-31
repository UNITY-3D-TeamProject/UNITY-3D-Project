# 엔딩 컷씬 (보스 처치 → 라이브 앱 TV 공연 → 크레딧) — intent-096

## 작업 요약 및 방법
- 기획안 `FID_엔딩_컷씬_기획안.pdf`(16컷)를 기존 컷씬 28~33번에 대응시키고, 새로 필요한 34번 마무리 컷씬과 크레딧만 추가했다. 설계는 [intent-096](../../../intent/intent-096-ending-cutscene.md).
- 대사 교체(SO): 28 `Security_Clear`(소멸 → 보안 앱 정상화·곡 무사), 29 `Lobby_ReturnSecurity`(전광판 LIVE 알림, 2번째 줄 `Lobby_LivePortal` 카메라 유지), 30 `Lobby_BeforeLive`, 31 `Live_Enter`, 32 `Live_BeforeScreen`.
- 신규 SO: `SOCutscene_Live_AfterShow`(34번, 표 등록) = [뒷모습 샷] → 기획안 대사 6줄(카메라 키 `Live_ScreenBack`) → [여운 샷]. 카메라샷 `SOCameraShot_Live_ScreenBack`, `SOCameraShot_Live_Linger`.
- 코드: `Core/Audio/AudioManager.StopBgm(float)`, 신규 `UI/Credits/EndingCredits.cs`.
- `Map_5_Live`(사용자 허락): 스크린 VideoPlayer 클립 → `ThirdParty/Audio/CutScene/4_Ending.mp4`, 오디오 Direct, 카메라 2개, `Canvas_EndingCredits`, `LiveEndingScreen._onVideoFinished` → `EndingCredits.Play`, 33번 트리거에 `AudioManager.StopBgm(2)` 추가.

## 결정 사항
- `Map/Live/LiveEndingScreen.cs`(양준범)는 고치지 않고 UnityEvent 연결만 했다.
- 크레딧 동안 34번 컷씬의 여운 샷(3600초)을 계속 재생해 조작 막기·HUD 숨김을 컷씬에 맡긴다. 끝나면 `GameManager.ReturnToMainMenu()`(설정창 버튼과 같은 경로, 커서·시간 정리 포함).
- 영상 오디오는 Direct 출력(로딩 영상과 같음). AudioSource 출력은 버퍼 넘침으로 디코딩이 멈췄다.
- 영상 파일은 원본(세로 824×1464, 10fps)이 Windows 디코더에서 멈춰, ffmpeg로 1920×1080(양옆 검정)·30fps·H.264 Baseline으로 다시 인코딩하고 Unity 임포트에서 VP8 트랜스코딩을 켰다. 원본은 `Downloads`에 그대로 있다.

## 현재 상태 및 이슈
- `Map_5_Live` 단독 Play로 33번 트리거 → BGM 정지 → 영상 재생 → (끝으로 넘김) → 34번 대사 6줄 → 여운 → 크레딧 → `MainScene`(커서 해제) 흐름 확인, 에러 0.
- 미확인: 영상 소리 청취, 28~32번 대사의 보안·로비 씬 실제 재생, 3분 영상 전체 재생, 전체 게임 흐름(보안 → 로비 → 라이브).
- `DialogueBoxes`(31번 입장 대화 트리거, 양준범 커밋 `f0d88e62`에서 꺼짐)를 사용자 지시로 켰다. Play에서 31번 대사·전경 샷·조작 복귀·32번 트리거 연결 확인. 양준범 님에게 켰다는 사실을 공유할 것.
- ThirdParty는 Git 제외: `4_Ending.mp4`와 `.meta`(GUID `3da896f593430c7468572868443a314e`)를 함께 공유해야 한다.
- 34번 대사 중 펫은 기존 연출대로 플레이어 앞에서 플레이어를 바라본다(기획의 "두 캐릭터 뒷모습"과 다름).
- 크레딧 명단은 `[임시] 이름`. 메인 메뉴의 이어하기는 라이브 단계로 저장된다(설정창 "메인 화면으로"와 같은 동작).
- 커밋 안 함.

## 다음 할 일 (Next Steps)
- 실제 제작진 명단을 `Canvas_EndingCredits/CreditsGroup/CreditsRoll` 텍스트에 입력.
- 공식 `4_Ending.mp4` 기준으로 `ScreenVideoZoom`의 들어가는/나오는 영상 시각(기본 4초·63초)을 직접 보며 조정.
- 대화 → 크레딧 → 메인 메뉴 구간을 새 연출 뒤에 한 번 더 확인.
- 보안 → 로비 → 라이브 전체 흐름 직접 플레이로 대사·카메라 확인.

## 추가 작업: TV → 화면 안 → 전체화면 → TV 밖 연출 (같은 날, 사용자 요청)
- 영상을 SVN 공식 `ThirdParty/Audio/CutScene/4_Ending.mp4`(68초, GUID `d15ef7e1…`)로 교체했다. 앞서 넣은 재인코딩본은 SVN 업데이트로 덮어써져 폐기.
- 신규 `UIandCinematic/CameraShot/ScreenVideoZoom.cs`(`Canvas_EndingCredits/FullscreenVideo`): 인스펙터의 `Enter At Video Seconds`(기본 4)에 줌 카메라로 들어가 전체화면, `Exit At Video Seconds`(기본 63)에 전체화면을 걷고 뒷모습 카메라로 나온다. 33번 발판 이벤트에서 `Play`.
- 카메라 `CinematicCam_LiveScreenZoom` 추가, `SOCameraShot_Live_ScreenBack` 4초 → 1초.
- Play 확인: TV → 줌·전체화면 → 63초 복귀 → 34번 첫 대사까지. 대화 → 크레딧 구간은 에디터 플레이 루프 정지(포커스)로 재확인 못 함(코드 변경 없음).
- 주의: SVN 업데이트로 Pretendard SDF 폰트 3개가 충돌 상태다(TortoiseSVN Revert로 해결 필요). 충돌 표시가 남은 동안 폰트가 깨질 수 있다.

## 추가 작업 2: 입장 거리 단축·대화 정리·뒷모습 컷 (같은 날, 사용자 요청)
- 스폰·체크포인트를 z=-15.3(`Pool_03`)으로 옮겨 TV 앞까지 약 6초(이동 속도 6m/s). 31번 트리거도 입장 지점으로 옮겨 입장 직후 재생.
- 32번 "시작한다" 트리거 삭제.
- 33번 연출 두 번째 샷과 `LiveEndingScreen._screenCamera`를 뒷모습 카메라(`Live_ScreenBack`)로 바꿔, TV를 보는 플레이어 컷에서 영상이 시작되게 함. 들어가는 시각 5초.
- Play 미확인: 에디터 플레이 루프가 1프레임에서 멈추는 현상 때문에 직접 플레이로 확인 필요.

## 추가 작업 3: 영상 중 독백·크레딧 2배속 (같은 날, 사용자 요청)
- 나오는 시각 50초(사용자 설정). TV 밖 뒷모습 컷에 도착하면 `ScreenVideoZoom`이 독백 `SOMonologue_Live_AfterShow`(6줄 × 2.3초)를 `MonologuePresenter`로 직접 띄워 68초 전에 끝남.
- 34번 컷씬은 여운 샷만 남김(대사는 독백으로 이동). `SOCameraShot_Live_ScreenBack` 삭제.
- 크레딧 명단 스크롤 중 E를 누르고 있으면 2배속(`EndingCredits._fastForwardMultiplier`).
- 검증은 사용자가 직접. 2배속 코드는 컴파일 미확인(에디터 Play 중), 커밋 안 함.

## 추가 작업 4: 노래가 끝나기 전 크레딧 시작·2배속 안내 (같은 날, 사용자 요청)
- `EndingCredits`가 영상 재생 위치 `Start At Video Seconds`(63초)에 크레딧을 시작(33번 발판에서 Play, 영상 끝 이벤트 연결 제거). 크레딧 시작 때 Cutscene 입력 맵을 직접 요청해 영상 끝 뒤에도 조작을 막음.
- TV 밖으로 나오는 시각 45초(독백 약 48.6~62.4초).
- 크레딧 오른쪽 아래 `E 누르고 있기 : 2배속` 안내.
- 34번 컷씬·여운 샷 SO·여운 카메라 삭제(영상 중에는 재생 불가해 안 씀).
- 컴파일 0에러, Play 검증은 사용자. 커밋 안 함. Play로 바뀐 Bold 폰트·RenderTexture·ShaderGraphSettings는 이번 작업과 무관하니 커밋에서 뺄 것.
- 크레딧 명단을 사용자가 준 4개 파트로 교체: 플레이어 이세훈 / AI, 적 이정빈 / 맵 양준범 / 시스템 김성준.

## 추가 작업 5: 로딩 화면 달리는 플레이어·펫 (intent-097)
- `Loading.unity`에 모습 전용 플레이어·펫 복사본(달리기 반복 컨트롤러)과 RT 카메라를 y=-2000에 두고, `ProgressGroup/Runners` RawImage로 화면 가운데 표시. 코드 변경 없음. [intent-097](../../../intent/intent-097-loading-runners.md)
- 카메라 렌더 미리보기로 구도만 확인, 실제 씬 전환 화면은 사용자 확인 필요. 커밋 안 함.
- 사용자 요청으로 캐릭터 화면 2.5배(RawImage 2400×1200, RT 2048×1024).
- 캐릭터 영역 중심을 화면 정중앙에 맞춤(Runners 위치 (-74, -59)).
