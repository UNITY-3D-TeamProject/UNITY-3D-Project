# 보스 A 그랩 빈도(보스-10)·잡힘 구속과 화면 흔들기(보스-13)

- 날짜: 2026-10-21
- intent: [intent-058](../../../intent/clear/intent-058-bossa-grab-frequency.md), [intent-059](../../../intent/clear/intent-059-bossa-grab-hold.md)
- 실행자: Claude 직접. 예외 이유: 이 PC 의 PATH 에 Codex CLI 없음(`which codex` 실패, 데스크톱 앱 번들 `codex.exe`만 있음).

## 1. 작업 요약 및 방법

### 보스-10 원인 조사
- 그랩(A4)은 확률이 아니라 `BossAPatternSelector` 기본 순서(A1 → A2 → A4 → A5 → A6) 차례로만 나온다.
- 상황 규칙 5(한 줄에 3초 이상 머물면 그 줄에 A2)가 거의 매번 먼저 걸린다. 패턴 하나가 4초 넘게 걸리고 가로·세로 중 하나만 3초 유지해도 걸리기 때문이다. 상황 규칙은 기본 순서를 넘기지 않아 A4 차례가 오지 않는다.
- 저장소 밖 콘솔 시뮬레이션(`%TEMP%/bosssim`, 실제 `Logic/*.cs`를 링크해 빌드, 플레이어는 일정 간격마다 이웃 칸으로 이동·점프 15%·B 돌진 준비 25%)으로 수치를 냈다.

| 구간 | 수정 전 A4 | 수정 후 A4 |
|---|---|---|
| 1-2, 3초마다 이동 | 0% | 25% |
| 1-2, 1.5초마다 이동 | 8% | 26% |
| 1-2, 0.7초마다 이동 | 17% | 28% |
| 최종 | 6% | 26% |
| 1-1 | 0%(기획상 없음) | 0% |

### 보스-10 수정 (사용자 결정: A4 보장 규칙)
- `BossAPatternSelector`: 끝까지 끝난 A4 가 아닌 패턴 수를 세고(`_finishedSinceGrab`, `NotifyPatternFinished`), N 이상이면 붕괴 제한 다음·상황 규칙보다 먼저 A4. 단계가 바뀌면 0. 취소·쉼은 세지 않는다.
- `BossController._grabGuaranteeCount`(임시값 3, 0 이면 끔)를 선택기 생성자에 넘긴다.

### 보스-13 수정 (사용자 결정: 끌려온 뒤 일부만 구속, 잡힐 때 화면 흔들기)
- 플레이어 경로: `IBossSituation` → `BossEncounter` → `PlayerFacade` → `CharacterMediator` → `MoveMediator`/`CameraMediator` → `PlayerBaseCamera` (캐릭터 아키텍처 준수).
- `MoveMediator.SetMoveLocked(bool)`: 중재자를 끄지 않고 잠금 플래그. 잠긴 동안 이동 방향을 0 으로 두고 입력 방향은 기억, 점프·점프 버퍼·구르기·공중 점프를 막는다. 풀리면 기억한 방향을 적용한다. 구르기가 끝날 때도 잠금이면 0 을 유지한다. 넉백(외부 변위)·중력은 그대로.
- `PlayerBaseCamera.PlayImpulse(force)`: 사격 쉐이크와 같은 `_fireImpulseSource`로 카메라만 흔든다(반동 없음).
- `BossAGrab`: 잡는 순간 `_pullSeconds + _holdSeconds`(0.4 + 임시값 0.7초) 잠그고 `_captureShakeForce`(임시값 2, 차지샷 = 1)로 흔든다. 해제 타이머는 패턴 단계와 따로 Update 에서 세고, `Stop`(취소·`OnDisable`)에서도 반드시 푼다.

## 2. 결정 사항
- TODO 방법1(Move 중재자 껐다 켜기)은 쓰지 않는다. `PlayerInputComponent.OnMove`는 값이 바뀔 때만 오므로, 다시 켜면 누르고 있던 이동키가 사라진다. 사망 처리도 `MoveMediator.enabled`를 쓰므로 섞인다. 방법2를 잠금 플래그로 구현했다.
- A4 보장은 상황 규칙(B 돌진 협공·점프 착지·줄 머묾)보다 우선한다. 붕괴 제한보다는 뒤다(A4 를 폴백으로 쓰지 않는 기존 결정 유지).

