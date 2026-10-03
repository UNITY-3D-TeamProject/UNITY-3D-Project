# 요청한 미사용 이벤트·메서드 제거

## 작업 요약 및 방법
- 사용자가 지정한 항목만 소스에서 제거했다.
- GameManager: OnPlayerDespawned 이벤트 및 UnregisterPlayer 메서드 제거.
- PlayerState: UnregisterPlayer 메서드 제거.
- Singleton: TryGetInstance 메서드 제거.
- Assets의 C# 및 직렬화 파일 검색에서 제거한 이름의 사용처가 남지 않음을 확인했다.

## 결정 사항
- IsInitialized를 포함한 다른 코드와 기존 미커밋 변경은 유지했다.
- 단순 미사용 코드 제거로 새 intent는 작성하지 않았다.

## 현재 상태 및 이슈
- dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet 성공: 오류 0개, 기존 StageManager 미사용 이벤트 경고 3개.
- 씬 전환 직전 저장 연결은 기존처럼 후속 작업이다.

## 다음 할 일
- 이번 제거 요청의 추가 작업은 없다.
