---
id: intent-042
title: 로비 하늘 전광판 RT 카메라 4대의 렌더 비용 줄이기
part: Map / Lobby / Rendering
status: open   # open | resolved
created: 2026-10-17
resolved: null
---

## 문제 (Problem)
- 로비 하늘 전광판 4개(시계·알림·음악·날씨)가 각자 카메라로 RenderTexture에 매 프레임 그린다. 메인 카메라까지 합치면 매 프레임 장면을 5번 그린다.
- 전광판 카메라가 기본 Renderer(`PC_Renderer`)를 같이 써서, 그림자도 없는 UI 화면 4개에 SSAO가 따로 돈다.
- 측정(에디터, [2026-10-17 측정 기록](../HANDOVER/JunBeom/2026-10-17/lobby-fps-factors.md)): 카메라 4대를 끄면 55 → 74fps(GPU 9.5 → 2.5ms, PlayerLoop −3.9ms). SSAO만 끄면 +6fps이고, 그 비용의 대부분이 전광판 카메라에서 나온다.

## 기대 결과 (Proposed outcome)
1. 전광판 카메라는 SSAO 같은 Renderer 기능이 없는 전용 Renderer(`SkySign_Renderer`)로 그린다.
2. 전광판 화면은 매 프레임이 아니라 일정 간격(기본 초당 15회)으로 갱신한다. 4대를 번갈아 그려서 한 프레임에 카메라 1대까지만 그린다.
3. 같은 방식으로 다시 측정해 기준보다 fps가 오른 것을 확인하고, 화면 움직임이 눈에 띄게 끊기지 않는지 확인한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 로비에 들어오는 모든 플레이어
- 어떤 시스템/모듈이 건드려지는가:
  - `Assets/Settings/PC_RPAsset.asset`, `Mobile_RPAsset.asset`: Renderer 목록에 1번(`SkySign_Renderer`) 추가. 기본 Renderer는 0번 그대로라 다른 카메라는 영향이 없다.
  - `Assets/_Project/Settings/SkySign_Renderer.asset`: 신규(`Mobile_Renderer` 복사본, Forward, 기능 없음)
  - `Prefabs/Lobby/SkySign/SkySign_*.prefab` 4개: `ScreenCamera` Renderer 1번
  - `Scripts/Map/Lobby/SkySign/SkySignCameraScheduler.cs`: 신규, `Map_1_Lobby`의 `/Sign`(카메라 4대의 부모)에 부착

## 제약 (Constraints)
- 지켜야 할 정책: 공용 URP 에셋은 Renderer를 추가만 하고 기존 값은 바꾸지 않는다.
- 쓰지 말아야 할 것: 전광판 구조를 World Space 캔버스로 바꾸는 큰 변경(수정 방향 ③)은 이번 범위가 아니다.

## 열린 질문 (Open questions)
- 갱신 빈도 기본값 15Hz가 시각적으로 괜찮은지(흐르는 제목·이퀄라이저). Inspector에서 조정할 수 있게 둔다. → 사용자 육안 확인

## 진행 기록
- 2026-10-17: 기대 결과 1·2를 적용했다. 에디터에서 3회 번갈아 측정한 결과 이전 상태 약 52fps → 적용 후 약 64fps(GPU 9.0 → 5.1ms, PlayerLoop 9.8 → 6.6ms). 전광판마다 초당 약 14.7회 갱신되는 것을 확인했고, RT 캡처로 화면 내용도 정상인 것을 확인했다. 15Hz 육안 확인은 남아 있다. 상세: [작업 기록](../HANDOVER/JunBeom/2026-10-17/lobby-skysign-camera-cost.md)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
