# 보스 A 3페이즈 박수 애니메이션·어깨포 투사체 (AI_TODO 보스-2·보스-5)

- intent: [intent-062](../../../intent/intent-062-bossa-clap-anim-cannon-bullet.md)
- 실행자: Claude 직접. Codex CLI 를 이 PC 의 PATH 에서 찾을 수 없어 C#·에디터 작업 모두 직접 진행했다(에디터 변경은 Unity Editor API eval 로, YAML 직접 수정 없음).

## 1. 작업 요약 및 방법
- **총알 에셋(보스-5)**: 사용자가 준 `Downloads/Boss_Bullet.fbx.glb`(Tripo 생성 모델, 메시 58개·512 텍스처 58장, 지름 약 1 m)를 사용자의 열린 Blender 세션은 건드리지 않고 별도 백그라운드 Blender(5.2.2)로 FBX 변환했다.
  - glTF 임포터가 텍스처와 BSDF 사이에 색 곱하기(Mix) 노드를 넣어 FBX 에 텍스처가 빠지던 것을, 텍스처를 Base Color 에 직접 연결하고 묶인 이미지를 `Textures/` 로 풀어 해결했다.
  - 결과: `Assets/ThirdParty/BossBullet/Boss_Bullet.fbx` + `Textures/`(58장, 11 MB). URP Lit 머티리얼 58개 모두 텍스처 연결 확인.
  - `Assets/_Project/Prefabs/AI/Boss/BossCannonBullet.prefab`: 루트(중심 피벗) + 모델 자식(배율 3, 임시값, 중심 맞춤 y -1.38).
- **어깨포 투사체**: `BossShoulderCannon` 에 `_projectilePrefab`·`_muzzle`(비우면 포 위치)·`_arcHeight`(6 m, 임시값) 추가. 발사 시 투사체 한 개를 부모 없이 만들어 재사용하고, 예고 시간 동안 `직선 + 위로 4h·t(1-t)` 포물선을 따라 진행 방향을 바라보며 날아가 터지는 순간 숨긴다. 취소 시에도 숨긴다. 판정은 기존 반경 검사 그대로.
- **박수(보스-2) — 5절의 본 이동 방식으로 대체되어 아래 내용은 모두 되돌림**: `BossController.RequestMotion(스킬 이름)`(패턴을 끊지 않는 스킬 요청) 추가, `BossAClap.RetargetHands`(1타 직후 2타 손 모으기 시작)에서 `_clapMotionSkillName`("A6Clap") 요청.
  - `BossA.prefab`: `Skills/A6Clap` 에 모션 알림 스킬(`BossBMotionSkill` 재사용, 동작 없음) 추가, `EnemyAnimator._skillAnimations` = `A6Clap → Attack` 트리거.
  - `BossA_Override`: `Enemy_Attack` → `Gripper Robot@Clap`(0.97초). 공용 `EnemyBase.controller` 는 수정하지 않음.

## 2. 결정 사항
- 박수는 A6 2타(가운데로 모을 때) 재생, 투사체는 포물선·폭발 시점 착탄, glb 는 Blender FBX 변환, 에셋은 `Assets/ThirdParty/`(Git 제외) — 모두 사용자 확정(2026-10-22).
- 패턴 코드는 애니메이터를 직접 부르지 않고 스킬 요청 → `EnemyAnimationMediator` 경로만 쓴다(intent-045 와 같음).
- 모션 알림 스킬은 새 클래스를 만들지 않고 동작 없는 `BossBMotionSkill` 을 A 에서도 재사용했다(이름이 B 지만 기능은 공용).

## 3. 현재 상태 및 이슈
- 리컴파일 통과(`gate.cjs check recompile`, 에러·경고 0).
- Play(`Assets/Test/Boss/Map_4_Security_Boss_Phase_T`, `SetStage(Final)` 강제, 시간 배율을 낮춰 샘플링):
  - 어깨포: 포(317.6, 23.3, -5.6) → 플레이어(231.0, 0.3, 10.0). t≈0.2·0.4·0.67 위치가 공식과 일치, 폭발 뒤 비활성. Game 뷰에서 텍스처가 입혀진 총알이 원 예고 쪽으로 날아가는 것 확인.
  - 박수: A6 를 강제 실행, 1타 직후(2타 손 이동 시작) `Clap` 으로 전환되고 끝나면 Idle 복귀. 콘솔 새 에러 없음.
