# 약점을 스캔 중에만 활성화 (2026-10-11)

## 작업 요약 및 방법
- 역할 분담이 정해졌다: **적 담당자**가 약점 오브젝트(콜라이더 + `WeakPoint` + 렌더러)를 만들고, **스캔 쪽**은 스캔 중에만 그 오브젝트를 활성화한다.
- `WeakPointReaction`을 고쳤다. 표시 오브젝트 수동 연결(`_markers`)과 크기 연출(`localScale`)을 없애고, 적 아래의 `WeakPoint`를 `GetComponentsInChildren<WeakPoint>(true)`로 자동 탐색해 `SetActive`로 켜고 끈다.
- 시험 씬(`TempScene`)의 `Enemy_WeakPoint`를 적 담당자가 만들 모양(콜라이더 + `WeakPoint` + 렌더러를 한 오브젝트 `WeakPoint_Head`에)으로 바꿨다.
- 이전 문서의 "약점 배율은 스캔과 무관하게 항상 적용" 가정을 정정하고 `scan-weakpoint-handoff-requests.md`의 요청 3과 intent-019의 열린 질문을 갱신했다.

## 결정 사항
- **약점은 스캔 중에만 보이고, 스캔 중에만 맞는다.** 평소에는 약점 오브젝트가 비활성이라 총알이 몸통으로 판정되어 배율 1이다.
- 그래서 `WeakPoint.cs`와 타 파트 피격 코드에는 스캔 여부 확인을 넣지 않는다. 비활성 여부가 곧 조건이다.
- 약점의 크기는 건드리지 않는다(콜라이더 크기도 함께 변하므로). 페이드 연출은 없고, 켜짐/꺼짐만 한다.
- 약점은 `Awake` 시점에 한 번만 찾는다.

## 현재 상태 및 이슈
- 컴파일 오류 0. Play에서 확인: 대기 중 약점 꺼짐(레이캐스트가 몸통에 맞고 `GetMultiplier` = 1) → 스캔 중 약점 켜짐(레이캐스트가 약점에 맞고 `GetMultiplier` = 2, 화면에 표시) → 스캔 종료 후 다시 꺼짐(배율 1).
- 약점 오브젝트가 `SetActive`로 꺼지므로, 적 담당자가 약점 오브젝트 자체에 다른 기능 스크립트를 붙이면 같이 꺼진다. 요청 문서에 주의를 적어 뒀으며 전달이 필요하다.
- 요청 2의 판정 순서 문제(몸통/약점 콜라이더가 동시에 겹칠 때 어느 쪽이 먼저 호출되는지)는 그대로이고 Projectile 담당과 협의가 필요하다.
- `BulletController` 등에서 `WeakPoint.GetMultiplier`를 호출하는 코드는 아직 없다.
- 작업 트리의 `SScanHit.cs`, `ScanTarget.cs`, `ScanWave.cs`, `WeakPoint.cs` 변경은 주석 정리이며 이번 작업의 코드 변경은 `WeakPointReaction.cs`뿐이다.

## 다음 할 일 (Next Steps)
- 적 담당자에게 `scan-weakpoint-handoff-requests.md`의 요청 3(약점 오브젝트 구성, 연결 필드 없음)을 전달한다.
- Projectile 담당에게 요청 2(맞은 콜라이더에서 `WeakPoint.GetMultiplier` 조회)와 판정 순서 협의를 전달한다.
- 스킬(`Scan.cs`)에서 `ScanWave.Begin`을 호출하도록 연결하고 시험 오브젝트(`ScanTest`, `ScanWave`)를 정리한다.
