# 0·1 낙하 개별 회전(Y축) 오프셋 (2026-10-29)

## 1. 작업 요약 및 방법
- 요청: AI_TODO2 [연출-1] 추가 요청 — 0과 1이 서로 다른 각도로 떨어지도록 개별 회전 오프셋 적용 (intent-074 후속, [가속도 오프셋](./falling-gravity-offset.md)에 이어서).
- 구현: `FallingObstacle` 에 `_yawOffsetRange`(도, 0~180, 기본 0) 추가. 낙하 사이클이 시작될 때마다 `AngleAxis(Random.Range(-범위, +범위), Vector3.up) * _baseRotation` 으로 회전을 정한다. `_baseRotation` 은 `Start`/`DropOnto` 에서 처음 놓인 회전으로 저장해, 반복(`_isLoop`) 낙하에서도 각도가 누적되지 않는다.
- 프리팹 `Security_FallingZero`·`Security_FallingVirus` 에 180 지정(Unity eval 로 `SerializedObject` 수정, YAML 직접 수정 없음).
- 실행자: Claude 직접(Codex CLI 미설치).

## 2. 결정 사항 (사용자 확인 완료)
- 축: **Y축(세로 축)만**. 콜라이더 높이(`bounds.extents.y`)와 착지점 계산이 그대로라 안전하다. 기울이는 회전(X/Z)은 착지 높이 보정이 필요해 제외했다.
- 방식: **낙하 중에는 돌지 않고 고정 각도**로 떨어진다. 낙하마다 새로 굴려 풀 재사용에도 안전하다.
- 예고 원(`_warningCircle`)은 루트에서 분리돼 있어 회전의 영향을 받지 않는다. 착지 연출은 `Quaternion.identity` 라 그대로다.

## 3. 현재 상태 및 이슈
- 리컴파일 에러 0.
- Play(`Map_4_Security`)에서 `Security_FallingZero` 10개를 `DropOnto`: Y각 113/309/79/263/123/164/185/194/341/214°, X·Z는 0 — 개체마다 모두 달랐다. 테스트 오브젝트는 정리하고 Play 종료.
- `Security_FallingVirus.prefab` diff 에 모델 스케일 2 수정이 함께 보이는데 이번 작업 이전부터 있던 변경이다.
- 모델이 Y축 회전에 대칭이면 각도 차이가 눈에 안 보일 수 있다(육안 확인 전).
- **육안 확인·게이트 루프 미실행, 커밋 전.**

## 4. 다음 할 일
- 사용자 직접 플레이: 2라운드에서 0·1 이 서로 다른 각도로 보이는지, 카메라에서 각도 차이가 눈에 띄는지. 부족하면 모델에 비대칭이 있는지 확인하거나 X/Z 기울기를 별도 결정으로 추가.
- 다음 권장 순서: [보스-15] 보스A 에너지 실드.