- **타이밍 차이**: 2타 충돌은 손 모으기 시작 약 1.4초 뒤인데 Clap 클립은 0.97초라, 팔 박수 모션이 손 충돌보다 먼저 끝난다. 육안 확인 후 요청 지연·재생 속도 조정 여부 결정 필요.
- **포물선이 거의 내리꽂힘**: 포가 23 m 높이라 6 m 아치로는 거의 바로 내려오는 궤적(진행 방향이 처음부터 아래). 더 둥근 궤적이 필요하면 `_arcHeight` 를 올린다.
- 보스가 `Hit` 상태일 때 Attack 트리거가 들어오면 Hit 이 끝난 뒤 늦게 재생될 수 있다(공용 컨트롤러 구조상 Hit → Attack 전이가 없음). 미관찰.
- `Assets/ThirdParty/` 는 Git 제외라 팀원은 `BossBullet`·`AddBossAnimation` 폴더를 따로 받아야 프리팹·오버라이드 참조가 살아난다.
- 씬 저장 없음. **육안 확인·게이트 루프 미실행, 커밋 전.**

## 4. 다음 할 일 (Next Steps)
- 사용자 육안 확인: 박수 모션과 손 충돌 타이밍, 총알 크기(배율 3)·포물선 높이(6 m)·방향. 필요하면 `BossCannonBullet` 모델 배율, `BossShoulderCannon._arcHeight`, Clap 요청 시점 조정.
- 확인 통과 후 intent-062 resolved, AI_TODO 보스-2·5 완료 처리 → 보스-1(밸런스) 진행 가능.

## 5. 추가: 박수를 손 본 이동으로 변경 (사용자 요청)
### 작업 요약 및 방법
- 사용자 육안 확인: Clap 클립은 손 본을 `BossHand` 가 덮어써 박수로 구분되지 않음 → 클립 방식 폐기.
  - 되돌림: `BossController.RequestMotion`, `BossAClap._clapMotionSkillName`, `BossA.prefab` 의 `Skills/A6Clap`·`EnemyAnimator._skillAnimations`(다시 비움), `BossA_Override` Attack 슬롯(다시 비움). `BossController`·`BossA_Override` 는 원래 내용과 같아짐.
- `BossHand.Clap(위치, 손바닥 방향, 시간, onDone)` 추가: 내려친(Landed) 상태에서 공중 한 점까지 일정한 속도로 날아가며 박수 자세로 선다. 도착하면 Landed 로 돌아가 기존 머묾·회수 흐름을 그대로 쓴다.
  - 박수 자세(`_isClapPose`, 회수 시작 때 해제): 손가락 아래 기울기·지면 박기를 끄고, `_fingerAxis` 는 위로, 새 `_palmAxis`(기본·프리팹 값 로컬 -Y)는 맞은편 손 쪽으로 돌린다. 이동 시간(`_moveSeconds`) 동안 섞어서 바뀐다.
  - 그리퍼 손은 검지·엄지가 로컬 Z 로 벌어져 있어서, 두 집게가 놓인 면(사람 손바닥 면)의 법선이 로컬 Y 다. Z 로도 Play 에서 비교해 봤는데, 그때는 두 집게가 줄 방향으로 벌어져 박수 면이 아니게 돼 -Y 로 확정했다.
- `BossAClap` 을 A2(`BossALineSlam`)와 같은 구조로 다시 씀: 1타(양 끝 칸 내려찍기·피해)는 그대로, `StartFollowUp` 에서 두 손이 각자 끝 칸에서 `가운데 ± _clapGap/2` 위 `_clapHeight` 지점으로 `_strikeInterval`(프리팹 1.4초) 동안 모인다. 예고는 사이 칸 3개로 바꿔 그동안 채운다.
  - 피해: 박수 중 매 프레임 각 손 아래 칸이 사이 칸이고 플레이어가 그 칸 위면 맞음(한 패턴에 한 번). 맞부딪친 순간 한 번 더 확인.
  - 박수 시작부터 두 손이 평소 자리로 돌아올 때까지 두 손 콜라이더와 플레이어 콜라이더 충돌을 끈다(그랩의 `SetGrabHandPassThrough` 와 같은 방식, `OnTick` 에서 손이 모두 Idle 이면 되돌림, `OnDisable` 에서도 되돌림).

