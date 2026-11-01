---
id: intent-113
title: 엔딩 크레딧 무대에 가비지 컬렉터 추격 장면 추가
part: UI / Ending Credits
status: resolved   # open | resolved
created: 2026-11-01
resolved: 2026-11-01
---

## 문제 (Problem)
엔딩 크레딧 왼쪽 무대가 인사(`01_Greeting`) → 춤(`02_PlayerDance`)까지만 있다. 사용자가 세 번째 장면으로 플레이어·펫이 미로 2라운드 몬스터 가비지 컬렉터(GC)에게 쫓기는 장면을 원한다.
예전 무대의 GC(`Cast_GC`)는 intent-109(커밋 `0587e272`)에서 지워져 지금 무대에는 없다.

## 기대 결과 (Proposed outcome)
- 춤 뒤 세 번째 장면 `03_Chase`. 컷 세 개로 나눈다.
  1. 플레이어(손을 앞으로 뻗고 달리는 `Run_Chase`)와 옆에 붙은 펫이 화면을 가로질러 도망간다.
  2. GC가 혼자 같은 방향으로 쫓아온다.
  3. 넓은 카메라로 셋이 한 화면에 나온다 (앞에 플레이어·펫, 뒤에 GC).
- 컷마다 카메라 자리, 달리는 시간, 시작·끝 위치를 인스펙터에서 고칠 수 있다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 엔딩까지 본 플레이어
- 어떤 시스템/모듈이 건드려지는가: 새 `EndingCastChaseBeat.cs`, `EndingCastDirector.cs`(멈춤 여부 프로퍼티), `EndingCast_Player.controller`(`Run_Chase` 상태), 되살린 `EndingCast_GC.controller`, `EndingCastStage.prefab`(GC 모습 복사본, `Beats/03_Chase`, 춤 반복 횟수)

## 제약 (Constraints)
- GC는 실제 프리팹과 연결 없는 모습 전용 복사본 (무대의 플레이어·펫과 같은 방식).
- 설정창이 열린 동안에는 GC도 함께 멈춘다.
- `Map_5_Live` 수정 없음.

## 열린 질문 (Open questions)
- 없음 (춤 1번 + 추격 크레딧 끝까지 반복으로 사용자 확정)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `EndingCastChaseBeat`(`Beats/03_Chase`)가 컷 1(플레이어 `Run_Chase`·펫 도망, 시작 배치 카메라) → 컷 2(GC 혼자, 올려다보는 카메라) → 컷 3(넓은 카메라, GC는 2m 뒤)를 일정한 속도로 화면을 가로질러 재생. GC는 `GC.prefab` 모델 복사본(1.4배, Animator만)에 되살린 `EndingCast_GC.controller`, 참조는 장면 스크립트가 가지고 장면 시작·끝에 끔. 설정창 멈춤은 `EndingCastDirector.IsPaused`로 GC 애니메이터 속도 0. 춤 `_repeatCount` 1, 추격 `_repeatCount` 0(크레딧 끝까지 반복) — 사용자 확정. 미리보기 렌더·코루틴 흐름 확인, 실제 엔딩 Play는 미확인.
- 관련 커밋/PR: 커밋 전
