# 로비 출렁이는 발판의 오낙하 판정 수정·검증

- 일시: 2026-10-25 15:33 KST
- 요청: 이전 작업을 먼저 커밋하고, `LobbyTiles_V4` 계열 발판이 내려갔다 올라오는 동안 플레이어가 낙하 모션을 취하는 문제를 Editor 테스트·원인 분석 후 수정한다.
- 역할: Claude Code Opus가 조사·최초 계획·검토 반영, Sonnet이 제품 수정, Codex가 요청 전달·계획 검토·실제 Editor 재현·검증·기존 작업 커밋·요약을 담당했다.

## 먼저 수행한 커밋

- 브랜치: `Fix/PlayerAnimation`.
- 커밋: `bba8e13c24d95cef0b98aa0baef9f52cb973778f` — `fix: 걸어서 낙하할 때 공중 애니메이션 전환 수정`.
- 이전 낙하 수정 5개 제품 파일과 관련 조사·구현 요약 2개만 명시적으로 스테이징했다.
- 기존 `AGENTS.md`, `ProjectSettings/ProjectSettings.asset`의 로컬 변경과 다른 미추적 기록은 포함하지 않았다. 푸시는 하지 않았다.
- 이번 발판 오낙하 수정은 위 커밋 이후의 미커밋 변경이다. 후속 수정의 커밋은 별도로 요청되지 않았다.

## 실제 재현과 원인

Unity 6000.4.8f1의 실제 `Map_1_Lobby` 씬, 실제 `Player(Clone)`, 실제 로비 발판으로 확인했다.

- `LobbyTiles_V4`는 모델이며, 움직임과 탑승 처리는 상위 `Step_XX_*`의 `BobbingPlatform`, `RideablePlatform`, `TransformMotor` 등이 담당한다. 실제 밟는 콜라이더는 자식 `Body`에 있다.
- `PlatformRider`는 실행 순서 -50에서 발판 이동량을 별도의 `CharacterController.Move`로 적용한다.
- 이 중간 이동으로 접지 결과가 일시적으로 false가 될 수 있다. 이후 순서 0인 `CharacterMotor`가 자신의 이동을 적용하면 다시 접지 true가 된다.
- 기존 `MoveMediator`는 실행 순서를 지정하지 않아 모터와 같은 기본 순서 0이었다. 모터보다 먼저 중간 접지 값을 읽으면 마지막 접지 시각이 갱신되지 않고, 코요테 시간 이후 낙하 이벤트를 잘못 발생시킨다.
- 수정 전 `Step_01_P` 7초 기록: 918개 샘플, 낙하 시작·종료 각각 3회, JumpAir 69개 샘플. 공중 플래그가 true인 67개 샘플은 모두 같은 타일이 발밑에 있었고, 기록 시점에는 접지도 true였다.
- 발바닥과 발판 윗면의 최대 간격은 0.07999992m로 실제 `skinWidth=0.08m` 수준이었다. 본 재현에서는 몸이 발판에서 분리되는 것이 아니라 접지를 읽는 시점의 문제였다.

관련 근거:

- `Assets/_Project/Scripts/Map/Platforms/PlatformRider.cs`: -50 실행 순서, `FollowPlatform`의 별도 `CharacterController.Move`.
- `Assets/_Project/Scripts/Map/Platforms/BobbingPlatform.cs`: -100 실행 순서, 실제 접지 이후 출렁임 시작과 발판 이동.
- `Assets/_Project/Scripts/Movement/CharacterMotor.cs`: `Move` 이후의 접지 결과와 착지 이벤트.
- `Assets/_Project/Scripts/Mediator/SubMediators/MoveMediator.cs`: 마지막 접지 시각·단순 낙하·버퍼 점프를 읽는 `Update`.

## 비교한 대안과 확정 계획

| 대안 | 범위·판단 |
|---|---|
| MoveMediator를 모터 뒤에 실행 — 채택 | 한 파일 내부 실행 순서만 확정한다. 공용 모터·발판·기존 낙하 유예를 유지한다 |
| 모터의 이동 직후 접지 값을 캐시해 공용 프로퍼티 의미 변경 | 중간 Move의 영향을 차단하지만 Enemy·Boss 등 공용 모터 구독자까지 영향이 넓다 |
| 발판 추종을 Transform 이동으로 변경 | 접지 플래그 변경은 줄일 수 있으나 추종 중 벽·천장 충돌 동작이 달라져 비권장 |

- 사용자 요청인 원인 분석 후 수정 범위에서, 공개 API·게임 정책 값·새 의존성·구조를 바꾸지 않는 내부 수정으로 계획을 확정했다.
- 낙하 유예 시간을 늘리거나 발판 근처에서 낙하 연출을 무조건 막는 방안은 적용하지 않았다.

## 이번 제품 변경 — 한 파일 한 줄

`Assets/_Project/Scripts/Mediator/SubMediators/MoveMediator.cs:28`

```csharp
[DefaultExecutionOrder(10)]   // CharacterMotor(0)가 이동한 뒤의 접지 결과를 읽는다. CharacterMediator(100)보다 먼저 실행한다
```

- 클래스 본문, 기존 0.1초 코요테 시간, 이벤트, Animator Controller, 발판·모터 코드는 변경하지 않았다.
- 모터(0)가 이동한 뒤, CharacterMediator(100) 이전에 접지·낙하·버퍼 판정을 읽는다.
- 착지 직후 예약 점프는 모터 이동 뒤에 선택되므로 실제 상승은 다음 프레임부터 이루어진다. 해당 경계를 별도로 실행 검증했다.
- `MonoImporter.GetExecutionOrder` 조회는 0으로 나왔으므로 그것만으로 성공을 판단하지 않았다. 로드된 타입의 `DefaultExecutionOrder` 속성 값 10과 실제 Play 결과를 확인했다. `.meta` 파일을 수동 변경하지 않았다.