### 결정 사항 (사용자 확정)
- 2타는 살짝 들어 공중에서 박수, 손바닥이 서로 마주 보게 세움, 손이 지나가는 칸(사이 칸 3개)도 피해.

### 현재 상태 및 이슈
- 리컴파일 통과(에러·경고 0).
- Play(`Map_4_Security_Boss_Phase_T`, `SetStage(Final)` + `CreateClap(1)` 강제, 시간 배율을 낮춰 정지 캡처):
  - 1타 → 박수 전환, 진행 중 손가락 위·손바닥(-Y)이 서로 마주 봄(수치 확인), 맞부딪칠 때 손 간격 8 m 로 두 손이 붙어 보임(씬 뷰 캡처).
  - 플레이어를 사이 첫 칸에 두고 실행: 충돌을 끄기 전에는 손이 플레이어를 가운데로 밀고 가 바닥 아래로 떨어뜨림(y -61) → 끈 뒤에는 피해 적용·충돌 복구·착지 유지.
  - 콘솔: 새 에러 없음(오디오 장치 FMOD 에러만).
- **플레이어 시점 가시성**: 손이 커서(손가락을 세우면 약 20 m 이상) Game 뷰에서 박수 위쪽이 화면 밖으로 잘린다. `_clapHeight` 를 낮추면 더 보이지만 손이 플레이어를 덮는다. 육안 판단 필요.
- 피해 시점과 예고 채움이 다르다: 사이 칸 예고는 박수 시간 전체에 걸쳐 차지만, 첫 사이 칸 피해는 손이 그 칸에 들어서는 순간(중간쯤)에 들어간다.
- 프리팹을 다시 저장하면서 이전 커밋에서 추가된 필드가 기본값 그대로 함께 기록되고 옛 필드(`_isDouble`·`_pullSeconds`)가 빠졌다(런타임 값 변화 없음).
- 에셋 저장 때 같이 저장된 `Pretendard-Bold SDF.asset`(Play 중 동적 아틀라스) 변경은 되돌렸다.
- **육안 확인·게이트 루프 미실행, 커밋 전.**

### 다음 할 일
- 사용자 육안 확인: 박수 모양·높이(10 m)·손 간격(8 m)·속도(1.4초), 피해 범위 체감, 총알 크기·포물선. 조정은 `BossA.prefab` 의 `A6` (`_clapHeight`·`_clapGap`·`_strikeInterval`), 손 `_palmAxis`, `BossShoulderCannon._arcHeight`.
- 통과 후 intent-062 resolved, AI_TODO 보스-2·5 완료 처리.

## 6. 추가: 박수를 바닥 위에서 치도록 변경 (사용자 요청)
- 요청: 박수는 보스맵 위에서 친다. 쓸어담기(A2)처럼 Y 로 올리지 말 것.
- `BossAClap._clapHeight` 를 없애고 모일 지점을 가운데 칸 중심 높이 그대로(`가운데 ± _clapGap/2`) 둠. 프리팹을 다시 저장해 남은 `_clapHeight` 값도 지움. 박수 자세에서 지면 박기를 끄는 것은 유지(손가락을 세우므로 박으면 손목이 바닥 아래로 들어감).
- 리컴파일 통과. Play(같은 강제 방식): 박수 중 두 손 높이 y 0.0 유지, 손가락 위·바닥 위에 선 채 가운데로 쓸려 옴(씬 뷰 캡처), 사이 첫 칸 플레이어 피해 적용·착지 유지, 콘솔 새 에러 없음.
- **육안 확인·게이트 루프 미실행, 커밋 전.** 조정할 값: A6 `_clapGap`(8 m)·`_strikeInterval`(1.4초), 손 `_palmAxis`.

