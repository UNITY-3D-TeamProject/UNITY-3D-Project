---
id: intent-090
title: 로딩 컷씬에 튜토리얼 전·피드 후·파일 후 영상 연결
part: SceneTransition / Video
status: open
created: 2026-10-30
resolved: null
---

## 문제 (Problem)
로딩 컷씬 데이터에는 튜토리얼→로비 임시 영상 하나만 있고, 메인 메뉴→튜토리얼 시작과 피드·파일 앱 종료 후 영상이 연결되지 않았다.

## 기대 결과 (Proposed outcome)
- 영상 오른쪽 아래 원형 게이지를 표시하고 E를 1.5초 유지하면 스킵한다. 손을 떼면 초기화하고, 영상 진입 전에 누르고 있던 키는 한 번 뗀 후부터 받는다.
- 새 게임 시작 후 튜토리얼이 열리기 전에 `1_BeforeTutorial.mp4`를 재생한다.
- 피드앱→로비에서 `2_AfterFeedApp.mp4`를 재생한다.
- 파일앱→로비에서 `3_AfterFileApp.mp4`를 재생한다.

## 영향 범위 (Affected users and systems)
- 게임 진행 중 로딩 화면을 보는 플레이어.
- 로딩 컷씬 SO·LoadingScreen·SceneLoader와 새 VideoClip 에셋.

## 제약 (Constraints)
- 기존 로딩 화면의 VideoPlayer 재생 방식을 사용한다.
- `4_Ending.mp4`는 이번에 복사하거나 연결하지 않는다.
- 영상 파일은 사용자 지정 위치 `Assets/ThirdParty/Audio/CutScene/`에 둔다 (GUID 유지, ThirdParty는 사용자가 직접 공유).
- 임시 영상의 로딩 컷씬 등록은 새 3개 등록으로 교체한다.

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution)
- 영상 스킵: `LoadingScreen`에서 E 1.5초 유지 시 영상을 정지한 뒤 기존 목적지 활성화 경로로 진행한다. 키를 떼면 초기화하고, 진입 전 누르고 있던 키는 해제 후부터 받는다.
- `Loading.unity` 오른쪽 아래에 어두운 배경·원형 트랙·시계 방향 청록색 게이지·한국어 안내를 연결했다. 안내는 영상 재생 중에만 표시한다.
- Unity 리컴파일 오류 0건. 프리뷰 씬에서 UI 참조·Radial360·초기 비활성·폰트/머티리얼·1초 설정을 확인했다. 실제 영상에서 키 유지·청취·육안 확인은 남아 있다. 기존 영상 3개 전환 검증도 남아 있어 이 intent는 open 유지.
