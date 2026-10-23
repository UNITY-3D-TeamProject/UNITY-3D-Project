# 낙하 복귀 후 방향 잠금 해제 문제 검토

## 결과와 작업 범위

- 사용자 요청: PlayerFacade/Rotate 브랜치로 체크아웃하고, 낙하 후 위치를 복구할 때 방향 잠금이 풀리지 않는 문제를 직접 해결할 방법을 확인한다.
- 현재 브랜치: `feature/PlayerFacade/Rotate`, HEAD `673044e9`. 로컬 추적 브랜치와 일치하는 상태로 전환했다. 이번 요청에 없는 pull·rebase·커밋·push는 하지 않았다.
- Claude Code Opus가 최초 조사와 해결안 비교를 수행하고, Codex가 사용자 추가 설명·소스·임시 객체 검증을 대조했다. Opus가 검토 의견을 반영해 해결안을 보완했다.
- **코드·씬·프리팹·설정은 수정하지 않았다.** 사용자가 요청한 단계는 확인·검토이며, 아래 수정안은 아직 구현하지 않았다.

## 이전 작업 보존

PlayerSound의 미커밋 사운드 구현과 기존 로컬 에셋·설정 변경 6개를 경로 지정 stash로 보관한 뒤 브랜치를 전환했다. Rotate 브랜치에 적용하지 않았다.

- 보관본: `479d4037e2437144878dcda9dbfe5db4ed4d788c`
- 메시지: `codex PlayerSound pending work before Rotate review 2026-10-23`
- 보관 대상: SoundController.cs, 폰트, 보스 재질, EnemyAI_Melee.asset, CLAUDE.md, ProjectSettings.asset.
- 사운드 변경을 새 커밋으로 만든 것은 아니다. 원래 PlayerSound로 돌아갈 때 해당 보관본을 복원해야 한다. 기존 미추적 논의 문서는 유지했다.

## 사용자 확인 사실과 소스 확인 사실

사용자가 확인한 사실:

- 카메라는 회전하지만 캐릭터 몸 방향은 고정되어 남는다.
- 외부 작업자가 낙하 후 복귀 처리에 SetLookDirection 호출을 추가한 뒤 발생했다.
- 그 호출부는 현재 체크아웃된 코드에는 없다.

현재 소스에서 확인한 사실:

1. [PlayerFacade.SetLookDirection](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Facade/Player/PlayerFacade.cs:236)은 CharacterMediator로 위임한다.
2. [CharacterMediator.SetLookDirection](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/CharacterMediator.cs:311)은 `LockLookDirection(direction)` 후 `AlignYaw(direction)`만 호출한다. 생성 연출 중 사용하는 함수라는 주석이 있으며, 연출과 무관하게 걸면 자동으로 풀리지 않는다고 명시돼 있다.
3. [CharacterRotator.Update](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Rotation/CharacterRotator.cs:72)는 `_isLookDirectionLocked`가 true이면 회전 갱신을 건너뛴다. 카메라 yaw 정렬 자체는 카메라 입력 잠금이 아니다.
4. [OnSpawnEffectEndedCallback](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/CharacterMediator.cs:514)은 `_isSpawnLocked`가 true일 때만 현재 시점 방향을 발행하고 회전 잠금을 해제한다. 스폰 연출이 끝난 뒤에는 `_isSpawnLocked`가 false이고, 같은 인스턴스의 생존 낙하 복귀에서는 새 종료 이벤트가 보장되지 않는다.
5. 현재 [StageManager.MovePlayerTo](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Core/Stage/StageManager.cs:503)는 CharacterController를 잠시 끄고 위치·몸 회전을 바꾼 뒤 다시 켠다. 기존 회전 잠금을 해제하지 않는다.
6. CharacterRotator의 OnEnable도 회전 잠금을 초기화하지 않는다. 컴포넌트를 껐다 켜는 방식으로 해결된다고 볼 수 없다.

외부 작업자의 실제 호출 순서·방향 인자는 확인하지 못했다. 현재 소스와 사용자 설명을 합치면 **스폰 연출에 묶인 잠금 해제 수명을 생존 낙하 복귀에 그대로 사용한 문제**다. 현재 StageManager 자체가 SetLookDirection을 호출한다고 보고하지 않는다.

## B1 — 보고된 사용 경로에 잠금 해제 단계가 없음

생성 연출이 이미 끝난 인스턴스에 SetLookDirection을 호출하면 다시 잠기지만, 기존 해제 이벤트는 다시 발생하지 않는다. 따라서 몸은 고정되고 카메라만 움직일 수 있다.

| 안 | 내용 | 장점 | 영향·단점 |
| --- | --- | --- | --- |
| **B — 추천** | 기존 CharacterMediator.SetLookDirection에서 연출이 남아 있는지 확인해 잠금 수명을 결정 | 기존 공개 시그니처·외부 호출부를 유지하고 현재 모듈 내부에서 해결 가능 | 연출 중에는 고정, 연출 외에는 일회 정렬로 의미가 확장되므로 주석 갱신 필요 |
| A | PlayerFacade·CharacterMediator에 명시적인 해제 API 추가 | 기존 API의 스폰 전용 의미를 유지하고 호출자가 해제 시점을 명시 | 새 공개 API의 이름·계약 결정과 외부 작업자의 호출부 수정 필요. 해제 호출 누락 가능 |

두 도구의 추천은 B이며, 사용자가 A/B 중 수정 방식을 확정하거나 구현을 요청한 것은 아니다.

### B안의 동작 조건

