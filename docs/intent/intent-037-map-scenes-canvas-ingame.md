---
id: intent-037
title: 맵 씬 6개의 HUD 프리팹을 Canvas_Legacy에서 Canvas_InGame으로 교체
part: UI / Scene
status: open   # open | resolved
created: 2026-10-17
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
인게임 UI로 쓰기로 한 것은 `Canvas_InGame`(옛 `RealCanvas`)인데, 맵 씬 6개(`Map_0_Tutorial` ~ `Map_5_Live`)는 아직 옛 HUD `Canvas_Legacy`(GUID `3eb1f113…`) 인스턴스를 쓴다. 그래서 맵에서 플레이하면 HP 10칸·크로스헤어 원호·차지 바·스킬 카드 쿨타임·Shift 설정창이 보이지 않는다.

## 기대 결과 (Proposed outcome)
- 6개 씬 모두 `Canvas_InGame`(GUID `514dc01a…`) 인스턴스를 쓴다. Hierarchy 이름은 지금처럼 `Canvas`로 둔다.
- 튜토리얼 씬 Play에서 HP·크로스헤어·스킬 카드·설정창이 보이고 콘솔 에러가 없다.

## 조사 결과 (2026-10-17)
- **컴포넌트 구성**: 두 프리팹 모두 루트 아래 `UIManager`(→ `PlayerHudPresenter`, `SettingsPresenter`), `PlayerHudRoot/PlayerHUD`(`PlayerHudPresenter` + `PlayerHudView`), `SettingsPresenter`, `SettingsView/SettingsPanel`을 같은 구조로 가진다.
- **Canvas_InGame 연결 상태**: `PlayerHudView`의 HP 10칸·퍼센트, 열·배터리 원호·퍼센트, 스킬 카드 3장 바·글자, 차지 바 10칸이 모두 연결돼 있다. `SettingsView._closeButton`도 연결돼 있다(Legacy는 비어 있음). 비어 있는 것은 `_roll/_bluetooth/_scanCooldownSlider` 3개뿐인데, 카드형 바(`_*CooldownBar`)로 대체된 옛 슬라이더 필드라 의도된 상태로 본다.
- **프리팹 내부 참조**: 끊긴 fileID 없음. 외부 에셋은 `Assets/ThirdParty/Shift`(28)·`SCI-FI`(6)·`_Project/Scripts`·UGUI/TMP 패키지뿐이고 `Assets/Test` 의존은 없다.
- **Canvas 설정 차이**: 기준 해상도 800×600 → 1920×1080. 설정창 `SettingsPanel`에 정렬 덮어쓰기 Canvas(sortingOrder 10)가 있어 HUD 위에 그린다.
- **씬 오버라이드**: 6개 씬 모두 루트 RectTransform 기본값과 `m_Name: Canvas`뿐. `Map_2_Feed`만 `InfoPanelRoot`·`MonologueRoot` `m_IsActive: 0` 오버라이드가 더 있는데, 두 프리팹 모두 이미 꺼져 있어 효과 없음.
- **외부 참조**: 6개 씬 모두 Canvas 하위를 가리키는 stripped 오브젝트가 없다. 즉 `StageManager`·`PlayerSpawner` 등 씬 오브젝트가 Canvas 안을 참조하지 않는다. 코드에서도 `UIManager`·`PlayerHudPresenter`를 이름이나 Find로 찾는 곳이 없다(`UIManager`가 `GameManager.Instance.OnPlayerSpawned`를 구독하는 방향).
- **EventSystem**: 맵 씬 6개 모두 EventSystem이 없다(MainScene에만 있음, 씬 오브젝트라 씬 전환 시 사라짐). 설정창은 ESC(입력 이벤트)로는 닫히지만, 새로 연결된 닫기 버튼은 마우스로 누를 수 없다.
- **충돌 위험**: `origin/feature/Map/Maze`(2026-10-16, 미병합 11커밋)가 `Map_0_Tutorial`·`Map_1_Lobby`·`Map_3_File`을 크게 수정 중이다. 이 브랜치에서 세 씬의 Canvas를 바꾸면 병합 때 씬 YAML 충돌(특히 `SceneRoots` 목록)이 날 수 있다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 맵 씬에서 플레이하는 팀 전체.
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scenes/Map_0_Tutorial` ~ `Map_5_Live`.unity (Canvas 인스턴스 블록과 `SceneRoots`). 코드·프리팹 변경 없음.

## 제약 (Constraints)
- 맵 씬은 팀원도 작업하는 파일이다. Canvas 인스턴스 외 다른 부분은 건드리지 않는다.
- `Canvas_InGame`은 ThirdParty Shift·SCI-FI UI Pack Pro 에셋을 쓴다. Git에 없는 폴더라 팀원도 이 에셋을 가져와야 HUD가 보인다.
- 커밋은 사용자가 요청할 때만 한다.

## 열린 질문 (Open questions)
1. `feature/Map/Maze`가 수정 중인 `Map_0_Tutorial`·`Map_1_Lobby`·`Map_3_File`도 지금 바꿀지, 그 브랜치가 develop에 병합된 뒤 바꿀지.
2. 맵 씬에 EventSystem이 없어 설정창 닫기 버튼을 마우스로 누를 수 없다. 이번 작업에 EventSystem 추가를 포함할지, 별도 작업으로 둘지. (2026-10-17: intent-038 AudioManager의 게임 안 볼륨 슬라이더도 같은 이유로 움직일 수 없어서, EventSystem 추가는 이 intent에서 처리하기로 함)
3. 교체 방식: Unity의 Prefab Replace(인스턴스 원본만 교체, 위치·이름 유지)로 할지, 지우고 새로 배치할지.
4. 교체 후 `Canvas_Legacy.prefab`을 삭제할지 남길지.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
