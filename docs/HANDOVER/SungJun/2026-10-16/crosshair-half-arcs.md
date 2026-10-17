# MainScene 크로스헤어 반원형으로 정리 (2026-10-16)

## 작업 요약 및 방법
- 사용자 참고 이미지(가운데 점 + 작은 링, 위 빨강·아래 파랑 원호)에 맞춰 Test MainScene `Canvas/Crosshair`를 Unity MCP `eval`로 수정했다.
  - `HeatArc`(빨강)·`BatteryArc`(파랑) fill 0.4/0.41 → 0.5: 위 반원 180°는 빨강, 아래 반원 180°는 파랑으로 꽉 채움.
  - `CenterDot` 16 → 8px로 줄이고, 그 바깥에 `CenterRing`(30px, 흰색, Shift `Circle Outline - Stroke 12px`, raycastTarget 끔) 추가.
  - 참고 이미지에 없는 `HeatLabel`·`BatteryLabel`(28%/61%)은 비활성화(삭제 안 함).
- 동서남북 작대기: 현재 `Canvas/Crosshair`에는 원래 없음(작대기 `Line_*`는 꺼진 `Canvas_HudReference`에만 있음, 손대지 않음).
- Game 뷰 캡처로 확인. 처음 캡처는 화면이 갱신되지 않아 `RepaintAllViews` 후 다시 찍었다. 캡처용 `Assets/Temp`는 삭제했다.

## 결정 사항
- 원호는 현재 값과 관계없이 각 반원을 꽉 채운 상태(fill 0.5)로 둔다(사용자 요청 "위 빨강 180도, 아래 파랑 180도").

## 현재 상태 및 이슈
- 사용자 요청으로 MainScene 저장(기존 사용자 미저장 변경 포함). 커밋 안 함. 씬 오버라이드로만 바뀌었고 Test `Canvas.prefab`에는 적용하지 않았다.
- 링이 밝은 하늘 배경 위에서는 다소 흐리게 보인다.

## 다음 할 일
- 필요하면 Test `Canvas.prefab`에 Apply.
- 열·배터리 값을 원호에 연결할 때는 fill = 값 × 0.5 규칙 사용.

## 추가 작업: 원호를 크로스헤어에 더 붙임
- 사용자 요청("빨파 원이 너무 커서 더 붙게")으로 `HeatArc`·`BatteryArc` 지름 176 → 84, `RingTrack` 172 → 82로 줄였다.
- 지름을 줄이면 선이 얇아지므로 스프라이트를 원호 `Stroke 5px` → `Stroke 10px`, 트랙 `Stroke 3px` → `Stroke 8px`로 바꿔 두께를 비슷하게 유지했다.
- Game 뷰 캡처가 계속 이전 화면으로 찍혀서, 크로스헤어 복제본을 임시 카메라·RenderTexture로 따로 렌더해 확인했다(임시 오브젝트는 바로 삭제).
- 사용자 요청으로 MainScene 저장(커밋 안 함).

## 추가 작업: 크로스헤어 정중앙 정렬
- 사용자가 직접 바꾼 상태: 원호·트랙 지름 120, 열·배터리 라벨을 `Canvas` 바로 아래 `HeatLabel`(95, 45)·`BatteryLabel`(95, -45)로 꺼내 둠(Crosshair 안 라벨은 꺼진 채).
- `Canvas/Crosshair` 위치 (0, 18) → (0, 0)으로 화면 정중앙에 맞췄다. 라벨이 이미 화면 중앙 기준 ±45라 크로스헤어를 내리면 위아래 간격이 대칭이 되어 라벨 값은 바꾸지 않았다.
- 임시 카메라 렌더(화면 중앙 가이드선 포함)로 확인. 사용자 요청으로 MainScene 저장(사용자 미저장 변경 포함), 커밋 안 함. Test `Canvas.prefab`에는 적용 안 함(씬 오버라이드).

