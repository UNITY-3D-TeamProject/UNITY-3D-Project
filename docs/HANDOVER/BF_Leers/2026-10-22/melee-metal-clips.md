# 근접 잡몹 스폰 시 팔이 떨어져 보이는 문제 — 클립을 Metal Robot 것으로 교체

## 1. 작업 요약 및 방법
- 증상: `Melee.prefab` 은 프리팹 화면에서 정상인데, 스폰되어 애니메이션이 돌면 팔 마디가 서로 떨어져 보임.
- 원인: 모델·Avatar 는 **Metal Robot** 인데 `Melee_Override` 클립 5개가 전부 **Tanker Robot** 것(10-15 `1d7fa16` 부터). 같은 팩이라 뼈 이름은 같지만 Tanker 클립에 위치 커브가 있어 Metal 뼈가 Tanker 길이로 벌어짐(예: `RigLArm2` -0.500 → -0.807, `RigPelvis` 높이 1.05 → 1.585, 위치 커브 27개 중 26개 1 cm 이상 차이).
- 조치 (실행자 Claude 직접, Unity MCP eval. 예외 이유: 환경에 codex CLI 없음):
  - `Melee_Override`: Idle → `Metal Robot@Idle 01`, Hit → `Metal Robot@Take Damage`, Death → `Metal Robot@Die`, Attack → `Metal Robot@Right Punch Attack`, Move → `Melee_Run.anim`.
  - `Assets/_Project/Animations/Enemy/Clips/Melee_Run.anim` 새로 만듦: `Metal Robot@Run Forward In Place` 복사본 + Loop Time 켬. 원본 FBX 는 루프가 꺼져 있고 ThirdParty 는 Git 제외·수정 금지라 `Special_WindCharge.anim` 과 같은 방식으로 복사.
- YAML 직접 수정 없음(`ApplyOverrides`·`CreateAsset`·`SaveAssets`).

## 2. 결정 사항
- Idle 은 `Idle 01` 사용(사용자 확정).
- 값 재측정 결과 변경 불필요:
  - `EnemyAnimator._referenceMoveSpeed` 8 유지: Metal 달리기 발 속도 1배속 8.32 m/s(Tanker 8.32 와 같음, 클립 길이만 1.0 → 0.833초).
  - `EnemyMeleeAttack._hitDelay` 0.3 유지: 오른손이 가장 앞으로 나가는 시점 Metal 0.342초(Tanker 0.325초).
  - 판정 위치(앞 0.6 m, 반경 1 m) 유지: Metal 펀치 손이 0.3~0.33초에 앞 0.83~0.92 m 로 판정 안에 들어옴.

## 3. 현재 상태 및 이슈
- 검증: 클립 5개 모두 Metal 모델에서 바인딩 누락 0, 팔 뼈 위치 차이 0. PreviewScene 렌더로 Idle·Run·펀치 포즈에서 팔이 붙어 있음 확인(`Temp/melee_metal_poses.png`, Git 제외).
- **Play 중 육안 확인 전, 게이트 루프는 Stop 훅에서 실행, 커밋 전.**
- `SaveAssets` 때 `Pretendard-Bold SDF.asset`(TMP 동적 아틀라스)도 같이 저장됨. 이번 작업과 무관한 변경이므로 커밋에 넣지 말 것.
- Ranged(Blast Robot)·Special(Nexus Robot)은 별도 컨트롤러라 이번에 확인하지 않음.

## 4. 다음 할 일
- 사용자 Play 육안 확인: 스폰된 근접 잡몹 팔 연결, 달리기 루프, 펀치 타격 시점.
