# 사운드 생존 조건을 CharacterMediator로 이동

## 요청과 확정 범위

- 사용자 요청: `isDead` 같은 캐릭터 상태에 따른 사운드 재생 허용 여부는 CharacterMediator에서 판단하고 이벤트 전달을 결정하도록 수정한다.
- Claude Code Opus가 조사·계획을 작성하고 Codex의 검토를 반영해 계획을 확정했다. Claude Code Sonnet이 제품 코드를 수정했고, Codex가 저장된 변경과 Unity 컴파일·상태 동작을 직접 검증했다.
- 변경 파일은 CharacterMediator, SoundMediator, SoundController 세 개다. 기존 사용자의 SoundController LateUpdate 이동 상태 갱신 구조와 사운드 정책 값은 보존했다.
- AudioManager의 실제 SFX 루프·정지는 기존 TODO로 유지한다. git·씬·프리팹·SO·권한 설정은 변경하지 않았다. HANDOVER·Intent·포인터·대화 원문 파일은 작성하지 않았다.

## 책임 변경

### CharacterMediator

기존 `_isDead`를 캐릭터 생존 여부의 기준으로 사용한다. 하위 사운드 구성에 사망 상태를 복사하지 않는다.

- 점프·스킬 단발음·스킬 루프·보드 상태 이벤트를 private 콜백으로 중계한다.
- 사망 후 점프·단발음·루프 시작·보드 부착 전달을 차단한다. 루프 정지와 보드 해제는 사망 후에도 전달한다.
- `_isDead = true` 직후 이동 상태 조회 연결과 기존 루프를 정리한다. 외부 OnDeath 알림과 스킬 비활성화보다 먼저 정리하므로 사망 처리 중 재진입하는 발사 등에도 새 소리를 요청하지 않는다.
- 이동 상태 연결을 `BindSoundMoveState()`로 모으고 사망 상태에서는 연결하지 않는다. 연결을 끊어 SoundController의 자동 이동·착지 갱신에서도 새 재생 요청이 생기지 않게 한다.
- `SyncSoundState()`는 사망 상태면 반환한다. 풀 재사용에서는 일반 소리 정리·착지 기준 초기화 후 활성 상태일 때 이동 조회를 다시 연결하고 현재 스킬 상태를 동기화한다.

근거: [바인딩](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/CharacterMediator.cs:296), [동기화와 사운드 콜백](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/CharacterMediator.cs:464), [사망 처리](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/CharacterMediator.cs:599).

### SoundController와 SoundMediator

- SoundController의 `_isDead` 필드를 제거했다. `CanPlay`는 컴포넌트의 `isActiveAndEnabled`만 확인한다.
- SO·클립·매니저 유효성 검사와 컴포넌트 활성 검사, 이동·착지 사운드 자체의 판정은 유지했다.
- SoundMediator는 신호·조회 함수를 전달하며 생존 여부를 소유하거나 판단하지 않는다.
- 기존 public `NotifyDeath()` / `ResetForReuse()`의 삭제와 유지가 검토되었다. 기존 공개 인터페이스 보존 원칙에 따라 호환용으로 유지하되 각각 일반 루프 정리 / 착지 기준 초기화만 수행한다. 사망 상태를 저장하거나 이후 소리를 차단하지 않는다. CharacterMediator의 정상 경로에서는 이 호환 메서드를 호출하지 않는다.
- 새 공개 API, Obsolete 경고, 사운드 정책 값은 추가하지 않았다.

근거: [SoundController 활성 검사](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/CharacterSound/SoundController.cs:57), [호환 메서드](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/CharacterSound/SoundController.cs:201), [SoundMediator](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/SubMediators/SoundMediator.cs:159).

## 직접 수행한 검증

