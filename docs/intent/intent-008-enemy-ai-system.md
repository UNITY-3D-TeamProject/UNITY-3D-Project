---
id: intent-008
title: 적 AI 시스템 — 조종부 교체 가능한 BT 기반 구조
part: AI
status: open
created: 2026-09-24
resolved: null
---

## 문제 (Problem)

적·NPC·과녁을 움직일 **조종부가 없다.** 현재 `ICharacterController`를 구현한 컴포넌트는 `PlayerInputComponent` 하나뿐이라, 캐릭터는 사람이 패드를 잡고 있을 때만 움직인다. 중재자 계층(`CharacterMediator` / `MoveMediator`)은 조종 주체를 모르도록 이미 설계돼 있지만, 그 자리에 꽂을 AI 쪽 구현이 한 줄도 없다.

기획이 **"맵에 미리 배치된 적이 순찰하다가 시야로 플레이어를 발견한다"**로 정해졌는데, 순찰·감지·수색을 담당할 컴포넌트가 전무하다. 시야 판정에 쓸 커스텀 레이어도, 경로 계산에 쓸 NavMesh 배선도 없다.

> **전제 변경 기록.** 설계 초기에는 "아레나 트리거 스폰 + 감지 없음"으로 진행했다. 설계 중반에 기획이 "월드에 미리 배치 + 순찰 + 시야 감지"로 바뀌면서 감지·순찰·수색이 1차 범위 안으로 들어왔다. 이 문서는 바뀐 뒤의 전제를 기준으로 쓴다.

## 기대 결과 (Proposed outcome)

중재자 계층은 **조종 주체를 모르는 채로 두고**, 조종부만 갈아끼워 같은 캐릭터를 AI가 조종한다.

```
┌─ 조종부 (AI일 때만 존재) ──────────────┐
│  [BT 그래프]                            │
│       ↓  액션·조건 노드는 컨트롤러만 안다 │
│  [AIController] ── 보유 ─→ [Sensor]     │
│       │           보유 ─→ [NavMeshAgent]│
└───────┼─────────────────────────────────┘
        │ 요청 (콜백)
┌───────▼─────────────────────────────────┐
│  [Center 중재자] ← OnEnable에 배선만 하고 빠짐
│      ├─ [MoveMediator]   → [CharacterMotor]
│      ├─ [CameraMediator] → ...
│      └─ [SkillMediator]  → [스킬 컴포넌트]  (나중)
└─────────────────────────────────────────┘
```

**소속 판별 기준: 조종 주체가 바뀌어도 남는 것만 중재자 쪽.** 플레이어에게 없는 것(`Sensor`, `NavMeshAgent`)은 조종부 소유이며 중재자가 참조하지 않는다.

**검증 기준:** `순찰 → 시야 감지 → 추격 → 시야 상실 → 수색 → 순찰 복귀` 루프가 Play 모드에서 완주할 것.

### 1차(이동만) 구현에 직접 걸리는 결정

전체 결정표(Q1~Q28)와 각 결정의 근거는 [AI 구조 설계 확정 (grilling 세션)](../HANDOVER/BF_Leers/2026-09-24/ai-architecture-design-grilling.md)에 있다. 여기에는 1차 구현에 바로 필요한 것만 옮긴다.

| # | 결정 |
|---|---|
| Q1 | BT는 `com.unity.behavior` (**미설치**) |
| Q3 | NavMesh로 경로 계산 → `Vector2` 환산 → **기존 `MoveMediator.CommandMove` 재사용** (이동 코드 신규 작성 없음) |
| Q4 | BT 그래프는 캐릭터별로 따로, **재사용 단위는 액션·조건 노드** |
| Q5 | `Sensor` 컴포넌트 신설 — 조종부 소속, 중재자가 관리하지 않음 |
| Q15 | Center = **배선 담당** (런타임 중계 아님, 현재 코드 유지) |
| Q18 | **1차 구현은 이동만.** 공격 제외 |
| Q20 | 순찰 = 스폰 지점 기준 반경 내 NavMesh 랜덤 지점 + 도착 후 대기 |
| Q21 | 시야 상실 → 마지막 목격 지점 수색 → 순찰 복귀 |
| Q22 | 시야 = 거리 + 각도 + **차폐 Raycast** |
| Q24 | `Obstacle` 레이어 신설 (시야 Raycast 전용) |
| Q25 | 감지 주기 0.2초 (적마다 시작 오프셋을 줘 프레임 몰림 방지) |
| Q26 | `Player` 레이어 + `OverlapSphere`로 탐색 (전역 Registry 미채택) |
| Q27 | BT 액션·조건 노드는 **`AIController`에게만** 묻는다 (`Sensor` 직접 접근 금지) |
| Q28 | `NavMeshAgent`는 조종부 소유 |

