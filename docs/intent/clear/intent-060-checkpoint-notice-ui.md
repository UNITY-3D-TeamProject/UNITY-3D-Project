---
id: intent-060
title: 체크포인트 도달 시 화면 왼쪽에 알림 표시 (라운드 체크포인트와 구분)
part: UI / Stage
status: resolved   # open | resolved
created: 2026-10-22
resolved: 2026-10-22 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
체크포인트에 닿아도 화면에 아무 표시가 없어, 지금 복귀 지점이 바뀌었는지 플레이어가 알 수 없다.
게다가 체크포인트는 두 종류라 의미가 다르다.
- 라운드 체크포인트: 사망하면 여기서 다시 시작한다 (낙하 복귀도 여기).
- 일반 체크포인트: 떨어지면 여기로 돌아온다 (사망하면 라운드 시작점으로 돌아간다).

## 기대 결과 (Proposed outcome)
- 새 체크포인트에 도달하면 화면 왼쪽(가운데 높이)에 알림 패널이 잠깐 떴다가 사라진다.
- 라운드 체크포인트와 일반 체크포인트는 제목·설명·색이 다르다.
  - 라운드: 주황색, "라운드 체크포인트" / "사망하면 이곳에서 다시 시작합니다."
  - 일반: 하늘색, "체크포인트 활성화" / "떨어지면 이곳으로 돌아옵니다."
- 이미 지난 체크포인트를 다시 밟거나, 낙하 복귀로 같은 체크포인트에 다시 닿을 때는 띄우지 않는다.
- 스테이지 처음 스폰 지점(1라운드 시작점)은 알림이 없다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어
- 어떤 시스템/모듈이 건드려지는가:
  - `Core/Stage/StageManager` — 체크포인트 도달 이벤트(`OnCheckpointReached(bool isRoundCheckpoint)`) 추가
  - `UI/Checkpoint/CheckpointNoticeView`·`CheckpointNoticePresenter` 새로 추가
  - `Prefabs/UI/Canvas_InGame` — `CheckpointNotice` 패널 추가

## 제약 (Constraints)
- 지켜야 할 정책: 기존 체크포인트 진행·라운드 저장 로직은 바꾸지 않는다. 이벤트만 추가한다.
- 쓰지 말아야 할 것: 새 ThirdParty 스프라이트(Git에 안 올라감). 이미 프리팹이 쓰는 `Cut Frame Filled`·`Cut Frame Glow`·`Glow Mote`만 쓴다.
- 대화 중에는 `Canvas_InGame` 루트 CanvasGroup으로 다른 인게임 UI와 함께 숨겨진다.

## 열린 질문 (Open questions)
- 문구·색·표시 시간(기본 2.5초)은 Inspector에서 바꿀 수 있게 두었다. 최종 값은 사용자 확인.
- 페이드 인/아웃 연출은 넣지 않았다(켜고 끄기만). 필요하면 추가.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `StageManager.OnCheckpointReached(bool)` 이벤트 + `UI.Checkpoint.CheckpointNoticePresenter/View`, `Canvas_InGame/CheckpointNotice`(왼쪽 가운데, 420×96, 2.5초). 문구·색·시간은 Presenter Inspector에서 조정. TestScene Play에서 일반(하늘색)·라운드(주황색) 표시, 자동 숨김, 지난 체크포인트 재접촉 시 미표시 확인.
- 관련 커밋/PR: feature/system/various-fix