## 추가 작업: 게이지 원호 네온 글로우
- 사용자 요청: 참고 이미지처럼 게이지 바(빨강·파랑 원호)만 빛나게. 다른 요소는 추가하지 않음. "에셋 사용할 수 있는 부분은 사용" 지시에 따라 처음 만든 자체 글로우 텍스처(`UnityAssets/Textures/UI/UI_RingGlow.png`)는 삭제하고 Shift 에셋만 사용했다.
- 원호마다 2개 추가(원호와 같은 Filled Radial360 설정·fill):
  - `HeatGlow`/`BatteryGlow`(원호 뒤): Shift `Glow & Shadow/Circle Outline Glow 64px`, 지름 161.4(스프라이트 안쪽 경계를 원호 안쪽 테두리에 맞춤), 원호 색 알파 0.75.
  - `HeatCore`/`BatteryCore`(원호 위): `Circle Outline - Stroke 3px`, 지름 113.3(선 중심을 원호 선 중심에 맞춤), 원호 색을 흰색 쪽으로 65% 섞은 밝은 색.
- 임시 카메라 렌더로 확인. 사용자 요청으로 MainScene 저장, 커밋 안 함.
- 나중에 게이지 값을 연결하면 `*Arc`·`*Glow`·`*Core` 세 개의 fillAmount를 함께 바꿔야 한다.

## 추가 작업: 위·아래 원호 사이에 틈 (3시·9시)
- 사용자 참고 이미지처럼 빨강(위)·파랑(아래)을 딱 두 개로 나눔. 3시·9시 지점에 14° 틈.
- `HeatGlow/Arc/Core`·`BatteryGlow/Arc/Core` 6개: fillAmount 0.5 → (180 − 14) / 360 ≈ 0.461, Z 회전 0 → 7°(시작점을 틈 절반만큼 반시계로 밀어 위·아래 가운데 정렬).
- 값 연결 시 최대 fill은 0.461이다 → fill = 값 × (166 / 360). 회전 7°는 유지.
- 틈 안으로 희미한 `RingTrack`(알파 0.14)이 비쳐 보인다(밝은 배경에서 약간 하얗게). 깔끔한 틈이 필요하면 RingTrack을 끄거나 같은 틈을 줘야 함.
- 임시 카메라 렌더로 확인. **MainScene 미저장**.

## 수정: 두 칸 → 90° 네 칸 (같은 간격)
- 사용자 정정: "90도 게이지 4개를 같은 간격으로". 위 14° 두 칸 분리 작업을 아래처럼 바꿈.
- 12·3·6·9시 네 곳에 같은 10° 틈. 각 칸 fill = (90 − 10) / 360 ≈ 0.222.
  - 기존 `HeatGlow/Arc/Core`·`BatteryGlow/Arc/Core`: 첫 칸, Z 회전 5° (Heat = 3시→12시, Battery = 9시→6시)
  - 복제 `HeatGlow2/HeatArc2/HeatCore2`·`BatteryGlow2/BatteryArc2/BatteryCore2`: 둘째 칸, Z 회전 95° (Heat = 12시→9시, Battery = 6시→3시)
- 값 연결 시 색마다 두 칸에 나눠 채워야 한다(예: 첫 칸 fill = min(값, 0.5) × 2 × 0.222, 둘째 칸은 나머지).
- 틈 안으로 `RingTrack`과 글로우 빈 자리가 비친다. 임시 카메라 렌더로 확인. 사용자 요청으로 MainScene 저장, 커밋 안 함.

