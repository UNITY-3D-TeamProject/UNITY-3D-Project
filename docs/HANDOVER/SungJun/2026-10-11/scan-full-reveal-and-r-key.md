# 스캔 R키 전환 및 닿으면 전체 드러내기 (2026-10-11)

관련: [intent-019](../intent/intent-019-scan-system.md), [검토 반영](./scan-design-review-fixes.md)

## 작업 요약 및 방법
1. **R키로 새 스캔 발사** (`Assets/Test`, Git 미포함)
   - `ScanTestTrigger.cs`: 입력 `tKey` → `rKey`, 주석 갱신.
   - `Test_Player.prefab`: 옛 `TerrainScanner` 컴포넌트를 비활성(`m_Enabled: 0`). 스크립트와 다른 시험 프리팹은 그대로 뒀다.
2. **닿으면 메시 전체 드러내기** (`MeshRevealReaction.cs`만 수정)
   - 셰이더 `_ScanRadius`가 파동 반경을 그대로 따라가서, 파동이 플랫폼 중간에서 끝나면 반만 보였다.
   - `_waveRadius` → `_revealRadius`(현재 드러난 반경). `OnScanBegin`은 닿은 순간 파동 반경(가장 가까운 지점)에서 시작하고, `OnScanUpdate`는 `Max`로 파동보다 뒤처지지 않게만 한다.
   - 활성 중 매 프레임 호출되는 `OnFade(fade, false)`에서 `_revealSpeed`(m/s, 기본 10)로 메시의 가장 먼 점까지 키운다. 파동이 멈추거나 끊겨도 끝까지 드러난다.
   - 중심은 파동 중심을 유지해 드러나는 앞면이 파동 앞면과 이어진다. 셰이더·`ScanTarget`·`ScanWave`·`SScanHit`는 수정 없음.
   - 매 프레임 반경을 다시 적용하므로 fade-out 중 재스캔 복구용 `_isFoldedOut`은 필요 없어져 제거했다.

## 스캔 속도·범위 (질문 답변 정리)
- `ScanTestTrigger`의 `_duration`(6초)·`_size`(30m) → `ScanWave.Begin` → 파티클 `startLifetime`·`startSize`. 매 프레임 반경 = 파티클 현재 크기 × 0.5, 크기 곡선은 프리팹 기준 0→1 직선.
- 속도는 별도 값이 없다: (지름/2) ÷ 지속시간 = 2.5 m/s. `duration`은 대상 유지 시간도 겸하므로 파동을 느리게 하면 유지도 길어진다.

## 결정 사항
- 시험 스캔 키는 R키(사용자 결정). 실제 스킬(Q키, `Scan.cs`)은 이세훈 님 파트라 그대로.
- 메시 드러남은 "닿았는지"만 판단하고 전체를 드러낸다. 속도는 대상별 Inspector 값(사용자 결정).

## 현재 상태 및 이슈
- Unity 재컴파일 성공, 콘솔 에러 0.
- **Play 검증 미실시.** 이전 세션처럼 MCP Play 게임 루프 정지 가능성 때문에 사용자 확인이 필요하다.
- 유지 시간 안에 다 못 드러날 만큼 크고 `_revealSpeed`가 느리면 드러나던 반경에서 접히기 시작한다.
- `Test_Player.prefab`은 디스크에서 직접 수정했으므로 에디터가 다시 불러왔는지 Inspector에서 확인한다.

## 다음 할 일 (Next Steps)
1. TempScene Play → R키: 새 파동만 나가고 옛 파티클이 안 나오는지.
2. `ScanTestTrigger._size`를 줄여 파동이 `Reveal_Platform` 중간에서 끝나게 한 뒤, 닿은 쪽부터 끝까지 드러나고 유지 후 접히며 사라지는지. X-Ray 캡슐도 확인.
3. `_revealSpeed` 튜닝값 결정.
