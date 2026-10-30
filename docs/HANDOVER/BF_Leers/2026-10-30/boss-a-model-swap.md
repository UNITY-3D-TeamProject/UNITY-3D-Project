# 보스A 모델 교체: Virus_King_Boss (AI_TODO2 보스-24·25, intent-091)

## 작업 요약 및 방법
- 보스A 의 모델을 `ThirdParty/Gripper Robot`(스킨드, 본 15개)에서 `ThirdParty/CuteRobots/New Boss/Virus_King_Boss.fbx`(본·애니메이션 없는 메시 5조각)로 바꿨다. 사용자 결정 4가지(팔은 그대로 둠, 오염 장갑 셰이더·칸 글리치를 새 텍스처로 이음, 원본 형식 사망 동작, 약점은 가슴 코어)대로 진행했다.
- `BossA.prefab` (Editor 로 `LoadPrefabContents` 편집, YAML 직접 편집 없음):
  - 옛 `Gripper Robot` 을 언팩해 `Virus King Boss`(배율 45, 위치 y -13.6)로 교체. 본 대신 빈 피벗을 둔 계층: `Body` → `Arm_L/R` → `Palm_L/R`, 메시 조각은 각 피벗 밑.
  - 옛 모델의 `WeakPointCandidate_1/HeadWeakPoint`·`GrabPoint`·`HandAnchor` 를 새 피벗 밑으로 옮겨 기존 참조를 유지.
  - `BossHand._bone` 은 `Palm_L/R` 로, `_fingerCurls` 는 비움(손가락 본 없음, 주먹·그립은 손 전체 회전만). `_fingerAxis`=+Z, `_palmAxis`=±X, `_fistDownAxis`=-Y, `_boneLift` 등 높이값을 새 손 크기에 맞춰 다시 정함.
  - 약점은 가슴 육각 코어 위치(구 반지름 4.5m 월드)로 이동. `BodyHitbox`·`Hand_L/R` 콜라이더·NavMesh 장애물·어깨 대포 위치를 새 크기로 조정.
- 코드:
  - `BossAMaterialSpace`: `Renderer` 를 받음. 일반 메시는 앵커 로컬(Y 위, Z 앞)을 셰이더 규약(Z 위, -Y 앞)으로 돌리는 -90°X 행렬을 씀. 스킨드는 기존과 동일.
  - `BossAGlitch`: `_renderers`(여러 조각이 같은 칸·마스크 공유), `_cellArea`(칸 범위를 직접 지정) 추가. 비우면 기존처럼 자기 스킨드 메시 bounds.
  - `SilhouetteReaction`: 자동 셸이 SkinnedMeshRenderer 가 없으면 MeshRenderer 마다 셸을 만들도록 폴백(`BuildMeshShellRenderers`). 이게 없으면 `HeadWeakPoint` 에서 "실루엣 렌더러가 연결되지 않았습니다" Assert 가 났다(Play 에서 발견·수정).
- 셰이더: `BossACorruptedArmor.hlsl` 에 스파크 계산을 `BossAGlitchSparkEmission` 으로 뽑고, 새 `BossAVirusKingArmor_float` 추가(텍스처 색 유지, 밝은 노랑·주황 칠을 빨간 에너지 발광으로, 가슴 육각 코어·바이저, 칸 글리치 공유). `SG_BossAVirusKingArmor.shadergraph` 는 기존 그래프 복사 후 함수 이름만 교체. 머티리얼 `M_BossAVirusKing{Body,Arm,Hand}`(Arm·Hand 는 발광 0).
- 애니메이션: `BossA_Idle.anim`(1초 루프, 몸통 오르내림·팔 흔들림), `BossA_Die.anim`(1.67초, 솟았다 앞으로 숙이며 주저앉고 팔이 늘어짐)을 만들어 `BossA_Override` 의 Idle·Death 슬롯에 연결. Take Damage 는 옛 Gripper 클립이 남아 있다(새 모델 본 경로가 달라 재생되지 않음).
- 실행자: Claude 직접 진행(Codex CLI 는 설치돼 있으나 이번 작업은 Editor 수치 조정이 반복돼 위임 비용이 커서 직접 수행).

## 결정 사항
- 모델은 본 없이 피벗 계층으로 간다. `BossHand` 는 `_bone` 이 Transform 이면 되므로 코드 변경 없이 피벗을 그대로 쓴다.
- 손가락이 따로 움직이지 않으므로 주먹·그랩 오므림은 없다(손 전체 회전만).
- 새 셰이더 함수는 같은 hlsl 에 두고 옛 그래프·머티리얼은 그대로 둔다(롤백 가능).

## 현재 상태 및 이슈
- 검증: 리컴파일 통과, 셰이더 오류 0, Play 에서 보스 생성 시 콘솔 에러 0. Idle 클립 재생, 몸통·손이 같은 글리치 마스크 공유, 약점이 코어 위치(rest 0, -0.2, -0.034)에 있음 확인. 프리뷰 렌더(`Logs/BossAModelSwap/`)로 정면·측면·사망 3컷·슬램·주먹·박수·그랩 자세 확인.
- 확인하지 못한 것(직접 플레이 필요): 실제 보스전 패턴 전체(내려찍기·그랩·박수·그로기) 시 손 위치·접촉, 피격 판정(약점 반지름 4.5m, 몸통·손 콜라이더), 사망 동작 길이·연출, 스파크·글리치 강도, 이펙트 위치(보스-16·17).
- 알려진 한계: 손가락이 없어 그랩 집게가 오므려지지 않는다. 팔은 손과 따로 움직이지 않는다(사용자 결정). Take Damage 클립 미교체.
- Play 확인 중 `AttributeClamper/Regenerator/MoveSpeedReducer not assigned` 경고가 나왔다. 스포너 없이 프리팹만 생성한 탓으로 보이며(`BossA(Clone)`), 이번 변경과 무관하다고 판단했으나 옛 모델에서 같은 경고가 나는지는 대조하지 않았다.
- 이번 작업과 무관한 변경이 작업 트리에 있다: `Map_4_Security.unity`(CombatZone `_clearDrop*` 필드·위치 값)·`ProjectSettings/*`. 커밋에 넣지 않는다.
- 새 FBX·텍스처·`Metrial/BigBoss*.mat` 는 `Assets/ThirdParty` 라 Git 에 안 올라간다. 다른 PC 에서 보려면 SVN 업로드가 필요하다.
- **직접 플레이·게이트 루프 미실행, 커밋 전.**

## 다음 할 일 (Next Steps)
- 보스전을 직접 플레이해 위 "확인하지 못한 것"을 본다. 손 높이(`_boneLift`·`_fingersDownLift`·`_sinkDepth`)나 콜라이더가 어긋나면 `BossA.prefab` 의 `Hand_L/R` 에서 조정한다.
- 사망 동작·약점 크기·발광 세기가 마음에 안 들면 `BossA_Die.anim`, `HeadWeakPoint` 반지름, `M_BossAVirusKingBody` 의 `_CoreIntensity`·`_EyeIntensity` 를 조정한다.
- 문제없으면 AI_TODO2 보스-24·25 를 체크하고 intent-091 을 resolved 로 바꿔 `clear/` 로 옮긴다. 보스-16·17(이펙트 위치)·보스-18(글리치)도 새 모델 기준으로 다시 본다.
