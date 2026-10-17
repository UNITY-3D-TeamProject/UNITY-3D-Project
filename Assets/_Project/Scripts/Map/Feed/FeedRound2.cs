using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Attribute.Effect;
using Core;
using Core.Stage;
using Facade.Player;
using Map.Gimmicks;
using Map.Platforms;

namespace Map.Feed
{
    /// <summary>2라운드의 해시태그 벽 하나의 설정. 목록의 순서가 FeedPost 의 Tag Index 와 짝을 이룬다.</summary>
    [Serializable]
    public struct SFeedWallConfig
    {
        [Tooltip("벽에 적을 글자")]
        public string Label;

        [Tooltip("벽의 색")]
        public Color Color;
    }

    /// <summary>
    /// 피드 맵 2라운드. Begin 을 부르면 씬의 주위 피드(FeedPost)를 모두 하늘로 올려 홀로그램으로 바꾸고,
    /// 이 오브젝트를 중심으로 해시태그 벽(정다각형)과 고정 발판을 만들고, 바로 아래의 라운드 발판을 줄인다.
    /// 1라운드 맵(이 오브젝트의 형제)은 지우고, 씬의 낙하 구역은 2라운드 발판 아래로 올린다.
    /// 피드는 총에 맞으면 플레이어에게서 멀어지는 방향(수평)으로 날아가 벽에 닿는다. 자기 태그의 벽이면 올라가고, 아니면 처음 자리로 돌아온다.
    /// 라운드 발판 윗면의 중앙에 놓는다(생성기의 Goal Prefab).
    /// </summary>
    public class FeedRound2 : MonoBehaviour
    {
        #region Constants
        // 피드가 떠 있을 자리를 다른 피드와 겹치지 않게 다시 뽑아 보는 횟수
        private const int SLOT_RETRY_COUNT = 20;

        // 한 프레임에 만드는 발판 수. 한꺼번에 만들면 시작 순간 화면이 끊긴다.
        private const int PLATFORMS_PER_FRAME = 30;
        #endregion

        #region Serialized Fields
        [Header("Walls")]
        [Tooltip("해시태그 벽 목록. 개수만큼의 변을 가진 정다각형으로 둘러싼다. 순서가 피드의 Tag Index 와 짝을 이룬다.")]
        [SerializeField] private SFeedWallConfig[] _walls;

        [Tooltip("벽 프리팹")]
        [SerializeField] private FeedTagWall _wallPrefab;

        [Tooltip("중심에서 벽까지의 거리")]
        [SerializeField, Min(1f)] private float _wallDistance = 80.0f;

        [Tooltip("벽의 높이")]
        [SerializeField, Min(1f)] private float _wallHeight = 50.0f;

        [Tooltip("벽 아랫변의 높이 (이 오브젝트 기준)")]
        [SerializeField] private float _wallBottom = -16.0f;

        [Tooltip("첫 번째 벽이 놓이는 방향 (도, 반시계 방향)")]
        [SerializeField] private float _wallStartAngle;

        [Header("Platforms")]
        [Tooltip("흩뿌릴 고정 발판 프리팹 목록. 자리마다 하나를 무작위로 고른다.")]
        [SerializeField] private GameObject[] _platformPrefabs;

        [Tooltip("가운데 라운드 발판의 원래 반지름")]
        [SerializeField, Min(0.1f)] private float _centerRadius = 27.0f;

        [Tooltip("시작하면 가운데 라운드 발판이 줄어들어 이 반지름이 된다. 이 바깥부터 발판을 놓는다.")]
        [SerializeField, Min(0.1f)] private float _centerShrunkRadius = 8.0f;

        [Tooltip("발판을 놓는 자리의 간격 (발판 중심 사이의 거리)")]
        [SerializeField, Min(1f)] private float _platformSpacing = 7.0f;

        [Tooltip("자리에서 무작위로 어긋나는 최대 거리")]
        [SerializeField, Min(0f)] private float _platformJitter = 0.3f;

        [Tooltip("자리마다 발판이 놓일 확률")]
        [SerializeField, Range(0f, 1f)] private float _platformDensity = 1.0f;

        [Tooltip("건너뛸 수 있다고 보는 발판 중심 사이의 최대 거리. 가운데 발판에서 이 거리로 이어지지 않는 발판은 만들지 않는다.")]
        [SerializeField, Min(1f)] private float _maxStepDistance = 8.4f;

