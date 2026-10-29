# 보스A 3×3 칸 글리치 (AI_TODO2 보스-18, intent-086)

## 작업 요약 및 방법
- 보스A 메시를 바인드 포즈 좌표(`BossAMaterialSpace` 가 넘기는 `_BossAWorldToRest`) 기준 3×3 칸(좌우 X × 높이 Z)으로 나눴다. 2~3초마다 2~3칸을 무작위로 골라, 그 칸에만 0·1 낙하물(`ThirdParty/Projectile/SG_DigitalProjectile` 의 `DigitalProjectileGlitch`)과 같은 띠 밀림 글리치와 스캔라인을 넣었다. 밝기 깜빡임은 넣지 않았다(사용자 결정).
- 0·1 일렁임의 정체: 그 셰이더 그래프의 버텍스 Custom Function이다. 높이로 12개 띠를 나누고, 1/8초마다 띠마다 15% 확률로 X 로 ±0.03 민다. 착지 연출(`VirusPixelSpread`)과는 다른 효과다.
- `BossACorruptedArmor.hlsl`:
  - 전역 값 `_BossAGlitchMask`(칸 비트), `_BossAGlitchBounds`(바인드 포즈 min X·Z, 크기 X·Z), `_BossAGlitchShift`(줄당 띠 수, 칸 너비 대비 밀림, 초당 갱신, 확률), `_BossAGlitchScan`(몸 높이당 줄 수, 속도, 세기)을 추가했다.
  - `BossAGlitchVertex_float` 추가: 월드 위치 → 바인드 포즈 → 칸·띠를 계산하고, 바인드 포즈 X 축(월드로 변환) 방향으로 민 뒤 오브젝트 공간으로 돌려준다. 해시에 칸 번호를 섞어 같은 높이의 좌우 칸이 따로 튀게 했다.
  - 표면 함수 끝에서, 고른 칸에만 위로 흐르는 스캔라인을 `EnergyColor`(빨강) 발광으로 더한다.
  - 마스크가 0(값 없음)이면 기존과 똑같이 그린다.
- `SG_BossACorruptedArmor.shadergraph`: `BossAGlitchVertex` Custom Function(File, 같은 hlsl) 노드를 추가하고 기존 Position(World)·Time 노드 → 노드 → `VertexDescription.Position` 으로 연결했다. 그래프 JSON 은 Node 스크립트로 노드·슬롯·엣지만 텍스트 삽입했다(기존 객체 변경 없음, CRLF 유지).
- 신규 `AI.Boss.BossA.BossAGlitchCells.PickMask`: 후보 칸 중 min~max개를 겹치지 않게 비트 마스크로 뽑는다(Unity 비참조).
  - 처음에는 `AI/Boss/Logic/` 에 만들었다. 그런데 이 폴더가 로컬 `.git/info/exclude` 에 들어 있어 새 파일이 커밋에서 빠지는 것을 발견했다. 그대로 두면 다른 PC 에서 `BossAGlitch` 컴파일이 깨지므로 `BossA/` 로 옮겼다(`AssetDatabase.MoveAsset`, GUID 유지).
  - 옮긴 뒤 리컴파일 통과, 테스트 4/4 다시 통과.
- 신규 `AI.Boss.BossA.BossAGlitch`(`Gripper Robot` 에 부착):
  - Awake 에서 `sharedMesh.bounds`(메시 공간 = 바인드 포즈)로 칸 범위를 정한다.
  - LateUpdate 에서 주기마다 칸을 다시 뽑고 MaterialPropertyBlock 으로 넘긴다. `BossAMaterialSpace` 와 같은 블록을 Get→Set 으로 공유한다.
  - OnDisable 에서 마스크를 0으로 되돌린다.
- `BossA.prefab`: `PrefabUtility.LoadPrefabContents` 로 컴포넌트를 추가했다(YAML 직접 편집 없음). 재직렬화로 `BossAClap._clapVfx` 위치와 `_skillSounds[7]._phase` 오버라이드 순서·공백이 바뀌었지만 값 변화는 없다.
- 실행자: Claude 직접 진행. Codex CLI 가 설치되어 있지 않다(`codex` 명령 없음).

## 결정 사항
- 사용자 결정: 버텍스 방식(B, 포스트 프로세싱·INab VFX 미사용), 3×3, 주기 2~3초, 칸 수 2~3 무작위, 띠 밀림 + 스캔라인만, 항상 켬.
- 메시 FBX(`ThirdParty/Gripper Robot`)는 Read/Write 가 꺼져 있어 런타임에 정점을 읽지 않는다.
  - 정점이 없는 아래 가운데 칸(1번, 양손 사이)은 `_candidateCells` 기본값 509 로 후보에서 뺐다.
  - 칸 번호 = 행(아래→위) × 3 + 열(메시 -X → +X). 정면에서 보면 좌우가 뒤집혀 보인다.
- 값은 머티리얼 프로퍼티가 아니라 MaterialPropertyBlock 전역 값으로 받는다(`_BossAWorldToRest` 와 같은 방식). 머티리얼 에셋은 바뀌지 않는다.
- 기본값:
  - 띠 밀림: 줄당 띠 8, 밀림 칸 너비의 8%, 초당 8회, 확률 15%
  - 스캔라인: 25줄, 속도 1.5, 세기 0.8
  - 모두 `BossAGlitch` 인스펙터에서 조정한다.

