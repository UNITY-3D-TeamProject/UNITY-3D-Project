---
id: intent-008
title: 박스 접촉으로 Home/Lobby 씬 전환 테스트
part: Scene Test
status: resolved
created: 2026-09-26
resolved: 2026-09-26
---

## 문제 (Problem)
스폰된 Player가 박스에 닿으면 KSJ_Home과 KSJ_Lobby를 왕복하는 테스트 스크립트가 필요하다.

## 기대 결과 (Proposed outcome)
두 씬의 박스에 같은 컴포넌트를 붙이면 현재 씬에 따라 반대 씬으로 Single 전환한다.
현재 GameManager에 등록된 플레이어만 감지하며 전환 전에 능력치를 저장한다.

## 영향 범위 (Affected users and systems)
- 테스트 담당자와 기존 PlayerSpawner/GameManager 등록 흐름.
- Assets/_Project/Scripts/Core에 독립적인 테스트 컴포넌트 추가.

## 제약 (Constraints)
- Player 프리팹과 기존 미커밋 코드는 수정하지 않는다.
- Test 씬 배치와 Scene List 등록은 사용자가 진행할 테스트 준비로 안내한다.
- 중복 접촉에 의한 중복 전환을 막고 씬 등록 누락을 명확히 알린다.

## 열린 질문 (Open questions)
- 없음. 감지 방식은 사용자가 위임했다.

## 해결 기록 (Resolution)
- TestSceneTransitionBox를 추가했다. 트리거/키네마틱 Rigidbody를 자동 설정하고, 등록된 플레이어와 그 자식 Collider를 감지한다.
- 현재 씬 이름으로 반대 목적지를 선택하고 로드 가능 여부 확인 → 능력치 저장 → 비동기 Single 전환 순으로 실행한다.
- 새 소스를 명시적으로 포함한 dotnet 빌드 성공: 오류 0개, 기존 StageManager 경고 3개. Unity Play Mode 왕복 테스트는 미실행.
- 관련 커밋/PR: 없음.