- 기존 즉시 몸 회전과 카메라 yaw 정렬은 유지한다.
- 연출 드라이버가 없거나 `SpawnEffectDriver.IsEnded`가 true라면, 현재 카메라 방향을 전달한 뒤 회전 잠금을 즉시 해제하는 방식이다.
- 연출이 아직 끝나지 않았다면 기존처럼 종료 이벤트가 올 때까지 잠금을 유지한다.
- 판단을 `_isSpawnLocked`로 하면 안 된다. 이 값은 CharacterMediator.Start에서 설정되므로, Start 전에 방향을 지정하는 첫 스폰에서는 false일 수 있다. 연출이 끝났는지에 대한 [SpawnEffectDriver.IsEnded](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Visual/SpawnEffectDriver.cs:81)를 기준으로 검토했다.
- 해제 순서는 `AlignYaw → PublishViewForward → UnlockLookDirection`이다. yaw만 맞춘 상태에서 바로 Unlock하면 이전 `_viewForward` 캐시를 따라 몸이 돌아갈 수 있다.
- 기능 로직 변경은 CharacterMediator의 기존 함수 한 곳으로 끝낼 수 있다. 의미를 설명하는 CharacterMediator·PlayerFacade 주석도 맞추는 것을 권장한다. 새로운 StageManager 호출이나 새로운 공개 API를 B안의 필수 조건으로 삼지 않는다. 외부 작업자가 이미 추가한 방향 지정 호출은 유지할 수 있다.

### 지금 존재하는 직접 해제 메서드

현재 코드에도 다음 두 공개 메서드가 존재한다.

1. [CameraMediator.PublishViewForward](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/SubMediators/CameraMediator.cs:94)
2. [RotateMediator.UnlockLookDirection](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/SubMediators/RotateMediator.cs:85)

이 순서로 호출하면 현재 잠금을 풀고 시점 방향 캐시를 맞출 수 있다. 원인 확인·임시 조치로는 가능하지만, 제품의 외부 호출부에서 서브 중재자를 직접 찾는 방식을 권장하지 않는다. PlayerFacade 자체에는 대응하는 공개 해제 API가 없다.

## Codex가 직접 수행한 검증

Editor는 Play Mode가 정지된 상태였고, 로드된 CharacterMediator에 사운드 필드가 없는 것을 확인해 현재 Rotate 브랜치의 타입 구성이 로드됐는지 점검했다.

임시 preview scene에서 현재 실제 메서드를 호출하고 상태를 확인한 13개 항목이 모두 통과했다.

1. PlayerFacade.SetLookDirection 호출 뒤 몸 회전 잠금이 남는다.
2. 카메라 yaw는 요청 방향으로 정렬된다.
3. SetLookDirection 자체는 새로운 시점 방향을 발행하지 않는다.
4. StageManager.MovePlayerTo는 복귀 위치를 바꾼다.
5. 그 이동이 기존 회전 잠금을 해제하지 않는다.
6. `_isSpawnLocked`가 false이면 연출 종료 콜백을 호출해도 해제하지 않는다.
7. CharacterRotator.OnEnable이 잠금을 초기화하지 않는다.
8. 기존 공개 UnlockLookDirection 호출은 잠금을 해제한다.
9. PublishViewForward를 먼저 호출하면 회전기의 시점 캐시가 현재 카메라 방향과 일치한다.
10. Start 전에는 연출이 미종료여도 `_isSpawnLocked`가 false일 수 있다.
11. 정상적인 스폰 잠금 상태의 종료 콜백은 시점 전달과 Unlock을 수행한다.
12. 연출이 이미 끝난 뒤 방향을 다시 지정하면 종료 이벤트 없이 잠금이 다시 남는다.
13. 이 상태에서도 기존 공개 메서드 순서로 직접 해제할 수 있다.

객체·연출 완료 값·lifecycle은 reflection으로 구성했다. 실제 낙하 플레이나 외부 작업자의 코드를 실행한 검증은 아니다. 검증 객체는 finally에서 모두 제거했고 소스·씬·프리팹·테스트 파일을 저장하지 않았다. 검사 종료 시 추적 중인 파일의 staged/unstaged diff는 없었다.

## 남은 검증·결정

- B의 자동 해제 또는 A의 명시적 해제 중 사용자 선택. A는 새 공개 API 이름도 선택해야 한다.
- 외부 작업자의 실제 낙하 복귀 호출 순서에서 위치 이동 뒤 방향을 정하는지 확인.
- 수정 후 첫 스폰 연출 중·연출 종료 후·생존 낙하 복귀에서 몸과 카메라가 지정 방향을 보고 다시 정상 추종하는지 Play 검증.
- 구르기 중 낙하하는 경우에도 B의 즉시 Unlock은 같은 회전 잠금 플래그를 해제한다. 구르기 방향 고정과 복귀 방향 중 우선순위는 이번 검토에서 정하지 않았다.
- 드라이버 종료 이벤트가 CharacterMediator 비활성 중 무시되는 별도 경로는 전체 스폰 조작 잠금의 수명 문제일 수 있다. 이번 보고된 생존 낙하 복귀와 동일 원인이라고 확대하지 않았다.

## Claude 협의 상태

최초 조사와 사용자 추가 설명·Codex 검토를 반영한 응답 모두 `state: completed`, exit 0 및 Remote Control 활성 증거를 확인했다. Claude는 정적 조사만 수행했고, 위 13개 임시 객체 검증은 Codex가 수행했다. 원문은 Claude 앱 Code 탭에서 확인하며, 별도 원문 로그·HANDOVER·Intent·포인터는 작성하지 않았다.
