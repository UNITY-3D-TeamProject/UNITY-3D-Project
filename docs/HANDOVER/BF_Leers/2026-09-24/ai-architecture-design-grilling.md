# AI 구조 설계 확정 (grilling 세션)

- **날짜**: 2026-09-24
- **참여자**: BF_Leers
- **브랜치**: `feature/AI/Design`
- **코드 변경**: 없음 (설계 전용 세션)

---

## 1. 작업 요약 및 방법

적·NPC·과녁 등에 재사용할 AI 구조를 grilling(반복 질의)으로 설계했다. 코드는 한 줄도 작성하지 않았고, 기존 코드(`CharacterMediator` / `MoveMediator` / `ICharacterController` / `PlayerInputComponent`)와 `docs/character-architecture.md`, `ProjectSettings/TagManager.asset`, `Packages/manifest.json`을 확인해 사실관계를 맞춘 뒤 28개 결정을 확정했다.

설계 중 **기획 전제가 한 번 크게 바뀌었다.** 초기에는 "아레나 트리거 스폰 + 감지 없음"으로 진행했으나, 중반에 "적이 맵에 미리 배치되어 순찰하다가 시야로 플레이어를 발견한다"로 바뀌면서 감지·순찰·수색이 1차 범위로 들어왔다.

---

## 2. 결정 사항

### 2-1. 구조

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

**소속 판별 기준: 조종 주체가 바뀌어도 남는 것만 중재자 쪽.** 플레이어에게 없는 것(Sensor, NavMeshAgent)은 조종부 소유이며 중재자가 참조하지 않는다.

### 2-2. 전체 결정 표

| # | 결정 |
|---|---|
| Q1 | BT는 `com.unity.behavior` (미설치 상태) |
| Q2 | 이동 완료 판정은 BT 액션이 직접 한다 |
| Q3 | NavMesh로 경로 계산 → Vector2 환산 → 기존 `CommandMove` 재사용 |
| Q4 | 컴포넌트는 인터페이스 노출 / BT 그래프는 캐릭터별 / **재사용 단위는 액션·조건 노드** |
| Q5 | **`Sensor` 컴포넌트 신설** (조종부 쪽, 중재자 미관리) |
| Q6 | 스킬 결과는 폴링 + **티켓 번호**로 stale 결과 차단 |
| Q7 | `AIActionBase`에서 컨트롤러 캐싱 |
| Q8 | 액션의 `OnEnd`에서 자기가 건 요청을 정리 |
| Q11 | 1차는 전투 스테이지에서 탈것 금지 / **최종 목표는 탈것 전투 지원** |
| Q12 | 약점 치명타에 적은 반응하지 않는다 (데미지만) |
| Q13 | 스킬중재자 이벤트 → Center가 배선 → 컨트롤러 |
| Q14 | 결과 미도착 시 타임아웃 없음 (버그로 드러나게 둔다) |
| Q15 | **Center = 배선 담당** (현재 코드 유지, 런타임 중계 아님) |
| Q16 | 사망 시 BT 비활성화 → Q8의 `OnEnd`가 자동 정리 |
| Q17 | 인터페이스만 분리(`ISkillController`), **컨트롤러 컴포넌트는 하나** |
| Q18 | **1차 구현은 이동만** (공격 제외) |
| Q20 | 순찰 = 스폰 지점 기준 반경 내 NavMesh 랜덤 + 도착 후 대기 |
| Q21 | 시야 상실 → **마지막 목격 지점 수색 후 순찰 복귀** |
| Q22 | 시야 = 거리 + 각도 + **차폐 Raycast** |
| Q23 | 동료 전파 없음 |
| Q24 | `Obstacle` 레이어 신설, 시야 Raycast 전용 |
| Q25 | 감지 주기 0.2초 간격 (적마다 시작 오프셋) |
| Q26 | `Player` 레이어 + `OverlapSphere`로 탐색 |
| Q27 | BT 조건 노드도 **AIController에게만** 묻는다 (Sensor 직접 접근 금지) |
| Q28 | NavMeshAgent는 조종부 소유 |

### 2-3. 주요 결정의 근거

