---
id: intent-081
title: 승인된 Test 로비 메인화면을 정식 MainScene에 적용
part: UI / MainMenu
status: resolved
created: 2026-10-27
resolved: 2026-10-27
---

## 문제 (Problem)
검은 첫 화면, 영문 이름, 로비 페이드 인과 카메라 이동, 인도 위 플레이어 및 도착 후 메뉴 표시가 Test 씬에만 있다.

## 기대 결과 (Proposed outcome)
정식 MainScene에서도 승인된 Test 화면/연출이 동일하게 동작하고 Assets/Test에 의존하지 않는다.

## 영향 범위 (Affected users and systems)
Assets/_Project/Scenes/MainScene.unity, MainMenu 전환 스크립트와 전용 렌더링/애니메이션/UI 에셋. 기존 정식 매니저와 설정창/메뉴 소리 연결은 보존한다.

## 제약 (Constraints)
Test 원본은 유지하고 공유 에셋은 _Project 아래에 복사한다. 정식 MainScene GUID와 기존 Build Settings 등록은 유지한다. 원본 로비/공유 플레이어 프리팹/프로젝트 설정을 변경하지 않는다. 커밋/푸시하지 않는다.

## 열린 질문 (Open questions)
없음. 사용자가 정식 메인씬에 동일 적용을 요청했다.

## 검증
컴파일, Test 종속성0개, Missing Script0개, GUID 유지, 검은 첫 화면과 메뉴 영문 이름, 페이드/카메라 동시 이동, 도착 후 메뉴 표시를 확인한다. 기존 시작/설정 동작도 회귀 확인한다.

## 해결 기록 (Resolution)
- 후속 복구(2026-10-27): 삭제되어 있던 MenuShade.png, MainMenuSplashBold.mat, MainMenuVolume.asset을 Test 원본에서 정식 경로로 복원했다. 저장된 씬/프리팹의 기존 GUID를 그대로 복원하고 VolumeProfile 내부 자체 참조도 맞춰 재연결했다. 씬 재오픈 후 두 음영 이미지·모든 타이틀 머티리얼·후처리 프로필5개 컴포넌트의 연결 및 Test 의존성0개를 확인했다. Play에서 흰색 화면 덮임 해소, 메뉴 표시, Console 오류0개 확인. 새 코드 변경 없음.
- 기존 MainScene의 카메라·EventSystem·Canvas_MainMenu·AudioManager·GameManager를 보존했다. 기존 메뉴/스플래시 하위 오브젝트의 시각 속성만 변경하고 필요한 제목/음영을 추가했다. 기존 Settings 및 버튼/소리 연결은 유지한다.
- Test 전용 배경을 _Project/Prefabs/UI/MainMenu/LobbyMenuBackdrop.prefab으로 생성하고 전시 플레이어와 전체 로비 시각/군중 구성을 포함했다. 복사 과정에서 딸려온 EventSystem은 새 배경 프리팹에서 제거했다.
- MainMenuCameraTransition, VolumeProfile, Bold 머티리얼, MenuShade, Idle 컨트롤러를 _Project에 별도 생성/복사했다. Test 원본은 변경하지 않았다.
- 정식 씬 GUID 2e4bad56b69a4e24a8d73b484cebaa00 유지. Build Settings/원본 로비/공유 플레이어 프리팹 수정 없음.
- 재컴파일 오류0개, 정식 씬 재오픈 후 Test 의존성0개·Missing Script0개·EventSystem1개 확인.
- MainScene Play에서 검은 첫 화면과 영문 이름 확인, Space 입력 후0.75초 페이드/2.2초 이동과 도착 후 메뉴 표시를 표본 기록으로 확인했다.
- 설정 버튼으로 기존 설정창이 열리고 ESC로 닫힘을 확인했다. 기존 시작 버튼과 Presenter/SceneLoader 연결은 유지하며, 저장 진행도를 변경하는 실제 새 게임 시작은 실행하지 않았다.
- 마지막 Play Console 오류0개, 알려진 렌더링 경고4개. 초기 검증 중 발생한 중복 EventSystem 경고는 수정 후 재발하지 않는다.
- 최초 씬 전체 교체 시도는 자동 승인 검토에서 거절되어 실행되지 않았다. 최종 변경은 기존 구조를 보존하는 추가/속성 변경 방식으로 수행했다.
- 커밋·푸시 없음. 기록: docs/HANDOVER/SungJun/Codex/2026-10-27/main-menu-lobby-promotion.md.