        [Tooltip("발판 윗면 높이의 범위 (이 오브젝트 기준, 최소 ~ 최대)")]
        [SerializeField] private Vector2 _platformHeightRange = new Vector2(-4.0f, 10.0f);

        [Tooltip("이어진 발판 사이의 최대 높이 차. 점프 높이보다 작아야 한다.")]
        [SerializeField, Min(0f)] private float _maxStepHeight = 1.0f;

        [Tooltip("발판이 앞 발판의 오르내리는 방향을 그대로 이어갈 확률. 높을수록 긴 오르막·내리막이 생긴다.")]
        [SerializeField, Range(0f, 1f)] private float _slopeKeepChance = 0.75f;

        [Tooltip("벽에서 발판을 띄우는 거리")]
        [SerializeField, Min(0f)] private float _wallMargin = 4.0f;

        [Header("Feeds")]
        [Tooltip("홀로그램 피드가 떠 있는 자리의 범위: 중심에서의 거리 (최소 ~ 최대)")]
        [SerializeField] private Vector2 _feedDistanceRange = new Vector2(10.0f, 65.0f);

        [Tooltip("홀로그램 피드가 떠 있는 높이의 범위 (이 오브젝트 기준, 최소 ~ 최대)")]
        [SerializeField] private Vector2 _feedHeightRange = new Vector2(2.0f, 12.0f);

        [Tooltip("피드와 피드 사이에 두는 최소 거리")]
        [SerializeField, Min(0f)] private float _feedMinSpacing = 5.0f;

