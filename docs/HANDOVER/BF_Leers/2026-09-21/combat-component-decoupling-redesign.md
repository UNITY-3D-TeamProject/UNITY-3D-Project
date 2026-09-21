# Combat 컴포넌트 재설계 — 의존성 0 + HP 옵서버 (2026-09-21)

## 1. 작업 요약 및 방법

사용자가 "기존 Attribute 코드를 안 건드린다"를 우선한 이전 설계(intent-004)를 뒤집고, 3가지 새 기준(이동 컴포넌트만큼 단순 / Attribute 직접 참조 금지 / 의존성 ≈ 0)으로 Combat 모듈을 전면 재작성했다.

- `Assets/_Project/Scripts/Combat/SHitInfo.cs` — `SOAttributeEffect`/`IEffectTarget` 필드를 제거하고 `GameObject Attacker` + `float Damage`만 남김.
- `Assets/_Project/Scripts/Combat/CharacterCombat.cs` — `IEffectTarget`, 속성 키 문자열, 이중 입구(`ReceiveHit`/`NotifyAttributeChanged`), 수동 이벤트 순서 보정을 전부 제거. `Health` 프로퍼티 setter에서 사망 판정, `ReceiveHit`은 `OnHit`만 발사(데미지 적용은 하지 않음). `using`이 `System`/`UnityEngine`뿐.
- `Assets/_Project/Scripts/Observer/AttributeToCombatObserver.cs` (신규) — `AttributeSet`을 대신 구독해 `CharacterCombat.Health`에 값을 밀어넣는 Observer. `AttributeToMotorAdapter`와 의도적으로 다르게 감(`Debug.LogError`로 연결 누락 소리내기, `Post` 콜백 구독, `_camelCase` 네이밍).
- `Assets/Test/Combat/CombatManualTest.cs` — 5개 시나리오를 새 API에 맞춰 전면 교체. 클래스/파일명은 유지해 `Assets/Test/Combat.unity`의 GUID 참조를 보존.
- `Assets/_Project/Scripts/Observer.meta`, `AttributeToCombatObserver.cs.meta` — 신규 폴더/파일이라 GUID를 수동 발급(PowerShell `[guid]::NewGuid()`)해 직접 작성 (Unity Editor를 열 수 없는 상황이라 임포트 자동 생성을 대신함).

작업 전 GoF 디자인 패턴 23종을 전수 대입해 Observer 채택 근거와 Bridge/State/CoR 기각·보류 사유를 문서화했다(아래 결정 사항 참고).

## 2. 결정 사항 (채택된 아키텍처 규칙)

- **AttributeSet↔Combat 연결은 Observer, Adapter 아님.** 역할이 "인터페이스 변환"이 아니라 "변경 통지 구독"이라서. Bridge(Combat이 인터페이스 직접 소유)도 검토했으나 Combat 내부 코드가 늘고 인스펙터 직렬화 문제가 생겨 기각.
- **옵서버는 `Post` 콜백을 구독한다** (`On`이 아니라). 나중에 실드/뎀감이 `On`에서 HP를 되돌려쓸 가능성 대비 — 사망 판정은 값이 완전히 확정된 뒤 봐야 한다.
- **연결 누락은 `Debug.LogError`.** `Debug.Assert`는 `UNITY_ASSERTIONS` 조건부라 릴리즈 빌드에서 사라져 "무증상으로 안 죽는 버그"를 못 잡는다.
- **중재자 → Combat 지시 채널은 public 메서드로만 연다.** Combat이 중재자를 구독하는 형태는 기준 3(의존성 0) 위반이라, 배선은 항상 중재자 쪽이 한다. 구체적인 메서드(예: `Revive`, `SetInvulnerable`)는 아직 안 만듦 — 필요해질 때 추가.
- **`_isDead` bool은 1개까지만.** 무적 등 두 번째 상태 플래그가 필요해지면 State 패턴으로 갈아탄다. 데미지 감쇄 파이프라인(무적/실드/방어력)은 Chain of Responsibility 자리이고, 그 위치는 Combat이 아니라 스킬 매니저 쪽으로 못 박아둠.
- **데미지를 AttributeSet에 반영하는 책임은 이번 범위 밖.** 옵서버는 AttributeSet → Combat 읽기 방향만 담당.