## 3. 현재 상태 및 이슈
- 리컴파일 통과(오류·경고 0, `gate.cjs check recompile`).
- Play(테스트 씬 `Assets/Test/Boss/Map_4_Security_Boss_Phase_T`, 보스 BT 끄고 `TryStartPattern("A4")`, 런타임에서만 `Application.runInBackground = true`):
  - 잡힘 1.94초 → 잠금 → 0.4초 동안 끌려옴(287.12, -2) → 잠긴 동안 이동 입력 (0,1)을 넣어도 방향 0·위치 고정 → 3.04초(1.10초 뒤) 해제와 동시에 기억한 방향으로 이동 재개.
  - 잠긴 동안 `CancelPattern` → 같은 프레임에 잠금 해제.
  - 화면 흔들기 소스(`LookPrefab`의 `_fireImpulseSource`) 연결 확인. **실제 흔들림 세기는 육안 확인 필요.**
- 씬·프리팹 저장 없음(테스트 씬 dirty 아님).
- 이미 시작한 구르기는 잠겨도 끝까지 간다(`RollMover`에 취소 API 없음).
- **육안 확인·게이트 루프 미실행, 커밋 전.**

## 4. 다음 할 일 (Next Steps)
- 사용자 육안 확인: 그랩 빈도(1-2·최종), 잡혔을 때 0.7초 구속 체감, 흔들기 세기 2 → 필요하면 `_grabGuaranteeCount`·`_holdSeconds`·`_captureShakeForce` 조정.
- 확인 후 intent-058·059 resolved 처리, 게이트 루프 통과 후 커밋.
- AI_TODO 보스 다음 순서: 보스-12 → 보스-11 (패턴 기획·연출 변경).

## 5. 추가: 끌려오지 않던 문제 수정 (2026-10-21, 육안 확인 후)
- 사용자 육안 확인: 그랩에 잡혀도 끌려오지 않아 구속·흔들기는 보지도 못함.
- 원인(Play 실험, 테스트 씬, A·B BT 끄고 `TryStartPattern("A4")`, 플레이어 (240, -2) 시작):
  - 손 콜라이더(`Hand_L/R` `BoxCollider` 6×17×6 m, 비트리거, Enemy 레이어)가 플레이어 `CharacterController` 와 충돌한다. 손 폭(3 m + 반경 0.3 m)이 잡기 반경(2.5 m)보다 커서 날아오는 손이 먼저 플레이어를 밀어냈다(x 240 → 221.3 에서 포획).
  - 끌려오는 동안(0.4초, 처음이 빠른 ease-out)에는 앞서 돌아가는(처음이 느린 smoothstep) 그랩 손에 막혀 목표 칸 중심 x 288 대신 x 272.1 에서 멈췄다.
  - 손 콜라이더만 끈 대조 실험: 목표 칸 중심 (288, -2) 까지 정확히 끌려오고 내려치기 피해 적용.
- 수정(`BossAGrab`): 그랩 손이 날아가는 순간(`StartFly`)부터 돌아올 때(`OnGrabHandBack`)까지 그랩 손 콜라이더–플레이어 콜라이더 쌍만 `Physics.IgnoreCollision`. `Stop`(취소·비활성)에서도 되돌린다. 총알 피격·다른 패턴의 손 막기는 그대로.
- 검증: 리컴파일 통과. Play(콜라이더 켠 상태): 제자리(x 240)에서 포획, 잠금·충돌 무시 켜짐 → (288.00, -2.00) 까지 끌려옴 → 1.1초 뒤 잠금 해제 → 내려치기 피해(HP 100 → 80) → 손 복귀 후 충돌 무시 해제 확인.
- 미확인: BT·B 를 켠 실제 흐름에서의 그랩, 낮은 프레임에서 손이 플레이어를 건너뛰는 경우(60 m/s, 잡기 지름 5 m 라 약 12 fps 아래에서 가능).
- 다음: 사용자 육안 재확인(끌려오기·0.7초 구속·흔들기 세기·그랩 빈도) → intent-058·059 resolved, 게이트 루프 후 커밋.

