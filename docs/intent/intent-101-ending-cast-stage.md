---
id: intent-101
title: 엔딩 크레딧 왼쪽에 플레이어·펫 장난 + 등장 캐릭터 커튼콜 애니메이션
part: UI / Credits / Ending
status: open   # open | resolved
created: 2026-10-31
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
엔딩 크레딧은 오른쪽 절반에서만 명단이 올라가고 왼쪽 절반은 비어 있다(원래 회상 사진 자리).

## 기대 결과 (Proposed outcome)
크레딧 왼쪽 절반에 캐릭터 장면이 나온다(사진은 넣지 않음, 사용자 선택).
- 앞부분: 플레이어와 펫이 장난친다(펫이 머리 위를 돌고 웃기, 플레이어 손 흔들기, 펫이 콩 → 플레이어 깜짝, 쫓아가기, 같이 점프, 펫 춤).
- 뒷부분: 적·NPC(GC·근접·원거리·특수·도둑·보스B)가 하나씩 걸어 들어와 줄을 서고, 다 같이 동작하는 커튼콜로 끝난다(크레딧 끝까지 반복).

### 설계
- 로딩 화면 러너(intent-097)와 같은 방식: 스크립트·충돌체를 뺀 **모습 전용 복사본** + 전용 RenderTexture 카메라를 먼 곳(y=-3000)에 두고, 크레딧 왼쪽 절반 RawImage로 띄운다.
- 프리팹 `Prefabs/UI/Credits/EndingCastStage.prefab`(캐릭터·카메라·조명·연출 스크립트). 캐릭터마다 쓸 동작만 담은 애니메이터 `Animations/Ending/`.
- 연출 스크립트 `UI.Credits.EndingCastDirector`: 켜지면 정해진 순서대로 동작(애니메이터 상태 전환 + 위치 이동)을 재생, 마지막 커튼콜은 반복.
- `EndingCredits`: 크레딧이 시작될 때(화면이 어두워지기 시작할 때) 무대를 켠다(`_castStage` 필드 하나).
- `Map_5_Live`: 무대 프리팹 인스턴스(꺼 둠) + `CreditsGroup` 안 왼쪽 절반 RawImage 추가, `EndingCredits` 연결만.

## 영향 범위 (Affected users and systems)
- 엔딩을 보는 플레이어.
- 신규: 프리팹 1개, 애니메이터 컨트롤러 여러 개, RenderTexture 1개, `EndingCastDirector.cs`.
- 수정: `EndingCredits.cs`(필드 1개·켜기 1줄), `Map_5_Live.unity`(위 추가·연결만).

## 제약 (Constraints)
- 실제 캐릭터 프리팹(Player·Pet·적·NPC)은 수정하지 않는다. 복사본만 쓴다.
- 팀 공용 설정(레이어 등)은 바꾸지 않는다.
- `Map_5_Live`는 위 추가 외에는 건드리지 않는다.

## 열린 질문 (Open questions)
- 캐릭터별 크기·위치·동작 타이밍은 기본값으로 만든 뒤 사용자가 보고 조정.

## 구현 기록 (2026-10-31)
- 애니메이터 8개 `Animations/Ending/EndingCast_*.controller`: 캐릭터마다 쓸 상태만(상태 이름 = 클립 이름). 플레이어는 Tacko3D 팩 클립, 펫은 Rob11Emo02 클립(춤·웃음·승리 등), 적·NPC는 각 프리팹 컨트롤러의 클립.
- `Prefabs/UI/Credits/EndingCastStage.prefab`(y=-3000): `CastCamera`(로딩 러너 카메라 설정 복사, 투명 배경, `Art/Credits/RT_EndingCast.renderTexture` 1024×1152), `KeyLight`·`FillLight`, 모습 전용 복사본 8명(Player·Pet 원래 크기, 적·NPC는 0.65배) + `EndingCastDirector`.
- `EndingCastDirector`(`Scripts/UI/Credits`): 켜지면 장난(약 17초, 가까운 카메라) → 커튼콜(카메라가 (0,3,-12.5)·8°로 빠지며 GC·근접·원거리·특수·도둑·보스B가 1.4초 간격으로 걸어 들어와 줄 섬) → 피날레(3초마다 플레이어 손 흔들기·펫 춤/승리·적 동작 반복). 실제 시간(unscaled)으로 움직임.
- `EndingCredits._castStage`: 크레딧이 어두워지기 시작할 때 무대를 켬.
- `Map_5_Live`: 무대 인스턴스(꺼 둠), `CreditsGroup/CastView`(왼쪽 절반 RawImage, 배경 바로 위) 추가·연결.
- 같은 작업에서 사용자 요청으로 명단 아래에 `ASSETS & SOURCES`(...) / `SPECIAL THANKS`(...) / `TOOLS`(Unity Engine) / `FRAGMENT IN DEVICE` / "누군가의 꿈을 위해, 작은 데이터 하나가 움직였습니다."를 `THANK YOU FOR PLAYING` 앞에 추가.
- 확인: 리컴파일 통과. Play(로딩씬에서 프리팹을 잠깐 생성)에서 시간대별 카메라 캡처로 장난·쫓아가기·커튼콜·피날레 구도 확인 후 쫓아가기 거리·콩 높이·커튼콜 카메라·줄 위치 조정. `Map_5_Live` diff는 위 추가·연결·문구뿐. 실제 엔딩 흐름(크레딧과 겹친 모습)은 미확인.
- 사용자 요청: 크레딧이 끝날 때도 페이드아웃. `EndingCredits._fadeOutCover`(Graphic)·`_fadeOutSeconds`(2초) 추가, 명단이 다 올라가고 `_endHoldSeconds` 뒤 `CreditsGroup/FadeOutCover`(맨 위 검은 이미지, 시작 시 투명)를 불투명하게 만든 뒤 메인 메뉴로. 설정창이 열려 있으면 멈춤. 리컴파일 통과, 실제 흐름 미확인.
- 알려진 점: 도둑은 색이 어두워 뒤줄에서 잘 안 보임. 크레딧 길이에 따라 장난 구간 중 명단이 시작됨(장면은 명단과 독립).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
