# AI 밸런스 창 행 설명 툴팁 (2026-10-26)

## 1. 작업 요약 및 방법
- 문제: `Tools > AI > 밸런스 수치 표`(`AI/Editor/AIBalanceWindow.cs`) 행 이름이 속성 이름뿐이라 뜻을 알기 어려웠다. 기존 툴팁은 라벨을 그대로 보여 줬다.
- 사용자 선택: 표시 방식은 마우스 오버 툴팁(A), 설명 저장 위치는 코드 사전.
- 변경(이 파일만 수정):
  - `Row.Tooltip` 추가. 툴팁은 `라벨 + 줄바꿈 + 설명`(설명이 없으면 라벨만, 기존 동작과 같음).
  - 컴포넌트 값: `SerializedProperty.tooltip`(필드의 `[Tooltip]`)을 그대로 사용.
  - 어트리뷰트·블랙보드·BT 노드 값: `static readonly Dictionary<string, string> Descriptions`에서 찾음.
    - 키: 어트리뷰트 이름, 블랙보드 변수 이름, `"노드(또는 조건) 이름 / 필드 이름"`. `#2` 같은 순서 번호와 부모 노드 이름은 키에서 뺀다(`AddFieldCells`의 `ownerName`).
- 설명 내용은 BT 노드 스크립트(`AI/BT/*.cs`)의 요약 주석·story 문장과, BT 에셋 YAML에서 확인한 필드↔블랙보드 연결을 근거로 적었다.
- 실행자: Claude 직접 수행. 한 파일 수십 줄 수정이라 Codex 위임 비용이 더 크다고 판단.

## 2. 결정 사항
- 새 BT 노드나 어트리뷰트를 추가하면 `Descriptions`에 같은 키 형식으로 한 줄을 추가한다. 컴포넌트 필드는 사전이 아니라 필드에 `[Tooltip]`을 단다.

## 3. 현재 상태 및 이슈
- 리컴파일 통과. eval로 창을 만들어 확인한 결과 설명이 붙은 행은 163/190(어트리뷰트 4/4, 블랙보드 13/13, BT 노드 24/24).
- 설명이 없는 27행은 `[Tooltip]`이 없는 컴포넌트 필드다: `BossController.Rest Seconds`, `BossHand`(Hover Height·Move Seconds·Slam Seconds), `BossShoulderCannon`(Telegraph Seconds·Knockback Distance·Knockback Seconds), `BossASlam`/`BossALineSlam` Telegraph Seconds, `BossBMeleeCombo.Last Knockback Seconds`, `BossBCharge`/`BossBLeap` Knockback Distance·Seconds, `BossBLeap.Is Evade`.
- 특수몹 BT의 `RetreateRange`·`RetreateMoveDistance`·`AttackExitRange`는 어떤 노드에도 연결되어 있지 않아 값을 바꿔도 효과가 없다(설명에 그렇게 적음). `AttackRange`도 특수몹 그래프에서는 연결되어 있지 않다.
- 창에서 실제로 마우스를 올려 보는 육안 확인 미실행. 커밋 전.

## 4. 다음 할 일
- 사용자 육안 확인(툴팁 내용·가독성).

## 5. 후속: 보스 필드 27행 `[Tooltip]` 추가 (사용자 요청)
- 위 3절의 필드 16개(27행)에 `[Tooltip]`만 추가. 값·필드 이름·직렬화는 그대로. 수정 파일: `BossController`·`BossHand`·`BossShoulderCannon`·`BossASlam`·`BossALineSlam`·`BossBMeleeCombo`·`BossBCharge`·`BossBLeap`.
- 설명은 각 필드의 사용 위치를 읽고 적음(예: A 예고 시간에는 `TelegraphScale`이 곱해짐, 넉백 방향은 패턴마다 다름).
- 리컴파일 통과, eval 결과 밸런스 창 190/190행에 설명이 붙음. 육안 확인 미실행, 커밋 전.
