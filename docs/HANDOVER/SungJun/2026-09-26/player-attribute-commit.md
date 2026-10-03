# 플레이어 Attribute 변경 커밋

## 작업 요약 및 방법
- 사용자 요청에 따라 기존 GameManager.cs, PlayerState.cs 및 PlayerState.cs.meta만 스테이징하고 커밋했다.
- 커밋: 5460c98, feat: 플레이어 Attribute 저장 및 복원 로직 추가.
- 커밋 본문의 작업자는 KSJ로 표기했다.

## 결정 사항
- PlayerSpawner는 별도 커밋 대상으로 남겼다.

## 현재 상태 및 이슈
- 코드 수정 및 실행 테스트는 수행하지 않았다.
- diff 검사에서 기존 PlayerState 주석의 줄 끝 공백과 파일 끝 빈 줄을 확인했다.
- 씬 전환 전 저장 호출 연결은 아직 없다.
- 인수인계 폴더 통합 변경은 미커밋 상태이며 푸시는 하지 않았다.

## 다음 할 일
- PlayerSpawner.cs와 .meta를 별도로 커밋한다.
- Unity에서 씬 전환 저장 호출 연결과 동작을 확인한다.
