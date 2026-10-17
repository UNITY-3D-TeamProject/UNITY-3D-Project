# 로비 참고 이미지 HUD 시안 (2026-10-16)

## 작업 요약
- `Map_1_Lobby`에 독립 `Canvas_ReferenceHud`를 추가했다.
- 참고 이미지에 맞춰 중앙 열(28%)·배터리(61%) 반원과 조준점, 왼쪽 아래 HP(85%) 빨간 8칸 바, 하단 Scan·Roll·Bluetooth 스킬 3개를 배치했다.

## 방법/접근
- Test MainScene의 `Canvas_HudReference (1)`를 로비 씬에 복사한 뒤 색·크기·배치를 조정했다.
- 스킬판과 하트판은 큰 모서리 컷 프레임 스프라이트로 만들고 Bluetooth 아이콘은 UI 선분으로 직접 구성했다.
- 화면 겹침을 막기 위해 기존 `Canvas/PlayerHudRoot`에 CanvasGroup(alpha 0)을 추가하고 기존 `Canvas/CrossHair`를 비활성화했다. 새 Canvas의 GraphicRaycaster는 꺼서 입력을 가로채지 않게 했다.

## 변경된 파일
- `Assets/_Project/Scenes/Map_1_Lobby.unity`
- `docs/intent/clear/intent-026-lobby-hud-reference.md`
- `docs/intent/INTENT_POINTER.md`
- 이 작업 기록과 Codex HANDOFF_POINTER.

## 결정 사항
- 이 Canvas는 시각 비교용 정적 시안이며 실제 HP·스킬 쿨타임 데이터와 연결하지 않았다.
- Test MainScene은 복사용으로만 열고 저장 없이 닫았다.

## 현재 상태 및 이슈
- 1448×1086 Game View Play 캡처에서 구성과 겹침 해소를 확인했다. Play Mode는 종료했고 로비 씬은 저장했다.
- 새 Canvas가 참조하는 UI 스프라이트·폰트 16개는 `Assets/ThirdParty/`의 로컬 에셋이다. 팀 저장소에는 이 에셋들이 포함되지 않으므로 다른 환경에서 동일하게 표시하려면 해당 패키지가 필요하다.
- 기존 `ProjectSettings/EditorBuildSettings.asset`와 Test 저장소 MainScene 변경은 작업 시작 전부터 있었으며 건드리지 않았다.

## 다음 할 일
- 이 시안을 실제 HUD로 채택하면 PlayerFacade 값과 쿨타임 데이터 연결, ThirdParty 에셋 공유 방식 결정이 필요하다.

## 후속 변경
- 같은 날 요청으로 Canvas_ReferenceHud를 Test MainScene으로 옮겼다. 로비에 새 Canvas는 남지 않으며 기존 HUD 숨김 조치는 복구했다. 자세한 내용은 main-scene-canvas-prefabs.md를 참고한다.
