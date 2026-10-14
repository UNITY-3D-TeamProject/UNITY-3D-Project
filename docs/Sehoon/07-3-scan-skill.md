# 07-3. 스캔 스킬

- **우선순위**: 높음
- **상태**: 대기
- **상위 작업**: [07. 스킬 구현](07-skill-implementation.md)
- **관련 작업**: [19. 펫 시스템](done/19-pet-system.md) — 펫 본체(추종·애니메이션, `PetMediator`)는 완료됐다. 펫이 스캔을 대리 사용하는 연출 질문은 아래 "펫 대리 사용" 으로 옮겨 왔다.

## 목표
`Scan` 스킬을 사용하면 기존 스캔 판정(`ScanWave`)이 발동되어 스캔 대상 반응이 동작하도록 연결한다.

## 현재 상황
- 스캔 스킬 `Scan` 은 `Debug.Log` 만 찍는 빈 껍데기다 (`Assets/_Project/Scripts/Skill/Skills/Scan.cs`).
- 입력은 이미 연결돼 있다: `PlayerInputComponent.OnScan` → `"Scan"` 스킬 실행 요청. HUD 도 `"Scan"` 이름으로 쿨타임을 표시한다 (`PlayerHudPresenter._scanSkillName`).
- 스캔 판정은 이미 있다: `ScanWave.Begin(origin, duration, maxDiameter)` 가 구 모양 파티클을 확산시키며 범위 안 `IScannable` 에 알린다 (`Assets/_Project/Scripts/Core/Scanning/ScanWave.cs:127`). 반응 컴포넌트(`MarkerReaction`, `MeshRevealReaction`, `SilhouetteReaction`, `WeakPointReaction`)도 있다.
  - `ScanWave` 는 "스킬 아래에 상주하다가 Begin 으로 재생"되는 전제로 작성돼 있고, 중심은 Begin 시점 위치로 고정된다.
  - `Assets/_Project/Prefabs/Skills/ScanWave.prefab` 이 있고 `Map Total test` 씬에서 쓰인다. `Scan` 스킬과는 아직 연결되지 않았다.
  - 시험용 값(SungJun 파트, `Assets/Test` 의 `ScanTestTrigger`): 지속 6초, 지름 30m → 확산 속도 2.5 m/s. `duration` 은 대상 활성 유지 시간도 겸한다.

## 확정 사항
- (없음)

## 작업 범위 (예상)
- `Scan` 스킬에서 `ScanWave.Begin` 호출. `ScanWave` 를 플레이어(스킬) 아래에 두는 방식과 참조 경로를 정한다.

## 열린 질문
- [ ] 07-3 범위: 펫 없이 **플레이어 위치에서 즉시 발동**까지만 하고, 펫 대리 사용은 19번에서 덧붙이는가?
- [ ] 발동 위치(origin): 플레이어 몸통 중심인가, 발밑인가?
- [ ] 스캔 지속 시간·최대 지름 값은? (시험값 6초 / 30m 를 그대로 쓰는가)
- [ ] `ScanWave` 배치: `Player.prefab` 의 `Scan` 스킬 아래에 `ScanWave.prefab` 을 넣는가? `Map Total test` 씬에 있는 `ScanWave` 는 어떻게 하나?
- [ ] 스캔 진행 중 재사용 시: 쿨타임으로 막는가, 새 스캔으로 다시 시작하는가(`Begin` 기본 동작)?
- [ ] 입력 키 / 쿨타임 / 코스트 값

### 펫 대리 사용 (19번에서 이관)
- [ ] 07-3 범위에 포함하는가, 별도 문서로 나누는가?
- [ ] 스캔 발동 위치: 펫 위치에서 파동이 퍼지는가, 플레이어 위치인가?
- [ ] "앞"의 기준: 캐릭터 정면인가, 카메라 정면인가? 거리는?
- [ ] 펫이 앞으로 나오는 데 걸리는 시간만큼 스캔이 늦게 발동돼도 되는가? (즉시 발동 + 펫은 연출만 하는 방식도 가능)
- [ ] 스캔 중 펫이 앞 위치에 머무는 시간, 복귀 조건은?
- [ ] 펫이 앞으로 갈 수 없는 위치(벽)일 때 처리는?

## 완료 기준
- 플레이 모드에서 스캔 스킬 사용 시 파동이 발동되고, 스캔 대상 반응이 동작한다. HUD 쿨타임 표시가 동작한다. 컴파일 에러 없음.