- Unity CLI `1.0.0-beta.13`이 최신임을 확인했다. Unity Editor는 `6000.4.8f1`이다.
- Unity recompile: completed, success true, exit 0, 컴파일 오류 0·경고 0.
- 변경 대상의 `git diff --check` 통과.
- 변경 전 메모리에 보관한 소스와 비교해 SoundController의 LateUpdate, UpdateLocomotion, UpdateLanding, ApplyMoveLoop, StartLoop, StopLoop, PlayOneShot, EnsureSkillSoundMap 본문이 보존됐음을 확인했다. CanPlay의 의미와 사망 상태 처리만 요청 범위에서 바뀌었다.
- SoundController/SoundMediator에는 `_isDead`가 없고, CharacterMediator가 하위의 NotifyDeath/ResetForReuse를 호출하지 않음을 확인했다.
- 임시 preview scene에서 29개 상태 검증이 모두 통과했다.

검증 내용은 다음과 같다.

1. 생존 시 이동 연결, 차지·보드·이동 루프 요청, 점프·발사 단발 요청 전달.
2. 외부 OnDeath 관찰자보다 먼저 기존 루프와 이동 조회 연결 정리.
3. 사망 처리 안에서 재진입하는 점프·발사·차지 시작·보드 부착 차단, 늦게 온 스캔 단발음 차단.
4. 사망 후에도 루프 정지·보드 해제 전달.
5. 사망 상태의 재바인딩 차단, CharacterMediator/SoundMediator/SoundController 각각의 재활성화 시 이동 연결과 루프가 복구되지 않음.
6. 사망 후 LateUpdate에서 착지음·이동음 재요청 없음.
7. 재사용 후 이동 재바인딩, 차지·점프 재요청, 첫 접지 표본 무음과 이후 정상 착지 요청.
8. CharacterMediator 비활성화 시 구독·루프·이동 연결 정리, 비활성 중 재사용 시 즉시 바인딩하지 않음, 재활성화 시 연결 복구.
9. 호환용 정리 메서드가 사망 상태를 저장하지 않음, 이동·착지 기본값 보존.

검증은 Editor의 임시 객체에서 reflection으로 lifecycle과 실제 이벤트 신호를 호출한 것이다. 단발음은 임시 AudioManager에 대한 PlaySfx 요청 여부로 관찰했으며 실제 소리를 재생하지 않았다. 테스트 객체·SO·클립은 finally에서 제거하고 AudioManager singleton을 원복했다. 테스트 소스·씬·에셋 파일을 저장하지 않았다.

실제 플레이에서의 Fire.Stop 실행 순서·적 풀 반납·보스 부활 전체 시퀀스, 청취와 루프 AudioSource의 실제 재생·정지는 미검증이다. 컴파일 경고 0건은 이번 컴파일 결과이며 기존 Editor 콘솔 경고 전체가 0이라는 의미는 아니다.

## Claude 중계 결과와 제한

- Opus 조사와 계획 수정 호출은 모두 `state: completed`, exit 0, Remote Control 활성 증거를 확인했다.
- Sonnet 구현 호출은 저장된 코드 변경이 있었지만 `idle/working` 상태에서 완료 확인에 실패했다. 잔여 세션으로 재개가 거절되어 소유권 검증을 거쳐 stop한 후 같은 UUID로 재개했다.
- 재개 호출은 마지막 Remote Control 상태가 `unknown`이어서 활성 증거 확인에 실패했고, `final_text`는 수신하지 못했다. Sonnet의 최종 완료 보고나 해당 호출의 정상 공개 완료를 확인했다고 간주하지 않는다.
- 이후 소유 세션을 중계기의 검증으로 정상 stop했다. Codex는 저장된 diff와 위 컴파일·29개 상태 검증을 직접 확인했으며, Claude 대신 제품 코드를 작성·적용하지 않았다. 중계기의 소유권·완료·공개 연결 검증이나 권한은 변경하지 않았다.

이 기록은 현재 코드 기준이다. 이전 사운드 설명 문서에 있는 SoundController의 사망 상태 보유 설명은 이 변경 전 동작에 해당한다.
