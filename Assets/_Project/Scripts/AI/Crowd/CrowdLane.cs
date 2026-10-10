using UnityEngine;
using UnityEngine.Splines;
using Mediator;

namespace AI.Crowd
{
    /// <summary>
    /// 닫힌 스플라인 하나를 군중 레인으로 쓰고, 씬 시작 시 개체를 일정 간격으로 생성해 순환시킨다.
    /// 개체의 속도는 생성 직후 이 컴포넌트가 CharacterMediator 의 속도 어트리뷰트에 넣는 값이 전부다.
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

        [Header("Speed")]
        [SerializeField] private string _speedAttributeKey = "MoveSpeed";
        [Tooltip("개체에 넣는 속도 값. 0 이하이면 개체가 움직이지 않는다.")]
        [SerializeField] private float _speedValue = 1.5f;
        #endregion

        #region Unity Lifecycle
        private void Reset()
        {
            _lane = GetComponent<SplineContainer>();
        }

        private void Start()
        {
            if (!_lane) _lane = GetComponent<SplineContainer>();

            if (!_prefab)
            {
                Debug.LogError($"[{name}] 생성할 프리팹이 지정되지 않았습니다.", this);
                return;
            }

            if (!IsLaneValid(out float laneLength)) return;

            int spawnCount = CalculateSpawnCount(laneLength);
            if (spawnCount <= 0) return;

            float spacing = laneLength / spawnCount;
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnMember(i * spacing, laneLength);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 레인의 스플라인이 쓸 수 있는지 확인하고 길이를 반환한다.
        /// </summary>
        /// <param name="laneLength">스플라인 길이(m)</param>
        /// <returns>생성에 쓸 수 있으면 true</returns>
        private bool IsLaneValid(out float laneLength)
        {
            laneLength = 0.0f;

            if (!_lane)
            {
                Debug.LogError($"[{name}] SplineContainer 가 없습니다.", this);
                return false;
            }

            laneLength = _lane.CalculateLength();
            if (laneLength <= Mathf.Epsilon)
            {
                Debug.LogError($"[{name}] 스플라인 길이가 0 입니다.", this);
                return false;
            }

            bool isClosed = _lane.Splines.Count > 0 && _lane.Splines[0].Closed;
            if (!isClosed)
            {
                Debug.LogWarning($"[{name}] 스플라인이 닫혀 있지 않습니다. 끝에 닿으면 시작점으로 순간 이동합니다.", this);
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
        /// 진행 거리 지점에 개체 하나를 생성하고 레인과 속도를 지정한다.
        /// </summary>
        /// <param name="distance">스플라인 시작점으로부터의 진행 거리(m)</param>
        /// <param name="laneLength">스플라인 길이(m)</param>
        private void SpawnMember(float distance, float laneLength)
        {
            float normalized = distance / laneLength;
            Vector3 position = _lane.EvaluatePosition(normalized);
            Vector3 tangent = Vector3.ProjectOnPlane(_lane.EvaluateTangent(normalized), Vector3.up);

            Quaternion rotation = tangent.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE
                ? Quaternion.LookRotation(tangent.normalized, Vector3.up)
                : Quaternion.identity;

            GameObject member = Instantiate(_prefab, position, rotation, transform);

            if (!member.TryGetComponent(out CrowdController controller) ||
                !member.TryGetComponent(out CharacterMediator mediator))
            {
                Debug.LogError(
                    $"[{name}] 프리팹 루트에 CrowdController 와 CharacterMediator 가 있어야 합니다. 이 개체는 건너뜁니다.",
                    _prefab);
                Destroy(member);
                return;
            }

            controller.SetLane(_lane, distance);

            // 잘못된 키는 AttributeSet 이 Debug.Log 만 찍어 묻히므로 먼저 검사한다
            if (!mediator.IsValidAttribute(_speedAttributeKey))
            {
                Debug.LogWarning(
                    $"[{name}] 어트리뷰트 '{_speedAttributeKey}' 가 개체에 없습니다. 속도가 적용되지 않습니다.",
                    _prefab);
                return;
            }

            mediator.SetAttribute(_speedAttributeKey, _speedValue);
        }
        #endregion
    }
}