## 7. 추가: 1타 주먹 내려치기, 2타 주먹 펴며 쓸어 박수 (사용자 요청)
### 작업 요약 및 방법
- 요청: 1타는 원래처럼 손가락 끝으로 찍지 말고 주먹으로 쾅 내려치기, 2타는 바닥을 쓸며 손바닥을 펴서 가운데에서 박수. 이해한 내용을 되묻고 확정함(주먹 아랫면으로 망치처럼 / 처음부터 손바닥 마주 보기 / 1타 직후 쓸기 시작할 때 펴기).
- `BossHand`:
  - `MoveToHover(point, isFist = false)`: `isFist` 면 주먹 자세(`_isFist`)로 떠올라 내려친다. 회수·박수 시작·`OnDisable` 에서 풀린다.
  - 주먹 자세는 `_moveSeconds` 동안 섞이며, 손가락 아래 기울기와 지면 박기를 끄고 `_fingerAxis` 를 판 쪽(평소 위치 → 내려칠 지점 수평 방향), `_fistDownAxis`(로컬 Z = 집게 벌어지는 축)를 아래로 돌린다. 그래서 집게가 위아래로 놓여 아래 집게 면이 지면에 닿는다. 손 본 높이는 `_fistLift`(4.58 m, 임시값).
  - `_fingerCurls`(손가락 본 + 오므림 각도)와 `_curlAxis`(로컬 Y): 애니메이션이 쓴 손가락 회전 위에 주먹 가중치만큼 오므림을 곱한다. Idle·Take Damage·Die 클립이 손가락 본을 매 프레임 쓰므로 누적되지 않는다(Move·Attack 슬롯은 비어 있어 기본 클립에 손가락 곡선 없음).
  - 프리팹: 왼손 Index -25° / Thumb +25°, 오른손 Index +25° / Thumb -25°(임시값, 쉬는 자세가 바깥으로 15° 벌어져 있어 안쪽으로 10° 더 오므림).
- `BossAPatternBase.IsFistStrike`(기본 false)를 `StartHover` 가 `MoveToHover` 에 넘기고, `BossAClap` 만 true. 다른 패턴과 그랩은 그대로.

### 현재 상태 및 이슈
- 리컴파일 통과(에러·경고 0).
- Play(같은 강제 방식, 정지 캡처): 떠오르는 동안 집게가 오므려지고 손가락이 판 쪽(-X), 집게 축이 아래를 향함(수치 확인), 양 끝 칸에 눕힌 주먹이 내려쳐짐, 박수 시작 직후 주먹 가중치가 줄며 손을 세우고 펴는 중간 모습 확인. 콘솔 새 에러 없음.
- 주먹 아랫면과 바닥 사이 간격(`_fistLift`)과 오므림 정도는 캡처로만 봤고 정밀히 맞추지 않았다.
- **육안 확인·게이트 루프 미실행, 커밋 전.**

### 다음 할 일
- 사용자 육안 확인: 주먹 모양(오므림 ±25°)·주먹이 바닥에 닿는 높이(`_fistLift`)·주먹→박수 전환. 조정은 `BossA.prefab` 의 `Hand_L`/`Hand_R` (`_fingerCurls`·`_fistLift`·`_fistDownAxis`).

## 8. 추가: 2타 바닥 밀착, 닿으면 피해, 붙잡아 가운데로 끌기 (사용자 요청)
### 작업 요약 및 방법
- **바닥 밀착**: 손 메시를 손 본 기준으로 계산(`SkinnedMeshRenderer` bindpose, 본 배율 20)하니 손 몸통(Palm 본 정점)은 손목 쪽으로 +2.43 m, 손바닥·옆 방향으로 약 ±5.8~6.0 m, 손가락 끝 쪽으로 -16.7 m(손가락 포함 -23.9 m). 박수 자세에서 손 본을 4.58 m(`_boneLift`)에 두어 손 아랫면이 약 2.1 m 떠 있었음 → `BossHand._clapLift`(박수 가중치만큼 섞음) 추가, 프리팹 2.43 m.
- **피해 방식(사용자 확정)**: 2타는 칸 기준이 아니라 B 돌진(`TryHitTarget` 반경 검사)처럼 손 중심과 플레이어 수평 거리 `_hitRadius`(6 m, 손 몸통 절반 폭, 임시값) 안이면 닿은 것으로 보고 피해 1회. 1타는 칸 기준 유지, 예고는 사이 칸 3개 유지.
- **붙잡아 끌기(사용자 확정)**: 닿은 순간 그랩과 같은 창구로 이동 잠금(`SetPlayerMoveLocked`)과 붙잡음(`SetPlayerHeld`, 중력 끔). 매 프레임 `PlayerMotor.AddExternalDisplacement` 로 잡은 손 위치 + 손바닥 방향 × `_clapGap/2`(맞부딪칠 때 정확히 가운데)에 잡을 때의 발 높이로 옮긴다. 박수 순간 놓고(중력 복구) 이동 잠금은 `_holdSeconds`(0.7초, 그랩과 같게) 뒤 `OnTick` 에서 푼다. `Stop`·`OnDisable` 은 바로 모두 푼다. 모터가 없으면 피해만 준다.