- **Q15 (Center = 배선)**: `CharacterMediator.cs:38`이 `_controller.SetMoveRequest(_moveMediator.CommandMove)`로 메서드 참조를 넘기므로, 이후 런타임 호출은 `컨트롤러 → MoveMediator`로 직행하고 Center는 경로에 없다. 런타임 중계로 바꾸면 이동·점프·카메라를 전부 재배선해야 해서 기각.
- **Q20 (랜덤 순찰)**: 웨이포인트 수동 배치는 적 수십 마리 × 레벨마다 `[USER]` 작업이 붙어 기각. 스폰 지점을 순찰 중심으로 삼으면 Q21의 "복귀할 곳"이 공짜로 생긴다.
- **Q26 (레이어 탐색)**: 전역 등록소(Registry) 안을 제시했으나 전역 상태를 피하는 쪽을 택함.
- **Q27 (컨트롤러 경유)**: BT 노드가 알아야 할 대상을 하나로 고정해, Sensor를 교체해도 노드를 안 고치게 한다.
- **Sensor 출력은 bool이 아니라 "대상 + 마지막 목격 위치"**: Q21(수색)과 Q11 최종 목표(예측 조준)가 둘 다 대상 자체를 요구한다.

---

## 3. 현재 상태 및 이슈

### 3-1. 문서와 코드가 어긋나 있음 (팀 확인 필요)

| 내용 | 조치 |
|---|---|
| `docs/character-architecture.md` 3장이 Center를 **런타임 중계처럼** 서술 — 실제 코드는 배선 방식 | 문서 수정 필요 (Q15를 코드 기준으로 확정) |
| 3장 표에 **"중재자 → 조종부: 결과 통보"** 방향이 없음 | 스킬 단계에서 필요. 팀 합의 필요 |
| 4장에 **"조종부 쪽 자료원"**(Sensor, NavMeshAgent) 범주가 없음 | 문서에 범주 추가 필요 |
| `character-architecture.md:89`의 `intent-003-enemy-ai.md` 링크가 **깨짐** (파일 없음, 실제 intent-003은 movement-playtest-bugfixes) | 링크 수정 또는 제거 |

### 3-2. 선행 의존이 아직 없는 것

- **스킬 매니저 미구현.** `Scripts/` 아래 Skill 폴더 없음. `CharacterCombat.OnHit`/`OnDeath`는 구독자 0. → Q18에서 1차 범위에서 공격을 뺀 이유.
- **애니메이션 컴포넌트 미구현.** Animation 폴더도 `AnimationMediator`도 없음. → **1차 결과물은 적이 T포즈로 미끄러지듯 이동한다.** 동작 실패로 오해하지 말 것. 별도 티켓.
- **`com.unity.behavior` 미설치.** `Packages/manifest.json`에 없음.
- **커스텀 레이어·태그 0개.** `TagManager.asset`이 기본값.
- 참고: 현재 브랜치의 `MoveMediator.CommandJump()`는 비어 있음(`//todo`). AI 1차에 점프가 없어 영향 없음.

### 3-3. 보류 — 스킬 매니저가 나온 뒤 확정

**Q6·Q13·Q14·Q17**은 방향만 잡았고 구현은 보류한다. 팀원의 스킬 매니저 인터페이스를 본 뒤 최종 확정한다 (CLAUDE.md 3장 1번: 확신 없으면 추측하지 않는다).

---

## 4. 다음 할 일 (Next Steps)

1. ~~**`docs/intent/`에 intent 문서 작성** (CLAUDE.md 6장).~~ → 완료: [intent-008 적 AI 시스템](../../../intent/intent-008-enemy-ai-system.md)
2. **3-1의 문서-코드 불일치 4건을 팀에 공유**하고 문서 수정 합의를 받는다.
3. `[USER]` 티켓 발행:
   - `com.unity.behavior` 패키지 설치
   - `Obstacle` / `Player` 레이어 추가 (`TagManager.asset`은 `.asset`이라 AI 직접 편집 금지 — CLAUDE.local.md 0장)
   - NavMesh 베이크 (`NavMeshSurface` 배치)
   - 적 프리팹 구성 + BT 그래프 에셋 작성
   - 순찰 반경 / 시야각 / 수색 시간 튜닝
4. **코드 구현 (1차, 이동만)**: `Sensor` / `AIController` / `AIActionBase` / 액션·조건 노드.
   검증 기준 = **순찰 → 시야 감지 → 추격 → 시야 상실 → 수색 → 복귀** 루프가 Play 모드에서 완주할 것.

---

## 관련 문서

- [캐릭터 공통 구조](../../../character-architecture.md)
- [코딩 컨벤션](../../../coding-convention.md)
