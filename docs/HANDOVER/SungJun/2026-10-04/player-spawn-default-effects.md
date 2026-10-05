# 저장 데이터 우선 스폰 초기화

## 작업 요약 및 방법
- 사용자가 요청한 저장값 존재 시 복원, 없을 때만 Effect 적용 분기를 구현했다.
- PlayerSpawner의 사용자가 추가한 _playerEffects 배열을 사용하고 기존 CP949 인코딩을 보존했다.
- PlayerState.HasSavedAttributes는 기존 저장 딕셔너리 Count를 조회하고 GameManager.HasSavedPlayerAttributes가 전달한다.
- 사용자 정정에 따라 Effect.Apply에는 플레이어 AttributeSet을 target으로만 전달한다. Set + Float Effect의 Amount를 대상 능력치에 적용하며 null 배열/항목은 건너뛴다. 이후 기존 RegisterPlayer가 저장값 복원 및 스폰 이벤트를 처리한다.

## 변경된 파일
- Assets/_Project/Scripts/Core/PlayerSpawner.cs
- Assets/_Project/Scripts/Core/GameManager.cs
- Assets/_Project/Scripts/Core/PlayerState.cs
- docs/HANDOVER/SungJun/intent/intent-012-player-spawn-default-effects.md 및 목차

## 결정 사항
- 저장 데이터가 있으면 초기화 Effect를 실행하지 않는다.
- 디스크 저장, SO 설정 및 씬 전환 이벤트 연결은 추가하지 않는다.

## 현재 상태 및 이슈
- 코드 분기/이벤트 순서 정적 검토 및 git diff --check 완료.
- Unity 컴파일/플레이 모드 미검증. intent-012는 배선 및 실행 검증 전이므로 open 유지.
- 기존 씬 전환 직전 저장 이벤트가 아직 연결되지 않았다.

## 다음 할 일
- Inspector에서 Effect 배열을 연결하고 첫 스폰 및 저장값 복원 경로를 Unity에서 검증한다.
- 사용자에게 실제 반영한 코드를 설명한다.
