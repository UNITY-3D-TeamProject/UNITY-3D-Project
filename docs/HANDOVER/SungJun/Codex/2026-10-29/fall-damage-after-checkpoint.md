# 낙하 복귀 후 데미지 적용

## 작업 요약
- 플레이어가 낙하 구역에 들어가면 먼저 현재 체크포인트로 이동하고, 이동이 성공한 뒤 낙하 Effect를 적용하도록 변경했다.

## 방법/접근
- `StageManager.HandleFallZoneEntered`의 처리 순서를 변경했다.
- `MovePlayerTo`가 이동 성공 여부를 반환하게 해 복귀 지점이나 CharacterController가 없으면 낙하 Effect를 적용하지 않도록 했다.
- 데미지가 치명적이면 체크포인트 위치에서 기존 사망 애니메이션과 라운드 복구 흐름이 시작된다.

## 변경된 파일
- `Assets/_Project/Scripts/Core/Stage/StageManager.cs`

## 결정 사항
- 낙하 데미지는 체크포인트 이동이 완료된 뒤에만 적용한다.

## 현재 상태 및 이슈
- `git diff --check` 통과.
- `dotnet build Assembly-CSharp.csproj --no-restore`는 오류 메시지 없이 종료 코드 1을 반환해 컴파일 확인 불가.
- Unity CLI의 프로젝트 인스턴스가 표시되지만 Pipeline 서버에 연결되지 않아 에디터 컴파일 및 Play 확인 불가.

## 다음 할 일
- Unity Editor가 연결되면 낙하 후 체크포인트 이동, 데미지, 치명타 사망 연출 순서를 Play에서 확인한다.