**`Sensor`의 출력은 bool이 아니라 "대상 + 마지막 목격 위치"다.** Q21(수색)과 최종 목표인 예측 조준이 둘 다 대상 자체를 요구하기 때문이다.

## 영향 범위 (Affected users and systems)

- **누가 사용하는가**: 적/NPC 프리팹, 레벨 디자인(적 배치·순찰 반경 조정), 이후의 스킬 매니저
- **어떤 시스템/모듈이 건드려지는가**
  - 신규 (코드): `Assets/_Project/Scripts/` 아래 AI 폴더 — `Sensor` / `AIController` / `AIActionBase` / BT 액션·조건 노드
  - 재사용 (수정 없음): `MoveMediator.CommandMove`, `CharacterMotor`, `ICharacterController`
  - `[USER]` 영역: `ProjectSettings/TagManager.asset`(레이어), `Packages/manifest.json`(BT 패키지), 적 프리팹, BT 그래프 에셋, NavMesh 베이크

## 제약 (Constraints)

- **`.unity` / `.prefab` / `.asset` / `.controller` 직접 편집 금지** (CLAUDE.local.md 0장). 따라서 레이어 추가·NavMesh 베이크·프리팹 구성·BT 그래프 작성은 전부 `[USER]` 티켓으로 분리한다.
- 작업 범위는 `Assets/_Project/` 안으로 한정하고 `docs/coding-convention.md`를 따른다.
- **선행 의존 미충족 4건**
  1. `com.unity.behavior` 미설치 (`Packages/manifest.json`에 없음)
  2. 커스텀 레이어·태그 0개 (`TagManager.asset`이 기본값)
  3. 스킬 매니저 미구현 (`Scripts/` 아래 Skill 폴더 없음) → Q18에서 1차 범위에서 공격을 뺀 이유
  4. 애니메이션 컴포넌트 미구현 (`AnimationMediator` 없음)
- **1차 결과물은 적이 T포즈로 미끄러지듯 이동하는 것이 정상이다.** 애니메이션 컴포넌트가 아직 없기 때문이며, 동작 실패로 오해하지 말 것. 애니메이션은 별도 티켓.
- `MoveMediator.CommandJump()`는 현재 비어 있으나(`//todo`), 1차 AI에 점프가 없어 영향 없음.

## 열린 질문 (Open questions)

- ① **Q6·Q13·Q14·Q17 (스킬 결과 통보 경로)** — 방향만 잡았고 최종 확정은 팀원의 스킬 매니저 인터페이스를 본 뒤로 미룬다. 1차 범위(이동만)의 블로커는 아니다.
- ② **`docs/character-architecture.md` 3장이 Center를 런타임 중계처럼 서술** — 실제 `CharacterMediator.cs:38`은 `_controller.SetMoveRequest(_moveMediator.CommandMove)`로 메서드 참조만 넘기는 배선 방식이라, 이후 호출은 `컨트롤러 → MoveMediator`로 직행하고 Center는 경로에 없다. 문서를 코드 기준으로 고칠지 팀 합의 필요.
- ③ **3장 표에 "중재자 → 조종부: 결과 통보" 방향이 없다.** 스킬 단계에서 필요해진다. 팀 합의 필요.
- ④ **4장에 "조종부 쪽 자료원"(`Sensor`, `NavMeshAgent`) 범주가 없다.** 문서에 범주 추가 필요.
- ⑤ **`character-architecture.md:89`의 `intent-003-enemy-ai.md` 링크가 깨져 있다** (해당 파일 없음. 실제 intent-003은 movement-playtest-bugfixes). 이 문서(intent-008)로 교체할지 확인 필요.
- ⑥ **튜닝 수치 미정** — 순찰 반경, 시야각·시야 거리, 수색 지속 시간. `[USER]` 조정 항목이며 코드에는 인스펙터 노출만 해 둔다.

②~⑤는 `docs/character-architecture.md` 소유자와의 협의 항목이라 BF_Leers 단독으로 확정하지 않는다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:

---

## 관련 문서
- [AI 구조 설계 확정 (grilling 세션) — 28개 결정 전문](../HANDOVER/BF_Leers/2026-09-24/ai-architecture-design-grilling.md)
- [캐릭터 공통 구조](../character-architecture.md)
- [코딩 컨벤션](../coding-convention.md)
