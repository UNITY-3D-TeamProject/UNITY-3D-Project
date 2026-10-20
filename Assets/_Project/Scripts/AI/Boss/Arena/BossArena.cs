using System;
using UnityEngine;
using AI.Boss.Logic;

namespace AI.Boss.Arena
{
    /// <summary>
    /// 5×5 보스 아레나. 순수 로직(ArenaGrid)의 상태 변화를 칸(BossTile)의 겉모습에 옮기고, 월드 좌표 질의를 칸 단위 좌표로 바꿔 준다.
    /// 오브젝트의 로컬 원점이 칸 (0,0) 의 바깥 모서리이고, 로컬 +X → col, 로컬 +Z → row 이다. 월드 스케일은 1 이어야 한다.
    /// </summary>
    public class BossArena : MonoBehaviour, IBossArena
    {
        #region Serialized Fields
        [Header("Tiles")]
        [Tooltip("칸 25개. 인덱스 = row × 5 + col")]
        [SerializeField] private BossTile[] _tiles;
        [Tooltip("칸 한 변의 길이(m)")]
        [SerializeField, Min(0.1f)] private float _tileSize = 20.0f;
        [Tooltip("무너진 칸이 복구되기까지의 시간(초)")]
        [SerializeField, Min(0.0f)] private float _restoreSeconds = 6.0f;

        [Header("Circle Telegraph")]
        [Tooltip("지름 1 m 짜리 원판 프리팹. 크기 조절로 반경을 맞춘다.")]
        [SerializeField] private GameObject _circlePrefab;
        [SerializeField, Min(0)] private int _circlePoolSize = 4;

        [Header("Line Telegraph")]
        [Tooltip("가로·세로 1 m 짜리 납작한 판 프리팹. 로컬 X 가 폭, Z 가 길이가 되도록 크기 조절로 맞춘다.")]
        [SerializeField] private GameObject _linePrefab;
        [SerializeField, Min(0)] private int _linePoolSize = 2;
        #endregion

        #region Private Fields
        private GameObject[] _circles;
        private Vector3 _circleBaseScale;
        private GameObject[] _lines;
        private Vector3 _lineBaseScale;
        private float _telegraphFillSeconds;
        private Color? _telegraphColor;
        #endregion

        #region Properties
        public ArenaGrid Grid { get; private set; }

        public float TileSize => _tileSize;
        #endregion

        #region Events
        public event Action<int> OnTileCollapsed;

        public event Action<int> OnTileRestored;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Grid = new ArenaGrid(_restoreSeconds);
            Grid.OnCollapsed += HandleCollapsed;
            Grid.OnRestored += HandleRestored;
            Grid.OnTelegraphChanged += HandleTelegraphChanged;

            if (!IsTilesValid()) return;

            foreach (BossTile tile in _tiles)
            {
                tile.SetCollapsed(false);
                tile.SetTelegraph(false);
            }

            CreateCirclePool();
            CreateLinePool();
        }

        private void Update()
        {
            Grid.Tick(Time.deltaTime);
        }

        private void OnValidate()
        {
            IsTilesValid();
        }
        #endregion

        #region Public Methods
        public bool TryGetTile(Vector3 world, out int index)
        {
            ToTileSpace(world, out float x, out float z);
            return Grid.TryGetTile(x, z, out index);
        }

        public bool IsHoleOrTelegraphed(int index)
        {
            return Grid.IsHoleOrTelegraphed(index);
        }

        public bool IsSafeAt(Vector3 world)
        {
            return TryGetTile(world, out int index) && Grid.IsSafe(index);
        }

        public Vector3 GetTileCenter(int index)
        {
            Grid.GetTileCenter(index, out float x, out float z);
            return transform.TransformPoint(new Vector3(x * _tileSize, 0.0f, z * _tileSize));
        }

        public int FindNearestSafe(Vector3 world)
        {
            ToTileSpace(world, out float x, out float z);
            return Grid.FindNearestSafe(x, z);
        }

        public bool HasHoleBetween(Vector3 a, Vector3 b)
        {
            ToTileSpace(a, out float ax, out float az);
            ToTileSpace(b, out float bx, out float bz);
            return Grid.HasHoleBetween(ax, az, bx, bz);
        }

        public void SetTelegraph(int index, bool isOn, float fillSeconds, Color? color = null)
        {
            // 칸 상태가 실제로 바뀔 때만 HandleTelegraphChanged 가 불려 채움이 시작된다
            _telegraphFillSeconds = fillSeconds;
            _telegraphColor = color;
            Grid.SetTelegraphed(index, isOn);
            _telegraphFillSeconds = 0.0f;
            _telegraphColor = null;
        }

