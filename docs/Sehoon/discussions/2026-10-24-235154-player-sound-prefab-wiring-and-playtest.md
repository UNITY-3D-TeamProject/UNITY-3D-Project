# Player 사운드 SO 연결과 Play 검증

- 일시: 2026-10-24 23:51 KST
- 브랜치: `feature/PlayerSound`
- 목적: Player용 SO 10개를 플레이어에 연결하고 임시 음원으로 실제 재생·반복·정지를 확인.
- 역할: Claude Opus 조사·설계·최종 정적 리뷰, Codex Unity Editor 에셋 작업·Play 검증·요약.

## 사용자 확정 사항

- 별도 `Assets/_Project/Prefabs/Actions/SoundPrefab.prefab`을 만들고 `Player.prefab`에 중첩한다.
- 이번 사운드 에셋 연결에 한해 Codex가 Unity Editor에서 직접 생성·연결하도록 명시 승인했다. Claude는 설계·리뷰를 맡았다. 기존의 다른 개발 작업 역할을 변경한 것은 아니다.
- 음원 품질이나 용도는 평가하지 않고 임시 클립으로 정상 작동만 확인한다. 테스트 클립과 AudioManager는 검증 후 제거한다.

## 저장한 변경

- [SoundPrefab.prefab](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Prefabs/Actions/SoundPrefab.prefab) 신규 생성. 같은 GameObject에 `SoundController`와 `SoundMediator`를 두고 서로 참조를 연결했다.
- [Player.prefab](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Prefabs/Player/Player.prefab)에 SoundPrefab을 root 직속 자식으로 중첩하고 `CharacterMediator._soundMediator`를 명시 지정했다. Move·Skill·Camera의 스폰 잠금 대상 GameObject와 분리되어 있다.
- `PrefabUtility`, `SerializedObject`로 생성·저장했다. `.meta`와 fileID는 Unity가 생성했다. 기존 Player 변경은 새 중첩 자식과 참조를 추가한 75줄뿐이다.

| 설정 | 연결한 Player SO |
| --- | --- |
| `_footstep` | `SOAudioData_SfxPlayerFootstep` |
| `_jump` | `SOAudioData_SfxPlayerJump` |
| `_land` | `SOAudioData_SfxPlayerLand` |
| `_hit` | `SOAudioData_SfxPlayerHit` |
| `Attack / Shot` | `SOAudioData_SfxPlayerShoot` |
| `Attack / Charge` | `SOAudioData_SfxPlayerCharge` |
| `Scan / Wave` | `SOAudioData_SfxPlayerScan` |
| `Roll / Start` | `SOAudioData_SfxPlayerRoll` |
| `SpawnVehicle / Mount` | `SOAudioData_SfxPlayerBoardMount` |
| `SpawnVehicle / Move` | `SOAudioData_SfxPlayerBoardMove` |

SO 위치는 `Assets/_Project/ScriptableObjects/Audio/Sfx/Player`다. 이름은 실제 스킬 prefab의 `_skillName`(Fire의 이름은 Attack)과 SkillMediator의 사운드 phase 발행 위치를 대조했다.

## 테스트 준비

- Unity `6000.4.8f1`에서 원래 열려 있던 깨끗한 `Map_2_Feed` 씬을 Play했다. 스폰된 실제 `Player(Clone)`을 사용했다.
- 프레임 카운터 증가와 스폰 연출 종료를 확인했다. 맵의 초기 컷씬이 게임 시간과 SFX를 정지하므로 기존 `CinematicManager.Stop()`으로 연출을 정상 종료했다. 이후 timeScale 1, AudioListener.pause false를 확인했다.
- 기존 AudioManager prefab을 런타임에만 임시 생성했다. 공유 씬에는 저장하지 않았다.
- SO의 원래 JSON과 dirty 상태를 보관하고 런타임에만 클립을 넣었다. 반복음에는 Archanor의 `loop_laser`, `loop_energy`, `loop_laser2`, 단발음에는 같은 패키지의 `shoot_laser*`와 Polygon Arsenal의 `poly_shoot01`, `poly_shoot_laser01`을 사용했다. ThirdParty 원본이나 SO 파일에 저장하지 않았다.

## 실제 검증 — 33/33 통과

대부분은 실제 스폰된 플레이어의 중재자에 자동 신호를 넣어 검증했고, 이동·점프·공격은 실제 명령 경로도 추가 확인했다. 키보드·마우스를 사람이 직접 조작한 검증과 구분한다.

