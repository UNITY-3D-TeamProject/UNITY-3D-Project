---
id: intent-049
title: 보스 B 예고를 보스 A 와 다른 색으로 + 돌진 예고 추가 + 도약 원 페이드 인
part: AI / Boss / BossB
status: resolved   # open | resolved
created: 2026-10-20
resolved: 2026-10-21
---

## 문제 (Problem)
- 보스 A(칸 예고·어깨 포 원·A4 직선)와 보스 B(도약 원)가 같은 머터리얼 `M_BossTelegraph`(빨강)를 써서 어느 보스의 공격인지 헷갈린다.
- 보스 B 돌진은 제자리 예고 시간(1.5초)만 있고 바닥 예고 표시가 없어 돌진 경로를 읽을 수 없다.
- 보스 B 도약 원은 켜지자마자 가득 차 있어 착지 시점을 읽을 수 없다(보스 A 칸 예고는 이미 중앙 채움 방식, intent-047).

## 기대 결과 (Proposed outcome)
- 보스 A 예고는 그대로 빨강. 보스 B 예고는 전용 머터리얼 `M_BossBTelegraph`(파랑 계열)로 구분된다.
- 보스 B 돌진 예고 동안 B 앞에서 돌진이 멈출 지점까지 직선 예고(폭 = 적중 반경 × 2)가 바닥에 깔린다. 돌진이 끝나면 사라진다.
- 보스 B 도약 원과 돌진 직선은 예고 시간 동안 알파가 0 → 머터리얼 알파로 올라가며 나타난다(페이드 인).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스전 플레이어
- 어떤 시스템/모듈이 건드려지는가: `BossBLeap`, `BossBCharge`, 새 `BossBTelegraph`(예고 판 생성·배치·페이드 도우미), `BossB.prefab`, 새 `M_BossBTelegraph.mat`·`BossBCircleTelegraph.prefab`·`BossBLineTelegraph.prefab`

## 제약 (Constraints)
- 보스 A 쪽 머터리얼·프리팹·코드는 건드리지 않는다.
- 프리팹·머터리얼은 YAML 직접 수정 금지, 에디터(MCP)로 만든다.

## 열린 질문 (Open questions)
- 보스 B 예고 색: 임시로 파랑(0.15, 0.6, 1.0, α 0.4). 사용자 육안 확인 후 조정 가능.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: B 전용 파랑 예고 머티리얼 + 돌진 직선 예고 + 페이드 인. 사용자 육안 확인 통과(2026-10-21).
- 관련 커밋/PR: 커밋 전 → [기록](../../HANDOVER/BF_Leers/2026-10-20/boss-b-telegraph-color.md)
