# 명중 이펙트 확대 상한 2배 조정

## 작업 요약 및 결정
- 크기가 과하다는 사용자 피드백에 따라 BulletController의 MAX_HIT_VFX_SCALE을 3에서 2로 낮추고 Inspector 설명도 갱신했다.
- 카메라 거리 기준 10m 이내 1배 유지, 10~20m는 거리/10에 비례, 20m 이상 최대 2배다. 일반·차지 탄환에 공통 적용된다.
- 후속 요청에 따라 고정 상수를 제거하고 `_hitVfxMaxScale`을 SerializeField로 노출했다. Inspector의 Hit Vfx Max Scale 기본값은 2, 최솟값은 1이다. 기존 Hit Vfx Reference Distance(기본 10m)도 함께 조절할 수 있다. 최대 배율을 1로 두면 확대하지 않는다.

## 검증 및 현재 상태
- 인스펙터 필드 추가 후 재컴파일도 성공했다. SerializedObject로 일반·차지 프리팹 모두 최대 배율 2, 기준 거리 10의 직렬화 필드를 확인했다.
- Unity 컴파일 completed, failed=false, errors=[] 확인. 최초 구현의 실제 충돌·풀 재사용 Play 검증은 이전 기록에 남아 있으며 이번에는 상한 수치만 변경했다. 이번 조정 후 Play 재검증은 하지 않았다.
- 검증 단계에서는 미커밋으로 유지했고, 사용자의 후속 요청으로 최대 배율 인스펙터 노출 변경과 관련 기록을 커밋·푸시한다. 기존 ShaderGraphSettings 변경과 솔루션 파일 삭제는 제외한다.

## 다음 할 일
- 필요 시 사용자 플레이에서 화면상 크기를 추가 조정한다.
