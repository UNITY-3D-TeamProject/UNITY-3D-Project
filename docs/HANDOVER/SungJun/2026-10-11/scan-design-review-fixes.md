# 스캔 설계 검토 반영 (2026-10-11)

관련: [스캔 설계 검토](./scan-design-review.md), [intent-019](../intent/intent-019-scan-system.md)

## 작업 요약 및 방법
검토 항목 1~5, 8, 9를 `Core/Scanning`에 반영했다. 6번(실제 스킬 연결)과 7번(피격 배율 조회)은 타 파트 코드라 제외했고, 시험은 기존 `Assets/Test/.../ScanTestTrigger.cs`(T키)를 수정 없이 그대로 쓴다.

| 항목 | 변경 |
|---|---|
| 2 중복 호출 | `ScanWave.Update`: 재사용 `HashSet<IScannable>`로 대상별 프레임당 1회만 `OnScanned` 호출 |
| 3 탐색 범위 | `ScanTarget.Awake`: `GetComponents`(같은 오브젝트만). 반응이 없으면 `Debug.Assert`. 시험 씬의 반응은 모두 ScanTarget과 같은 오브젝트라 씬 수정 없음 |
| 1 재스캔 | `ScanTarget`: `_revealScanId`로 반응을 시작시킨 스캔의 `OnScanUpdate`만 전달. 재스캔은 종료 시각만 연장. `MeshRevealReaction`은 fade-out 중 재스캔으로 돌아오면 접힌 반경을 복구(`_isFoldedOut`) |
| 4 값 복원 | `MeshRevealReaction.OnScanEnd`: `SetPropertyBlock(null)` 제거, `RestoreProperties`로 `_ScanOrigin`/`_ScanRadius`만 머티리얼 기본값으로 복원 |
| 5 약점 시간 | `WeakPointReaction.OnFade`: 유지 시간이 끝나 fade-out이 시작되면 즉시 끄고, 재스캔으로 Active 복귀 시 다시 켠다 |
| 8 이름·주석 | `Begin(size)` → `Begin(maxDiameter)`, `WeakPoint`/`ScanReactionBase`/`IScannable`/`ScanTarget` 주석을 새 계약에 맞춤 |
| 9 복수 파동 | 코드 추가 없이 `ScanWave` 주석에 단일 파동 전제 명시 |

## 결정 사항
- 재스캔은 유지 시간만 연장한다. 스킬은 쿨타임으로 겹치지 않게 할 예정이다(사용자 결정).
- 약점은 스캔 유지 시간이 끝나면 즉시 꺼진다(사용자 결정).
- 반응은 `ScanTarget`과 같은 오브젝트에 둔다. 자식 배치가 필요하면 `_reactions`에 직접 연결한다.

## 현재 상태 및 이슈
- Unity 컴파일 성공, 에러 0. 경고는 스캔 코드와 무관한 기존 것뿐이다.
- **Play 검증은 못 했다.** MCP로 Play에 진입했지만 게임 루프가 진행되지 않아(`Time.frameCount`가 1~2에 고정) T키/`Begin` 시험이 불가능했다. 아래 항목은 직접 확인이 필요하다.
- 재스캔이 이전 파동 진행 중에 오면(같은 `ScanWave.Begin`은 이전 파동을 중단) 메시가 일부만 드러난 채 남을 수 있다. 쿨타임이 있으면 발생하지 않는 전제다.
- 256칸 콜라이더 버퍼 포화는 중복 제거와 무관하다. 정식 배치 때 `_scannableLayers`를 지정해야 한다.

## 다음 할 일 (Next Steps)
1. TempScene에서 T키로 확인: 단일 스캔, 약점이 유지 시간 끝에 바로 꺼지는지, 유지 중 재스캔(연출 불변·종료 시각만 연장), fade-out 중 재스캔(메시 복구·약점 재활성), 콜라이더가 여러 개인 대상의 호출 횟수.
2. `_shouldHideWhenIdle = false`인 렌더러에 다른 PropertyBlock 값을 넣고 스캔 후에도 남는지 확인.
3. 6번(`Scan.cs`가 `ScanWave.Begin` 호출·쿨타임), 7번(총알·근접 판정의 `WeakPoint.GetMultiplier`)은 담당 파트 연결을 기다린다. 요청 내용은 [약점 배율 요청](../2026-10-10/scan-weakpoint-handoff-requests.md)에 있다.

## 변경된 파일
`Assets/_Project/Scripts/Core/Scanning/`의 `ScanWave.cs`, `ScanTarget.cs`, `MeshRevealReaction.cs`, `WeakPointReaction.cs`, `WeakPoint.cs`(주석), `ScanReactionBase.cs`(주석), `IScannable.cs`(주석), 그리고 intent-019 문서.
