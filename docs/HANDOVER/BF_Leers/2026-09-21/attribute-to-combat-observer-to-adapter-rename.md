# AttributeToCombatObserver → AttributeToCombatAdapter 개명 + Adapter 폴더 이동

- 날짜: 2026-09-21
- 참여자: BF_Leers + Claude (Sonnet 5)
- 브랜치: `feature/Combat`
- 관련 문서: [intent-005](../../../intent/intent-005-combat-component-decoupling.md)

## 1. 작업 요약 및 방법

사용자가 `AttributeToCombatObserver`와 `AttributeToMotorAdapter`가 구조상 같은 역할(AttributeSet 콜백 인터페이스를 대상 컴포넌트 프로퍼티 대입으로 변환)을 한다는 점을 지적, 이름과 폴더 위치를 통일하기로 결정했다.

- **이동 + 개명**: `Assets/_Project/Scripts/Observer/AttributeToCombatObserver.cs`(+`.meta`) → `Assets/_Project/Scripts/Adapter/AttributeToCombatAdapter.cs`(+`.meta`). `.meta`의 guid(`a81b7160a9c8458bb54551b44c5979a1`)는 그대로 유지 — 아직 어떤 프리팹에도 배선되지 않은 컴포넌트라 GUID 보존 실익은 없지만, CLAUDE.local.md §0(이동 시 `.meta` 반드시 동반) 원칙대로 처리했다.
- **코드 변경 (3곳)**: `namespace Scripts.Observer` → `Scripts.Adapter`, `class AttributeToCombatObserver` → `AttributeToCombatAdapter`, XML 주석의 "얇은 구독자" → "어댑터"로 정리, pull 관련 주석 문구 정리. **`Post` 콜백 구독은 그대로 유지** — Motor는 `AddOnAttributeChangedCallback`을 쓰지만 Combat은 사망 판정이 모든 수정자 적용 후 최종값을 봐야 하므로 `AddPostAttributeChangedCallback`을 유지한다. `AttributeToMotorAdapter.cs`(이세훈 담당)는 손대지 않음.
- **빈 폴더 정리**: `Assets/_Project/Scripts/Observer/`, `Observer.meta`(guid `ba79964fb6c0417788b97604b2324e3e`) 삭제.
- **intent-005 갱신**: "왜 Adapter가 아니라 Observer인가" 절에 파기 근거를 추가(제목 유지, 기존 논증은 지우지 않고 남김 — intent 문서 관례), 본문 전역의 `AttributeToCombatObserver`/`Scripts/Observer`/"옵서버" 표기를 `AttributeToCombatAdapter`/`Scripts/Adapter`/"어댑터"로 교체. **과거 HANDOVER 기록(`combat-*-redesign.md` 등)과 `docs/intent/clear/`의 resolved 문서는 그 시점의 사실 기록이므로 손대지 않았다.**

## 2. 결정 사항

1. **이름은 Adapter, 구조는 불변.** GoF Bridge가 아니라 Adapter로 부르는 게 맞다 — 서로 호환되지 않는 두 인터페이스(AttributeSet 콜백 vs `CharacterCombat.Health` 프로퍼티)를 변환해 잇는 것이 본질이고, 확장할 추상/구현 축이 없어 Bridge는 해당 없다.
2. **`Post` vs `On` 콜백 차이는 이름 통일과 무관하게 유지.** 의도적 차이(사망 판정은 보정 후 최종값을 봐야 함)이지 실수가 아니므로 Motor 쪽에 맞추지 않는다.
3. **`AttributeToMotorAdapter`는 범위 밖.** 담당자가 다르고(이세훈), 이미 잘 동작하는 코드라 외과 수술적 변경 원칙에 따라 건드리지 않는다.
4. **폴더 담당 경계 관련:** `Scripts/Adapter/`는 기존에 이세훈 담당 폴더였다. 이번 이동으로 BF_Leers 소유 파일이 그 폴더에 들어가게 됐다 — **이세훈과 공유 필요** (아래 다음 할 일 참고).

## 3. 현재 상태 및 이슈

- 파일 이동·개명·문서 갱신 완료. **Unity Editor 컴파일 검증은 아직 안 함** — 다음 세션에서 `unity` CLI EditMode 배치로 컴파일 0에러 + `Combat.Tests` 5/5 PASS 재확인 필요 (Combat 폴더 자체는 안 건드렸으므로 회귀는 없어야 정상).
- `grep -rn "Scripts.Observer\|AttributeToCombatObserver" Assets/` → 0건 확인함(코드 기준). `docs/`의 과거 기록은 의도적으로 남겨둠.
- 커밋 전 상태.

## 4. 다음 할 일 (Next Steps)

1. Unity Editor로 컴파일 확인 + `Combat.Tests` 재실행.
2. **이세훈에게 공유**: `Scripts/Adapter/` 폴더에 BF_Leers 담당 파일(`AttributeToCombatAdapter.cs`)이 추가됐다는 것.
3. 변경분 커밋 (`docs/commit-convention.md` 준수).
4. intent-005의 열린 질문 ①·②·③은 이번 작업과 무관하게 그대로 미해결.
