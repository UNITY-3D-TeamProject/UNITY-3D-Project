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