1. 플레이어에 사운드 컴포넌트 쌍이 하나씩 존재하고 root 중재자와 연결됨.
2. 실제 이동 상태 조회 델리게이트가 자동으로 연결됨.
3. Jump·Hit·Shoot·Roll·Scan·BoardMount 단발음 각각이 해당 SO의 클립으로 실제 AudioSource에서 재생됨.
4. 차지 루프 재생과 차지 종료 시 정확한 핸들 정지.
5. 발소리 재생, 보드음으로 교체하면서 발소리 정지, 하차 시 발소리 복구, 이동 종료 시 정지.
6. Land 단발음, 구르기·공중 상태의 이동음 억제.
7. 활성 AudioListener 하나와 정상 일시정지 상태.
8. 실제 Attack 실행 요청이 차지음을 시작하고, 해제 요청이 차지음을 멈추면서 사격음을 재생함.
9. 여러 실제 프레임에 걸쳐 루프 샘플 위치가 증가하고, 소스 위치가 플레이어를 추적함.
10. AudioSource와 AudioListener의 출력 데이터에서 각각 peak `0.573906064`를 확인함. 초기 버퍼 읽기에서는 0이었으나 후속 프레임의 재측정에서 둘 다 0이 아닌 값을 확인했다. 출력 확인은 사람의 청취 평가가 아니다.
11. 같은 SO를 쓰는 두 Controller의 핸들이 다르고, 한쪽을 정지해도 다른 쪽이 유지됨. 다른 쪽 비활성화 시 해당 루프도 정지함.
12. 실제 이동 입력 명령으로 위치가 변하면서 발소리가 시작되고, 입력 해제 후 정지함. 프레임 감시 콜백이 완료됨.
13. 실제 점프 명령에서 점프음, 실제 공중 이동 후 접지에서 착지음이 재생됨.

실행 중 테스트용 프레임 감시 헬퍼의 delegate 타입 오류를 수정했다. 파일에 저장하지 않은 eval 헬퍼 오류였으며 제품 코드의 컴파일 오류는 아니었다. 최종 검증 종료 시 Editor 콘솔 ground truth는 오류 0, 경고 0, compilationFailed false였다.

## 원복과 최종 상태

- 임시 클립 10개를 원래 SO JSON으로 복원했다. 모든 ClipCount가 다시 0이고 원래 dirty 상태도 복원했다.
- 테스트용 두 번째 캐릭터·AudioManager·프레임 감시 콜백·메모리 상태를 제거했다. 테스트 이름의 잔여 GameObject는 0개다.
- Play 종료 후 원래 `Map_2_Feed` 씬은 dirty false이며, runInBackground는 원래 false로 복원했다.
- 저장된 SoundPrefab을 다시 읽어 공통 4칸·스킬 6행·고유 SO 10개, 양방향 중재자 참조, 중첩 prefab 원본을 재확인했다. 새 SoundPrefab의 missing script는 0이며 ThirdParty 의존성은 없다.
- 기존 미커밋 `SoundController.cs`와 ProjectSettings 두 파일의 SHA256이 작업 전후 동일했다. 기존 변경을 수정하거나 커밋하지 않았다.
- `git diff --check`는 Unity가 생성한 stripped MonoBehaviour의 빈 `m_Name: ` 한 줄에서 trailing whitespace를 보고한다. 엔진 직렬화 결과이며 파일 구조를 수동 수정하지 않았다.

## 미검증과 실사용 조건

- 보드 스킬 전체를 손으로 조작하는 흐름, 사망·씬 전환 전체 흐름, 사람의 실제 청취는 이번에 실행하지 않았다. 보드 사운드의 교체·정지 신호 경로는 Play에서 검증했다.
- **최종 저장 상태의 SO는 클립이 비어 있으므로 바로 소리가 나지는 않는다.** 정식 사용 시 각 SO에 실제 음원을 넣어야 한다.
- **현재 맵 씬에는 AudioManager가 없다.** 테스트용 매니저는 제거했으므로 정식 사용 시 맵에도 AudioManager 배치가 필요하다. 이번 작업에서 공유 씬을 변경하지 않았다.

## Claude 협의·리뷰 상태

- 최초 Opus 조사 호출은 agents `idle/blocked`로 중계 완료 검증에 실패했다. 동일 소유 세션에 저장된 조사 응답을 Codex가 직접 읽어 매핑·도구 제약을 확인했고 소유 세션을 정리했다. 이 호출을 정상 완료로 보고하지 않았다.
- 사용자 선택·역할 변경 승인 후 Codex가 에셋을 구현·검증했다.
- 최종 Opus 리뷰는 `state: completed`, exit 0, Remote Control 활성 증거를 모두 확인했다. Claude가 prefab의 GUID·fileID·phase·참조·스폰 잠금 분리·정지 수명을 읽어 검토한 범위에서 B/M/Minor/Nit 문제는 없다고 보고했다. 33개 Play 검증은 Claude가 아닌 Codex가 수행했다.
- 원문 확인처는 Claude 앱 Code 탭이며 별도 원문 로그·HANDOVER·Intent·포인터는 생성하지 않았다.
