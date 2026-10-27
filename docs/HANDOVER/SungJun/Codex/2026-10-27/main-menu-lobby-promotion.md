# 정식 MainScene 로비 메인화면 적용

## 작업 요약 및 방법
- 사용자 승인된 Test 메인화면을 Assets/_Project/Scenes/MainScene.unity에 적용했다.
- 기존 정식 씬의 루트6개와 UI 프리팹 연결을 보존하고, 로비 배경/페이드 Canvas/카메라 포즈를 추가했다. 기존 Directional Light는 비활성화하고 배경 프리팹의 로비 조명을 사용한다.
- 최초 씬 전체 교체 시도는 자동 승인 검토에서 거절되어 실행되지 않았다. 기존 구조 보존 방식으로 변경해 완료했다.

## 변경 파일 및 결정 사항
- Scripts/UI/MainMenu/MainMenuCameraTransition.cs: Test 전환 스크립트와 같은 동작을 정식 이름으로 추가.
- Prefabs/UI/MainMenu/LobbyMenuBackdrop.prefab: 로비 시각 오브젝트/군중/전시 플레이어를 하나로 묶은 공유 프리팹. Test Scene의 루트/컴포넌트 간 참조를 새 프리팹 내부 참조로 재연결했다. UI·게임 진행용 시스템은 포함하지 않는다.
- Art/UI/MainMenu/: MainMenuVolume.asset, MainMenuSplashBold.mat, MenuShade.png.
- Animations/MainMenu/MainMenuIdle.controller.
- MainScene의 기존 Menu/AnyPress 시각 속성을 조정하고 FullGameTitle/음영을 추가했다. 원래 Settings, AudioManager, GameManager와 버튼/설정/소리 연결은 유지한다.
- 첫 화면은 검은 배경 + FID / FRAGMENT IN DEVICE / PRESS ANY KEY TO CONTINUE. 입력 후0.75초 페이드 인과2.2초 카메라 이동, 도착 후0.25초 메뉴 표시.
- 시작 카메라(-75.96519,42.30582,102.87990), 최종(-42.2,.9,49)/회전(0,155,0)/FOV45.
- 기존 MainScene GUID 2e4bad56b69a4e24a8d73b484cebaa00 유지. 기존 빌드 등록을 그대로 사용한다.

## 검증 결과
- 컴파일 오류0개. 저장 후 MainScene 재오픈으로 참조 유지 확인.
- Test 의존성0개, Missing Script0개, 활성 EventSystem1개, GameManager1개.
- 검은 첫 화면과 최종 메뉴를 직접 캡처했다.
- 0.15초 간격17개 표본으로 검은 덮개1→0과 위치 이동, 이동 중 메뉴0/inputFalse, 도착 후 메뉴1/inputTrue 확인.
- 기존 SETTINGS 버튼 클릭으로 설정창 열림, ESC 입력으로 닫힘 확인.
- 시작 버튼과 원래 Presenter/SceneLoader 참조 유지. 저장 진행도에 영향을 줄 수 있는 실제 새 게임 시작은 실행하지 않았다.
- 마지막 Play Console 오류0개, 기존 렌더링 경고4개. 초기 중복 EventSystem 경고는 새 배경의 불필요 EventSystem 제거 후 재발하지 않는다.
- Test 원본 및 공유 Map_1_Lobby/Player 프리팹/ProjectSettings는 이번 작업에서 수정하지 않았다. 원래 존재하던 ProjectSettings/RT/솔루션 파일 작업 트리 변경은 건드리지 않았다.
- runInBackground 원래 false 복원. 커밋·푸시 없음.

## 현재 상태 및 다음 할 일
- 정식 MainScene을 연 상태로 종료했다. 사용자 화면 최종 확인.
- 실제 새 게임/이어하기 씬 전환과 기존 게임 전체 회귀는 미실행.
- 관련 intent: docs/intent/clear/intent-081-main-menu-lobby-promotion.md.
