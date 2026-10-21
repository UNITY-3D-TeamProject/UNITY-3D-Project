---
id: intent-053
title: 피드 앱 라운드 진행도를 Active Mission(InfoPanel)에 표시
part: UI / Stage / FeedApp
status: open   # open | resolved
created: 2026-10-21
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
피드 앱 1라운드에서 재료 아이템을 몇 개 모았는지, 2라운드에서 피드를 몇 개 분류했는지 화면에서 알 수 없다.
지금은 2라운드 중앙에 떠 있는 월드 홀로그램(`FeedRound2Counter`)뿐이라 1라운드를 올라가는 동안에는 보이지 않는다.
`InfoPanelPresenter`/`InfoPanelView`는 클래스만 있고 내용이 비어 있다.

## 기대 결과 (Proposed outcome)
`Canvas_InGame/Active Mission`에 두 줄로 진행도를 표시한다.
- 1라운드: 제목 `1라운드`, 내용 `피드 재료 3/5` (모은 수 / 필요 수)
- 2라운드: 제목 `2라운드`, 내용 `피드 분류 1/12` (보낸 수 / 전체, 0부터 올라감)
- 피드 앱이 아닌 씬에서는 Active Mission을 숨긴다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 피드 앱 스테이지를 플레이하는 사람
- 어떤 시스템/모듈이 건드려지는가:
  - `Core/Stage/FeedApp.cs` : 라운드·진행 숫자 계산, `OnProgressChanged` 이벤트
  - `UI/InfoPanel/InfoPanelView.cs`, `InfoPanelPresenter.cs` : 표시
  - `Prefabs/UI/Canvas_InGame.prefab` : Active Mission에 컴포넌트 연결

## 제약 (Constraints)
- 지켜야 할 정책: 집계 로직은 Core 스크립트만 수정한다. Map/Feed 스크립트(`FeedRound2` 등)는 건드리지 않는다.
  - `FeedRound2`에 남은 수 변경 이벤트가 없으므로 `FeedApp.Update`에서 `RemainingCount`를 비교해 바뀔 때만 알린다.
- `FeedApp.cs`는 CP949 인코딩이므로 인코딩을 유지해서 수정한다.
- 쓰지 말아야 할 것: 스테이지 클리어 보고(`StageManager` 수정)는 이번 범위가 아니다.

## 열린 질문 (Open questions)
- (없음) 2라운드 표기 = 보낸 수/전체, 두 줄 = 제목+내용, 클리어 보고는 제외로 확정 (2026-10-21 사용자 결정)

## 진행 상황 (2026-10-21)
- 코드·프리팹 반영 완료, 컴파일 0에러, TestScene에서 1라운드·2라운드 숫자 갱신 확인
- Active Mission 텍스트 2개의 폰트를 Pretendard로 변경(UIManagerText 제거). Rajdhani에는 한글이 없음
- 남은 것: `Map_2_Feed` 실제 플레이 확인

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