        public void Collapse(int index)
        {
            Grid.Collapse(index);
        }

        public int ShowCircle(Vector3 center, float radius)
        {
            if (_circles == null) return -1;

            for (int i = 0; i < _circles.Length; i++)
            {
                if (_circles[i].activeSelf) continue;

                float diameter = radius * 2.0f;
                _circles[i].transform.SetPositionAndRotation(center, Quaternion.identity);
                _circles[i].transform.localScale = new Vector3(diameter, _circleBaseScale.y, diameter);
                _circles[i].SetActive(true);
                return i;
            }

            Debug.LogWarning($"[{name}] 빈 원형 예고 원판이 없습니다. _circlePoolSize 를 늘려 주세요.", this);
            return -1;
        }

        public void HideCircle(int handle)
        {
            if (_circles == null || handle < 0 || handle >= _circles.Length) return;

            _circles[handle].SetActive(false);
        }

        public int ShowLine(Vector3 from, Vector3 to, float width)
        {
            if (_lines == null) return -1;

            Vector3 delta = to - from;
            delta.y = 0.0f;
            float length = delta.magnitude;
            if (length < 0.001f) return -1;

            for (int i = 0; i < _lines.Length; i++)
            {
                if (_lines[i].activeSelf) continue;

                Vector3 center = from + (delta * 0.5f);
                _lines[i].transform.SetPositionAndRotation(center, Quaternion.LookRotation(delta / length, Vector3.up));
                _lines[i].transform.localScale = new Vector3(width, _lineBaseScale.y, length);
                _lines[i].SetActive(true);
                return i;
            }

            Debug.LogWarning($"[{name}] 빈 직선 예고 판이 없습니다. _linePoolSize 를 늘려 주세요.", this);
            return -1;
        }

        public void HideLine(int handle)
        {
            if (_lines == null || handle < 0 || handle >= _lines.Length) return;

            _lines[handle].SetActive(false);
        }

        public void ResetAll()
        {
            Grid.ResetAll();

            if (_circles != null)
            {
                foreach (GameObject circle in _circles)
                {
                    circle.SetActive(false);
                }
            }

            if (_lines == null) return;

            foreach (GameObject line in _lines)
            {
                line.SetActive(false);
            }
        }
        #endregion

        #region Private Methods
        private bool IsTilesValid()
        {
            bool isValid = _tiles != null && _tiles.Length == ArenaGrid.COUNT;
            if (isValid)
            {
                foreach (BossTile tile in _tiles)
                {
                    if (!tile) isValid = false;
                }
            }

            if (!isValid) Debug.LogError($"[{name}] _tiles 는 비어 있지 않은 BossTile {ArenaGrid.COUNT}개여야 합니다.", this);

            return isValid;
        }

        private void CreateCirclePool()
        {
            if (!_circlePrefab || _circlePoolSize <= 0) return;

            _circleBaseScale = _circlePrefab.transform.localScale;
            _circles = new GameObject[_circlePoolSize];
            for (int i = 0; i < _circlePoolSize; i++)
            {
                _circles[i] = Instantiate(_circlePrefab, transform);
                _circles[i].SetActive(false);
            }
        }

        private void CreateLinePool()
        {
            if (!_linePrefab || _linePoolSize <= 0) return;

            _lineBaseScale = _linePrefab.transform.localScale;
            _lines = new GameObject[_linePoolSize];
            for (int i = 0; i < _linePoolSize; i++)
            {
                _lines[i] = Instantiate(_linePrefab, transform);
                _lines[i].SetActive(false);
            }
        }

        /// <summary>월드 좌표를 아레나 로컬 칸 단위 좌표(x → col, z → row)로 바꾼다.</summary>
        private void ToTileSpace(Vector3 world, out float x, out float z)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            x = local.x / _tileSize;
            z = local.z / _tileSize;
        }

        private void HandleCollapsed(int index)
        {
            if (IsTileReady(index)) _tiles[index].SetCollapsed(true);

            OnTileCollapsed?.Invoke(index);
        }

        private void HandleRestored(int index)
        {
            if (IsTileReady(index)) _tiles[index].SetCollapsed(false);

            OnTileRestored?.Invoke(index);
        }

        private void HandleTelegraphChanged(int index, bool isOn)
        {
            if (IsTileReady(index)) _tiles[index].SetTelegraph(isOn, _telegraphFillSeconds, _telegraphColor);
        }

        private bool IsTileReady(int index)
        {
            return _tiles != null && index >= 0 && index < _tiles.Length && _tiles[index];
        }
        #endregion
    }
}