        [Tooltip("피드가 제자리까지 떠오르는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.1f)] private float _riseDuration = 4.0f;

        [Tooltip("총에 맞은 피드가 벽으로 날아가는 속도")]
        [SerializeField, Min(0.1f)] private float _flySpeed = 30.0f;

        [Tooltip("틀린 벽에 닿은 피드가 제자리로 돌아오는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.1f)] private float _returnDuration = 1.5f;

        [Tooltip("틀린 벽에 보냈을 때 플레이어에게 위에서부터 순서대로 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect[] _wrongWallEffects;

        [Header("Round 1")]
        [Tooltip("시작하면 씬의 낙하 구역(FallZoneTrigger)을 옮길 높이 (이 오브젝트 기준)")]
        [SerializeField] private float _fallZoneHeight = -10.0f;

        [Header("Event")]
        [Tooltip("2라운드가 시작될 때 실행할 동작")]
        [SerializeField] private UnityEvent _onStarted = new UnityEvent();

        [Tooltip("피드를 모두 올려 보냈을 때 실행할 동작")]
        [SerializeField] private UnityEvent _onCleared = new UnityEvent();
        #endregion

        #region Private Fields
        // 피드마다 떠 있는 자리. 올라간 피드는 뺀다.
        private readonly Dictionary<FeedPost, Vector3> _slots = new Dictionary<FeedPost, Vector3>();
        #endregion

        #region Properties
        /// <summary>2라운드가 시작됐는지 여부.</summary>
        public bool IsStarted { get; private set; }

        /// <summary>피드를 모두 올려 보냈는지 여부.</summary>
        public bool IsCleared { get; private set; }

        /// <summary>아직 올려 보내지 않은 피드 수.</summary>
        public int RemainingCount => _slots.Count;

        /// <summary>2라운드가 시작될 때 실행되는 이벤트.</summary>
        public UnityEvent OnStarted => _onStarted;

        /// <summary>피드를 모두 올려 보냈을 때 실행되는 이벤트.</summary>
        public UnityEvent OnCleared => _onCleared;
        #endregion

        #region Unity Lifecycle
        private void OnDestroy()
        {
            foreach (FeedPost post in _slots.Keys)
            {
                if (post != null)
                {
                    post.OnShot -= HandlePostShot;
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 2라운드를 시작한다. 두 번째 호출부터는 무시한다.
        /// </summary>
        public void Begin()
        {
            if (IsStarted) return;

            if ((_walls == null) || (_walls.Length < 3))
            {
                Debug.LogError($"[{name}] 벽(Walls)이 3개 이상 있어야 합니다.", this);
                return;
            }

            IsStarted = true;

            BuildWalls();
            BuildPlatforms();
            ShrinkCenter();
            RaiseFeeds();
            ClearRound1();
            RaiseFallZones();

            _onStarted.Invoke();

            if (_slots.Count == 0)
            {
                Debug.LogWarning($"[{name}] 씬에 주위 피드(FeedPost)가 하나도 없어 바로 클리어 처리합니다.", this);
                Clear();
            }
        }
        #endregion

        #region Private Methods
        // index 번째 벽이 놓인 방향 (중심에서 바깥쪽, 수평)
        private Vector3 GetWallDirection(int index)
        {
            float angle = (_wallStartAngle + ((360.0f / _walls.Length) * index)) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
        }

        private void BuildWalls()
        {
            if (_wallPrefab == null)
            {
                Debug.LogError($"[{name}] 벽 프리팹(Wall Prefab)이 연결되지 않았습니다.", this);
                return;
            }

            // 정다각형의 한 변의 길이
            float width = 2.0f * _wallDistance * Mathf.Tan(Mathf.PI / _walls.Length);

            for (int i = 0; i < _walls.Length; i++)
            {
                Vector3 direction = GetWallDirection(i);
                Vector3 position = transform.position + (direction * _wallDistance);

                FeedTagWall wall = Instantiate(_wallPrefab, position, Quaternion.LookRotation(-direction), transform);
                wall.name = $"TagWall_{i:00}_{_walls[i].Label}";
                wall.Setup(width, _wallHeight, _wallBottom, _walls[i].Label, _walls[i].Color);
            }
        }

        // 줄어든 가운데 발판 바깥부터 벽 안쪽 전체(모서리 포함)에, 동심원 위의 자리마다 확률로 발판을 놓는다.
        // 가운데 발판에서 걸어서(뛰어서) 닿을 수 있는 것만, 가까운 것부터 여러 프레임에 나눠 만든다.
        private void BuildPlatforms()
        {
            if ((_platformPrefabs == null) || (_platformPrefabs.Length == 0)) return;

            // 다각형의 꼭짓점까지 훑고, 벽에서 Wall Margin 안쪽에 드는 자리만 쓴다
            float cornerRadius = _wallDistance / Mathf.Cos(Mathf.PI / _walls.Length);
            var points = new List<Vector3>();

            for (float radius = _centerShrunkRadius + (_platformSpacing * 0.5f); radius <= cornerRadius; radius += _platformSpacing)
            {
                int count = Mathf.Max(1, Mathf.FloorToInt((Mathf.PI * 2.0f * radius) / _platformSpacing));
                float ringOffset = UnityEngine.Random.value * Mathf.PI * 2.0f;

                for (int i = 0; i < count; i++)
                {
                    if (UnityEngine.Random.value > _platformDensity) continue;

                    float angle = ringOffset + ((Mathf.PI * 2.0f * i) / count);
                    Vector2 jitter = UnityEngine.Random.insideUnitCircle * _platformJitter;

                    // 높이는 아래 LinkPlatforms 에서 정한다
                    var point = new Vector3(
                        (Mathf.Cos(angle) * radius) + jitter.x,
                        0.0f,
                        (Mathf.Sin(angle) * radius) + jitter.y);

                    if (IsInsideWalls(point))
                    {
                        points.Add(point);
                    }
                }
            }

            Transform root = new GameObject("Platforms").transform;
            root.SetParent(transform, false);

            StartCoroutine(CoSpawnPlatforms(root, LinkPlatforms(points)));
        }

        // point(이 오브젝트 기준, 수평)가 모든 벽에서 Wall Margin 이상 안쪽에 있는지 여부
        private bool IsInsideWalls(Vector3 point)
        {
            for (int i = 0; i < _walls.Length; i++)
            {
                if (Vector3.Dot(point, GetWallDirection(i)) > (_wallDistance - _wallMargin)) return false;
            }

            return true;
        }

        private void SpawnPlatform(Transform root, Vector3 point)
        {
            GameObject prefab = _platformPrefabs[UnityEngine.Random.Range(0, _platformPrefabs.Length)];
            if (prefab == null) return;

            // 발판이 중심을 바라보게 놓는다
            Vector3 outward = new Vector3(point.x, 0.0f, point.z).normalized;
            GameObject platform = Instantiate(prefab, transform.position + point, Quaternion.LookRotation(-outward), root);

            // 기본 발판의 겉모습으로 맞춘다. 하지 않으면 다른 역할용 장식(유리 속심 등)이 몸체와 겹쳐 깜빡인다.
            if (platform.TryGetComponent(out PlatformSkin skin))
            {
                skin.SetRole(EPlatformRole.Basic);
            }

            // 프리팹마다 두께가 달라도 윗면이 정한 높이에 오도록 맞춘다
            Renderer platformRenderer = platform.GetComponentInChildren<Renderer>();
            if (platformRenderer != null)
            {
                float targetTop = transform.position.y + point.y;
                platform.transform.position += Vector3.up * (targetTop - platformRenderer.bounds.max.y);
            }
        }

        // 가운데 발판에서 Max Step Distance 이내로 차례차례 이어지는 자리만 남기고, 이어지는 순서대로 높이를 정한다.
        // 높이는 앞 발판보다 Max Step Height 이내로만 오르내리므로, 가운데에서 그 발판까지 올라가는 길이 반드시 하나는 있다.
        private List<Vector3> LinkPlatforms(List<Vector3> points)
        {
            var linked = new List<Vector3>();
            var isVisited = new bool[points.Count];
            var slopes = new float[points.Count];
            var queue = new Queue<int>();

            // 줄어든 가운데 발판의 가장자리에서 바로 건너갈 수 있는 자리에서 시작한다
            for (int i = 0; i < points.Count; i++)
            {
                float distance = new Vector2(points[i].x, points[i].z).magnitude;
                if ((distance - _centerShrunkRadius) > _platformSpacing) continue;

                isVisited[i] = true;
                slopes[i] = (UnityEngine.Random.value < 0.5f) ? 1.0f : -1.0f;
                queue.Enqueue(i);
            }

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                linked.Add(points[current]);

                for (int i = 0; i < points.Count; i++)
                {
                    if (isVisited[i]) continue;

                    Vector3 offset = points[i] - points[current];
                    offset.y = 0.0f;
                    if (offset.magnitude > _maxStepDistance) continue;

                    // 앞 발판의 오르내리는 방향을 이어가거나 뒤집는다. 높이 범위를 벗어나면 반대로 꺾는다.
                    float slope = (UnityEngine.Random.value < _slopeKeepChance) ? slopes[current] : -slopes[current];
                    float step = Mathf.Lerp(0.4f, 1.0f, UnityEngine.Random.value) * _maxStepHeight;
                    float height = points[current].y + (slope * step);

                    if ((height < _platformHeightRange.x) || (height > _platformHeightRange.y))
                    {
                        slope = -slope;
                        height = points[current].y + (slope * step);
                    }

                    Vector3 point = points[i];
                    point.y = height;
                    points[i] = point;

                    isVisited[i] = true;
                    slopes[i] = slope;
                    queue.Enqueue(i);
                }
            }

            return linked;
        }

        // 바로 아래의 라운드 발판을 피드가 떠오르는 동안 줄이고, 그 위에 있던 체크포인트는 줄어든 발판 안으로 옮긴다.
        private void ShrinkCenter()
        {
            Transform centerPlatform = FindCenterPlatform();
            if (centerPlatform == null)
            {
                Debug.LogWarning($"[{name}] 바로 아래에서 라운드 발판을 찾지 못해 줄이지 않습니다.", this);
                return;
            }

            foreach (CheckpointTrigger checkpoint in FindObjectsByType<CheckpointTrigger>())
            {
                Vector3 offset = checkpoint.transform.position - transform.position;
                float height = offset.y;
                offset.y = 0.0f;

                // 줄어들면 허공에 남게 되는, 이 발판 위의 체크포인트만 옮긴다
                bool isOnPlatform = (Mathf.Abs(height) < 3.0f) && (offset.magnitude <= _centerRadius);
                if (!isOnPlatform || (offset.magnitude <= (_centerShrunkRadius * 0.6f))) continue;

                Vector3 position = transform.position + (offset.normalized * (_centerShrunkRadius * 0.6f));
                position.y = checkpoint.transform.position.y;
                checkpoint.transform.position = position;
            }

            StartCoroutine(CoShrinkCenter(centerPlatform));
        }

        // 이 오브젝트 바로 아래에 있는 콜라이더(자기 자식은 제외)의 Transform. 없으면 null.
        private Transform FindCenterPlatform()
        {
            RaycastHit[] hits = Physics.RaycastAll(
                transform.position + (Vector3.up * 0.5f),
                Vector3.down,
                2.0f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (!hit.transform.IsChildOf(transform)) return hit.transform;
            }

            return null;
        }

        // 씬의 주위 피드를 모두 홀로그램으로 바꾸고, 경기장 위의 무작위 자리로 떠오르게 한다.
        private void RaiseFeeds()
        {
            var positions = new List<Vector3>();

            foreach (FeedPost post in FindObjectsByType<FeedPost>())
            {
                if (post.TagIndex >= _walls.Length)
                {
                    Debug.LogWarning($"[{name}] {post.name} 의 Tag Index({post.TagIndex})에 맞는 벽이 없습니다.", post);
                }

                Vector3 slot = PickSlot(positions);
                positions.Add(slot);
                _slots[post] = slot;

                // 다 떠오른 뒤부터 쏠 수 있다
                FeedPost target = post;
                post.OnShot += HandlePostShot;
                post.BecomeHologram();
                post.MoveTo(slot, _riseDuration, () => target.SetShootable(true));
            }
        }

        // 1라운드 맵에서 만든 것(이 오브젝트의 형제)을 지운다. 이 오브젝트와 바로 아래의 라운드 발판은 남긴다.
        // 주위 피드는 1라운드 맵 아래에 있지만 2라운드에서 쓰므로, 먼저 이 오브젝트 아래로 옮긴다.
        private void ClearRound1()
        {
            Transform mapRoot = transform.parent;
            if (mapRoot == null) return;

            foreach (FeedPost post in _slots.Keys)
            {
                post.transform.SetParent(transform, true);
            }

            Transform centerPlatform = FindCenterPlatform();

            foreach (Transform sibling in mapRoot)
            {
                if (sibling == transform) continue;
                if ((centerPlatform != null) && centerPlatform.IsChildOf(sibling)) continue;

                Destroy(sibling.gameObject);
            }
        }

        // 씬의 낙하 구역을 2라운드 발판 아래로 올린다.
        private void RaiseFallZones()
        {
            foreach (FallZoneTrigger fallZone in FindObjectsByType<FallZoneTrigger>())
            {
                Vector3 position = fallZone.transform.position;
                position.y = transform.position.y + _fallZoneHeight;
                fallZone.transform.position = position;
            }
        }

        // 다른 피드와 Feed Min Spacing 이상 떨어진 자리를 고른다. 몇 번 뽑아도 없으면 마지막으로 뽑은 자리를 쓴다.
        private Vector3 PickSlot(List<Vector3> takenPositions)
        {
            Vector3 slot = transform.position;

            for (int attempt = 0; attempt < SLOT_RETRY_COUNT; attempt++)
            {
                float angle = UnityEngine.Random.value * Mathf.PI * 2.0f;
                float distance = Mathf.Lerp(_feedDistanceRange.x, _feedDistanceRange.y, UnityEngine.Random.value);
                float height = Mathf.Lerp(_feedHeightRange.x, _feedHeightRange.y, UnityEngine.Random.value);

                slot = transform.position + new Vector3(Mathf.Cos(angle) * distance, height, Mathf.Sin(angle) * distance);

                bool isFarEnough = true;
                foreach (Vector3 taken in takenPositions)
                {
                    if (Vector3.Distance(taken, slot) < _feedMinSpacing)
                    {
                        isFarEnough = false;
                        break;
                    }
                }

                if (isFarEnough) break;
            }

            return slot;
        }

        // 맞은 피드를 플레이어에게서 멀어지는 방향(수평)으로 날려 보낸다.
        private void HandlePostShot(FeedPost post)
        {
            if (!_slots.ContainsKey(post)) return;

            Vector3 direction = GetPushDirection(post.transform.position);
            int wallIndex = FindWall(post.transform.position, direction, out Vector3 hitPoint);
            float duration = Vector3.Distance(post.transform.position, hitPoint) / _flySpeed;

            post.SetShootable(false);
            post.MoveTo(hitPoint, duration, () => ResolveArrival(post, wallIndex));
        }

        // 플레이어의 몸 중심에서 피드를 향하는 수평 방향. 플레이어가 없거나 바로 아래에 있으면 중심에서 바깥쪽으로 민다.
        private Vector3 GetPushDirection(Vector3 postPosition)
        {
            Vector3 origin = transform.position;

            GameManager gameManager = GameManager.Instance;
            PlayerFacade playerFacade = (gameManager != null) ? gameManager.CurrentPlayerFacade : null;
            if (playerFacade != null)
            {
                // 실제로 움직이는 몸체는 플레이어 루트가 아니라 자식의 CharacterController 다
                CharacterController playerController = playerFacade.transform.root.GetComponentInChildren<CharacterController>();
                if (playerController != null)
                {
                    origin = playerController.bounds.center;
                }
            }

            Vector3 direction = postPosition - origin;
            direction.y = 0.0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = postPosition - transform.position;
                direction.y = 0.0f;
            }

            return (direction.sqrMagnitude < 0.0001f) ? GetWallDirection(0) : direction.normalized;
        }

        // position 에서 direction(수평) 으로 나아갈 때 처음 닿는 벽의 순서와 닿는 지점을 구한다.
        private int FindWall(Vector3 position, Vector3 direction, out Vector3 hitPoint)
        {
            Vector3 offset = position - transform.position;
            int nearestIndex = 0;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < _walls.Length; i++)
            {
                Vector3 wallDirection = GetWallDirection(i);
                float approach = Vector3.Dot(direction, wallDirection);

                // 이 벽에서 멀어지는 방향이면 닿지 않는다
                if (approach <= 0.0001f) continue;

                float distance = (_wallDistance - Vector3.Dot(offset, wallDirection)) / approach;
                if (distance >= nearestDistance) continue;

                nearestDistance = distance;
                nearestIndex = i;
            }

            hitPoint = position + (direction * Mathf.Max(nearestDistance, 0.0f));
            return nearestIndex;
        }

        // 벽에 닿은 피드를 판정한다. 자기 태그의 벽이면 올려 보내고, 아니면 플레이어에게 벌칙을 주고 제자리로 돌려보낸다.
        private void ResolveArrival(FeedPost post, int wallIndex)
        {
            if (post.TagIndex == wallIndex)
            {
                post.OnShot -= HandlePostShot;
                _slots.Remove(post);
                Destroy(post.gameObject);

                if (_slots.Count == 0)
                {
                    Clear();
                }

                return;
            }

            GameManager gameManager = GameManager.Instance;
            PlayerFacade playerFacade = (gameManager != null) ? gameManager.CurrentPlayerFacade : null;
            if (playerFacade != null)
            {
                GimmickUtility.ApplyEffects(_wrongWallEffects, playerFacade.EffectTarget);
            }

            post.MoveTo(_slots[post], _returnDuration, () => post.SetShootable(true));
        }

        private void Clear()
        {
            if (IsCleared) return;

            IsCleared = true;
            _onCleared.Invoke();
        }
        #endregion

        #region Coroutines
        // points 의 순서(가운데에서 가까운 것부터)대로 발판을 여러 프레임에 나눠 만든다.
        private IEnumerator CoSpawnPlatforms(Transform root, List<Vector3> points)
        {
            for (int i = 0; i < points.Count; i++)
            {
                SpawnPlatform(root, points[i]);

                if (((i + 1) % PLATFORMS_PER_FRAME) == 0)
                {
                    yield return null;
                }
            }
        }

        // 피드가 떠오르는 시간 동안 라운드 발판의 반지름을 Center Shrunk Radius 까지 줄인다. 두께는 그대로 둔다.
        private IEnumerator CoShrinkCenter(Transform centerPlatform)
        {
            float ratio = _centerShrunkRadius / _centerRadius;
            Vector3 startScale = centerPlatform.localScale;
            Vector3 endScale = new Vector3(startScale.x * ratio, startScale.y, startScale.z * ratio);

            for (float elapsed = 0.0f; elapsed < _riseDuration; elapsed += Time.deltaTime)
            {
                if (centerPlatform == null) yield break;

                centerPlatform.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0.0f, 1.0f, elapsed / _riseDuration));
                yield return null;
            }

            if (centerPlatform != null)
            {
                centerPlatform.localScale = endScale;
            }
        }
        #endregion
    }
}
