using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Core;
using Core.ObjectPool;
using Facade.Player;
using Mediator;

namespace AI.Crowd
{
    /// <summary>
    /// 스플라인 하나를 군중 레인으로 쓰고, 개체를 생성해 이동시킨다.
    /// 닫힌 스플라인은 씬 시작 시 전체에 일정 간격으로 깔고, 개체가 계속 돈다.
    /// 열린 스플라인은 씬 시작 시 일부를 미리 깔고 나머지는 시작점에서 차례대로 꺼내며, 끝에 닿은 개체를 풀에 반납한 뒤 시작점에서 다시 꺼낸다.
    /// 시작점과 개체 수는 0번 스플라인 기준이다. 갈림길은 같은 컨테이너 안에서 Knot 으로부터 바깥쪽으로 새 스플라인을 그려 만든다 (개체가 무작위로 고른다).
    /// 개체를 꺼낼 때마다 프리팹 목록에서 하나를 무작위로 고른다.
    /// 개체는 씬의 ObjectPoolManager(Crowd 카테고리)에서 꺼내므로 목록의 프리팹이 모두 매니저에 등록돼 있어야 한다.
    /// 개체의 속도는 프리팹 SOAttributeData 의 기본 속도에 이 레인의 배율과 개체별 무작위 배율(조종부 설정)을 곱한 값이다.
    /// 스플라인이 있는 오브젝트의 스케일은 1 이어야 한다.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class CrowdLane : MonoBehaviour
    {
        #region Constants
        /// <summary>수평 접선 벡터의 제곱 크기가 이 값 이하이면 방향이 없는 것으로 본다.</summary>
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

        /// <summary>시작 방향을 구할 때 비교하는 앞쪽 점까지의 거리(m).</summary>
        private const float TANGENT_SAMPLE_DISTANCE = 0.1f;
        #endregion

        #region Serialized Fields
        [Header("Lane")]
        [SerializeField] private SplineContainer _lane;

        [Header("Spawn")]
        [Tooltip("생성할 프리팹 목록. 꺼낼 때마다 이 중 하나를 무작위로 고른다.")]
        [SerializeField] private List<GameObject> _prefabs = new List<GameObject>();
        [SerializeField] private int _count = 10;
        [Tooltip("개체 사이 최소 간격(m). 감지 거리 + 몸 길이보다 크게 두어야 한 바퀴를 다 채웠을 때 영원히 서지 않는다. 보행자 2.5 / 차량 9 권장.")]
        [SerializeField] private float _minSpacing = 2.5f;

        [Header("Respawn (열린 스플라인만)")]
        [Tooltip("시작점에서 개체를 꺼내는 시간 간격(초)의 최소값. 꺼낼 때마다 최소~최대 사이에서 간격을 새로 뽑는다. 씬 시작 후 처음 채울 때도 이 간격을 쓴다. 시작점 반경(최소 간격의 절반) 안에 프리팹의 감지 레이어 대상이 있으면 꺼내지 않는다.")]
        [SerializeField] private float _minSpawnInterval = 1.5f;
        [Tooltip("시작점에서 개체를 꺼내는 시간 간격(초)의 최대값.")]
        [SerializeField] private float _maxSpawnInterval = 3.5f;
        [Tooltip("씬 시작 시 0번 스플라인 위에 미리 깔아 둘 개체 비율(개수 대비). 나머지는 시작점에서 차례대로 꺼낸다. 0 이면 미리 깔지 않는다.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float _initialFillRatio = 0.3f;
        [Tooltip("플레이어가 레인(모든 스플라인)의 어느 점에서도 이 거리(m)보다 멀면 개체를 새로 꺼내지 않는다. 이미 나온 개체는 끝까지 걸어가 반납되므로 먼 레인은 서서히 비워진다. 0 이하이거나 플레이어가 없으면 거리와 상관없이 꺼낸다.")]
        [SerializeField] private float _playerSpawnDistance = 25.0f;

        [Header("Speed")]
        [SerializeField] private string _speedAttributeKey = "MoveSpeed";
        [Tooltip("프리팹 SOAttributeData 의 속도에 곱하는 배율. 1 이면 SO 값 그대로. 0 이하이면 개체가 움직이지 않는다.")]
        [SerializeField] private float _speedMultiplier = 1.0f;
        #endregion

        #region Private Fields
        private bool _isReady;
        private int _activeCount;
        private int _spawnCount;
        private float _laneLength;
        private bool _isLaneClosed;
        private float _spawnTimer;
        private float _nextSpawnInterval;
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

            if (!IsPrefabListValid()) return;
            if (!IsLaneValid(out _laneLength)) return;

            _spawnCount = CalculateSpawnCount(_laneLength);
            if (_spawnCount <= 0) return;

            _isLaneClosed = _lane.Splines[0].Closed;

            // 개체가 멈추는 대상과 시작점을 막는 대상은 같으므로 목록 프리팹들의 감지 레이어를 합쳐 쓴다
            foreach (GameObject prefab in _prefabs)
            {
                _spawnBlockLayers |= prefab.GetComponent<CrowdController>().DetectLayers;
            }

            // ObjectPoolManager.Instance 는 Awake 에서 채워지므로 Start 에서 확인한다
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError($"[{name}] 씬에 ObjectPoolManager 가 없어 군중을 생성할 수 없습니다.", this);
                return;
            }

            _isReady = true;

            // 열린 레인은 일부만 미리 깔고 나머지는 Update 가 시작점에서 차례대로 꺼낸다. 첫 간격이 0 이라 첫 개체는 바로 나온다.
            if (!_isLaneClosed)
            {
                FillInitialMembers();
                return;
            }

            float spacing = _laneLength / _spawnCount;
            for (int i = 0; i < _spawnCount; i++)
            {
                SpawnMember(i * spacing);
            }
        }

        private void Update()
        {
            if (!_isReady || _isLaneClosed) return;

            _spawnTimer += Time.deltaTime;

            bool isSpawnDue = (_activeCount < _spawnCount) && (_spawnTimer >= _nextSpawnInterval);
            if (!isSpawnDue) return;

            // 플레이어에게서 먼 레인은 꺼내지 않는다. 거리 계산은 간격마다 한 번만 하려고 타이머를 되돌린다
            if (!IsPlayerNearLane())
            {
                _spawnTimer = 0.0f;
                return;
            }

            if (!IsSpawnPointClear()) return;

            _spawnTimer = 0.0f;
            _nextSpawnInterval = Random.Range(_minSpawnInterval, _maxSpawnInterval);
            SpawnMember(0.0f);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 생성할 프리팹 목록이 비어 있지 않고, 모든 프리팹 루트에 필요한 컴포넌트가 있는지 확인한다.
        /// </summary>
        /// <returns>생성에 쓸 수 있으면 true</returns>
        private bool IsPrefabListValid()
        {
            if ((_prefabs == null) || (_prefabs.Count == 0))
            {
                Debug.LogError($"[{name}] 생성할 프리팹이 지정되지 않았습니다.", this);
                return false;
            }

            for (int i = 0; i < _prefabs.Count; i++)
            {
                GameObject prefab = _prefabs[i];
                if (!prefab)
                {
                    Debug.LogError($"[{name}] 프리팹 목록 {i}번 칸이 비어 있습니다.", this);
                    return false;
                }

                if (!prefab.TryGetComponent(out CrowdController _) ||
                    !prefab.TryGetComponent(out CharacterMediator _))
                {
                    Debug.LogError($"[{name}] 프리팹 루트에 CrowdController 와 CharacterMediator 가 있어야 합니다.", prefab);
                    return false;
                }
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
        /// 플레이어가 레인의 어느 스플라인에서든 <see cref="_playerSpawnDistance"/> 이내에 있는지 확인한다.
        /// 기능을 껐거나 플레이어를 찾지 못하면 true 를 돌려줘 평소처럼 꺼내게 한다.
        /// </summary>
        /// <returns>개체를 꺼내도 되면 true</returns>
        private bool IsPlayerNearLane()
        {
            if (_playerSpawnDistance <= 0.0f) return true;

            GameManager gameManager = GameManager.Instance;
            if (!gameManager) return true;

            PlayerFacade player = gameManager.CurrentPlayerFacade;
            if (!player) return true;

            // 스플라인은 컨테이너 로컬 좌표이므로 플레이어 위치를 로컬로 바꿔 비교한다
            Vector3 localPlayerPosition = _lane.transform.InverseTransformPoint(player.transform.position);
            for (int i = 0; i < _lane.Splines.Count; i++)
            {
                float distance = SplineUtility.GetNearestPoint(_lane.Splines[i], localPlayerPosition, out _, out _);
                if (distance <= _playerSpawnDistance) return true;
            }

            return false;
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
        /// 열린 레인의 0번 스플라인을 균등한 칸으로 나누고, 칸마다 위치를 조금씩 흔들어 개체를 미리 깐다.
        /// 칸 길이가 최소 간격 이상이고 흔들림을 (칸 - 최소 간격)의 절반으로 제한하므로 이웃 개체는 최소 간격 이상 떨어진다.
        /// </summary>
        private void FillInitialMembers()
        {
            int count = Mathf.RoundToInt(_spawnCount * _initialFillRatio);
            if (count <= 0) return;

            float slot = _laneLength / count;
            float jitter = Mathf.Max(0.0f, slot - _minSpacing) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                SpawnMember((i + 0.5f) * slot + Random.Range(-jitter, jitter));
            }
        }

        /// <summary>
        /// 끝에 닿은 개체를 풀에 반납한다.
        /// </summary>
        /// <param name="member">반납할 개체의 조종부</param>
        private void ReleaseMember(CrowdController member)
        {
            // 재사용될 때 다른 레인이 다시 구독하므로 반납 전에 해제한다
            member.OnLaneEndReached -= ReleaseMember;
            _activeCount--;

            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.Release(member.gameObject);
            else
                Destroy(member.gameObject);
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
        /// 프리팹 목록에서 하나를 무작위로 골라 풀에서 꺼내 진행 거리 지점에 놓고 레인을 지정한다.
        /// </summary>
        /// <param name="distance">스플라인 시작점으로부터의 진행 거리(m)</param>
        private void SpawnMember(float distance)
        {
            GameObject prefab = _prefabs[Random.Range(0, _prefabs.Count)];
            float lateralOffsetRange = prefab.GetComponent<CrowdController>().LateralOffsetRange;

            float normalized = distance / _laneLength;
            Vector3 position = _lane.EvaluatePosition(normalized);

            // Linear Knot 은 그 지점의 접선이 0 이라 EvaluateTangent 대신 바로 앞 점과의 위치 차이로 방향을 구한다
            Vector3 ahead = _lane.EvaluatePosition((distance + TANGENT_SAMPLE_DISTANCE) / _laneLength);
            Vector3 tangent = Vector3.ProjectOnPlane(ahead - position, Vector3.up);

            Quaternion rotation = tangent.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE
                ? Quaternion.LookRotation(tangent.normalized, Vector3.up)
                : Quaternion.identity;

            // 개체마다 옆 오프셋을 뽑아 스폰 위치에도 같이 적용한다. 조종부가 반납될 때까지 이 오프셋을 유지한다.
            float lateralOffset = Random.Range(-lateralOffsetRange, lateralOffsetRange);
            if (tangent.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
                position += Vector3.Cross(Vector3.up, tangent.normalized) * lateralOffset;

            // 매니저가 새 위치로 옮긴 뒤 켜서 돌려준다
            GameObject obj = ObjectPoolManager.Instance.Get(prefab, position, rotation);
            if (obj == null) return;

            CrowdController member = obj.GetComponent<CrowdController>();
            member.ApplySpeed(_speedAttributeKey, _speedMultiplier);

            member.OnLaneEndReached += ReleaseMember;
            _activeCount++;

            member.SetLane(_lane, distance, lateralOffset);
        }
        #endregion
    }
}
