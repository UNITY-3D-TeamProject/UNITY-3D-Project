---
id: intent-019
title: 스캔 스킬 구현 — 지속시간 동안 파동이 닿은 대상이 IScannable 반응으로 드러났다가 서서히 사라짐
part: Skill / Scan / Core
status: open   # open | resolved
created: 2026-10-10
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- `Scan` 스킬(`Skill/Skills/Scan.cs`)은 `Debug.Log("Scan")`뿐인 스텁이다. 입력(`OnScan`)과 쿨타임 HUD 연결만 되어 있다.
- 스캔 파동 프로토타입(`TerrainScanner`, `TerrainScanWave`, `ScanOrderRevealTarget`, `SG_ScanReveal`)이 Git에 올라가지 않는 `Assets/Test/`에만 있어 팀이 쓸 수 없다. R키 입력·월드 Instantiate 방식이라 플레이어 스킬 구조와도 맞지 않는다.
- 스캔에 닿은 대상이 어떻게 반응할지(적 약점, 아이템 위치, 플랫폼, 상호작용 오브젝트)를 정하는 구조가 없다.

## 기대 결과 (Proposed outcome)
- (정정 2026-10-10) Player/Skill 연결(`Scan.cs`, `Scan.prefab`)은 이세훈 님 파트이고 후속 작업이다. 지금은 기존 R키 `TerrainScanner`(`Assets/Test`)가 쏜 스캔에 대상이 반응하는 부분만 만든다. 파동 생성/재생은 사용자의 기존 `TerrainScanner`·`TerrainScanWave`를 그대로 쓴다.
- 스캔을 쓰면 `duration` 동안 파동이 확산한다. (~~파동은 플레이어 프리팹 하위 `Scan` 아래에 상주~~ — 철회)
- 파동이 닿은 대상은 `IScannable.OnScanned(SScanHit)`를 통해 자기 반응을 수행한다. **닿은 시점부터 `duration`만큼 활성화**되고, 이후 `fade`(1→0)로 서서히 사라진다.
  1. 적: 약점 부위 노출 (+ 약점 데미지 배율, Phase B)
  2. 아이템: 벽에 가려져도 실루엣/위치가 보임
  3. 플랫폼: 메시가 투명 상태에서 파동처럼 점점 보임 (충돌은 항상 유지)
  4. 상호작용 오브젝트: 위치 마커 표시
- 구조: `IScannable` + `ScanTarget`(타이머·상태) + `ScanReactionBase`(Strategy) 조합. 상세는 설계 계획(`C:\Users\inha\.claude\plans\declarative-snuggling-haven.md`)을 따른다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(스캔 스킬), 레벨 디자이너(대상 프리팹에 `ScanTarget` 부착)
- 어떤 시스템/모듈이 건드려지는가: `Skill/Skills/Scan.cs`, `Scan.prefab`, `Player.prefab`(중첩 프리팹 변경), 신규 `Scripts/Core/Scanning/`, 에셋 이전(`Assets/Test` → `Assets/_Project`), (Phase B) Attribute/Projectile/Hit 경로

## 제약 (Constraints)
- 스캔 파동 방식은 프로토타입(파티클 구 + `OverlapSphere`)을 그대로 이식한다. 새로 만들지 않는다.
- `Assets/Test`는 Git 제외 경로이므로 건드리지 않고 `Assets/_Project/`로 복사해 쓴다.
- 약점 데미지 배율은 우선 Core 안에서 작성하고, 타 파트(Attribute/Projectile/Hit) 변경은 요청 문서로 전달한다. intent-016(`SEffectContext`)과 경로가 겹친다.
- 플랫폼은 충돌 유지, 외형만 숨긴다.

## 열린 질문 (Open questions)
- 약점 데미지 배율을 스캔과 무관하게 항상 적용할지, 스캔으로 드러났을 때만 적용할지 (현재 가정: 항상 적용)
- "Core 스크립트 안에서" 작성한다는 범위를 `Scripts/Core/Scanning/`(`Core.Scanning`)으로 이해함. 다른 의미면 폴더만 조정
- `fadeOutTime`(제안 1.0초), fade-in 평활 시간(제안 0.2초) 튜닝값
- 숨은 플랫폼이 충돌은 있는데 안 보이는 상태가 레벨 디자인상 의도에 맞는지

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
