# 플레이어에게 막힌 보행자 짜증 표정

## 작업 요약 및 방법
- 목표: 플레이어가 앞을 막아 보행자가 멈추면 눈 표정을 Annoyed 로 바꾼다.
- `CrowdController`
  - `_playerLayers`(LayerMask)와 `IsBlockedByPlayer` 프로퍼티를 추가했다. 매 프레임 `Update` 맨 앞에서 false 로 지우고, `TryPassAhead` 에서 플레이어 레이어 콜라이더가 감지되면 true 로 둔다.
  - `TryPassAhead` 는 첫 막힘에서 바로 끝내지 않고 모든 히트를 본다 (`SphereCastNonAlloc` 결과가 거리순이 아니라 플레이어 히트를 놓칠 수 있어서). 이동 가능 여부 결과는 기존과 같다.
- `CrowdExpression`(신규, `Scripts/AI/Crowd/`)
  - `IsBlockedByPlayer` 가 true 인 시각을 기억하고, 그로부터 `_holdDuration`(기본 1.5초) 안이면 Animator `IsAnnoyed` 를 켠다. 값이 바뀔 때만 파라미터를 쓴다. `OnEnable` 에서 초기화해 풀에서 꺼낸 개체에 표정이 남지 않게 한다.
- `Pedestrian.controller`: 파라미터 `IsAnnoyed`(Bool), 레이어 `Face`(Override, weight 1), 상태 `Default`/`Annoyed`(`ThirdParty/LilRobot/Eye Expressions/` 클립), 전환은 Exit Time 없음·Duration 0.
- `Pedstrian.prefab`: `CrowdExpression` 추가, `CrowdController._playerLayers` = Player(64).

## 결정 사항
- 표정은 Annoyed, 플레이어에게만 반응, 풀린 뒤 1.5초 유지 (사용자 결정, intent-017 에 기록).
- 중재자를 거치지 않는다. 같은 프리팹 내부 연출이고 다른 컴포넌트와 통신하지 않기 때문이다 (`CrowdAppearance` 와 같은 방식).

## 현재 상태 및 이슈
- 컴파일 에러 0.
- Play(Map_1_Lobby) 검증: 보행자 앞에 Player 레이어 임시 캡슐을 세우자 근처 2마리가 `IsBlockedByPlayer=true`, `IsAnnoyed=true`, Face 레이어 `Annoyed` 로 바뀌었고, 캡슐을 치운 뒤 수 초 뒤 전체 21마리가 `IsAnnoyed=false` 였다. 보행자끼리 줄 선 개체는 `IsAnnoyed=false` 였다.
- 검증하지 못한 것: 실제 플레이어 캐릭터로 막은 경우, 유지 시간 1.5초 경계(막힘 해제 후 1.5초 안에는 켜져 있는지)의 정확한 측정, 게임 뷰에서 눈 모양이 실제로 바뀌는 모습(캡처 안 함).
- 표정 클립이 `Assets/ThirdParty/`(Git 제외)에 있어, 클립이 없는 환경에서는 `Pedestrian.controller` 의 Face 상태 모션이 비어 표정이 바뀌지 않는다. 프리팹이 이미 같은 폴더의 모델을 참조하고 있어 조건은 같다.
- MCP `add_component` 는 프리팹 에셋에 저장되지 않았고(기존 인스턴스 메모리만 수정), LayerMask 는 `set_serialized_field` 이 지원하지 않는다. 프리팹은 `PrefabUtility.LoadPrefabContents` + eval 로 수정했다.

## 다음 할 일 (Next Steps)
- 실제 플레이어로 보행자 앞을 막아 눈 모양이 바뀌는지 눈으로 확인하고, `_holdDuration` 을 조정한다.
- 차량(`Car`)에도 표정이 필요하면 `Car` 프리팹에 같은 방식으로 붙일 수 있다 (현재는 보행자만).
