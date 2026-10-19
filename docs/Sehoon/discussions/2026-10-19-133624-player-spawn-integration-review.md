# 플레이어 스폰 연출 연결 — 현재 상태 대조와 계획 검토

작성: 2026-10-19 13:36:24 KST

## 목적과 역할

- 사용자 요청: `2026-10-19-132603-player-spawn-integration-todo.md`를 기반으로 플레이어 스폰 이펙트 작업 진행.
- Codex: 요청 전달, Opus 계획 검토, 현재 상태 독립 대조. 선택 확정 후 구현 결과와 Play 검증 담당.
- Claude Code Opus: 읽기 전용 조사·최초 계획 및 검토 의견을 반영한 계획 보완.
- Claude Code Sonnet: 사용자 선택 확정 후 구현 예정. 아직 호출하지 않음.
- 지정 문서의 과거 Codex 직접 구현 허가는 현 세션 역할 변경으로 적용하지 않았다.

## 확인 결과

- Unity CLI `1.0.0-beta.13`은 최신이며 Editor `6000.4.8f1` 연결은 ready다.
- TestScene 하나가 열려 있고 dirty=true다. Play=false, pause=false, timeScale=1, captureDeltaTime=0, runInBackground=false를 Codex도 직접 확인했다.
- 기존 씬 연출의 Spawn Rate는 1000, Color는 (0, 0.085, 0.5, 1)이다. 문서와 디스크의 3000, (0.85, 0.95, 1, 1)과 다르며 Codex가 실시간 조회로 재확인했다.
- Opus는 프리팹의 렌더러 4개, Readable 메시 서브에셋, 셰이더, VFX/binder 구성과 스폰 흐름이 기존 계획에 부합한다고 보고했다. Codex는 Driver 및 스폰 흐름 코드를 대조했다.
- 현재 Driver는 사용자가 배치한 기존 미추적 파일이다. 이번 변경에 새 코드로 포함하거나 수정하지 않는다. INab 미보유 환경에서의 컴파일은 미검증이며, 배포 문제를 이번 로컬 프리팹 연결의 추가 승인 항목으로 확대하지 않는다.

## 주요 대안과 미확정 사항

| 항목 | A | B | 도구 추천과 이유 |
|---|---|---|---|
| D1 프리팹 VFX 값 | 문서값 3000·밝은 하늘색 | 현재 씬값 1000·진한 파란색 | A: 기존 검증 기록에 맞음. 최근 사용자 조정 가능성이 있어 선택 필요 |
| D2 씬의 기존 added 연출 | 비활성화 | 삭제 | A: 중복 재생을 막고 미저장 튜닝을 보존. Undo 적용, 씬 저장 안 함 |
| D4 실행 검증 범위 | TestScene 초기 재생·재생성·중단·복원 | 실제 Map 스폰·리스폰·로딩 경로 | A: 현재 미저장 씬 유지 가능. 실제 흐름은 미검증으로 구분. B는 미저장 씬 보존 방법을 먼저 확정 |

세 항목을 현재 채팅의 선택 질문으로 전달했으며, 작성 시점에는 사용자 응답이 없다. 추천은 사용자 확정 사항이 아니다. 선택 전 종속 작업은 진행하지 않는다.

## 검토 후 계획

1. Sonnet이 실행 중 Unity Editor의 API로 `Player.prefab`을 `LoadPrefabContents`하여 기존 테스트 연출 계층을 복제한다. 로드 후에는 예외가 발생해도 finally에서 Unload한다.
2. Driver를 기존 `Visual.SpawnEffectDriver`로 연결하고, 모든 참조를 프리팹 내부 객체 및 `ThirdParty/SpawnEffect` 사본으로 재매핑한다. 선택된 VFX 값과 기존 1.2초·padding 0.02·playRate 2 등 나머지 튜닝을 적용한다.
3. 전체 직렬화 참조에 대해 Missing Script, Test 에셋 의존, 외부 씬 참조를 검사한다. 저장 전 검사 통과 후 해당 프리팹만 저장하고 재로드 검증한다. 기존 프리팹 블록 보존을 diff로 확인한다.
4. TestScene의 기존 added 연출에만 D2를 적용한다. 무관한 added 오브젝트와 사용자 오버라이드를 유지하고 씬을 저장하지 않는다.
5. Sonnet 자체 참조·해시 검증 후 Codex가 Play 검증한다. captureDeltaTime=0과 frameCount 증가를 확인하고 재생·재생성·중단·복원·입자 소멸·임시 재질 정리를 점검한다.
6. Play 종료 후 상태와 보호 대상 해시를 대조한다. D4=A의 Destroy/Instantiate 재현을 실제 PlayerSpawner/StageManager 리스폰 검증으로 보고하지 않는다.

제품 변경 대상은 `Assets/_Project/Prefabs/Player/Player.prefab` 하나와 TestScene 메모리의 승인된 중복 처리다. 코드, 원본 에셋, ProjectSettings, 스폰·로딩 흐름은 변경 대상이 아니다. 새 공개 API·정책·구조 변경이 필요하면 별도로 확인한다.

## 실제 수행과 미검증

- 수행: 지침·원본 문서 확인, 터미널 연결 대조, 최신 CLI 및 Unity 연결 확인, Opus 조사·계획 검토·보완, Codex의 코드·실시간 씬 값 대조, 작업 전 상태 및 관련 파일 해시 확보.
- 최초 Claude 호출은 PowerShell 옵션 전달 오류로 시작되지 않았고, 새 연결 검증 후 재호출은 정상 완료했다. 샌드박스의 named pipe/Editor 검색 접근 제한은 승인 검토를 거친 샌드박스 밖 읽기 검증으로 확인했다.
- 아직 수행하지 않음: 프리팹 구현, 씬 중복 처리, Play 실행, 실제 Map 스폰·리스폰·로딩, INab 미보유 환경 컴파일.
- 제품 코드·에셋·씬은 수정하지 않았다. 이번 협의 요약만 프로젝트에 추가했다. HANDOVER·Intent·포인터·원문 로그·git 조작은 수행하지 않았다.
