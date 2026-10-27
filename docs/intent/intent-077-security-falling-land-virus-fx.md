---
id: intent-077
title: 보안 맵 0·1 낙하물 착지 시 바닥 감염(픽셀 붕괴) 연출
part: Map / Gimmicks / VFX
status: open   # open | resolved
created: 2026-10-26
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
보안 맵 2라운드의 0·1 낙하 위험물(`Security_FallingZero`·`Security_FallingVirus`)은 예고 원이 채워진 뒤 떨어지고, 바닥에 닿는 순간 아무 연출 없이 사라진다.
보안·바이러스 컨셉에 맞는 착지 연출이 없어 밋밋하다.

## 기대 결과 (Proposed outcome)
- 바닥에 닿으면 착지점에서 빨간 경고 톤의 정사각 픽셀이 바이러스처럼 들쭉날쭉 번지고, 일부 픽셀이 깜빡인 뒤 무작위 순서로 깨지듯 꺼지며 사라진다.
- 번지는 범위는 예고 원 크기, 전체 시간 약 1.2초.
- 보기만 하는 연출이다. 피해·판정 없음.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안 맵을 플레이하는 플레이어, 맵 담당자
- 어떤 시스템/모듈이 건드려지는가:
  - `Map.Gimmicks.FallingObstacle`: 선택 필드 `_landEffect` 추가. 비어 있으면 지금과 같다. `_isDestroyOnLand` 착지 때 바닥 위치에 생성.
  - 신규 `Map.Gimmicks.PixelSpreadEffect`: 진행도를 0→1로 올리고 끝나면 스스로 사라짐
  - 신규 셰이더 `Project/VirusPixelSpread`, 머티리얼, 이펙트 프리팹
  - `Security_FallingZero`·`Security_FallingVirus` 프리팹에 이펙트 연결

## 제약 (Constraints)
- 씬·프리팹은 YAML 직접 수정 금지, Unity Editor MCP 로 작업한다.
- 이펙트는 1개씩 2초 간격으로 생겨 수가 적으므로 풀을 쓰지 않고 Instantiate/Destroy 한다(풀 매니저 씬 등록 변경 없음).
- 파편(조각) 연출은 넣지 않는다(사용자 결정).

## 열린 질문 (Open questions)
- 없음. (사용자 결정: 시각 연출만, 빨간 경고 톤, 바닥 감염만, 크기·시간은 권장안)

## 진행 상황 (2026-10-26)
- 구현 완료: 셰이더 `Project/VirusPixelSpread`, `M_VirusPixelSpread`, `Security_VirusSpreadFX`(지름 4 m, 1.2초), `FallingObstacle._landEffect`·`GetGroundPosition`·`SpawnLandEffect`, 두 낙하 프리팹 연결.
- 검증: 리컴파일 통과, `FallingHazardSpawnerTests` 6/6, 진행도별 프리뷰 렌더(`Logs/VirusFx/progress_grid.png`), Play 착지 위치·자동 소멸 확인.
- 후속: 사용자 피드백으로 정사각 픽셀을 계속 바뀌는 0·1 글자(일부 블록) + 줄 글리치 + 스캔라인 바탕으로 교체(`progress_grid_v2.png`).
- 후속 2(사용자 픽스 요청): 예고가 대기+낙하 시간 동안 차서 착지 순간 다 차도록 `FallingObstacle` 수정(다른 장애물에도 적용). 예고원 반지름 2→1, 히트박스 XZ 1.6×1.6, 착지 연출 지름 2 m·격자 12. Play에서 예고 완료 0.03초 뒤 착지 확인, 테스트 6/6.
- 남음: 실제 플레이 육안 확인(색·크기·시간 조정 여부).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
