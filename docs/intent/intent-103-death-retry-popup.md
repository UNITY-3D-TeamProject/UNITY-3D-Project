---
id: intent-103
title: 사망 시 다시하기·메인 메뉴 선택 팝업
part: UI / Core / Stage
status: open   # open | resolved
created: 2026-10-31
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
플레이어가 죽으면 사망 애니메이션이 끝나자마자 바로 라운드 체크포인트에서 리스폰된다(또는 `Restart Scene On First Round Death`가 켜진 씬은 바로 씬을 다시 불러온다). 플레이어가 다시 할지, 메인 메뉴로 나갈지 고를 수 없다.

보안맵 1라운드는 리스폰하면 깬 방의 입구 문이 잠긴 채로 남아 진행이 막히고, 깬 방의 쫄몹도 돌아오지 않는다. 그래서 1라운드는 씬을 다시 불러오는 쪽으로 정했다.

## 기대 결과 (Proposed outcome)
- 사망 애니메이션이 끝나면 `RETRY` / `MAIN MENU` 두 버튼이 있는 팝업이 뜬다. 모든 스테이지에 공통으로 적용한다.
- 팝업이 떠 있는 동안 게임은 멈추지 않고 계속 진행된다. 커서만 풀어 버튼을 누를 수 있게 하고, 입력은 UI 맵으로 바꾼다(ESC 설정창 안 열림).
- `RETRY`: 지금까지의 사망 복구를 그대로 실행한다. 첫 라운드이고 재시작 옵션이 켜져 있으면 씬을 다시 불러오고, 아니면 현재 라운드 체크포인트에서 리스폰한다(보안맵 2라운드는 발판 전체 리셋 후 리스폰).
- `MAIN MENU`: 설정창의 메인 화면 버튼과 같이 `GameManager.ReturnToMainMenu`. HP 0은 저장되지 않으므로 이어하기는 마지막 저장값으로 시작한다.
- 보안맵은 `Map_4_Security` StageManager의 `Restart Scene On First Round Death`를 켠다(사용자가 직접 켬).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 모든 스테이지의 플레이어
- 어떤 시스템/모듈이 건드려지는가:
  - `Core/Stage/StageManager.cs`: 사망 종료 때 바로 복구하지 않고 선택 요청 이벤트를 낸다. 구독자가 없으면(팝업이 없는 씬) 예전처럼 바로 복구한다. 선택 뒤 복구를 시작하는 공개 메서드.
  - `Core/GameManager.cs`: 커서 잠금 함수를 공개(팝업 중 커서 표시, 리스폰 뒤 다시 잠금).
  - `UI/Death/` 신규 View: 버튼 두 개와 클릭 이벤트.
  - `UI/UIManager.cs`: 선택 요청을 받아 팝업 열기·닫기, 입력 맵·커서 처리, 버튼별 처리.
  - `Prefabs/UI/Canvas_InGame.prefab`: 팝업 UI(설정창 버튼 모양 재사용).

## 제약 (Constraints)
- 지켜야 할 정책: 코드는 `Scripts/Core`, `Scripts/UI` 안에서만 고친다. 맵 씬은 열지 않는다(검증은 TestScene).
- 쓰지 말아야 할 것: 게임 시간 정지(사용자 선택: 계속 진행).

## 열린 질문 (Open questions)
- 없음 (적용 범위: 모든 스테이지 / 팝업 중 게임 계속 진행 / 영어 문구 — 2026-10-31 사용자 확인)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
