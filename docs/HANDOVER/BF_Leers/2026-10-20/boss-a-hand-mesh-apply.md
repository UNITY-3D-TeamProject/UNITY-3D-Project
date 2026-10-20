# 보스 A 손 본 이동 방식 적용 (2026-10-20)

## 작업 요약 및 방법
- 분리 손 메쉬 방식(1차: 몸 손 숨김 / 2차: 위에서 나타나 사라짐)은 둘 다 부자연스럽다는 사용자 피드백으로 **폐기**했다. 최종은 **원본 손 본(Palm)만 움직여서 내려찍는 방식**이다. 팔은 애니메이션 그대로 두고, 내려찍을 때 손 본만 표적으로 이동한다.
- `BossHand`(`Scripts/AI/Boss/BossA/`) 수정:
  - `_bone`(움직일 손 본), `_boneLift`(손 본 원점이 `BossHand` 오브젝트보다 위에 놓이는 높이 m) 필드 추가.
  - `LateUpdate`(Animator가 본을 쓴 뒤에 덮어써야 함): 쉬는 동안(Idle)은 `BossHand` 오브젝트(콜라이더)가 평소 위치를 따라가고, 그 외 상태에서는 `_bone.position = 오브젝트 위치 + 위로 _boneLift`로 손 본이 오브젝트를 따라간다.
  - `GetRestPosition()`: 평소 위치(`_restPoint`) − 본 높이. 회수 목적지에 쓴다. `_bone`이 비어 있으면 기존 동작과 같다.
- `BossA.prefab`:
  - `RigLArm1`·`RigRArm1` 밑에 `HandAnchor`(Palm과 같은 로컬 위치·회전)를 만들어 `BossHand._restPoint`로 연결했다. 손 본은 이제 `BossHand`를 따라 움직이므로 평소 위치는 따로 있어야 순환이 안 생긴다. (이전에는 `_restPoint`가 Palm 본이었다.)
  - `_bone` = `RigLArmPalm`/`RigRArmPalm`, `_boneLift` = 4.58.
  - `Gripper Robot`의 `SkinnedMeshRenderer.updateWhenOffscreen`을 켰다(손이 멀리 가면 원래 경계 밖이라 몸이 화면 밖일 때 잘릴 수 있음).
- 폐기한 것 정리: 손 FBX 복사본(`Art/Enemy/Boss/`)·`Hand_L/R/Model`·`BossHandBoneHider` 모두 제거. `BossHand`의 분리 손용 수정(`_model`·`_appearHeight`)은 되돌렸다.

## 결정 사항
- 아바타 마스크 불가(`BossA_Override`가 공유 `EnemyBase`를 덮어쓰는 구조). Animator가 매 프레임 손 본을 덮어쓰므로 LateUpdate 쓰기를 쓴다.
- `_boneLift` 4.58은 Idle 자세(손이 앞을 향한 자세)에서 몸 메쉬를 구워 잰 "Palm 본 원점에서 손 가장 아래까지"다. 내려칠 때 손 아래가 `BossHand` 위치(지면)에 닿는다. 자세가 바뀌면 값이 달라지므로 Play에서 맞춘다.
- 콜라이더·NavMeshObstacle·`BossHitRelay`는 `BossHand` 오브젝트(`Hand_L/R`)에 그대로 두었다.
- intent 문서는 만들지 않았다(`BossHand` 소규모 수정 + 프리팹 연결).

## 현재 상태 및 이슈
- 확인: 컴파일 에러 0. 미리보기 씬에서 Idle 애니메이션을 샘플링한 뒤 Palm 본만 지면으로 옮긴 렌더링에서, 손이 팔에서 떨어져 지면에 놓이고 팔은 몸에 남았다. 긴 막처럼 늘어나는 현상은 없고, 손목 쪽에 가늘고 작은 뾰족한 조각이 하나 보인다.
- **Play 육안 확인 미완.**
- **손 방향**: Idle에서 손이 앞(+z)을 향해 있어 내려칠 때도 손가락이 앞을 향한 채 눌러 놓는 모양이다(회전은 하지 않는다). 손가락이 아래로 향하는 "쾅" 느낌이 필요하면 슬램 동안 손 본을 아래로 회전하는 코드가 더 필요하다.
- **자세 의존**: `_boneLift`는 Idle 기준이다. 공격 애니메이션 중에는 손 높이가 달라져 지면에 약간 뜨거나 묻힐 수 있다.
- 콜라이더(6×2×6 m)는 손 모양(약 12×13 m 규모)보다 작다.
- **Play 중 에러 스팸**: 이 작업 중 에디터가 Play 상태였고 `BossArena.Update`의 NullReference(`Grid`가 null)가 약 2000회 이상 쌓였다. `Grid`는 Awake에서 만드는 비직렬화 값이라 Play 중 도메인 리로드가 있으면 null이 된다. 원인은 이 작업(스크립트 변경·애셋 삭제·프리팹 저장)이 Play 중 리로드를 일으킨 것으로 추정되며 확정하지 못했다. Play를 껐다 다시 켜면 사라질 것으로 보인다(미확인).
- 신규·수정 파일은 커밋 전이다: `BossHand.cs`, `BossA.prefab`.

## 다음 할 일 (Next Steps)
- Play를 껐다 다시 켠 뒤 A1·A2·A4·A5·A6·그로기 때 손이 내려가는 모양, 지면 닿음, 팔과 손목 모습을 확인한다.
- 손 방향(손가락 아래로)과 `_boneLift`, 콜라이더 크기를 보고 조정한다.
