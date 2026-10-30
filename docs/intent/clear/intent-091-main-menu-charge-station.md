---
id: intent-091
title: 메인 메뉴 로비 충전소 지붕 복원과 오디오 범위 확인
part: UI / MainMenu / Audio
status: resolved
created: 2026-10-30
resolved: 2026-10-30
---

## 문제 (Problem)
메인 씬 배경 프리팹의 충전소에 로비 원본 씬의 큰 지붕 모델이 빠져 있어 주유소 외형이 다르게 보인다.

## 기대 결과 (Proposed outcome)
메인 씬에서 로비 원본과 같은 충전소 지붕이 보이고 BGM과 UI 소리만 들린다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 메인 메뉴 사용자
- 어떤 시스템/모듈이 건드려지는가: `LobbyMenuBackdrop` 프리팹, 메인 씬 배경/오디오 확인

## 제약 (Constraints)
- 로비 원본 씬과 메인 메뉴의 기존 BGM·UI 연결을 보존한다.
- 기존 작업 트리의 다른 변경을 보존한다.

## 열린 질문 (Open questions)
- 없음. 첨부 이미지의 충전소 지붕은 로비 원본의 `ChargeStation` 자식 모델로 확인했다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 로비 원본 `ChargeStation`의 지붕 모델을 메인 메뉴 배경 프리팹의 같은 부모와 변환값으로 복원했다. 메인 메뉴 배경의 번개 표시는 사용자의 후속 요청에 따라 지붕 위로 4m 올렸다. 메인 씬의 카메라 이동·최종 플레이어 구도는 원래 설정을 유지한다. 메인 씬의 3D 효과음 요청은 AudioManager에서 차단하고 BGM·UI 재생은 유지한다. Unity Play에서 최종 플레이어 구도, BGM 재생, 3D 소스 재생 0개를 확인했다.
- 관련 커밋/PR: 없음
