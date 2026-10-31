using System;
using System.Collections.Generic;
using UnityEngine;
using DTT.AreaOfEffectRegions;
using AI.Boss.Logic;
using Boss.VFX;

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
        [Tooltip("최종 단계(A 단독)에서 무너진 칸이 복구되기까지의 시간(초)")]
        [SerializeField, Min(0.0f)] private float _restoreSecondsFinal = 3.0f;

        [Header("Circle Telegraph")]
        [Tooltip("지름 1 m 짜리 원판 프리팹. 크기 조절로 반경을 맞춘다. DTT CircleRegion 이 있으면 크기 조절 대신 Radius 를 넣는다.")]
        [SerializeField] private GameObject _circlePrefab;
        [SerializeField, Min(0)] private int _circlePoolSize = 4;

        [Header("Line Telegraph")]
        [Tooltip("가로·세로 1 m 짜리 납작한 판 프리팹. 로컬 X 가 폭, Z 가 길이가 되도록 크기 조절로 맞춘다. " +
                 "DTT LineRegion 이 있으면 크기 조절 대신 시작점에 놓고 Length·Width 를 넣는다.")]
        [SerializeField] private GameObject _linePrefab;
        [SerializeField, Min(0)] private int _linePoolSize = 2;

        [Header("Arrow Telegraph")]
        [Tooltip("화살표 예고 프리팹(WarningSignals_Arrows). 화살표 머리가 로컬 +X 를 향하고 Arrow_01~03 이 꼬리에서 머리 순으로 놓여 있어야 한다.")]
        [SerializeField] private GameObject _arrowPrefab;
        [Tooltip("프리팹에 곱할 크기. 칸 위 맵 장식(WarningSignals)과 같게 1.2")]
        [SerializeField, Min(0.1f)] private float _arrowScale = 1.2f;
        [Tooltip("프리팹 크기 1 일 때 화살표 한 토막의 진행 방향 길이(m). 그랩 직선에 이어 붙이는 간격이 된다")]
        [SerializeField, Min(0.1f)] private float _arrowStripLength = 7.6f;
        [Tooltip("임시값. 화살표가 하나씩 더 켜지는 간격(초)")]
        [SerializeField, Min(0.01f)] private float _arrowStepSeconds = 0.15f;
        #endregion

        #region Private Fields
        private sealed class ArrowGroup
        {
            public readonly List<BossArrowTelegraph> Arrows = new List<BossArrowTelegraph>();
            public readonly List<int> HiddenTiles = new List<int>();
        }

        private readonly Stack<BossArrowTelegraph> _arrowPool = new Stack<BossArrowTelegraph>();
        private readonly Dictionary<int, ArrowGroup> _arrowGroups = new Dictionary<int, ArrowGroup>();
        private readonly List<int> _arrowHandleBuffer = new List<int>();
        private int[] _tileArrowHandles;
        private int[] _signalHideCounts;
        private int _nextArrowHandle;
        private bool _isTileVisualSuppressed;
        private GameObject[] _circles;
        private CircleRegion[] _circleRegions;
        private AoeVfxPlayer[] _circlePlayers;
        private Vector3 _circleBaseScale;
        private GameObject[] _lines;
        private LineRegion[] _lineRegions;
        private AoeVfxPlayer[] _linePlayers;
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

            _tileArrowHandles = new int[ArenaGrid.COUNT];
            Array.Fill(_tileArrowHandles, -1);
            _signalHideCounts = new int[ArenaGrid.COUNT];

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

        public int FindClearLineTile(Vector3 from, Vector3 target)
        {
            ToTileSpace(from, out float fromX, out float fromZ);
            ToTileSpace(target, out float targetX, out float targetZ);
            return Grid.FindClearLineTile(fromX, fromZ, targetX, targetZ);
        }

        public void SetReservedTile(int index)
        {
            Grid.SetReservedTile(index);
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

        public void SetArrowTelegraph(int index, bool isOn, Vector3 direction)
        {
            if (index < 0 || index >= ArenaGrid.COUNT) return;

            // 칸 상태는 SetTelegraph 와 같이 바꾸되 칸 채움은 켜지 않는다
            _isTileVisualSuppressed = true;
            Grid.SetTelegraphed(index, isOn);
            _isTileVisualSuppressed = false;

            HideArrowLine(_tileArrowHandles[index]);
            _tileArrowHandles[index] = -1;
            if (!isOn) return;

            _tileArrowHandles[index] = ShowArrows(GetTileCenter(index), direction, 1);
        }

        public int ShowArrowLine(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta.y = 0.0f;
            float length = delta.magnitude;
            if (length < 0.001f) return -1;

            int count = Mathf.Max(1, Mathf.FloorToInt(length / (_arrowStripLength * _arrowScale)));
            return ShowArrows(from + (delta * 0.5f), delta, count);
        }

        public void HideArrowLine(int handle)
        {
            if (!_arrowGroups.TryGetValue(handle, out ArrowGroup group)) return;

            _arrowGroups.Remove(handle);
            foreach (BossArrowTelegraph arrow in group.Arrows)
            {
                arrow.Stop();
                _arrowPool.Push(arrow);
            }

            foreach (int tile in group.HiddenTiles)
            {
                if (--_signalHideCounts[tile] == 0 && IsTileReady(tile)) _tiles[tile].SetWarningSignalsVisible(true);
            }
        }

        public void Collapse(int index)
        {
            Grid.Collapse(index);
        }

        public int ShowCircle(Vector3 center, float radius, float fillSeconds = 0.0f)
        {
            if (_circles == null) return -1;

            for (int i = 0; i < _circles.Length; i++)
            {
                if (_circles[i].activeSelf) continue;

                _circles[i].transform.SetPositionAndRotation(center, Quaternion.identity);
                CircleRegion region = _circleRegions[i];
                if (region)
                {
                    // DTT 원은 Radius 로 안쪽 메시 크기를 직접 맞추므로 루트는 늘리지 않는다
                    region.Radius = radius;
                }
                else
                {
                    float diameter = radius * 2.0f;
                    _circles[i].transform.localScale = new Vector3(diameter, _circleBaseScale.y, diameter);
                }

                _circles[i].SetActive(true);
                PlayFill(_circlePlayers[i], fillSeconds);
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

        public int ShowLine(Vector3 from, Vector3 to, float width, float fillSeconds = 0.0f)
        {
            if (_lines == null) return -1;

            Vector3 delta = to - from;
            delta.y = 0.0f;
            float length = delta.magnitude;
            if (length < 0.001f) return -1;

            for (int i = 0; i < _lines.Length; i++)
            {
                if (_lines[i].activeSelf) continue;

                Quaternion rotation = Quaternion.LookRotation(delta / length, Vector3.up);
                LineRegion region = _lineRegions[i];
                if (region)
                {
                    // DTT 직선은 기준점이 시작점이고, 몸통·화살촉을 Length·Width 로 따로 맞춘다
                    _lines[i].transform.SetPositionAndRotation(from, rotation);
                    region.Angle = 0.0f;
                    region.Length = length;
                    region.Width = width;
                }
                else
                {
                    Vector3 center = from + (delta * 0.5f);
                    _lines[i].transform.SetPositionAndRotation(center, rotation);
                    _lines[i].transform.localScale = new Vector3(width, _lineBaseScale.y, length);
                }

                _lines[i].SetActive(true);
                PlayFill(_linePlayers[i], fillSeconds);
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

        /// <summary>단계에 맞는 칸 복구 시간을 쓴다. 최종 단계만 <c>_restoreSecondsFinal</c>.</summary>
        public void SetStage(EBossStage stage)
        {
            Grid.SetRestoreSeconds(stage == EBossStage.Final ? _restoreSecondsFinal : _restoreSeconds);
        }

        public void ResetAll()
        {
            Grid.ResetAll();
            Grid.SetRestoreSeconds(_restoreSeconds);

            _arrowHandleBuffer.Clear();
            _arrowHandleBuffer.AddRange(_arrowGroups.Keys);
            foreach (int handle in _arrowHandleBuffer)
            {
                HideArrowLine(handle);
            }
            if (_tileArrowHandles != null) Array.Fill(_tileArrowHandles, -1);

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
            _circleRegions = new CircleRegion[_circlePoolSize];
            _circlePlayers = new AoeVfxPlayer[_circlePoolSize];
            for (int i = 0; i < _circlePoolSize; i++)
            {
                _circles[i] = Instantiate(_circlePrefab, transform);
                _circleRegions[i] = _circles[i].GetComponent<CircleRegion>();
                _circlePlayers[i] = _circles[i].GetComponent<AoeVfxPlayer>();
                _circles[i].SetActive(false);
            }
        }

        private void CreateLinePool()
        {
            if (!_linePrefab || _linePoolSize <= 0) return;

            _lineBaseScale = _linePrefab.transform.localScale;
            _lines = new GameObject[_linePoolSize];
            _lineRegions = new LineRegion[_linePoolSize];
            _linePlayers = new AoeVfxPlayer[_linePoolSize];
            for (int i = 0; i < _linePoolSize; i++)
            {
                _lines[i] = Instantiate(_linePrefab, transform);
                _lineRegions[i] = _lines[i].GetComponent<LineRegion>();
                _linePlayers[i] = _lines[i].GetComponent<AoeVfxPlayer>();
                _lines[i].SetActive(false);
            }
        }

        /// <summary>
        /// 화살표 토막 count 개를 center 가운데로 direction 방향에 이어 붙여 띄운다. 토막이 놓인 칸의 맵 WarningSignals 는 끈다.
        /// 화살표 머리는 프리팹의 로컬 +X 이므로 +X 가 direction 을 향하게 돌린다.
        /// </summary>
        /// <returns>HideArrowLine 에 쓸 핸들. 프리팹이 없거나 방향이 없으면 -1</returns>
        private int ShowArrows(Vector3 center, Vector3 direction, int count)
        {
            direction.y = 0.0f;
            if (!_arrowPrefab || direction.sqrMagnitude < 0.0001f) return -1;

            direction.Normalize();
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0.0f, -90.0f, 0.0f);
            float stripLength = _arrowStripLength * _arrowScale;

            var group = new ArrowGroup();
            for (int i = 0; i < count; i++)
            {
                Vector3 position = center + (direction * ((i - ((count - 1) * 0.5f)) * stripLength));
                BossArrowTelegraph arrow = TakeArrow();
                arrow.transform.SetPositionAndRotation(position, rotation);
                arrow.Play(_arrowStepSeconds);
                group.Arrows.Add(arrow);

                if (!TryGetTile(position, out int tile) || group.HiddenTiles.Contains(tile)) continue;

                group.HiddenTiles.Add(tile);
                if (_signalHideCounts[tile]++ == 0 && IsTileReady(tile)) _tiles[tile].SetWarningSignalsVisible(false);
            }

            int handle = _nextArrowHandle++;
            _arrowGroups.Add(handle, group);
            return handle;
        }

        private BossArrowTelegraph TakeArrow()
        {
            if (_arrowPool.Count > 0) return _arrowPool.Pop();

            GameObject instance = Instantiate(_arrowPrefab, transform);
            instance.transform.localScale = Vector3.one * _arrowScale;
            if (!instance.TryGetComponent(out BossArrowTelegraph arrow)) arrow = instance.AddComponent<BossArrowTelegraph>();
            return arrow;
        }

        /// <summary>채움 연출이 있는 예고면 fillSeconds 동안 채운다. 0 이하면 처음부터 가득 채운다.</summary>
        private void PlayFill(AoeVfxPlayer player, float fillSeconds)
        {
            if (!player) return;

            if (fillSeconds > 0.0f)
            {
                player.duration = fillSeconds;
                player.Play();
            }
            else
            {
                player.Stop();
                player.FillProgress = 1.0f;
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
            if (!_isTileVisualSuppressed && IsTileReady(index)) _tiles[index].SetTelegraph(isOn, _telegraphFillSeconds, _telegraphColor);
        }

        private bool IsTileReady(int index)
        {
            return _tiles != null && index >= 0 && index < _tiles.Length && _tiles[index];
        }
        #endregion
    }
}