## 추가 작업: 이어진 원 버전 Canvas 따로 보관
- 사용자 요청: 지금 Canvas를 그대로 복사해서, 복사본은 예전 버전(원이 끊김 없이 이어진 형태)으로 남기기.
- `Canvas`를 프리팹 연결 유지한 채 복제(`DuplicateGameObjectsUsingPasteboard`) → `Canvas_CrosshairFullRing`(루트 4번째, 꺼짐).
- 복사본 `Crosshair`: `*2` 6개 삭제, `HeatGlow/Arc/Core`·`BatteryGlow/Arc/Core` fill 0.5·회전 0으로 되돌림(위 반원 빨강·아래 반원 파랑이 3시·9시에서 맞닿음). 그 외(차지 바·독백·설정창 등)는 원본과 같음.
- 복사본에도 `UIManager`·Presenter가 그대로 있어서 원본 `Canvas`와 동시에 켜면 HUD 로직이 둘 다 돈다. 하나씩만 켤 것.
- 확인 중 발견: 원본 `Canvas`가 마지막 커밋(e254916) 이후 꺼짐(m_IsActive 0)으로 저장돼 있음. 렌더 확인 코드는 매번 다시 켰으므로 에디터에서 꺼진 것으로 보이며 그대로 둠.
- 임시 카메라 렌더로 확인. **MainScene 미저장**.

## 추가 작업: RingTrack도 90° 네 칸으로 분리
- 사용자 요청: 나중에 fill로 채울 때 빈 칸 트랙이 칸 모양과 맞도록 `RingTrack`도 같은 틈으로 나눔(원본 `Canvas`만, `Canvas_CrosshairFullRing`은 이어진 트랙 그대로).
- `RingTrack`: Simple → Filled Radial360(원점 Right, 반시계), fill (90 − 10) / 360 ≈ 0.222, Z 회전 5°.
- 복제 `RingTrack2`·`RingTrack3`·`RingTrack4`: Z 회전 95°·185°·275°. 색·스프라이트는 그대로(알파 0.14).
- 트랙만 진하게 한 임시 렌더로 원호 칸과 틈이 일치하는 것 확인, 틈 안에 트랙이 더 이상 비치지 않음. **MainScene 미저장**.

## 다시 만듦: 90° 온전한 네 칸 + 평행한 틈(패딩)
- 사용자 정정: 10° 틈을 깎는 게 아니라 칸마다 90°가 온전히 살아 있어야 함(값 0~0.5 → 첫 칸, 0.5~1 → 둘째 칸으로 채우기 위해). 칸 사이는 패딩처럼 띄워 이어지지 않게. 참고 이미지: 끝이 축과 평행하게 잘린 일정한 폭의 틈.
- 방법: 모든 칸 fill 0.25(90°), 칸 전체를 바깥 대각선으로 7씩 이동 → 12·3·6·9시에 폭 14의 평행한 틈. (반지름 방향으로 0°·90°에서 잘린 선은 수평·수직선이라 이동해도 평행 유지)
  - `HeatArc/Core` (7, 7) 회전 0, `HeatArc2/Core2` (−7, 7) 회전 90
  - `BatteryArc/Core` (−7, −7) 회전 0, `BatteryArc2/Core2` (7, −7) 회전 90
  - `RingTrack` (7, 7) 0°, `RingTrack2` (−7, 7) 90°, `RingTrack3` (−7, −7) 180°, `RingTrack4` (7, −7) 270° (모두 원점 Right·반시계)
  - 글로우(`HeatGlow/2`, `BatteryGlow/2`): 같은 위치에 fill (90 + 12) / 360 ≈ 0.283, 회전 −6°/84°로 양 끝을 6°씩 늘려 끝을 감싸게 함. 8°는 이웃 글로우가 겹쳐 틈이 밝아지고, 늘리지 않으면 틈이 바깥까지 검은 줄로 보여서 6° 선택.
- 값 연결 규칙(색마다): 첫 칸 fill = 0.25 × clamp01(값 × 2), 둘째 칸 fill = 0.25 × clamp01(값 × 2 − 1). Heat는 3시→12시→9시, Battery는 9시→6시→3시 방향으로 참. 글로우는 늘린 6°만큼 따로 계산해야 함.
- 원본 `Canvas`만 적용. `Canvas_CrosshairFullRing`은 이어진 원 그대로. 실제 배경·어두운 배경 임시 렌더로 확인. 사용자 요청으로 MainScene 저장, Test 저장소 0bea967 커밋·push(이전 미push 커밋 12개 함께 push).
