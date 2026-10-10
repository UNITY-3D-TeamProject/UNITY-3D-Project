---
id: intent-018
title: HUD를 PlayerFacade 기반으로 전환하고 스킬 쿨 UI 추가, 낙하 데미지를 Effect SO로 변경
part: UI / Stage / Player State
status: open   # open | resolved
created: 2026-10-10
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- HUD(체력·배터리·Heat)가 `AttributeSet`을 직접 구독한다. 플레이어 참조는 이미 `PlayerFacade` 기반인데 `GameManager.CurrentPlayerState`가 Facade에서 AttributeSet을 역조회하는 임시 다리로 남아 있다. `UIManager`의 설정창 입력도 `AttributeSet.GetComponentInParent<PlayerInput>()`로 Facade를 우회한다.
- 스킬 쿨(구르기·블루투스·스캔) UI가 없다.
- 낙하 데미지가 `StageManager`에서 `GetAttribute` → `SetAttribute(hp - _fallDamage)`로 직접 계산된다. 총알·근접·`EffectZone`은 모두 `SOAttributeEffect`를 쓴다.

## 기대 결과 (Proposed outcome)
- HUD와 UIManager가 `PlayerFacade`(`OnAttributeChanged`, `OnSkillUsed`, 설정창 입력 이벤트)만 참조한다. `CurrentPlayerState` 브리지는 제거된다.
- HUD에 체력, 배터리, Heat(총 게이지)와 구르기(`Roll`)·블루투스(`SpawnVehicle`)·스캔(`Scan`) 쿨 슬라이더 3개가 표시된다.
- 낙하 데미지는 `SOAttributeEffect` 에셋 하나를 `Apply`하는 방식으로 적용되며, `PlayerFacade.OnHit` 경로는 그대로 유지된다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(HUD), 디자이너(낙하 데미지 수치 조정)
- 어떤 시스템/모듈이 건드려지는가: `GameManager`(OnPlayerSpawned 시그니처, CurrentPlayerState 삭제), `StageManager`, `PlayerHudPresenter`, `PlayerHudView`, `UIManager`, HUD Canvas 프리팹/씬, 낙하 Effect SO 에셋

## 제약 (Constraints)
- 예산/기한: 없음
- 지켜야 할 정책: `docs/coding-convention.md`, 외과 수술적 변경. 스킬 남은 시간은 Facade에 API가 없어 HUD가 `OnSkillUsed(이름, 쿨타임)`로 직접 계산한다.
- 쓰지 말아야 할 것: Facade에 쿨 조회 API 추가(이번 범위 아님)

## 열린 질문 (Open questions)
- 쿨 표시 형태는 슬라이더로 가정(변경 시 View만 수정).
- Player 프리팹의 `PlayerFacade._playerInput` 연결 여부 — 구현 중 프리팹에서 확인.

## 결정 기록
- 총 게이지 = `CurrentHeat`/`MaxHeat` (사용자 확정)
- 블루투스 스킬 = `SpawnVehicle` (사용자 확정)
- 낙하 데미지는 Effect SO 1개 + `Apply` (사용자 확정)
- 작업 전 `PlayerHudView`·`UIManager`·`StageManager`를 CP949 → UTF-8(BOM 없음, CRLF 유지)로 변환함. 변환 전 상태에서 도구로 저장하면 한글 주석이 깨지기 때문.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
