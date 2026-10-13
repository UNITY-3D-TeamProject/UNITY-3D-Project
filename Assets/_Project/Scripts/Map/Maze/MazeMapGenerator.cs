using System.Collections.Generic;
using UnityEngine;
using Map.Common;

namespace Map.Maze
{
    /// <summary>
    /// 타일 프리팹을 이어 붙여 미로를 런타임에 생성한다.
    /// 시작 칸은 가장자리에서 무작위로 고르고, 도착 칸은 시작 칸에서 가장 먼 칸으로 정한다.
    /// 타일 프리팹은 "북쪽(+Z)이 열린 상태"를 기본 방향으로 만든다.
    /// </summary>
    public class MazeMapGenerator : MapGeneratorBase
    {
        #region Constants
        // 플레이어가 바닥에 파묻히지 않도록 시작 위치를 띄우는 높이
        private const float START_HEIGHT_OFFSET = 0.1f;
        #endregion

        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("미로 크기와 타일 프리팹 5종을 담은 설정 에셋")]
        [SerializeField] private SOMazeMapSettings _settings;
        #endregion

        #region Protected Methods
        /// <inheritdoc />
        protected override void Build(
            Transform root,
            System.Random random,
            List<Vector3> placementPoints,
            out Vector3 startPosition,
            out Vector3 goalPosition)
        {
            startPosition = transform.position;
            goalPosition = transform.position;

            if (_settings == null)
            {
                Debug.LogError($"[{name}] 설정 에셋(Settings)이 연결되지 않았습니다.", this);
                return;
            }

            bool hasAllPrefabs = (_settings.DeadEndPrefab != null) && (_settings.StraightPrefab != null) && (_settings.CornerPrefab != null) &&
                                 (_settings.TJunctionPrefab != null) && (_settings.CrossPrefab != null);
            if (!hasAllPrefabs)
            {
                Debug.LogError($"[{name}] 미로 타일 프리팹 5종이 모두 연결되어야 합니다.", this);
                return;
            }

            var generator = new MazeGenerator();
            Vector2Int startCell = PickEdgeCell(random);
            MazeGenerator.SCell[,] grid = generator.GenerateMaze(_settings.Width, _settings.Height, startCell, random);
            Vector2Int goalCell = FindFarthestCell(generator.ComputeDistances(grid, startCell));

            for (int x = 0; x < _settings.Width; x++)
            {
                for (int y = 0; y < _settings.Height; y++)
                {
                    CreateTile(grid[x, y], root);

                    // 시작·도착 칸에는 장애물이나 아이템을 놓지 않는다
                    var cell = new Vector2Int(x, y);
                    if ((cell != startCell) && (cell != goalCell))
                    {
                        placementPoints.Add(GetCellCenter(root, cell));
                    }
                }
            }

            startPosition = GetCellCenter(root, startCell) + (Vector3.up * START_HEIGHT_OFFSET);
            goalPosition = GetCellCenter(root, goalCell);
        }
        #endregion

        #region Private Methods
        // 네 변 가운데 하나를 고르고 그 변 위의 칸 하나를 무작위로 고른다.
        private Vector2Int PickEdgeCell(System.Random random)
        {
            switch (random.Next(4))
            {
                case 0:  return new Vector2Int(random.Next(_settings.Width), 0);             // 아래
                case 1:  return new Vector2Int(random.Next(_settings.Width), _settings.Height - 1);   // 위
                case 2:  return new Vector2Int(0, random.Next(_settings.Height));            // 왼쪽
                default: return new Vector2Int(_settings.Width - 1, random.Next(_settings.Height));   // 오른쪽
            }
        }

        // 시작 칸에서 가장 먼 칸 (걸어가야 하는 칸 수 기준)
        private Vector2Int FindFarthestCell(int[,] distances)
        {
            Vector2Int farthest = Vector2Int.zero;
            int maxDistance = -1;

            for (int x = 0; x < _settings.Width; x++)
            {
                for (int y = 0; y < _settings.Height; y++)
                {
                    if (distances[x, y] > maxDistance)
                    {
                        maxDistance = distances[x, y];
                        farthest = new Vector2Int(x, y);
                    }
                }
            }

            return farthest;
        }

        // 칸의 바닥 중앙 (월드 좌표)
        private Vector3 GetCellCenter(Transform root, Vector2Int cell)
        {
            return root.TransformPoint(new Vector3(cell.x * _settings.TileSize, 0.0f, cell.y * _settings.TileSize));
        }

        // 칸 하나에 맞는 타일을 골라 놓는다.
        private void CreateTile(MazeGenerator.SCell cell, Transform root)
        {
            SelectTile(cell, out GameObject prefab, out float rotationY);
            if (prefab == null) return;

            GameObject tile = Instantiate(prefab, root);
            tile.name = $"Tile_{cell.X}_{cell.Y}";
            tile.transform.localPosition = new Vector3(cell.X * _settings.TileSize, 0.0f, cell.Y * _settings.TileSize);
            tile.transform.localRotation = Quaternion.Euler(0.0f, rotationY, 0.0f);
        }

        /// <summary>
        /// 칸의 열린 방향에 맞는 타일 프리팹과, 기본 방향에서 돌려야 할 각도를 고른다.
        /// </summary>
        private void SelectTile(MazeGenerator.SCell cell, out GameObject prefab, out float rotationY)
        {
            prefab = null;
            rotationY = 0.0f;

            int openCount = 0;
            if (cell.OpenNorth) openCount++;
            if (cell.OpenEast) openCount++;
            if (cell.OpenSouth) openCount++;
            if (cell.OpenWest) openCount++;

            switch (openCount)
            {
                case 1:
                    prefab = _settings.DeadEndPrefab;
                    if (cell.OpenEast) rotationY = 90.0f;
                    else if (cell.OpenSouth) rotationY = 180.0f;
                    else if (cell.OpenWest) rotationY = 270.0f;
                    break;

                case 2:
                    if (cell.OpenNorth && cell.OpenSouth)
                    {
                        prefab = _settings.StraightPrefab;
                    }
                    else if (cell.OpenEast && cell.OpenWest)
                    {
                        prefab = _settings.StraightPrefab;
                        rotationY = 90.0f;
                    }
                    else
                    {
                        prefab = _settings.CornerPrefab;
                        if (cell.OpenEast && cell.OpenSouth) rotationY = 90.0f;
                        else if (cell.OpenSouth && cell.OpenWest) rotationY = 180.0f;
                        else if (cell.OpenWest && cell.OpenNorth) rotationY = 270.0f;
                    }
                    break;

                case 3:
                    prefab = _settings.TJunctionPrefab;
                    if (!cell.OpenNorth) rotationY = 90.0f;        // 동·남·서
                    else if (!cell.OpenEast) rotationY = 180.0f;   // 남·서·북
                    else if (!cell.OpenSouth) rotationY = 270.0f;  // 서·북·동
                    break;

                case 4:
                    prefab = _settings.CrossPrefab;
                    break;
            }
        }
        #endregion
    }
}