## 현재 상태 및 이슈
- 검증:
  - `gate.cjs check recompile` 통과. 셰이더 그래프 `ShaderHasError` false, 메시지 0.
  - 로컬 `Assets/Test/Boss/BossAGlitchCellsTests.cs`(Git 제외) 4/4 통과. 이 프로젝트에서는 Assembly-CSharp 테스트라 PlayMode 로 실행된다.
  - 프리뷰 씬 렌더(실제 씬 변경 없음) `Logs/BossAGlitch/`:
    - `0_off`: 마스크 0, 기존과 같음
    - `1_all_strong`: 전체 칸, 확률 0.6·밀림 0.15
    - `2_three_default`: 0·4·8번 칸, 기본값
    - `3_three_strong`: 0·4·8번 칸, 강하게
    - 고른 칸에만 스캔라인이 나오고 띠가 밀린다.
  - Play(`Map_4_Security`, 측정 중에만 timeScale 1 강제): 보스 메시를 복제한 탐침 오브젝트에서 확인했다. 재선택 간격 2.42~2.77초, 칸 수 2·3 이 번갈아 나왔고 1번 칸은 뽑히지 않았다(`Logs/BossAGlitch/play_samples.txt`). 탐침은 Play 종료로 사라졌다.
  - 콘솔에 이번 변경 관련 오류 없음.
- 알려진 한계:
  - 정점이 3772개로 성겨서, 띠 경계를 가로지르는 큰 면은 칼같이 잘리지 않고 비스듬히 늘어난다(`3_three_strong` 의 팔 부분). 사용자에게 사전 설명했고 B 방식으로 결정했다.
  - 칸은 `RigHead` 기준이라 손을 움직이면 손이 칸을 지나가고, 그때 그 부분의 글리치가 켜졌다 꺼질 수 있다. 손에 칸을 고정하려면 메시 복제본 UV 채널에 바인드 위치를 굽는 방식이 필요하다(미구현).
  - 그림자 패스도 같은 버텍스 함수를 써서 그림자도 함께 밀린다.
- 이번 작업과 무관하게 `ProjectSettings/ProjectSettings.asset`(Standalone 정의 기호에 `UNIVERSAL_PACKAGE` 추가)과 `EditorBuildSettings.asset`·`ShaderGraphSettings.asset`(줄바꿈만)이 수정됨으로 나온다. 에디터가 바꾼 것으로 보이며 이번 커밋에는 넣지 않는다.
- **실제 보스전 육안 확인·게이트 루프 미실행, 커밋 전.**

## 후속: 스캔라인 → 보스B 손 같은 스파크 (사용자 요청)
- 보스B 손 스파크의 정체: `BossBHandSparks` 가 손가락 본에 붙은 `LightningBolt`(LineRenderer) 3개를 켠다.
  - 줄기 모양: 마디 5개, 흔들림 0.18, 굵기 0.024. 모양은 0.03~0.09초마다 다시 만든다.
  - 번쩍임: 0.8~1.6초마다 2~3가닥이 0.26~0.38초 보이고, 끝 45% 동안 사라진다.
  - 재질: `M_BossBHandSpark`(`Project/ParticleAdditive`, HDR 색 3.2/0.32/0.06).
- 사용자 선택: LineRenderer 를 그대로 쓰지 않고 셰이더로 표면에 그린다. 몸·손을 따라가고 가려짐이 자연스러우며 오브젝트가 늘지 않는다.
- `BossACorruptedArmor.hlsl`:
  - 스캔라인을 지우고 `BossAGlitchSpark` 를 추가했다. 칸 안 좌표(0~1)에서 2~3가닥 지그재그 선분까지의 거리로 심선과 약한 번짐을 그린다.
  - 위 수치를 그대로 옮겼다: 마디 5, 흔들림 0.18, 모양 갱신 초당 18회, 끝 45% 페이드.
  - 칸마다 주기(`_sparkPeriod` 1.2초) 안의 무작위 시점에 `_sparkDuration`(0.32초) 동안 보인다.
  - 측면·윗면은 바인드 포즈 깊이축(Y)으로 투영한다. 평면 사이 경사면에서는 투영된 번개가 점선처럼 압축되어(1차 렌더에서 확인) `smoothstep(0.8, 0.95, 최대 법선 성분)` 으로 지웠다.
- `BossAGlitch`: Scanline 항목을 Sparks(`_sparkPeriod`, `_sparkDuration`, `_sparkWidth` 0.008, `_sparkColor` 보스B와 같은 HDR, `_sparkIntensity` 1)로 바꿨다. 프리팹을 다시 저장해 옛 스캔라인 값을 지웠다.
- 검증:
  - 리컴파일 통과, 셰이더 오류 0.
  - 프리뷰 렌더 `Logs/BossAGlitch/spark_*.png`. `spark_5`·`spark_6` 은 점선 수정 후 정면·측면이며, 확인을 위해 항상 보이도록 렌더했다.
  - Play 탐침: 마스크·스파크 값·색(SetColor 로 선형 변환되어 보스B 재질과 같은 값) 전달을 확인했다. 새 콘솔 오류 없음.
- 실제 시간 흐름에서의 번쩍임 빈도·밝기는 육안 확인이 필요하다.

## 다음 할 일 (Next Steps)
- 실제 보스전에서 직접 보며 확인한다. 밀림 폭·확률·스파크 빈도·굵기·세기가 과하거나 약하면 `BossA.prefab` > `Gripper Robot` > `BossAGlitch` 값을 조정한다.
- 확인이 끝나면 AI_TODO2 보스-18 을 체크하고, intent-086 을 resolved 로 바꿔 `clear/` 로 옮긴다.
