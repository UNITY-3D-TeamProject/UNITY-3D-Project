---
id: intent-030
title: Shift 고유 연출을 입힌 HUD 비교용 Canvas
part: UI / Test Scene
status: resolved
created: 2026-10-16
resolved: 2026-10-16
---

## 문제 (Problem)
사용자는 Shift - Complete Sci-Fi UI 에셋이 더 고급스러워 보여서, 스킬 패널·일반 패널을 이 에셋 위주로 만든 버전을 보고 싶어 한다.
그런데 지금 켜져 있는 MainScene `Canvas`의 패널은 이미 Shift 스프라이트(`Cut Frame Filled` + `Cut Frame Glow - 3px` 한 겹)를 쓰고 있어서, 스프라이트만 바꾸면 화면이 달라지지 않는다.
Shift가 아닌 것은 아이콘 6종(Shift에 대응 아이콘 없음)과 꺼져 있는 예전 패널(독백·안내·예전 슬라이더)뿐이다.

## 기대 결과 (Proposed outcome)
`Canvas`를 복제한 `Canvas_Shift`에서 패널을 Shift 데모(Quick Match 버튼)처럼 여러 겹으로 꾸민다.
- 겹: 아래로 번진 그림자(`Cut Frame Filled Glow Big`) + 시안 번짐 + Big 프레임 바탕(`Cut Frame Filled Big`) 안에 그라데이션(`Gradient Background 3`) + Big 글로우 테두리 + 코너 글로우.
- 대상: HP 패널, 스킬 카드 3장(키 배지 포함), 미션 패널, 독백 패널, 안내 패널, 설정창 Window.
- 그 밖에 Shift가 아닌 스프라이트·Legacy 글꼴은 Shift 것으로 교체(꺼진 패널 포함). 아이콘 6종은 대응 아이콘이 없어 그대로 둔다.
- 비교를 위해 `Canvas_Shift`를 켜고 원본 `Canvas`는 끈다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: HUD 시안을 비교하는 개발자(성준).
- 어떤 시스템/모듈이 건드려지는가: Test MainScene, Test `UnityAssets/Prefabs/UI/MainScene/Canvas_Shift.prefab`(새 파일).

## 제약 (Constraints)
- 배치·크기·아이콘·글자 내용은 원본과 같게 두고 패널 모양만 바꾼다.
- 원본 `Canvas`와 `Canvas.prefab`은 수정하지 않는다(활성 상태만 끔).
- 커밋·Push는 요청이 있을 때만.

## 열린 질문 (Open questions)
- 없음 (사용자가 "Shift 고유 연출 입히기"를 선택).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `Canvas`를 복제한 `Canvas_Shift`의 패널 11개(HP·미션·독백·안내·설정창·스킬 카드 3장·키 배지 3개)에 그림자 + 시안 번짐 + `Cut Frame Filled Big` 안 `Gradient Background 3` + `Cut Frame Glow Big - 3px` 테두리를 넣었다. 나머지 Shift 아닌 스프라이트·Legacy 글꼴 교체, 아이콘 6종은 대응 아이콘이 없어 유지. Test `Canvas_Shift.prefab` 저장, 원본 `Canvas`는 끔. MainScene 저장(사용자 요청).
- 관련 커밋/PR: 없음 (미커밋). 작업 기록: [shift-style-hud-canvas](../../HANDOVER/SungJun/2026-10-16/shift-style-hud-canvas.md)