## Codex가 직접 수행한 검증

| 시나리오 | 결과 |
|---|---|
| 같은 `Step_01_P`, 실제 플레이어 정지, 7초 | 731개 샘플. 낙하 시작·종료 0, 접지 유실 0, 공중 플래그 0, 마지막 접지 시각 지연 0 |
| `Step_03_P` 위 보행 및 출렁임, 7초 | 779개 샘플 모두 같은 타일이 발밑에 있음. 낙하 신호 0, 접지 유실 0, 공중 플래그 0. Idle·Locomotion 유지 |
| `Step_02_Dr`에서 실제 점프 후 착지, 7초 | 725개 샘플. 기존 점프 시작·종료 각 1, 오낙하 신호 0. JumpStart → JumpAir → JumpLand 확인 |
| 실제 타일 가장자리 이탈 후 낮은 임시 바닥 착지 | 낙하 시작·종료 각 1, 점프 이벤트 0. JumpAir·JumpLand 확인 |
| 별도 물리 환경에서 코요테 점프 | 점프 시작·종료 각 1, 낙하 이벤트 0 |
| 별도 물리 환경에서 착지 직전 버퍼 입력 | 낙하 시작·종료 각 1 이후 점프 시작·종료 각 1. 버퍼 점프와 후속 착지 확인 |

- 스크립트 컴파일 오류 0개. 최종 재확인은 `up_to_date`였다.
- 이번 소스 변경의 `git diff --check` 통과.
- 실제 로비 테스트에는 씬의 플레이어·콜라이더·발판을 사용했다. 보행과 실제 낙하를 제어하기 위한 발판 및 별도 회귀 검증 오브젝트는 Play 세션에서만 만들고 저장하지 않았다.
- 시작 컷씬이 게임 시간을 정지시키므로 `CinematicManager.Stop()`의 정상 종료 경로로 연출을 마친 뒤 측정했다. `Time.timeScale`을 직접 변경하지 않았다.
- Play 프레임 진행을 확인하고, 검증 세션에서만 `Application.runInBackground=true`로 설정한 뒤 원래 false로 복원했다. 입력 비활성화도 복원했다.
- 검증 종료 후 Play 정지, 원래 `Map_1_Lobby` 씬 유지, scene dirty=false, 임시 검증 오브젝트 0개를 확인했다.
- 기존 개인 지침과 프로젝트 설정은 작업 전후 SHA-256이 동일했다.

## 검증 중 구분한 상황과 한계

- 최초 컷씬 정지 중 기록은 게임 시간이 흐르지 않아 동작 판정에서 제외했다.
- 최초 재현에서 입력을 먼저 꺼 둔 상태로 컷씬 제어권을 해제하여 `Cannot switch to actions 'Player'; input is not enabled` 오류 1건이 발생했다. 검증 절차의 순서 문제이며, 수정 후 세션은 컷씬 종료·시간 재개를 확인한 다음 입력을 제어했다. 이를 제품 변경으로 고치지 않았다. 최종 consoleErrors=0, compilationFailed=false였다.
- 임시 진입 발판을 만드는 보조 실험 1회는 물리 위치 동기화 전 bounds를 읽어 배치 높이가 잘못되어 판정에서 제외했다.
- 보조 진입 발판을 교정한 실험에서는 타일로 넘어갈 때 실제 발바닥 간격이 약 0.265m, 접지 유실이 약 0.107초인 구간에 낙하가 1회 발생했다. 이것을 접지 중 오낙하 통과로 취급하지 않았다. 실제 지지가 끊기는 경계에서도 낙하를 숨기는 예외 처리는 이번 범위에 넣지 않았다.
- 모터 내부의 중간 접지 기반 중력·공중 가속 처리 자체는 변경하지 않았다. 이번에 확인한 접지 중 연출 오판정과 별도 영역이다.
- 모든 로비 발판·다른 Oscillating/Waypoint 발판·프레임률별 전체 회귀는 수행하지 않았다. 공중 구르기·탑승·추가 점프의 전체 회귀는 이전 낙하 수정 단계에서 검증했고, 이번에는 실제 타일 점프·이탈과 실행 순서 영향이 있는 코요테·버퍼를 우선 재검증했다.
- 새 프리팹·씬·테스트 코드 파일·의존성은 남기지 않았다. Intent·HANDOVER·누락된 애니메이션 기준 문서는 생성하지 않았다.

## Claude 중계 상태

- Opus 조사 및 검토 반영 호출은 모두 최종 JSON `state=completed`, exit 0으로 수신했다.
- Sonnet은 승인된 한 줄을 직접 적용했다. Codex가 실제 diff·로드된 속성·컴파일·Play 결과를 대조했다.
- Sonnet 최초 호출과 검증 피드백 재개 호출은 remote control 마지막 상태 `unknown` 오류로 실패해 완결된 `final_text`를 수신하지 못했다. 정상 완료로 판정하지 않는다.
- Codex가 제품 수정을 대신하거나 일반 Claude 호출로 우회하지 않았으며, 남은 소유 background 세션은 정지했다. 별도 원문 로그는 생성하지 않았다.
- 이 요약의 수정·검증 결과는 Codex가 실제 코드와 Editor에서 확인한 범위다. Claude의 미수신 최종 응답을 근거로 삼지 않는다.