### 현재 상태 및 이슈
- 리컴파일 통과(에러·경고 0).
- Play(`Map_4_Security_Boss_Phase_T`, 플레이어를 사이 첫 칸에 두고 A6 강제):
  - 왼손에 닿아 붙잡힘 → 손바닥 앞 4 m 에 붙어 끌려감(손 z -2.4 / 플레이어 z 1.6, 높이 바닥) → 박수 순간 가운데 칸 중심(z 10.0)에 놓임, 착지, 0.7초 뒤 잠금 해제.
  - 박수 중 손 본 y 2.43, 씬 뷰에서 손 아랫면이 판에 닿음.
  - 콘솔 새 에러 없음.
- **기존 문제 발견(이번 작업에서 미수정)**: 이 테스트 씬에서 `BossEncounter.PlayerMotor` 가 null 이었다. 보스전 시작(`SetPlayer`) 시점에 플레이어 자식 `MovePrefab`(`CharacterMotor`)이 아직 없던 것으로 보인다(나중에 찾으면 있음). 이 상태면 그랩·박수의 붙잡기와 넉백이 동작하지 않는다(박수는 피해만). 검증은 런타임에서만 참조를 채워 진행했다. 실제 맵(`Map_4_Security` 3라운드)은 플레이어가 들어간 뒤 시작하므로 영향이 없을 가능성이 크지만 확인 필요.
- 맞부딪칠 때 손 간격 8 m 는 손바닥 면 기준(±5.8 m)으로 보면 두 손이 약 3.6 m 겹친다. 육안으로 어색하면 `_clapGap` 을 약 11.6 m 로.
- 주먹 자세 높이 `_fistLift`(4.58 m)는 같은 계산으로 보면 주먹 아랫면(+Z 5.96 m)이 바닥 아래로 약 1.4 m 박힌다. 이번 요청 범위가 아니라 그대로 두었다.
- **육안 확인·게이트 루프 미실행, 커밋 전.**

### 다음 할 일
- 사용자 육안 확인: 2타 바닥 밀착, 닿는 판정 반경(6 m), 끌려가는 느낌·놓인 뒤 0.7초 잠금, 손 간격, 주먹 박힘 정도.
- 테스트 씬의 `PlayerMotor` null 원인 확인(보스전 시작 시점 vs 플레이어 모터 생성 시점).

## 9. 추가: 박수 줄을 양 끝 칸이 남은 가로줄로 한정 (사용자 요청)
### 작업 요약 및 방법
- 문제: A6 은 항상 플레이어가 선 가로줄을 골라, 양 끝 칸이 무너진 줄이면 1타가 구멍 위를 치고 2타까지 이어졌다. 붕괴 칸이 9개 이상이면 최종 단계는 A6 만 폴백으로 고르므로(`BossAPatternSelector.Choose`) 이런 경우가 잦았다.
- 수정: `BossAPatternSelector.CreateClap(grid, targetRow)` 가 1타가 치는 양 끝 칸(col 0·끝)이 둘 다 무너지지 않은 가로줄만 후보로 삼고, 플레이어 줄에서 가장 가까운 줄(거리가 같으면 무작위)을 고른다. 후보가 없으면 Rest. 기본 순서·붕괴 제한 폴백 두 호출 모두 적용.

### 결정 사항 (사용자 확정)
- 기준은 "양 끝 칸만 남은 줄"(사이 칸에 구멍이 있어도 2타 진행). 플레이어 줄이 안 맞으면 가장 가까운 기준 충족 가로줄, 없으면 쉰다.
- 칸 상태는 무너짐(`Collapsed`)만 뺀다(예고 칸은 남은 칸으로 본다).

