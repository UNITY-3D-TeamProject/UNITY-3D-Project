# TestScene Player Spawn Dissolve 적용·검증 및 남은 복구 결정

- 일시: 2026-10-19 00:38 KST
- 목적: Showcase 오른쪽 Gravity Angel Dust Dissolve를 활용하여 TestScene Player가 아래에서 위로 생성되게 한다.
- 참여: Claude Opus 상세 설계·검토 보완·재질 읽기 전용 감사, Claude Sonnet 구현·수정·일회성 도구 정리, Codex 계획 검토·중계기 복구·독립 검증.
- 사용자 승인: A안(임시 INab 재질 후 원본 Robot 재질 복원), Assets/Test/SpawnDissolve 테스트 격리 및 C# 위치 예외, 프로젝트 관련 자료의 Claude 서비스 전송, Codex의 중계기 응답 파서 수정 및 구현 재개.
- 상태: **연출 적용 및 Play 검증 완료. 원본 데모 재질 재저장 부작용의 복구는 미완료이며 사용자 선택 대기.**

## 적용 결과

- 씬: `Assets/Test/TestScene.unity`
- 인스턴스: `Player/PlayerSpawnDissolve/Interactive Dissolve`
- 남은 신규 파일:
  - `Assets/Test/SpawnDissolve/PlayerSpawnDissolveDriver.cs`
  - `Assets/Test/SpawnDissolve/MikeZ_Readable.fbx`
  - `Assets/Test/SpawnDissolve/SpawnBurn.mat`
  - Unity가 생성한 각 .meta
- 일회성 Editor installer와 임시 검증 훅은 Claude가 Unity API로 정리했다. installer GUID의 외부 참조가 없음을 확인했다.
- 원본 Player prefab은 유지한다. Scene의 기존 35개 직렬화 블록을 비교했을 때 Player prefab instance의 added GameObject 항목만 바뀌었고, 카메라·Testing 등 다른 기존 블록은 동일했다.

## 확정 구현

- 6초 선형, Plane 마스크, 아래→위 이동. 본체에 Angel Dust VFX를 적용하고 Body/Lens/무기 2개/PowerPod의 5개 재질 슬롯을 같은 마스크로 제어한다.
- runtime에서 독립 INab Material 1개를 사용한다. 원본 재질 에셋을 수정하지 않고 생성 완료 시 원래 Robot 재질 슬롯을 복원한다.
- 원본 FBX를 수정하지 않고 읽기 가능한 사본 메시를 생성 중에만 참조한다. 실제 Player mesh는 `Art/Mesh/MikeZ.fbx`였다. 이전 계획에서 언급한 `Anim_ZMIKE_Blinking.fbx`는 메시가 없는 애니메이션 전용 파일로 확인됐다.
- 원본 bounds와 mask 끝점에서 패딩 비율을 구했다. 임의 0.1m 여유값 및 5초 타임아웃은 추가하지 않았다.
- 아래 패딩 비율 0.406748, 위 패딩 비율 0.183781. 대상 bounds Y 약 -0.1474~1.5175, 시작 plane Y -0.8245716, 종료 Y 1.82347715.
- 생성 완료 시 재질을 복원한다. 원본 2초 VFX 이벤트 간격 이후 입자 수가 정확히 0이면 effect 비활성화 → 원본 메시 복원 → runtime 재질 파기 순서로 정리한다.
- 중단·비활성화·파괴 시 멱등 복원한다. 재활성화 시 자동 재생하지 않는다. Player 자식으로 이동을 추종한다.

## 검증과 한계

### Claude Sonnet 수행

- 컴파일 오류·경고 없음.
- 초기 프레임 준비 상태, 아래→위 화면 생성, VFX 자동 시작 억제, 완료 재질 복원 및 입자 종료 후 메시 복원 확인.
- driver disable/재활성화, GameObject 파괴, Player 순간이동 시 추종 확인.
- 원본·사본 FBX 바이트 및 importer 차이 확인. 개별 skin weight/bindpose 값 전체 비교는 미실행.
- 임시 검증 훅 정리.
- 빌드와 실제 사용자 입력 조작은 미실행.

### Codex 독립 수행

- 프레임 카운터 변경 조건이 충족되어 실제 Play 진행을 확인했다.
- 실제 Game view에서 생성 중 머리가 아직 없는 상태와 하단에서 발생하는 Angel Dust, 이후 전신 생성 상태를 확인했다.
- 완료 상태에서 Body/Lens 원본 재질 경로, 원본 MikeZ mesh 경로, `AreMaterialsSwapped=false`, `IsMeshSwapped=false`, `RuntimeMaterial=null`, `ParticleCountAtRestore=0`, effect 비활성 상태를 확인했다.
- Play 검증 중 새 Console 오류 없음. 정리 후 `compilationFailed=false`, Editor error/warning 0 확인.
- 최종 TestScene 열림·Play 정지·dirty=false. Time.timeScale=1, runInBackground=false로 검증 설정을 되돌렸다.
- 보호 대상 9개 중 Player prefab, 원본 Anim FBX와 meta, Showcase, VFX graph, ProjectSettings 3개 등 8개 해시는 작업 전과 같았다. 아래 Statue Burn 1 해시는 달랐다.
- 일회성 installer 정리 전후 데모 재질 8개 해시는 모두 동일했다.

