# develop 병합 목차 충돌 해결

## 작업 요약 및 결정 사항
- feature/System/UISoundAndVariusThing에서 develop(fff49053)을 병합하던 중 INTENT_POINTER.md의 맨 위 신규 항목 충돌이 발생했다.
- UI/InfoPanel 항목(intent-080)을 유지하고, develop의 보안 낙하물 VFX(intent-077) 및 보스 A 박수 예고(intent-076) 항목도 모두 유지했다.
- 자동 병합되어 스테이징된 develop 변경은 수정하지 않고 병합에 포함한다. 메인화면 커밋은 유지한다.

## 검증 및 다음 할 일
- 양쪽 원본 문서의 모든 행 보존(누락0개), 충돌 표식0개, 새 항목의 링크 파일 존재를 확인했다.
- MainScene 및 메인 메뉴 스크립트는 HEAD 대비 추가 변경 없음. 병합 상태에서 Unity 재컴파일 오류0개 확인.
- 문서 충돌 해결이며 게임 로직을 추가/수정하지 않는다. 병합 커밋은 로컬에서만 생성하고 Push하지 않는다.