### 현재 상태 및 이슈
- 리컴파일 통과(`gate.cjs check recompile`, 에러 0). 첫 실행은 남은 변수명 오류, 두 번째는 직전 실패 상태가 남아 `compilationFailed` 로 표시, 세 번째 통과.
- eval(시드 50개씩, 최종 단계): 구멍 없음 → 플레이어 줄 / 플레이어 줄 왼끝 붕괴 → 위·아래 줄 반반 / 사이 칸만 붕괴 → 플레이어 줄 / 위 줄까지 끝 붕괴 → 아래 줄 / 붕괴 제한에서 row1~3 끝 붕괴 → row0·row4 / 모든 줄 끝 붕괴 → Rest. 모두 기대대로.
- 사이 칸 구멍 위로 2타가 지나가는 것은 사용자 결정대로 그대로다.
- `Assets/Test/Boss/BossAPatternSelectorTests.cs` 는 주석 처리돼 있고 Git 제외 폴더라 테스트는 추가하지 않았다.
- **육안 확인·게이트 루프 미실행, 커밋 전.**

### 다음 할 일
- 사용자 육안 확인: 구멍 난 줄에서 박수가 다른 줄로 옮겨 가는지, 모든 줄 끝이 무너졌을 때 쉬는 빈도가 괜찮은지.

## 10. 추가: 2타 예고를 세운 상자로도 표시 (사용자 요청)
### 작업 요약 및 방법
- 2타 예고가 바닥 칸 판뿐이라 잘 안 보이던 것을, 바닥 예고는 그대로 두고 사이 칸 3개 위에 바닥에서 `_wallHeight` 만큼 선 반투명 상자를 함께 띄우도록 변경(사용자 선택: "세운 상자(벽) 추가").
- `BossAClap` 에 `_wallPrefab`·`_wallHeight`(6 m, 임시값) 추가. `StartFollowUp` 에서 줄 가운데 칸 중심 + 높이 절반에 줄 방향으로 놓고, 크기는 (칸 간격, 높이, 칸 간격 × 사이 칸 수). 칸 간격은 양 끝 칸 중심 거리 ÷ (SIZE − 1). 박수 순간(`EndClap`)과 `Stop`(취소·비활성화)에서 끄고, `OnDestroy` 에서 지운다. 어깨포 투사체처럼 부모 없이 한 번 만들어 재사용.
- 새 에셋 없이 `BossLineTelegraph.prefab`(콜라이더 없는 큐브 + `M_BossTelegraph` 반투명 빨강)을 `BossA.prefab` 의 `A6` 오브젝트 `_wallPrefab` 에 Editor API(`LoadPrefabContents` → `SaveAsPrefabAsset`)로 연결. 프리팹 변경은 두 줄(`_wallPrefab`, `_wallHeight`).

### 현재 상태 및 이슈
- 리컴파일 통과(에러 0).
- Play(`Map_4_Security_Boss_Phase_T`, Final + A6 row1 강제): 2타 중 상자 켜짐, 위치 (276, 3, 10), 범위 z -8~28(사이 칸 6~8 중심 -2·10·22 ± 6, 칸 간격 12 m) · y 0~6. 임시 카메라 캡처로 손이 상자 안으로 쓸려 오는 모습 확인. 박수 끝나면 꺼짐, 2타 중 `CancelPattern` 하면 즉시 꺼짐, 상자 오브젝트 1개만 재사용. 콘솔 새 게임 에러 없음.
- 검증 중 알게 된 점: 이 PC 에서 Editor 가 포커스를 잃으면 Play 프레임이 멈춘다(`Application.runInBackground` 를 런타임에서만 켜고 진행, 설정 파일은 바꾸지 않음).
- **육안 확인·게이트 루프 미실행, 커밋 전.**

### 다음 할 일
- 사용자 육안 확인: 상자 높이(6 m)·투명도(머티리얼 알파 0.4)가 플레이어 시점에서 잘 보이는지, 박수 끝까지 켜 두는 시점이 괜찮은지. 조정은 `BossA.prefab` → `A6` 의 `_wallHeight`.
