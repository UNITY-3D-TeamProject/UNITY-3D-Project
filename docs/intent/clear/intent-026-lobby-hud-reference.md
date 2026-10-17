---
id: intent-026
title: 로비 참고 이미지 HUD 시안
part: UI / Lobby
status: resolved
created: 2026-10-16
resolved: 2026-10-16
---

## 문제 (Problem)
현재 로비 씬에는 첨부 이미지의 HP, 중앙 열·배터리 게이지, 하단 스킬 버튼 구성을 비교할 별도 HUD 시안이 없다.

## 기대 결과 (Proposed outcome)
`Map_1_Lobby`에 독립 Canvas를 추가해 참고 이미지와 유사한 위치·색·형태의 정적 HUD 시안을 표시한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 로비 HUD를 검토하는 개발자와 디자이너.
- 어떤 시스템/모듈이 건드려지는가: `Map_1_Lobby` 씬의 새 Canvas 오브젝트와 작업 기록.

## 제약 (Constraints)
- 기존 Canvas와 HUD 기능 연결은 변경하지 않는다.
- UI는 시각 비교용 정적 시안으로 만들고, 로직·입력 처리 코드는 추가하지 않는다.
- 기존 UI 에셋을 재사용한다.

## 열린 질문 (Open questions)
- 없음. 현재 열린 로비 씬에 독립 시안을 만들고 이미지에 보이는 수치를 표시한다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: Map_1_Lobby에 독립 Canvas_ReferenceHud를 추가했다. HP 85%, 열 28%, 배터리 61%, Scan/Roll/Bluetooth를 첨부 이미지의 배치와 색에 맞춘 정적 시안으로 만들었다. 기존 PlayerHudRoot는 CanvasGroup alpha 0, 기존 CrossHair는 비활성화해 화면 겹침을 없앴다. 새 Canvas의 GraphicRaycaster는 비활성화했다.
- 관련 커밋/PR: 없음 (로컬 변경, 미커밋). 사용한 UI 스프라이트·폰트 16개는 로컬 ThirdParty 에셋 참조이므로 공유 환경에서 해당 에셋 설치가 필요하다.
- 후속 변경: intent-027에서 Canvas_ReferenceHud를 Test MainScene으로 옮기고 로비의 기존 HUD 표시를 복구했다. 현재 시안은 Test MainScene 프리팹에 있다.
