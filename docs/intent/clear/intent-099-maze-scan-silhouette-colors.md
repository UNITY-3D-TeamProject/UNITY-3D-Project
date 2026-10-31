---
id: intent-099
title: 미로 아이템·GC·도둑 스캔 실루엣 색 구분 (연산자 파랑·피연산자 초록·GC·도둑 빨강)
part: Core / Scanning / Stage(FileApp)
status: resolved   # open | resolved
created: 2026-10-31
resolved: 2026-10-31
---

## 문제 (Problem)
파일 앱 미로 2라운드에서 스캔(R)하면 벽 뒤 대상이 모두 같은 초록 실루엣(`M_ScanSilhouette`)으로 보인다.
- 숫자 퍼즐 아이템(`MazePuzzleItem`)은 숫자(피연산자)와 연산자 두 종류인데, 스캔으로는 둘을 구분할 수 없다.
- GC(`GC.prefab`)도 같은 초록 셸이라 아이템과 위험 요소가 구분되지 않는다.

## 기대 결과 (Proposed outcome)
스캔하면 벽에 가려진 부분이 종류별 색으로 비쳐 보인다(보이는 방식은 지금 실루엣과 같고 색만 다르다).

| 대상 | 색 | 머티리얼 |
| --- | --- | --- |
| 연산자 아이템 (+ − × ÷) | 파랑 | 새 `M_ScanSilhouette_Operator` |
| 피연산자 아이템 (숫자 1~9) | 초록 | 기존 `M_ScanSilhouette` 그대로 |
| 가비지 컬렉터(GC)·도둑 | 빨강 | 새 `M_ScanSilhouette_Enemy` |

### 설계
- **아이템**: 값(숫자/연산자)은 라운드 시작·리셋 때 `MazePuzzleItem.Setup`에서 정해지므로 색도 그때 바꾼다.
  - 기존 `_kindRenderers` / `_numberMaterials` / `_operatorMaterials` 구조를 그대로 쓴다. 프리팹에서 실루엣 자식(`ScanSilhouette`)의 렌더러를 4번째 칸으로 넣고, 숫자 쪽에 `M_ScanSilhouette`, 연산자 쪽에 `M_ScanSilhouette_Operator`를 넣는다.
  - `SilhouetteReaction`은 Awake에서 머티리얼 `_Color`를 캐시해 PropertyBlock으로 덮어쓰므로, 머티리얼만 바꾸면 색이 안 바뀐다. 그래서 `SilhouetteReaction.RefreshBaseColors()`(공개 메서드 1개)를 추가해 현재 머티리얼 색을 다시 읽게 하고, `MazePuzzleItem.Setup`이 머티리얼을 바꾼 뒤 부른다.
  - Awake 전에 Setup이 불려도(비활성 아이템) Awake가 바뀐 머티리얼을 읽으므로 문제없다.
- **GC·도둑**: 코드 변경 없음. `GC.prefab`·`Thief.prefab`의 `SilhouetteReaction._autoShellMaterial`을 `M_ScanSilhouette` → `M_ScanSilhouette_Enemy`로 바꾼다.
- 새 머티리얼 2개는 `M_ScanSilhouette`을 복제해 `_Color`만 바꾼다(셰이더 `Project/ScanSilhouetteHologram` 동일).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 파일 앱 미로 2라운드에서 스캔을 쓰는 플레이어
- 어떤 시스템/모듈이 건드려지는가:
  - `Scripts/Core/Scanning/SilhouetteReaction.cs`: `RefreshBaseColors()` 추가
  - `Scripts/Core/Stage/MazePuzzleItem.cs`: Setup에서 `RefreshBaseColors()` 호출
  - `Prefabs/Map/Maze/Puzzle/MazePuzzleItem.prefab`: Kind 배열에 실루엣 렌더러·머티리얼 추가
  - `Prefabs/AI/NPC/GC/GC.prefab`, `Prefabs/AI/NPC/Thief.prefab`: 셸 머티리얼 교체 (이정빈 파트 프리팹)
  - 새 에셋 `Art/Scan/M_ScanSilhouette_Operator.mat`, `M_ScanSilhouette_Enemy.mat`
  - `docs/scan-setup-guide.md`: 3장에 색 구분 메모

## 제약 (Constraints)
- 맵 씬(`Map_3_File`)은 열거나 고치지 않는다. 확인은 TestScene에서 한다.
- `M_ScanSilhouette`는 도둑·예시 프리팹도 같이 쓰므로 색을 바꾸지 않는다.
- AI 스크립트(`Scripts/AI`)는 고치지 않는다.

## 열린 질문 (Open questions)
- (없음) 2026-10-31 사용자 결정: 도둑도 빨강. GC 프리팹 교체·색 값은 제안대로 진행.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
  - 새 머티리얼 `M_ScanSilhouette_Operator`(HDR 0.3, 0.7, 1.5), `M_ScanSilhouette_Enemy`(HDR 1.5, 0.2, 0.2). 둘 다 `M_ScanSilhouette` 복제, `_Color`만 다름.
  - `SilhouetteReaction.RefreshBaseColors()` 추가, `MazePuzzleItem.Setup`이 Kind 머티리얼 교체 뒤 호출.
  - `MazePuzzleItem.prefab` Kind 배열 4번째 칸 = `ScanSilhouette` 렌더러 (숫자 `M_ScanSilhouette`, 연산자 `M_ScanSilhouette_Operator`).
  - GC·도둑 프리팹 `_autoShellMaterial` → `M_ScanSilhouette_Enemy`. GC·도둑은 같은 머티리얼을 쓰므로 하나만 바꾸려면 복제해야 한다.
  - 확인: 리컴파일 0에러, 프리팹 연결값 확인, 격리 로드한 아이템에서 Setup(연산자) → 파랑, Setup(숫자) → 초록, 페이드 0.5 → 알파 0.5. 실제 스캔 스킬로 벽 뒤 모습은 미확인.
- 관련 커밋/PR: (커밋 전)