### 외관·동작 한계

- A안 선택에 따라 생성 중에는 Statue 재질 외관과 불투명 렌즈를 사용한다. 약 6초 시점 원래 Robot 외관으로 전환되는 차이가 있다.
- 입자 수가 컬링 등의 이유로 0으로 집계되지 않으면 정리 대기가 끝나지 않을 수 있다. 임의 타임아웃은 넣지 않았다.

## 중계기 복구

- 사용자 승인 후 `C:/Users/inha/.codex/claude-codex-live.py`의 ResponseStream만 Codex가 수정했다.
- 메시지 전체 누적 텍스트를 개별 tool-only assistant 이벤트와 잘못 비교하던 문제를 content block 단위로 구분했다.
- 혼합 text/tool, 여러 text block, 누적 snapshot, suffix 보완, 실제 불일치 거부, 비스트리밍 및 error result 회귀 검증을 통과했다.
- AST 비교로 ResponseStream 외 코드가 동일함을 확인했다. 채팅·프로젝트·표시기·일회용 확인·요청 표시 후 실행 및 권한 검증은 유지했다.
- 수정 후 실제 도구 사용을 포함한 Claude 협업 호출들이 정상 종료했다.

## 미완료: INab 데모 재질 재저장 부작용

설치 중 Showcase를 additive로 읽었을 때 ExecuteAlways 효과가 sharedMaterial 상태를 갱신하고, 재질 8개가 2026-10-19 00:16:54 부근 함께 재저장됐다. 원본 보호 조건을 충족하지 못한 부분이다.

| 대상 | 상태 |
|---|---|
| Statue Burn 1 | Codex 작업 전 SHA256 C26882C73EDB52A799C646C3F6AB3696DD268F78DDD5D3A22D1B00D4805FCD0C → 현재 961CE2257F0667626EC04923EBD03041CFE6FF2D316CC9E01D5EFCBA4A687325. 내용 변경 확인 |
| Statue Smooth 1 | 작업 전 해시 없음. 현재와 로컬 package 내용 다름 |
| Arms/Body/Legs Burn 1 | 각 작업 전 해시 없음. 현재와 로컬 package 내용 다름 |
| Arms/Body/Legs Smooth 1 | 각 작업 전 해시 없음. 현재와 로컬 package 내용 다름 |

- 로컬 원본 후보: `C:/Users/inha/AppData/Roaming/Unity/Asset Store-5.x/INab Studio/Particle Systems/Interactive Dissolve Effects.unitypackage`.
- Opus 읽기 전용 감사: 430개 asset의 GUID 매핑은 맞고 421개 내용은 바이트까지 같지만, meta의 packageVersion/uploadId는 cache 2.1/864772와 설치본 2.2.0/987672로 다르다.
- 패키지의 Statue Burn bytes, 줄바꿈/BOM 후보와 테스트 템플릿으로 재구성한 후보가 작업 전 SHA256과 일치하지 않았다. Codex도 package/현재의 개별 변경 줄 조합과 줄바꿈 후보를 읽기 전용으로 확인했지만 일치 후보를 찾지 못했다.
- 따라서 package 복원은 정확한 작업 전 복원이 아니라 package 기본값으로의 교체가 된다. 기존 개인 수정이 있었다면 덮어쓸 수 있다.
- 자동 승인 검토가 ThirdParty 재질 8개 복구 호출을 거부했다. 이유: 테스트 격리 범위 밖이며 7개의 실제 변경 여부·원본 상태가 불확실하여 잘못된 복구 시 기존 내용이 손상될 수 있음.
- 거부된 복구는 실행하지 않았다. 이후 읽기 전용 감사와 승인된 Test 폴더의 일회성 installer 정리만 수행했다. 재질 추가 덮어쓰기는 없었다.

## 사용자 선택이 필요한 다음 단계

- **A:** 현재 8개 재질을 `Assets/Test/SpawnDissolve/RecoveryBackup~/`에 백업하고, 해당 8개 .mat 내용만 로컬 package 기본값으로 복원한다. .meta·GUID 및 다른 package 자산은 유지한다. package 버전 표기 차이와 기존 개인 수정 덮어쓰기 가능성을 허용하는 선택이다.
- **B:** 현재 8개 상태를 유지하고, 정확한 작업 전 백업 또는 적합한 2.2.0 원본을 확보한 뒤 복구한다. 복구 문제는 미완료로 남는다.
- 위 선택은 Claude/Codex의 합의와 다르며 아직 사용자가 승인하지 않았다.