## 3. 현재 상태 및 이슈

- **컴파일/PlayMode 검증 완료 (같은 세션 내 후속 작업).** Editor MCP가 세션 초반 연결되지 않아 처음엔 미확인 상태로 문서화했으나, `unity` CLI로 Editor를 열고(`unity open`) `com.unity.pipeline` 패키지를 설치(`unity pipeline install`)해 MCP를 연결한 뒤 직접 검증을 마쳤다.
  - 컴파일 에러 0건 (`recompile_status` → `compilationFailed: false`), 콘솔 에러 0건.
  - `Assets/Test/Combat.unity`를 Play 모드로 돌려 `CombatManualTest`의 5개 시나리오 메서드를 reflection으로 호출, 10개 어서션 전부 PASS — OnHit/OnDeath 카운트, `IsDead`, 중복 사망 방지, 사망 후 재피격 무시, **시나리오 5(AttributeSet → AttributeToCombatObserver → CharacterCombat.Health) 연동**까지 확인.
  - 참고(재현 시 주의): `unity command eval`은 **Edit 모드**에서는 새로 생성한 컴포넌트의 `Awake`/`OnEnable`이 즉시 발화하지 않았다(원인 미확정 — Pipeline 패키지 eval 실행 경로의 특성으로 추정). **Play 모드**에서는 정상 발화하므로, MonoBehaviour 라이프사이클에 의존하는 검증은 항상 Play 모드에서 수행할 것.
- **`AttributeToCombatObserver`는 프리팹에 배선하지 않았다 (의도적).** `SOAttributeData_Player.asset`/`_Enemy.asset`의 `CurrentHp`(및 `CurrentBattery`/`CurrentHeat`)가 전부 0으로 들어있고 이를 `Max*`로 채우는 코드가 프로젝트 어디에도 없다. 지금 배선하면 옵서버의 씨앗 pull이 0을 읽어 **모든 캐릭터가 첫 프레임에 사망**한다. `Current*` 초기화 주체가 정해지기 전까지는 코드만 존재하는 상태로 둔다.
- `Assets/Test/Combat/FakeEffectTarget.cs`는 더 이상 어디서도 쓰이지 않지만 `Attribute.Core`에만 의존해 컴파일에는 문제없으므로 그대로 뒀다(지시 없이 데드 코드 삭제 금지 원칙).
- 새로 만든 `Observer.meta`/`AttributeToCombatObserver.cs.meta`의 GUID는 Unity가 다음에 이 폴더를 임포트할 때 재검증해야 한다 — 혹시 fileFormatVersion/DefaultImporter 포맷이 안 맞으면 Unity가 재생성하니 큰 위험은 아니지만 첫 Editor 오픈 시 확인 필요.

## 4. 다음 할 일 (Next Steps)

1. ~~Unity Editor로 컴파일 에러 0 확인~~ — 완료.
2. ~~`Assets/Test/Combat.unity` Play 모드에서 5개 시나리오 검증~~ — 완료(reflection 호출로 1~5 전부 PASS). 사람이 직접 키보드 1~5/R로 눌러보는 수동 확인은 아직 안 했으니 원하면 한 번 더 훑어볼 것.
3. intent-005 열린 질문 ③(`Current*` 초기화 주체) 결정 — 정해지면 `AttributeToCombatObserver`를 실제 캐릭터 프리팹에 배선.
4. intent-005 열린 질문 ①(데미지 적용 경로), ②(서브 중재자 지시 목록)는 스킬 매니저/서브 중재자 설계 때 진행.
5. 위 항목이 검증되면 intent-005를 `resolved`로 옮기고 `docs/intent/clear/`로 이동.
