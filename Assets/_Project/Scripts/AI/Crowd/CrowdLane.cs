using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Splines;
using Mediator;

namespace AI.Crowd
{
    /// <summary>
    /// 스플라인 하나를 군중 레인으로 쓰고, 씬 시작 시 개체를 일정 간격으로 생성해 이동시킨다.
    /// 닫힌 스플라인은 개체가 계속 돈다. 열린 스플라인은 끝에 닿은 개체를 풀에 반납하고, 시작점에서 다시 꺼낸다.
    /// 개체는 이 레인이 소유한 풀에서 나오므로 씬이 끝나면 함께 사라진다.
    /// 개체의 속도는 프리팹 SOAttributeData 의 기본 속도에 이 레인의 배율을 곱한 값이다.
    /// 스플라인이 있는 오브젝트의 스케일은 1 이어야 한다.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class CrowdLane : MonoBehaviour
    {
        #region Constants
        /// <summary>수평 접선 벡터의 제곱 크기가 이 값 이하이면 방향이 없는 것으로 본다.</summary>
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
        #endregion

        #region Serialized Fields
        [Header("Lane")]
        [SerializeField] private SplineContainer _lane;

        [Header("Spawn")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _count = 10;
        [Tooltip("개체 사이 최소 간격(m). 감지 거리 + 몸 길이보다 크게 두어야 한 바퀴를 다 채웠을 때 영원히 서지 않는다. 보행자 2.5 / 차량 9 권장.")]
        [SerializeField] private float _minSpacing = 2.5f;

        [Header("Respawn (열린 스플라인만)")]
        [Tooltip("시작점에서 개체를 다시 꺼내는 최소 시간 간격(초). 시작점 반경(최소 간격의 절반) 안에 프리팹의 감지 레이어 대상이 있으면 꺼내지 않는다.")]
        [SerializeField] private float _spawnInterval = 2.5f;

        [Header("Speed")]
        [SerializeField] private string _speedAttributeKey = "MoveSpeed";
        [Tooltip("프리팹 SOAttributeData 의 속도에 곱하는 배율. 1 이면 SO 값 그대로. 0 이하이면 개체가 움직이지 않는다.")]
        [SerializeField] private float _speedMultiplier = 1.0f;
        #endregion

        #region Private Fields
        private ObjectPool<CrowdController> _pool;
        private int _spawnCount;
        private float _laneLength;
        private bool _isLaneClosed;
        private float _spawnTimer;
        private LayerMask _spawnBlockLayers;
        #endregion

        #region Unity Lifecycle
        private void Reset()
        {
            _lane = GetComponent<SplineContainer>();
        }

        private void Start()
        {
            if (!_lane) _lane = GetComponent<SplineContainer>();

            if (!IsPrefabValid()) return;
            if (!IsLaneValid(out _laneLength)) return;

            _spawnCount = CalculateSpawnCount(_laneLength);
            if (_spawnCount <= 0) return;

            _isLaneClosed = _lane.Splines[0].Closed;

            // 개체가 멈추는 대상과 시작점을 막는 대상은 같으므로 프리팹의 감지 레이어를 그대로 쓴다
            _spawnBlockLayers = _prefab.GetComponent<CrowdController>().DetectLayers;

            // 꺼낼 때는 활성화하지 않는다. SpawnMember 가 위치를 옮긴 뒤 켠다.
            _pool = new ObjectPool<CrowdController>(
                CreateMember,
                null,
                member => member.gameObject.SetActive(false),
                member => Destroy(member.gameObject),
                true,
                _spawnCount,
                _spawnCount);

            float spacing = _laneLength / _spawnCount;
            for (int i = 0; i < _spawnCount; i++)
            {
                SpawnMember(i * spacing);
            }
        }

        private void Update()
        {
            if ((_pool == null) || _isLaneClosed) return;

            _spawnTimer += Time.deltaTime;

            bool canSpawn = (_pool.CountActive < _spawnCount) &&
                            (_spawnTimer >= _spawnInterval) &&
                            IsSpawnPointClear();
            if (!canSpawn) return;

            _spawnTimer = 0.0f;
            SpawnMember(0.0f);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 생성할 프리팹이 지정되어 있고 루트에 필요한 컴포넌트가 있는지 확인한다.
        /// </summary>
        /// <returns>생성에 쓸 수 있으면 true</returns>
        private bool IsPrefabValid()
        {
            if (!_prefab)
            {
                Debug.LogError($"[{name}] 생성할 프리팹이 지정되지 않았습니다.", this);
                return false;
            }

            if (!_prefab.TryGetComponent(out CrowdController _) ||
                !_prefab.TryGetComponent(out CharacterMediator _))
            {
                Debug.LogError($"[{name}] 프리팹 루트에 CrowdController 와 CharacterMediator 가 있어야 합니다.", _prefab);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 레인의 스플라인이 쓸 수 있는지 확인하고 길이를 반환한다.
        /// </summary>
        /// <param name="laneLength">스플라인 길이(m)</param>
        /// <returns>생성에 쓸 수 있으면 true</returns>
        private bool IsLaneValid(out float laneLength)
        {
            laneLength = 0.0f;

            if (!_lane || (_lane.Splines.Count == 0))
            {
                Debug.LogError($"[{name}] SplineContainer 또는 스플라인이 없습니다.", this);
                return false;
            }

            laneLength = _lane.CalculateLength();
            if (laneLength <= Mathf.Epsilon)
            {
                Debug.LogError($"[{name}] 스플라인 길이가 0 입니다.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 최소 간격을 지키며 레인에 들어가는 개체 수를 구한다. 모자라면 경고한다.
        /// </summary>
        /// <param name="laneLength">스플라인 길이(m)</param>
        /// <returns>생성할 개체 수</returns>
        private int CalculateSpawnCount(float laneLength)
        {
            int maxCount = _minSpacing > 0.0f
                ? Mathf.FloorToInt(laneLength / _minSpacing)
                : _count;

            if (_count > maxCount)
            {
                Debug.LogWarning(
                    $"[{name}] 요청 {_count}개가 최소 간격 {_minSpacing}m 에 들어가지 않아 {maxCount}개만 생성합니다. (길이 {laneLength:F1}m)",
                    this);
                return maxCount;
            }

            return _count;
        }

        /// <summary>
        /// 풀에 넣을 개체를 새로 만든다. 속도 배율을 적용하고, 열린 레인 끝에 닿으면 반납되도록 구독한다.
        /// </summary>
        /// <returns>새 개체의 조종부</returns>
        private CrowdController CreateMember()
        {
            GameObject member = Instantiate(_prefab, transform);
            CrowdController controller = member.GetComponent<CrowdController>();
            CharacterMediator mediator = member.GetComponent<CharacterMediator>();

            controller.OnLaneEndReached += ReleaseMember;

            // 잘못된 키는 AttributeSet 이 Debug.Log 만 찍어 묻히므로 먼저 검사한다
            if (!mediator.IsValidAttribute(_speedAttributeKey))
            {
                Debug.LogWarning(
                    $"[{name}] 어트리뷰트 '{_speedAttributeKey}' 가 개체에 없습니다. 속도가 적용되지 않습니다.",
                    _prefab);
                return controller;
            }

            // SO 초기값은 모터에 전달되지 않으므로(MediatorBase.InitValue 미호출) 배율이 1 이어도 다시 넣어 전달한다.
            // 풀이 개체를 새로 만들 때만 호출되므로 재사용해도 배율이 누적되지 않는다.
            float baseSpeed = mediator.GetAttribute(_speedAttributeKey);
            mediator.SetAttribute(_speedAttributeKey, baseSpeed * _speedMultiplier);
            return controller;
        }

        /// <summary>
        /// 끝에 닿은 개체를 풀에 반납한다.
        /// </summary>
        /// <param name="member">반납할 개체의 조종부</param>
        private void ReleaseMember(CrowdController member)
        {
            _pool.Release(member);
        }

        /// <summary>
        /// 시작점 근처에 막는 대상(플레이어나 다른 개체)이 없는지 확인한다.
        /// </summary>
        /// <returns>비어 있으면 true</returns>
        private bool IsSpawnPointClear()
        {
            Vector3 startPosition = _lane.EvaluatePosition(0.0f);

            return !Physics.CheckSphere(
                startPosition,
                _minSpacing * 0.5f,
                _spawnBlockLayers,
                QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// 풀에서 개체 하나를 꺼내 진행 거리 지점에 놓고 레인을 지정한다.
        /// </summary>
        /// <param name="distance">스플라인 시작점으로부터의 진행 거리(m)</param>
        private void SpawnMember(float distance)
        {
            float normalized = distance / _laneLength;
            Vector3 position = _lane.EvaluatePosition(normalized);
            Vector3 tangent = Vector3.ProjectOnPlane(_lane.EvaluateTangent(normalized), Vector3.up);

            Quaternion rotation = tangent.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE
                ? Quaternion.LookRotation(tangent.normalized, Vector3.up)
                : Quaternion.identity;

            // 재사용 개체는 반납된 자리(레인 끝)가 아니라 새 위치에서 켜지도록 옮긴 뒤 활성화한다
            CrowdController member = _pool.Get();
            member.transform.SetPositionAndRotation(position, rotation);
            member.gameObject.SetActive(true);

            // SetLane 이 현재 위치를 기준점으로 저장하므로 위치를 먼저 옮긴다
            member.SetLane(_lane, distance);
        }
        #endregion
    }
}