## 6. 추가: 잡으면 끝까지 끌고 갔다가 끌고 오기 (2026-10-21, 사용자 요청)
- 요청: 손이 처음 닿으면 플레이어를 통과했다가 돌아올 때 끌어오는 대신, 닿은 순간부터 붙잡아 손이 가는 만큼 끌고 갔다가 그대로 끌고 오기.
- 사용자 확정: 손은 판 반대편 끝까지만 날아감 / 끌려가는 동안 손에 매달림(중력 끔, 구멍 위도 안 떨어짐) / 돌아올 때 날아갈 때와 같은 속도, 보스 쪽 가장 가까운 멀쩡한 칸 중심에 놓기.
- `BossAGrab`:
  - 날아갈 거리 `_hookDistance` = 손 출발점에서 판 반대편 끝까지(판 축 기준 슬랩 교차, `GetHookDistance`), `_hookRange` 가 상한. 직선 예고 길이도 같다. 빗나가도 판 끝까지만 간다.
  - 잡으면(`TryCapture`) 손은 멈추지 않고 끝까지 간다. 매 프레임 플레이어를 손 아래(잡을 때 발 높이)로 `CharacterMotor` 외부 변위로 옮긴다(`DragPlayer`, 손보다 한 프레임 늦게 따라감). 잡는 순간 잠금(놓을 때까지 무한) + 붙잡힘(중력 끔) + 흔들기.
  - 끝에 닿으면(`OnFlyArrived`) 놓을 칸(`FindNearestSafe(_origin)`)으로 `_hookSpeed` 로 날아 돌아온다(`Returning`). 칸이 없으면 그 자리에서 놓고 손만 회수.
  - 칸에 닿으면(`OnReturnArrived`) 칸 중심에 놓고(`DropPlayer`: 중력 복구, 구속 `_holdSeconds` 시작), 그랩 손 회수, 그 칸 예고 `_slamTelegraphSeconds × 배율` 후 반대 손 내려치기(기존과 같음).
  - 쓰이지 않게 된 `_pullSeconds`·`BossKnockback` 사용 제거(프리팹에 남은 값은 Unity 가 무시).
- 플레이어 경로(캐릭터 아키텍처 준수): `IBossSituation.SetPlayerHeld` → `BossEncounter` → `PlayerFacade.SetHeld` → `CharacterMediator.SetHeld` → 기존 `MoveMediator.SetHover(isHeld, 0)`(상승 속도 0 = 중력 멈춤).
- 검증: 리컴파일 통과. Play(테스트 씬, A·B BT 끔, 플레이어 (264, -2), `TryStartPattern("A4")`, 0.1초 표본):
  - 10.32 포획(x 263, 밀림 없음) → 손에 붙어 10.80 판 끝 x 234.5 → `Returning` 같은 속도로 11.71 칸 1 중심 (288, -2) 에 놓임. 매달린 동안 잠금 유지, 손과 0.6~1 m 차이로 따라감.
  - 12.44 잠금 해제(놓은 뒤 약 0.73초), 13.2 내려치기 피해(HP 100 → 80), 이어 구멍에 떨어져 낙사 복귀로 (288, 10) 이동(기존 동작). 패턴 정상 종료.
- 알려진 점: 끌려가는 길에 B 몸이 있으면 막힐 수 있음(B 와의 충돌은 끄지 않음). 붙잡힘은 플레이어 일정 속도 상승(탈것 호버)과 같은 모터 상태를 쓰므로, 호버 중에 잡혀 놓이면 호버가 끝난다. BT·B 를 켠 실제 흐름은 미확인.
- 다음: 사용자 육안 확인(끌려가고 오는 모습·속도·놓인 뒤 0.7초 구속·흔들기) → intent-058·059 resolved, 게이트 루프 후 커밋.

## 7. 육안 확인 (2026-10-21)
- 사용자 육안 확인 통과: 끌고 가고 오기·속도·놓인 뒤 구속·흔들기·그랩 빈도. 임시값 유지. intent-058·059 resolved.
- 남은 것: 게이트 루프 통과 후 커밋.
